namespace SE.Renderer
open System
open System.Diagnostics
open System.Runtime.InteropServices

open OpenTK.Core
open OpenTK.Windowing.Common
open OpenTK.Windowing.Desktop
open OpenTK.Windowing.GraphicsLibraryFramework
open OpenTK.Graphics.OpenGL4

open ImGuiNET
open Dear_ImGui_Sample.Backends


module DebugProc =
    let Window_DebugProc (source:DebugSource) (_type:DebugType) (id:int) (severity:DebugSeverity) (length:int) (messagePtr:nativeint) (userParam:nativeint) =
        let message = Marshal.PtrToStringAnsi(messagePtr, length)
        let mutable showMessage = true

        match source with
        | DebugSource.DebugSourceApplication -> showMessage <- false
        | DebugSource.DontCare
        | DebugSource.DebugSourceApi
        | DebugSource.DebugSourceWindowSystem
        | DebugSource.DebugSourceShaderCompiler
        | DebugSource.DebugSourceThirdParty
        | DebugSource.DebugSourceOther
        | _ -> showMessage <- true

        if showMessage then
            match severity with
            | DebugSeverity.DontCare -> Console.WriteLine($"[DontCare] [{source}] {message}")
            | DebugSeverity.DebugSeverityNotification -> () //Logger?.LogDebug($"[{source}] {message}");
            | DebugSeverity.DebugSeverityHigh -> Console.Error.WriteLine($"Error: [{source}] {message}")
            | DebugSeverity.DebugSeverityMedium -> Console.WriteLine($"Warning: [{source}] {message}")
            | DebugSeverity.DebugSeverityLow -> Console.WriteLine($"Info: [{source}] {message}")
            | _ -> Console.WriteLine($"[default] [{source}] {message}")


// module UI =

//     let debug_proc = DebugProc.Window_DebugProc
//     let error_callback error description = 
//             Console.WriteLine($"GLFW Error ({error}): {description}")
    
//     let load (wnd:NativeWindow) =
//         GL.DebugMessageCallback(debug_proc, IntPtr.Zero)
//         GL.Enable(EnableCap.DebugOutput)
//         GL.Enable(EnableCap.DebugOutputSynchronous)
        
//         // Before creating your window
//         // GLFW.SetErrorCallback(fun error description ->
//         //     // Log the error but don't throw
//         //     Console.WriteLine($"GLFW Error ({error}): {description}")
//         // ) |> ignore
//         GLFW.SetErrorCallback(error_callback) |> ignore

//         ImGui.CreateContext() |> ignore
//         let mutable io = ImGui.GetIO()
//         io.ConfigFlags <- io.ConfigFlags ||| ImGuiConfigFlags.NavEnableKeyboard
//         io.ConfigFlags <- io.ConfigFlags ||| ImGuiConfigFlags.NavEnableGamepad
//         io.ConfigFlags <- io.ConfigFlags ||| ImGuiConfigFlags.DockingEnable
//         io.ConfigFlags <- io.ConfigFlags ||| ImGuiConfigFlags.ViewportsEnable

//         ImGui.StyleColorsDark()

//         let mutable style = ImGui.GetStyle()
//         if (io.ConfigFlags &&& ImGuiConfigFlags.ViewportsEnable) <> ImGuiConfigFlags() then
//             style.WindowRounding <- 0.0f
//             style.Colors[(int)ImGuiCol.WindowBg].W <- 1.0f

//         ImguiImplOpenTK4.Init(wnd) |> ignore
//         ImguiImplOpenGL3.Init()


//     let render (wnd:NativeWindow) (fns:seq<unit -> unit>) =
//         ImguiImplOpenGL3.NewFrame()
//         ImguiImplOpenTK4.NewFrame()
//         ImGui.NewFrame()

//         ImGui.DockSpaceOverViewport() |> ignore
//         ImGui.ShowDemoWindow()

//         for fn in fns do
//             fn ()

//         ImGui.Render()
//         ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData())

//         if (ImGui.GetIO().ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable)) then
//             ImGui.UpdatePlatformWindows()
//             ImGui.RenderPlatformWindowsDefault()
//             wnd.Context.MakeCurrent()   
    

//     let dispose () =
//         ImguiImplOpenGL3.Shutdown()
//         ImguiImplOpenTK4.Shutdown()
        


