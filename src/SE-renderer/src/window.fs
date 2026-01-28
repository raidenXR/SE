namespace SE.Renderer
open System
open System.Diagnostics
open System.Runtime.InteropServices

open OpenTK.Core
open OpenTK.Windowing.Common
open OpenTK.Windowing.Desktop
open OpenTK.Windowing.GraphicsLibraryFramework
open OpenTK.Graphics.OpenGL4


// CREATE an NEW CLASS for GameWindow with Different Run method

type SE_Window(settings:NativeWindowSettings) =
    inherit NativeWindow(settings)
    
    let mutable ExpectedSchedulerPeriod = 16
    let MaxFrequency = 500.0
    let _watchUpdate = new Stopwatch()
    let _updateFrequency = GameWindowSettings.Default.UpdateFrequency
    let UpdateFrequency = _updateFrequency

    let mutable _slowUpdates = 0
    let mutable elapsed = 0.
    let mutable UpdateTime = 0.
    let mutable window_update_frame = true
    let mutable window_render_frame = true
    let mutable IsRunningSlowly = false

    let update_frame_event = new Event<FrameEventArgs>()
    let render_frame_event = new Event<FrameEventArgs>()

    [<CLIEvent>] member this.UpdateFrameEvent = update_frame_event.Publish
    [<CLIEvent>] member this.RenderFrameEvent = render_frame_event.Publish

    [<DefaultValue>] val mutable UpdateRenderLoopList: list<unit -> unit> 

    member this.UpdateFrame with get() = window_update_frame

    member this.RenderFrame with get() = window_render_frame

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
        elapsed <- _watchUpdate.Elapsed.TotalSeconds    

        if elapsed > updatePeriod then
            _watchUpdate.Restart()

            // Update input state for next frame
            base.NewInputFrame()
            NativeWindow.ProcessWindowEvents(base.IsEventDriven)

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
        GL.Viewport(0, 0, size_x, size_y)
