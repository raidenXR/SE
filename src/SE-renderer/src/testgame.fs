namespace SE.Renderer
// open ImGuiNET
open OpenTK.Graphics
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics
open OpenTK.Windowing.Common
open OpenTK.Windowing.Desktop
open OpenTK.Windowing.GraphicsLibraryFramework
open System
open System.ComponentModel
open System.Diagnostics
open FSharp.NativeInterop

// open Dear_ImGui_Sample.Backends

type Particles(ob_model:Model, game_window_settings:GameWindowSettings, native_window_settings:NativeWindowSettings) =
    inherit GameWindow(game_window_settings, native_window_settings)
    
    let mutable vbo = 0
    let mutable vao = 0
    // let mutable ebo = 0
    let mutable shader: Shader = null
    let mutable camera: Camera = null
    let mutable first_move = true
    let mutable last_pos = Vector2.Zero
    let mutable wireframe_on = false
    let mutable gltf: option<GLTF.Deserializer> = None

    new(model:Model) = new Particles(model, GameWindowSettings.Default, NativeWindowSettings(ClientSize = Vector2i(800, 600), Title = "opetk-window", Flags = ContextFlags.ForwardCompatible))

    override this.OnLoad() =
        base.OnLoad()
        GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f)
        GL.Enable(EnableCap.DepthTest)
        GL.Enable(EnableCap.ProgramPointSize)

        // GL.DebugMessageCallback(Debu
        do
            shader <- new Shader("shaders/particles.vert", "shaders/particles.frag")
            shader.Use()        
        
            vbo <- GL.GenBuffer()
            GL.BindBuffer (BufferTarget.ArrayBuffer, vbo)
            GL.BufferData (BufferTarget.ArrayBuffer, ob_model.VerticesBufferSize, ob_model.Vertices, BufferUsageHint.StaticDraw)
            
            vao <- GL.GenVertexArray()
            GL.BindVertexArray(vao)
            GL.EnableVertexAttribArray(0)
            GL.EnableVertexAttribArray(1)            
            GL.VertexAttribPointer(0, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.L, ob_model.Attrib0)
            GL.VertexAttribPointer(1, (GLTF.size "VEC4"), VertexAttribPointerType.Float, false, ob_model.L, ob_model.Attrib1)
            
        camera <- Camera(Vector3.UnitZ * 3f, 800f / 600f)
        this.CursorState <- CursorState.Grabbed

    override this.OnRenderFrame(e:FrameEventArgs) =
        base.OnRenderFrame(e)
        GL.Clear(ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit)

        shader.Use()
        shader.SetMatrix4("view", camera.GetViewMatrix())
        shader.SetMatrix4("projection", camera.GetProjectionMatrix())
        shader.SetMatrix4("model", Matrix4.CreateScale(10.f))

        GL.BindVertexArray(vao)
        GL.DrawArrays(PrimitiveType.Points, 0, ob_model.Vertices.Length)

        this.SwapBuffers()

    override this.OnUpdateFrame(e:FrameEventArgs) =
        base.OnUpdateFrame(e)
        let input = this.KeyboardState

        let camera_speed = 1.5f
        let sensitivity = 0.2f

        if input.IsKeyDown(Keys.W) then
            wireframe_on <- not wireframe_on
            if wireframe_on then GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line)
            if not wireframe_on then GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill)
 
        if input.IsKeyDown(Keys.Escape) then this.Close()
        if input.IsKeyDown(Keys.Up) then camera.Position <- camera.Position + camera.Front * camera_speed * (float32 e.Time)
        if input.IsKeyDown(Keys.Down) then camera.Position <- camera.Position - camera.Front * camera_speed * (float32 e.Time)
        if input.IsKeyDown(Keys.Right) then camera.Position <- camera.Position + camera.Right * camera_speed * (float32 e.Time)
        if input.IsKeyDown(Keys.Left) then camera.Position <- camera.Position - camera.Right * camera_speed * (float32 e.Time)
        if input.IsKeyDown(Keys.Space) then camera.Position <- camera.Position + camera.Up * camera_speed * (float32 e.Time)
        if input.IsKeyDown(Keys.LeftShift) then camera.Position <- camera.Position - camera.Up * camera_speed * (float32 e.Time)
                
        let mouse = this.MouseState
        if first_move then
            last_pos <- Vector2(mouse.X, mouse.Y)
            first_move <- false
        else
            let dx = mouse.X - last_pos.X
            let dy = mouse.Y - last_pos.Y
            last_pos <- Vector2(mouse.X, mouse.Y)
            camera.Yaw <- camera.Yaw + dx * sensitivity
            camera.Pitch <- camera.Pitch - dy * sensitivity
        
    override this.OnResize(e:ResizeEventArgs) =
        base.OnResize(e)
        let size_x = this.Size.X
        let size_y = this.Size.Y
        GL.Viewport(0, 0, size_x, size_y)

    member this.OnClosed() =
        match gltf with
        | Some g -> g.Dispose()
        | None -> ()
        