type SE_UI() =
    static let debugProcCallback = DebugProc(SE_UI.windowDebugProc)
    let render_fns = ResizeArray<unit -> unit>()
    let render_fns_set = System.Collections.Generic.HashSet<unit -> unit>()
        
    static let _shared = lazy (new SE_UI())

    // Keep a static reference to the GLFW error callback to prevent garbage collection
    static let glfwErrorCallback = GLFWCallbacks.ErrorCallback(fun error description ->
        // printfn "GLFW Error (%A): %s" error description
        ()
    )


    member this.OnLoad(wnd:NativeWindow) =
        GL.DebugMessageCallback(debugProcCallback, IntPtr.Zero)
        GL.Enable(EnableCap.DebugOutput)
        GL.Enable(EnableCap.DebugOutputSynchronous)
        GLFW.SetErrorCallback(glfwErrorCallback) |> ignore
        
        ImGui.CreateContext() |> ignore
        let io = ImGui.GetIO()
        // io.ConfigFlags <- io.ConfigFlags ||| ImGuiConfigFlags.NavEnableKeyboard
        // io.ConfigFlags <- io.ConfigFlags ||| ImGuiConfigFlags.NavEnableGamepad
        io.ConfigFlags <- io.ConfigFlags ||| ImGuiConfigFlags.DockingEnable
        io.ConfigFlags <- io.ConfigFlags ||| ImGuiConfigFlags.ViewportsEnable

        ImGui.StyleColorsDark()

        let style = ImGui.GetStyle()
        if (io.ConfigFlags &&& ImGuiConfigFlags.ViewportsEnable) <> ImGuiConfigFlags() then
            style.WindowRounding <- 0.0f
            style.Colors[(int)ImGuiCol.WindowBg].W <- 1.0f

        ImguiImplOpenTK4.Init(wnd) |> ignore
        ImguiImplOpenGL3.Init()

        

    member this.OnRenderFrame(wnd:NativeWindow) =
        ImguiImplOpenGL3.NewFrame()
        ImguiImplOpenTK4.NewFrame()
        ImGui.NewFrame()

        ImGui.DockSpaceOverViewport() |> ignore

        // ImGui.ShowDemoWindow() |> ignore

        for render_fn in render_fns do
            render_fn ()

        ImGui.Render()
        GL.Viewport(0, 0, wnd.FramebufferSize.X, wnd.FramebufferSize.Y)
        // GL.ClearColor(Color4(0uy, 32uy, 48uy, 255uy))
        // GL.Clear(ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit ||| ClearBufferMask.StencilBufferBit)
        ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData())

        if (ImGui.GetIO().ConfigFlags &&& ImGuiConfigFlags.ViewportsEnable) <> ImGuiConfigFlags() then
            ImGui.UpdatePlatformWindows()
            ImGui.RenderPlatformWindowsDefault()
            wnd.Context.MakeCurrent()


    member this.OnRender (fn: unit -> unit) =
        if render_fns_set.Add(fn) then
            render_fns.Add(fn)

    member this.OnClosed() =
        ImguiImplOpenGL3.Shutdown()
        ImguiImplOpenTK4.Shutdown()

    static member Shared = _shared.Force()
    
    static member private windowDebugProc (source: DebugSource) (debugType: DebugType) (id: int) (severity: DebugSeverity) (length: int) (messagePtr: IntPtr) (userParam: IntPtr) =
        let message = Marshal.PtrToStringAnsi(messagePtr, length)

        let showMessage =
            match source with
            | DebugSource.DebugSourceApplication -> false
            | DebugSource.DontCare
            | DebugSource.DebugSourceApi
            | DebugSource.DebugSourceWindowSystem
            | DebugSource.DebugSourceShaderCompiler
            | DebugSource.DebugSourceThirdParty
            | DebugSource.DebugSourceOther
            | _ -> true

        if showMessage then
            match severity with
            | DebugSeverity.DontCare ->
                printfn "[DontCare] [%A] %s" source message
            | DebugSeverity.DebugSeverityNotification ->
                () // Skip notifications
            | DebugSeverity.DebugSeverityHigh ->
                eprintfn "Error: [%A] %s" source message
            | DebugSeverity.DebugSeverityMedium ->
                printfn "Warning: [%A] %s" source message
            | DebugSeverity.DebugSeverityLow ->
                printfn "Info: [%A] %s" source message
            | _ ->
                printfn "[default] [%A] %s" source message


    // static member private windowDebugProc(source: DebugSource, debugType: DebugType, id: int, severity: DebugSeverity, length: int, messagePtr: IntPtr, userParam: IntPtr) =
    //     let message = Marshal.PtrToStringAnsi(messagePtr, length)

    //     let showMessage =
    //         match source with
    //         | DebugSource.DebugSourceApplication -> false
    //         | DebugSource.DontCare
    //         | DebugSource.DebugSourceApi
    //         | DebugSource.DebugSourceWindowSystem
    //         | DebugSource.DebugSourceShaderCompiler
    //         | DebugSource.DebugSourceThirdParty
    //         | DebugSource.DebugSourceOther
    //         | _ -> true

    //     if showMessage then
    //         match severity with
    //         | DebugSeverity.DontCare ->
    //             printfn "[DontCare] [%A] %s" source message
    //         | DebugSeverity.DebugSeverityNotification ->
    //             () // Skip notifications
    //         | DebugSeverity.DebugSeverityHigh ->
    //             eprintfn "Error: [%A] %s" source message
    //         | DebugSeverity.DebugSeverityMedium ->
    //             printfn "Warning: [%A] %s" source message
    //         | DebugSeverity.DebugSeverityLow ->
    //             printfn "Info: [%A] %s" source message
    //         | _ ->
    //             printfn "[default] [%A] %s" source message

