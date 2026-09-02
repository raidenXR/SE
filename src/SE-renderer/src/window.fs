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

module ClientSize =
    let [<Literal>] W = 1240.f
    let [<Literal>] H = 720.f

type SE_Window(settings:NativeWindowSettings) =
    inherit NativeWindow(settings)
    
    let mutable ExpectedSchedulerPeriod = 16
    let MaxFrequency = 500.0
    let _watchUpdate = new Stopwatch()
    let _updateFrequency = GameWindowSettings.Default.UpdateFrequency
    let UpdateFrequency = _updateFrequency
    let CaptureFrequency = 20. 
    let mutable _capture_countdown = CaptureFrequency

    let mutable _slowUpdates = 0
    let mutable elapsed = 0.
    let mutable UpdateTime = 0.
    let mutable window_update_frame = true
    let mutable window_render_frame = true
    let mutable IsRunningSlowly = false

    let camera = Camera(OpenTK.Mathematics.Vector3.UnitZ, ClientSize.W / ClientSize.H)
    let mutable first_move = true
    let mutable last_pos = OpenTK.Mathematics.Vector2()
    let mutable record = false
    let mutable record_prev = false

    let pre_render_fns = ResizeArray<unit -> unit>()
    let render_fns = ResizeArray<unit -> unit>()
    let post_render_fns = ResizeArray<unit -> unit>()
    // let render_fns_set = System.Collections.Generic.HashSet<unit -> unit>()
  

    static let _shared = lazy (
        let n_settings = NativeWindowSettings(
            ClientSize = OpenTK.Mathematics.Vector2i(int ClientSize.W, int ClientSize.H),
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

    member this.IsRecording with get() = record and set(value) = record <- value

    static member Shared = _shared.Force()

    member this.OnRenderFns = render_fns
    member this.PreRenderFns = pre_render_fns
    member this.PostRenderFns = post_render_fns

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
        
        base.OnResize(new ResizeEventArgs(settings.ClientSize))
        _watchUpdate.Start()        


    member this.Update(render_fn:unit -> unit) =
        let updatePeriod = if UpdateFrequency = 0. then 0. else 1. / UpdateFrequency
        let capturePeriod = if CaptureFrequency = 0. then 0.67 else 1. / CaptureFrequency
        elapsed <- _watchUpdate.Elapsed.TotalSeconds    

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
                printfn "%s" (if record then "recording: start" else "recording: stop")
            elif not (input.IsKeyDown(Keys.R)) then
                record_prev <- false

            UpdateTime <- elapsed
            this.ElapsedTime <- elapsed

            render_fn()
            // for render_fn in pre_render_fns do
            //     render_fn ()
                
            // for render_fn in render_fns do
            //     render_fn ()

            // for render_fn in post_render_fns do
            //     render_fn ()

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


    // member this.OnRenderFrame (fn: unit -> unit) =
    //     if render_fns_set.Add(fn) then
    //         render_fns.Add(fn)
        

    override this.Dispose (): unit = 
        base.Dispose()