type TestGame(ob_model:ValueModel, game_window_settings:GameWindowSettings, native_window_settings:NativeWindowSettings) =
    inherit GameWindow(game_window_settings, native_window_settings)

    let mutable vbo = 0
    let mutable vao = 0
    let mutable ebo = 0
    let mutable shader: Shader = null
    let mutable camera: Camera = null
    let mutable first_move = true
    let mutable last_pos = Vector2.Zero
    let mutable wireframe_on = false
    let mutable gltf: option<GLTF.Deserializer> = None

    let update () =
        shader.Use()        
    
        GL.BindBuffer (BufferTarget.ArrayBuffer, vbo)
        GL.BufferData (BufferTarget.ArrayBuffer, ob_model.vertices.BufferSize, ob_model.vertices.ToInt(), BufferUsageHint.StaticDraw)
        // GL.BufferData (BufferTarget.ArrayBuffer, ob_model.VerticesBufferSize, ob_model.Vertices, BufferUsageHint.StaticDraw)
        
        GL.BindVertexArray(vao)
        GL.EnableVertexAttribArray(0)
        GL.EnableVertexAttribArray(1)
        GL.EnableVertexAttribArray(2)            
        GL.VertexAttribPointer(0, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib0)
        GL.VertexAttribPointer(1, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib1)
        GL.VertexAttribPointer(2, (GLTF.size "VEC4"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib2)

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo)
        GL.BufferData(BufferTarget.ElementArrayBuffer, ob_model.indices.BufferSize, ob_model.indices.ToInt(), BufferUsageHint.StaticDraw)
        // GL.BufferData(BufferTarget.ElementArrayBuffer, ob_model.IndicesBufferSize, ob_model.Indices, BufferUsageHint.StaticDraw)
        
        
        
    let Window_DebugProc (source:DebugSource) (_type:DebugType) (id:int) (severity:DebugSeverity) (length:int) (messagePtr:IntPtr) (userParam:IntPtr) =
        let message = System.Runtime.InteropServices.Marshal.PtrToStringAnsi(messagePtr, length)
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
            | DebugSeverity.DontCare -> printfn $"[DontCare] [{source}] {message}"
            | DebugSeverity.DebugSeverityNotification -> printfn "[Notification]"
            | DebugSeverity.DebugSeverityHigh -> printfn "Error: [{source}] {message}"
            | DebugSeverity.DebugSeverityMedium -> printfn "Warning: [{source}] {message}"
            | DebugSeverity.DebugSeverityLow -> printfn "Info: [{source}] {message}"
            | _ -> printfn $"[default] [{source}] {message}"


    new(model:ValueModel) = new TestGame(model, GameWindowSettings.Default, NativeWindowSettings(ClientSize = Vector2i(800, 600), Title = "opetk-window", Flags = ContextFlags.ForwardCompatible))

    member this.GltfRoot with get() = gltf and set(value) = gltf <- value

    override this.OnLoad() =
        base.OnLoad()
        GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f)
        GL.Enable(EnableCap.DepthTest)

        // GL.DebugMessageCallback(Debu
        do
            shader <- new Shader("shaders/shader.vert", "shaders/shader.frag")
            shader.Use()        
        
            vbo <- GL.GenBuffer()
            GL.BindBuffer (BufferTarget.ArrayBuffer, vbo)
            GL.BufferData (BufferTarget.ArrayBuffer, ob_model.vertices.BufferSize, ob_model.vertices.ToInt(), BufferUsageHint.StaticDraw)
            // GL.BufferData (BufferTarget.ArrayBuffer, ob_model.VerticesBufferSize, ob_model.Vertices, BufferUsageHint.StaticDraw)
            
            vao <- GL.GenVertexArray()
            GL.BindVertexArray(vao)
            GL.EnableVertexAttribArray(0)
            GL.EnableVertexAttribArray(1)
            GL.EnableVertexAttribArray(2)            
            GL.VertexAttribPointer(0, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib0)
            GL.VertexAttribPointer(1, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib1)
            GL.VertexAttribPointer(2, (GLTF.size "VEC4"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib2)

            ebo <- GL.GenBuffer()
            GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo)
            GL.BufferData(BufferTarget.ElementArrayBuffer, ob_model.indices.BufferSize, ob_model.indices.ToInt(), BufferUsageHint.StaticDraw)
            // GL.BufferData(BufferTarget.ElementArrayBuffer, ob_model.IndicesBufferSize, ob_model.Indices, BufferUsageHint.StaticDraw)
            
        camera <- Camera(Vector3.UnitZ * 3f, 800f / 600f)
        this.CursorState <- CursorState.Grabbed

        let debug_proc = DebugProc(Window_DebugProc)
        GL.DebugMessageCallback(Window_DebugProc, IntPtr.Zero)
        GL.Enable(EnableCap.DebugOutput)
        GL.Enable(EnableCap.DebugOutputSynchronous)
        // ignore (ImGui.CreateContext())
        // let io = ImGui.GetIO()
        // io.ConfigFlags <- io.ConfigFlags ||| ImGuiConfigFlags.NavEnableKeyboard ||| ImGuiConfigFlags.NavEnableGamepad ||| ImGuiConfigFlags.DockingEnable ||| ImGuiConfigFlags.ViewportsEnable
        // ImGui.StyleColorsDark()
        // let style = ImGui.GetStyle()
        // if (int (io.ConfigFlags &&& ImGuiConfigFlags.ViewportsEnable) <> 0) then
        //     style.WindowRounding <- 0.0f
        //     style.Colors[int ImGuiCol.WindowBg].W <- 1.0f

        // ignore (ImguiImplOpenTK4.Init(this))
        // ignore (ImguiImplOpenGL3.Init())


    override this.OnRenderFrame(e:FrameEventArgs) =
        base.OnRenderFrame(e)
        GL.Clear(ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit)

        shader.Use()
        shader.SetMatrix4("view", camera.GetViewMatrix())
        shader.SetMatrix4("projection", camera.GetProjectionMatrix())
        shader.SetMatrix4("model", Matrix4.CreateScale(10.f))
        // shader.SetMatrix4("model", ob_model.Transform)
        shader.SetVector3("viewPos", camera.Position)

        shader.SetVector3("material.ambient", Vector3(1.0f, 0.5f, 0.31f))
        shader.SetVector3("material.diffuse", Vector3(1.0f, 0.5f, 0.31f))
        shader.SetVector3("material.specular", Vector3(0.5f, 0.5f, 0.5f))
        shader.SetFloat("material.shininess", 32.0f)
        
        shader.SetVector3("dirLight.direction", Vector3(-0.2f, -1.0f, -0.3f))
        shader.SetVector3("dirLight.ambient", Vector3(0.05f, 0.05f, 0.05f))
        shader.SetVector3("dirLight.diffuse", Vector3(0.4f, 0.4f, 0.4f))
        shader.SetVector3("dirLight.specular", Vector3(0.5f, 0.5f, 0.5f))
        
        shader.SetVector3("pointLight.position", Vector3(2.0f, 2.0f, 4.0f))
        shader.SetVector3("pointLight.ambient", Vector3(0.05f, 0.05f, 0.05f))
        shader.SetVector3("pointLight.diffuse", Vector3(0.8f, 0.8f, 0.8f))
        shader.SetVector3("pointLight.specular", Vector3(1.0f, 1.0f, 1.0f))
        shader.SetFloat("pointLight.constant", 1.0f)
        shader.SetFloat("pointLight.linear", 0.09f)
        shader.SetFloat("pointLight.quadratic", 0.032f)
        
        shader.SetVector3("spotLight.position", camera.Position)
        shader.SetVector3("spotLight.direction", camera.Front)
        shader.SetVector3("spotLight.ambient", Vector3(0.0f, 0.0f, 0.0f))
        shader.SetVector3("spotLight.diffuse", Vector3(1.0f, 1.0f, 1.0f))
        shader.SetVector3("spotLight.specular", Vector3(1.0f, 1.0f, 1.0f))
        shader.SetFloat("spotLight.constant", 1.0f)
        shader.SetFloat("spotLight.linear", 0.09f)
        shader.SetFloat("spotLight.quadratic", 0.032f)
        shader.SetFloat("spotLight.cutOff", MathF.Cos(MathHelper.DegreesToRadians(12.5f)))
        shader.SetFloat("spotLight.outerCutOff", MathF.Cos(MathHelper.DegreesToRadians(17.5f)))

        GL.BindVertexArray(vao)
        GL.DrawElements(PrimitiveType.Triangles, ob_model.Indices.Length, DrawElementsType.UnsignedInt, 0)

        // ImguiImplOpenGL3.NewFrame()
        // ImguiImplOpenTK4.NewFrame()
        // ImGui.NewFrame()

        // ignore (ImGui.DockSpaceOverViewport())
        // ImGui.ShowDemoWindow()
        // ImGui.Render()
        // ImguiImplOpenGL3.RenderDrawData(ImGui.GetDrawData())
        // if (ImGui.GetIO().ConfigFlags.HasFlag(ImGuiConfigFlags.ViewportsEnable)) then
        //     ImGui.UpdatePlatformWindows()
        //     ImGui.RenderPlatformWindowsDefault()
        //     this.Context.MakeCurrent()       

        this.SwapBuffers()
        

    override this.OnUpdateFrame(e:FrameEventArgs) =
        base.OnUpdateFrame(e)
        let input = this.KeyboardState

        let camera_speed = 1.5f
        let sensitivity = 0.2f

        match gltf with
        | Some g when g.Root.animations <> null -> g.UpdateAnimation_unmanaged(ob_model, e.Time * 16.)
        // | Some g when g.Root.animations <> null -> g.UpdateAnimation(ob_model, e.Time * 16.)
        | None -> ()
        update ()

        if input.IsKeyDown(Keys.W) then
            wireframe_on <- not wireframe_on
            if wireframe_on then GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line)
            if not wireframe_on then GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill)
 
        if input.IsKeyDown(Keys.Escape) then this.Close()
        if input.IsKeyDown(Keys.Up) then camera.Position <- camera.Position + camera.Front * camera_speed * (float32 e.Time)
        if input.IsKeyDown(Keys.Down) then camera.Position <- camera.Position - camera.Front * camera_speed * (float32 e.Time)
        if input.IsKeyDown(Keys.Right) then camera.Position <- camera.Position + camera.Right * camera_speed * (float32 e.Time)
        if input.IsKeyDown(Keys.Left) then camera.Position <- camera.Position - camera.Right * camera_speed * (float32 e.Time)
        if input.IsKeyDown(Keys.Space) then camera.Position <- camera.Position + camera.Up * camera_speed * (float32 e.Time)
        if input.IsKeyDown(Keys.LeftShift) then camera.Position <- camera.Position - camera.Up * camera_speed * (float32 e.Time)
                
        let mouse = this.MouseState
        if first_move then
            last_pos <- Vector2(mouse.X, mouse.Y)
            first_move <- false
        else
            let dx = mouse.X - last_pos.X
            let dy = mouse.Y - last_pos.Y
            last_pos <- Vector2(mouse.X, mouse.Y)
            camera.Yaw <- camera.Yaw + dx * sensitivity
            camera.Pitch <- camera.Pitch - dy * sensitivity
        
    override this.OnResize(e:ResizeEventArgs) =
        base.OnResize(e)
        let size_x = this.Size.X
        let size_y = this.Size.Y
        GL.Viewport(0, 0, size_x, size_y)

    member this.OnClosed() =
        match gltf with
        | Some g -> g.Dispose()
        | None -> ()
        // ImguiImplOpenGL3.Shutdown()
        // ImguiImplOpenTK4.Shutdown()

    // static member DebugProcCallback = DebugProc(CubeGame.Window_DebugProc) 


