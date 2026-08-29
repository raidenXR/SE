namespace SE.Renderer
open System
open System.Diagnostics
open System.Runtime.InteropServices

open OpenTK.Core
open OpenTK.Windowing.Common
open OpenTK.Windowing.Desktop
open OpenTK.Windowing.GraphicsLibraryFramework
open OpenTK.Graphics.OpenGL4

open SkiaSharp
open FFMpegCore
open FFMpegCore.Pipes

type VideoFrame(bmp:SKBitmap, pixels:byte[]) =
    do
        if bmp.ColorType <> SKColorType.Bgra8888 then
            printfn "colortype: %A" bmp.ColorType
            failwith "only 'bgra' colortype is supported"

    interface IDisposable with
        member this.Dispose() =
            bmp.Dispose()

    interface IVideoFrame with
        member this.Width  = bmp.Width
        member this.Height = bmp.Height
        member this.Format = "bgra"
    
        member this.Serialize(pipe:System.IO.Stream) =
            pipe.Write(bmp.Bytes, 0, bmp.Bytes.Length)

        member this.SerializeAsync(pipe:System.IO.Stream, token:System.Threading.CancellationToken) =
            pipe.WriteAsync(bmp.Bytes, 0, bmp.Bytes.Length, token)

    member this.Bitmap = bmp
    member this.Pixels = pixels


// CREATE an NEW CLASS for GameWindow with Different Run method