type [<Struct>] Vertex = {pos:Vector3; color:Vector4} 

type GltfWithParticles(_gltf:option<GLTF.Deserializer>, ob_model:ValueModel, game_window_settings:GameWindowSettings, native_window_settings:NativeWindowSettings) =
    inherit GameWindow(game_window_settings, native_window_settings)
    
    let mutable animation_active = true
    let mutable render_ob_model = true
    let mutable render_ob_model_prev = false
    let mutable current_space = false
    let mutable prev_space = false
    
    let mutable vbo1 = 0
    let mutable vao1 = 0
    let mutable shader1: Shader = null
    let N = 80
    let voxels = Array3D.zeroCreate N N N
    let voxels_particles = new NativeArray<float32>(N * N * N * 7)
    let particles = new ValueModel(voxels_particles, new NativeArray<uint32>(10), [3;4])

    let mutable vbo2 = 0
    let mutable vao2 = 0
    let mutable ebo2 = 0
    let mutable shader2: Shader = null
    let mutable gltf = _gltf

    let mutable camera: Camera = null
    let mutable first_move = true
    let mutable last_pos = Vector2.Zero
    let mutable wireframe_on = false

    do
        Geometry.assign_particles_unmanaged ob_model particles voxels N 

    let load_particles () =
        shader1 <- new Shader("shaders/particles.vert", "shaders/particles.frag")
        shader1.Use()        
    
        vbo1 <- GL.GenBuffer()
        GL.BindBuffer (BufferTarget.ArrayBuffer, vbo1)
        GL.BufferData (BufferTarget.ArrayBuffer, particles.vertices.BufferSize, voxels_particles.ToInt(), BufferUsageHint.DynamicDraw)
        
        vao1 <- GL.GenVertexArray()
        GL.BindVertexArray(vao1)
        GL.EnableVertexAttribArray(0)
        GL.EnableVertexAttribArray(1)            
        GL.VertexAttribPointer(0, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, 7, 0)
        GL.VertexAttribPointer(1, (GLTF.size "VEC4"), VertexAttribPointerType.Float, false, 7, 3)

    let load_gltf () =
        shader2 <- new Shader("shaders/shader.vert", "shaders/shader.frag")
        shader2.Use()        
    
        vbo2 <- GL.GenBuffer()
        GL.BindBuffer (BufferTarget.ArrayBuffer, vbo2)
        GL.BufferData (BufferTarget.ArrayBuffer, ob_model.vertices.BufferSize, ob_model.vertices.ToInt(), BufferUsageHint.StaticDraw)
        
        vao2 <- GL.GenVertexArray()
        GL.BindVertexArray(vao2)
        GL.EnableVertexAttribArray(0)
        GL.EnableVertexAttribArray(1)
        GL.EnableVertexAttribArray(2)            
        GL.VertexAttribPointer(0, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib0)
        GL.VertexAttribPointer(1, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib1)
        GL.VertexAttribPointer(2, (GLTF.size "VEC4"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib2)

        ebo2 <- GL.GenBuffer()
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo2)
        GL.BufferData(BufferTarget.ElementArrayBuffer, ob_model.indices.BufferSize, ob_model.indices.ToInt(), BufferUsageHint.StaticDraw)
        
    let update_particles (e:FrameEventArgs) =
        Geometry.assign_particles_unmanaged ob_model particles voxels N

        shader1.Use()        
    
        GL.BindBuffer (BufferTarget.ArrayBuffer, vbo1)
        GL.BufferData (BufferTarget.ArrayBuffer, particles.vertices.BufferSize, voxels_particles.ToInt(), BufferUsageHint.DynamicDraw)
        
        GL.BindVertexArray(vao1)
        GL.EnableVertexAttribArray(0)
        GL.EnableVertexAttribArray(1)            
        GL.VertexAttribPointer(0, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, 7, 0)
        GL.VertexAttribPointer(1, (GLTF.size "VEC4"), VertexAttribPointerType.Float, false, 7, 3)
        
    let update_gltf (e:FrameEventArgs) =
        if gltf.IsSome && gltf.Value.Root.animations <> null then 
            gltf.Value.UpdateAnimation_unmanaged(ob_model, e.Time * 16.)
            // gltf.Value.UpdateAnimation(ob_model, e.Time * 16.)

        shader2.Use()        
    
        GL.BindBuffer (BufferTarget.ArrayBuffer, vbo2)
        GL.BufferData (BufferTarget.ArrayBuffer, ob_model.vertices.BufferSize, ob_model.vertices.ToInt(), BufferUsageHint.DynamicDraw)
        
        GL.BindVertexArray(vao2)
        GL.EnableVertexAttribArray(0)
        GL.EnableVertexAttribArray(1)
        GL.EnableVertexAttribArray(2)            
        GL.VertexAttribPointer(0, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib0)
        GL.VertexAttribPointer(1, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib1)
        GL.VertexAttribPointer(2, (GLTF.size "VEC4"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib2)

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo2)
        GL.BufferData(BufferTarget.ElementArrayBuffer, ob_model.indices.BufferSize, ob_model.indices.ToInt(), BufferUsageHint.DynamicDraw)
    

    let render_particles () =
        shader1.Use()
        shader1.SetMatrix4("view", camera.GetViewMatrix())
        shader1.SetMatrix4("projection", camera.GetProjectionMatrix())
        shader1.SetMatrix4("model", Matrix4.CreateScale(10.f))

        GL.BindVertexArray(vao1)
        GL.DrawArrays(PrimitiveType.Points, 0, particles.vertices.BufferSize)

    let render_gltf () =
        shader2.Use()
        shader2.SetMatrix4("view", camera.GetViewMatrix())
        shader2.SetMatrix4("projection", camera.GetProjectionMatrix())
        shader2.SetMatrix4("model", Matrix4.CreateScale(10.f))
        shader2.SetVector3("viewPos", camera.Position)

        shader2.SetVector3("material.ambient", Vector3(1.0f, 0.5f, 0.31f))
        shader2.SetVector3("material.diffuse", Vector3(1.0f, 0.5f, 0.31f))
        shader2.SetVector3("material.specular", Vector3(0.5f, 0.5f, 0.5f))
        shader2.SetFloat("material.shininess", 32.0f)
        
        shader2.SetVector3("dirLight.direction", Vector3(-0.2f, -1.0f, -0.3f))
        shader2.SetVector3("dirLight.ambient", Vector3(0.05f, 0.05f, 0.05f))
        shader2.SetVector3("dirLight.diffuse", Vector3(0.4f, 0.4f, 0.4f))
        shader2.SetVector3("dirLight.specular", Vector3(0.5f, 0.5f, 0.5f))
        
        shader2.SetVector3("pointLight.position", Vector3(2.0f, 2.0f, 4.0f))
        shader2.SetVector3("pointLight.ambient", Vector3(0.05f, 0.05f, 0.05f))
        shader2.SetVector3("pointLight.diffuse", Vector3(0.8f, 0.8f, 0.8f))
        shader2.SetVector3("pointLight.specular", Vector3(1.0f, 1.0f, 1.0f))
        shader2.SetFloat("pointLight.constant", 1.0f)
        shader2.SetFloat("pointLight.linear", 0.09f)
        shader2.SetFloat("pointLight.quadratic", 0.032f)
        
        shader2.SetVector3("spotLight.position", camera.Position)
        shader2.SetVector3("spotLight.direction", camera.Front)
        shader2.SetVector3("spotLight.ambient", Vector3(0.0f, 0.0f, 0.0f))
        shader2.SetVector3("spotLight.diffuse", Vector3(1.0f, 1.0f, 1.0f))
        shader2.SetVector3("spotLight.specular", Vector3(1.0f, 1.0f, 1.0f))
        shader2.SetFloat("spotLight.constant", 1.0f)
        shader2.SetFloat("spotLight.linear", 0.09f)
        shader2.SetFloat("spotLight.quadratic", 0.032f)
        shader2.SetFloat("spotLight.cutOff", MathF.Cos(MathHelper.DegreesToRadians(12.5f)))
        shader2.SetFloat("spotLight.outerCutOff", MathF.Cos(MathHelper.DegreesToRadians(17.5f)))

        GL.BindVertexArray(vao2)
        GL.DrawElements(PrimitiveType.Triangles, ob_model.Indices.Length, DrawElementsType.UnsignedInt, 0)

        
    new(_gltf:option<GLTF.Deserializer>, model:ValueModel) = new GltfWithParticles(_gltf, model, GameWindowSettings.Default, NativeWindowSettings(ClientSize = Vector2i(800, 600), Title = "opetk-window", Flags = ContextFlags.ForwardCompatible))
    
    override this.OnLoad() =
        base.OnLoad()
        GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f)
        GL.Enable(EnableCap.DepthTest)
        GL.Enable(EnableCap.ProgramPointSize)

        load_gltf ()
        load_particles ()
        
        camera <- Camera(Vector3.UnitZ * 3f, 800f / 600f)
        this.CursorState <- CursorState.Grabbed


        
    override this.OnRenderFrame(e:FrameEventArgs) =
        base.OnRenderFrame(e)
        GL.Clear(ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit)

        if render_ob_model then 
            render_gltf ()
        render_particles ()
        
        this.SwapBuffers()

        
    override this.OnUpdateFrame(e:FrameEventArgs) =
        base.OnUpdateFrame(e)
        let input = this.KeyboardState

        let camera_speed = 1.5f
        let sensitivity = 0.2f

        if animation_active then 
            update_gltf (e)
            update_particles (e)

        if input.IsKeyDown(Keys.W) then
            wireframe_on <- not wireframe_on
            if wireframe_on then GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line)
            if not wireframe_on then GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill)
 
        if input.IsKeyDown(Keys.Escape) then this.Close()
        if input.IsKeyDown(Keys.Up) then camera.Position <- camera.Position + camera.Front * camera_speed * (float32 e.Time)
        if input.IsKeyDown(Keys.Down) then camera.Position <- camera.Position - camera.Front * camera_speed * (float32 e.Time)
        if input.IsKeyDown(Keys.Right) then camera.Position <- camera.Position + camera.Right * camera_speed * (float32 e.Time)
        if input.IsKeyDown(Keys.Left) then camera.Position <- camera.Position - camera.Right * camera_speed * (float32 e.Time)
        if input.IsKeyDown(Keys.Space) then camera.Position <- camera.Position + camera.Up * camera_speed * (float32 e.Time)
        if input.IsKeyDown(Keys.LeftShift) then camera.Position <- camera.Position - camera.Up * camera_speed * (float32 e.Time)
        if input.IsKeyDown(Keys.Space) && not prev_space then animation_active <- not animation_active
        if input.IsKeyDown(Keys.K) && not render_ob_model_prev then render_ob_model <- not render_ob_model

        prev_space <- input.IsKeyDown(Keys.Space)
        render_ob_model_prev <- input.IsKeyDown(Keys.K)
                
        let mouse = this.MouseState
        if first_move then
            last_pos <- Vector2(mouse.X, mouse.Y)
            first_move <- false
        else
            let dx = mouse.X - last_pos.X
            let dy = mouse.Y - last_pos.Y
            last_pos <- Vector2(mouse.X, mouse.Y)
            camera.Yaw <- camera.Yaw + dx * sensitivity
            camera.Pitch <- camera.Pitch - dy * sensitivity

            
    override this.OnResize(e:ResizeEventArgs) =
        base.OnResize(e)
        let size_x = this.Size.X
        let size_y = this.Size.Y
        GL.Viewport(0, 0, size_x, size_y)

    member this.OnClosed() =
        if gltf.IsSome then gltf.Value.Dispose()


    override this.Dispose() =
        base.Dispose()
        ob_model.Dispose()
        particles.Dispose()

    
        