type SE_Window(settings:NativeWindowSettings) =
    inherit NativeWindow(settings)
    
    let mutable ExpectedSchedulerPeriod = 16
    let MaxFrequency = 500.0
    let _watchUpdate = new Stopwatch()
    let _updateFrequency = GameWindowSettings.Default.UpdateFrequency
    let UpdateFrequency = _updateFrequency
    let CaptureFrequency = 20. 
    let mutable _capture_countdown = CaptureFrequency

    let frames = ResizeArray<VideoFrame>(1000)

    let mutable _slowUpdates = 0
    let mutable elapsed = 0.
    let mutable UpdateTime = 0.
    let mutable window_update_frame = true
    let mutable window_render_frame = true
    let mutable IsRunningSlowly = false

    let camera = Camera(OpenTK.Mathematics.Vector3.UnitZ, 800f/600f)
    let mutable first_move = true
    let mutable last_pos = OpenTK.Mathematics.Vector2()
    let mutable record = false
    let mutable record_prev = false
  

    static let _shared = lazy (
        let n_settings = NativeWindowSettings(
            ClientSize = OpenTK.Mathematics.Vector2i(800, 600),
            Title = "opetk-window",
            Flags = ContextFlags.ForwardCompatible
        )
        new SE_Window(n_settings)
    )

    let update_frame_event = new Event<FrameEventArgs>()
    let render_frame_event = new Event<FrameEventArgs>()

    [<CLIEvent>] member this.UpdateFrameEvent = update_frame_event.Publish
    [<CLIEvent>] member this.RenderFrameEvent = render_frame_event.Publish

    [<DefaultValue>] val mutable UpdateRenderLoopList: list<unit -> unit> 

    member this.UpdateFrame = window_update_frame

    member this.RenderFrame = window_render_frame

    member this.Camera = camera

    member this.IsRecording = record

    static member Shared = _shared.Force()

    member val ElapsedTime = 0. with get,set


    member this.Load() =
        let TIME_PERIOD = 8
        // We do this before OnLoad so that users have some way to affect these settings in OnLoad if they need to.
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) then
            // Make this thread only run on one core, avoiding timing issues with context switching
            // SetThreadAffinityMask(GetCurrentThread(), new IntPtr(1))

            // Make Thread.Sleep more accurate.
            // FIXME: We probably only care about this if we are not event driven.
            // timeBeginPeriod(TIME_PERIOD)
            ExpectedSchedulerPeriod <- TIME_PERIOD
        elif (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) || RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD)) then
            // Seems like `Thread.Sleep` can accurately sleep for 1ms on Ubuntu 20.04
            // - 2023-07-13 Noggin_bops
            ExpectedSchedulerPeriod <- 1
        elif (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) then
            // Seems like `Thread.Slepp` can accurately sleep for 1ms on a 2018 Macbook Air running macos 12.3.1.
            // - 2023-07-13 Noggin_bops
            ExpectedSchedulerPeriod <- 1

        base.Context.MakeCurrent()

        // if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows) && _win32SuspendTimerOnDrag) then
        //     _win32WndProc <- new Win32WindowProc(WindowPtr);
        //     _win32WndProc.OnModalSizeMoveBegin += Win32_OnModalSizeMoveBegin;
        //     _win32WndProc.OnModalSizeMoveEnd += Win32_OnModalSizeMoveEnd;
        
        base.OnResize(new ResizeEventArgs(settings.ClientSize));
        _watchUpdate.Start()
    

    member this.Update(render_fn:unit -> unit) =
        let updatePeriod = if UpdateFrequency = 0. then 0. else 1. / UpdateFrequency
        let capturePeriod = if CaptureFrequency = 0. then 0.67 else 1. / CaptureFrequency
        elapsed <- _watchUpdate.Elapsed.TotalSeconds    

        // if elapsed > capturePeriod && record then
        // if elapsed > updatePeriod && record then
        //     _capture_countdown <- _capture_countdown - 1.
        //     if _capture_countdown <= 0. then
        //         this.CaptureFrame()
        //         _capture_countdown <- CaptureFrequency

        if elapsed > updatePeriod then
            _watchUpdate.Restart()

            // Update input state for next frame
            base.NewInputFrame()
            NativeWindow.ProcessWindowEvents(base.IsEventDriven)

            let input = this.KeyboardState
            let e = elapsed
            
            if input.IsKeyDown(Keys.Escape) then this.Close()
            if input.IsKeyDown(Keys.Up) then camera.Position <- camera.Position + camera.Front * camera.Speed * (float32 e)
            if input.IsKeyDown(Keys.Down) then camera.Position <- camera.Position - camera.Front * camera.Speed * (float32 e)
            if input.IsKeyDown(Keys.Right) then camera.Position <- camera.Position + camera.Right * camera.Speed * (float32 e)
            if input.IsKeyDown(Keys.Left) then camera.Position <- camera.Position - camera.Right * camera.Speed * (float32 e)
            if input.IsKeyDown(Keys.Space) then camera.Position <- camera.Position + camera.Up * camera.Speed * (float32 e)
            if input.IsKeyDown(Keys.LeftShift) then camera.Position <- camera.Position - camera.Up * camera.Speed * (float32 e)
    
            let mouse = this.MouseState
            if first_move then
                last_pos <- OpenTK.Mathematics.Vector2(mouse.X, mouse.Y)
                first_move <- false
            else
                let dx = mouse.X - last_pos.X
                let dy = mouse.Y - last_pos.Y
                last_pos <- OpenTK.Mathematics.Vector2(mouse.X, mouse.Y)
                camera.Yaw <- camera.Yaw + dx * camera.Sensitivity
                camera.Pitch <- camera.Pitch - dy * camera.Sensitivity
                
            if input.IsKeyDown(Keys.R) && not record_prev then
                record_prev <- true
                record <- not record
            elif not (input.IsKeyDown(Keys.R)) then
                record_prev <- false

            UpdateTime <- elapsed
            this.ElapsedTime <- elapsed
            render_fn ()

            let MaxSlowUpdates = 80
            let SlowUpdatesThreshold = 45

            let time = _watchUpdate.Elapsed.TotalSeconds
            if updatePeriod < time then
                _slowUpdates <- _slowUpdates + 1
                if (_slowUpdates > MaxSlowUpdates) then
                    _slowUpdates <- MaxSlowUpdates
            else
                _slowUpdates <- _slowUpdates - 1
                if (_slowUpdates < 0) then 
                    _slowUpdates <- 0
                    
            IsRunningSlowly <- _slowUpdates > SlowUpdatesThreshold;

            if this.API <> ContextAPI.NoAPI then
                if this.VSync <> VSyncMode.Adaptive then
                    GLFW.SwapInterval(if IsRunningSlowly then 0 else 1)

        // The time we have left to the next update.
        let timeToNextUpdate = updatePeriod - _watchUpdate.Elapsed.TotalSeconds

        if timeToNextUpdate > 0 then
            Utils.AccurateSleep(timeToNextUpdate, ExpectedSchedulerPeriod)

            
    override this.OnResize(e:ResizeEventArgs) =
        base.OnResize(e)
        let size_x = this.Size.X
        let size_y = this.Size.Y
        camera.AspectRatio <- float32(size_x) / float32(size_y)
        GL.Viewport(0, 0, size_x, size_y)


    member this.CaptureFrame() =
        let size = this.FramebufferSize
        let w = size.X
        let h = size.Y
        let pixels = Array.zeroCreate<byte> (w*h*4)  // This leaks memory, use regular arrays, DO NOT POOL

        use ptr = fixed pixels
        let p'  = FSharp.NativeInterop.NativePtr.toNativeInt ptr
        GL.ReadPixels(0, 0, w, h, PixelFormat.Bgra, PixelType.UnsignedByte, p')

        let bitmap = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Premul)
        let success = bitmap.InstallPixels(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul), p', w*4)

        if not success then
            failwith "failed to install pixels on SKBitmap"
        
        frames.Add(new VideoFrame(bitmap, pixels))
        

    override this.Dispose (): unit = 
        base.Dispose()

        // for frame in frames do frame.Bitmap.Dispose()


    /// Exports a .mp4 video from the captured frames if any
    member this.ExportVideo () =
        if frames.Count > 0 then
            let path = DateTime.Now.ToString(Globalization.CultureInfo("gr-GR")).Replace('/', '-').Replace(':',' ')
            let vid_path = "vid_" + path + ".mp4"
            let img_path = "img_" + path + ".png"

            if System.IO.File.Exists(vid_path) then
                System.IO.File.Delete(vid_path)
            if System.IO.File.Exists(img_path) then
                System.IO.File.Delete(img_path)

            let size = this.FramebufferSize
            printfn "framebuffer: (%d, %d)" size.X size.Y
            // save last frame as image
            printfn "image png conversion"
    
            let last_frame = (Seq.last frames)
            let bmp = last_frame.Bitmap
            use tmp_img = SKImage.FromBitmap(bmp)
            use tmp_dat = tmp_img.Encode(SKEncodedImageFormat.Png, 80)
            use tmp_stm = System.IO.File.OpenWrite(img_path)
            tmp_dat.SaveTo(tmp_stm)
            tmp_stm.Close()
            printfn "image png saved"

            let _frames = frames.ToArray() |> Array.map (fun v -> v :> IVideoFrame)

            let source = new RawVideoPipeSource(_frames, FrameRate = 30)
            let success = FFMpegArguments
                            .FromPipeInput(source)
                            .OutputToFile(vid_path, true, (fun options -> options.WithVideoCodec("libvpx-vp9").WithVideoFilters(fun filter -> filter.Mirror(Enums.Mirroring.Vertical) |> ignore) |> ignore))
                            // .ProcessSynchronously()

            printfn "start processing video conversion on %d frames" (Seq.length frames)
            let s = success.ProcessSynchronously()
            // success
            let str = if s then "video conversion done!" else "video conversion failed"
            printfn "%s" str
        
