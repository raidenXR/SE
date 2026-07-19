#r "../bin/Debug/net10.0/SE-renderer.dll"
#r "../bin/Debug/net10.0/SE-core.dll"
// #r "../bin/Release/net10.0/SE-renderer.dll"
// #r "../bin/Release/net10.0/SE-core.dll"
#r "nuget: OpenTK, 4.9.4"
#r "nuget: SkiaSharp, 2.88.6"
#r "nuget: FFMPegCore, 5.4.0"


open OpenTK.Core
open OpenTK.Graphics
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics
open OpenTK.Windowing.Common
open OpenTK.Windowing.Common.Input
open OpenTK.Windowing.Desktop
open OpenTK.Windowing.GraphicsLibraryFramework

open System
open System.Runtime.InteropServices
open System.Runtime.CompilerServices
open FSharp.NativeInterop

open SkiaSharp
open FFMpegCore
open FFMpegCore.Pipes

open SE
open SE.Core
open SE.ECS
open SE.Spatial
open SE.Renderer

open SE.Renderer.VideoCapture

type HasAnimation = struct end
type HasParticles = struct end

type [<Struct>] AnimationActive = {animation_active:bool; prev_key:bool}
type [<Struct>] WireFrameActice = {wireframe_active:bool; prev_key:bool}
type [<Struct>] SliceLen = {count:int}
type [<Struct>] DefaultTransform = {T:Matrix4}

type [<Struct>] State = {
    mutable wireframe_on:bool
    mutable first_move:bool
    mutable last_pos:Vector2
}

type [<Struct>] DiscretizedVolume = {
    cv: CVBounds
    voxels: narray3d<bool>
    t_filled: int
    m_transform: Matrix4
}

let path = System.Environment.GetCommandLineArgs()[2]
let gltf = if path.Contains(".gltf") then Some (new GLTF.Deserializer(path)) else None
let N = 200

let mutable camera: Camera = null
let mutable state: Entity = 0u

let mutable model_on = true
let mutable model_prev = false
let mutable wireframe_on = false
let mutable wireframe_prev = false
let mutable animation_on = true
let mutable animation_prev = false
let mutable particles_on = false
let mutable particles_prev = false

let mutable record = false
let mutable record_prev = false
let mutable total_time = 0.

// clear on exit
system OnExit [] (fun _ ->
    match gltf with
    | Some g -> g.Dispose()
    | None -> ()
)

let frames = ResizeArray<IVideoFrame>(1000)
let mutable octree: Octree.Root<double> option = None

let mean_vector (vertices:Span<float32>) =
    let mutable v_min = System.Numerics.Vector3.Zero
    let mutable v_max = System.Numerics.Vector3.Zero
    
    let len = vertices.Length / 10
    for i in 0..len-1 do
        let x = vertices[i*10+0]
        let y = vertices[i*10+1]
        let z = vertices[i*10+2]
        let v = System.Numerics.Vector3(x,y,z)
        v_min <- System.Numerics.Vector3.Min(v, v_min)
        v_max <- System.Numerics.Vector3.Max(v, v_min)

    let p = v_min + (v_max - v_min) / 2.f
    (p.X,p.Y,p.Z)


let settings = GameWindowSettings.Default
let n_settings = NativeWindowSettings(ClientSize = Vector2i(800, 600), Title = "opetk-window", Flags = ContextFlags.ForwardCompatible)
let window = new SE_Window(n_settings)


// create a state instance"
system OnLoad [] (fun _ ->
    state <- entity()
    state
    |> Entity.set {wireframe_on=false; first_move=true; last_pos=Vector2()}
    |> ignore
)

// create entities on load
system OnLoad [] (fun _ -> 
    let _mesh =
        match gltf with
        | _ when path.Contains(".txt") ->
            let (vertices,indices) = RGeometry.load_txt(path, 0.55f, 0.55f, 0.55f, 1.0f)
            {vertices = NativeArray.ofArray vertices; L = 10; indices = NativeArray.ofArray indices}
        | Some gltf -> gltf.ReadMeshF(0)
        | None -> RGeometry.load_ply_unmanaged (path, 0.55f, 0.55f, 0.53f, 1.0f)
            
    let L = 10
    octree <- Some (Octree.ofSurface<double> N L 4 (_mesh.vertices.AsSpan()) (_mesh.indices.AsSpan()))
    let ob_model = new Model(_mesh.vertices, _mesh.indices, [3;3;4])

    let mesh = Helpers.createMesh ob_model    

    // create particles
    let particles_len = octree.Value.GetCount()
    let particles_array = Array.zeroCreate<float32> (particles_len*7)
    let mutable i = 0
    octree.Value.IterParallel 1 (fun node ->
        match node with
        | Octree.Internal -> 
            let pos = Octree.center node
            particles_array[i+0] <- pos.X
            particles_array[i+1] <- pos.Y
            particles_array[i+2] <- pos.Z
            particles_array[i+3] <- 0.95f
            particles_array[i+4] <- 0.25f
            particles_array[i+5] <- 0.23f
            particles_array[i+6] <- 1.f
            i <- i + 7

        | Octree.Boundary ->
            let pos = Octree.center node
            particles_array[i+0] <- pos.X
            particles_array[i+1] <- pos.Y
            particles_array[i+2] <- pos.Z
            particles_array[i+3] <- 0.55f
            particles_array[i+4] <- 0.55f
            particles_array[i+5] <- 0.53f
            particles_array[i+6] <- 1.f
            i <- i + 7

        | Octree.External -> ()
    )

    let particles = particles_array |> NativeArray.ofArray
    
    let pt_model = new Model(particles,[3;4])
    let prim = Helpers.createPrim_sliced pt_model particles_len

    printfn "for N: %d, filled_voxels: %d, particles_size: %d" N particles_len (particles.Length / 7)
    GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f)
    GL.Enable(EnableCap.DepthTest)
    GL.Enable(EnableCap.ProgramPointSize)

    let (x,y,z) = mean_vector (_mesh.vertices.AsSpan())
    camera <- Camera(Vector3(x,y,z), 800f / 600f)
    // camera <- Camera(Vector3.UnitZ * 3f, 800f / 600f)
    window.CursorState <- CursorState.Grabbed

    Shaders.load [
        "model_shader", "shaders/shader.vert", "shaders/shader.frag"
        "particles_shader", "shaders/particles.vert", "shaders/particles.frag"
    ]

    let matrix_transform = if path.Contains(".txt") then Matrix4.CreateScale(0.2f) else Matrix4.CreateScale(10.f)
    
    let model = 
        entity()
        |> Entity.set ob_model
        |> Entity.set mesh
        |> Entity.set {T = matrix_transform}
        |> Entity.set matrix_transform
        |> Entity.set {wireframe_active = true; prev_key = false}
        |> Entity.add<HasAnimation>

    let particles = 
        entity()
        |> Entity.set pt_model
        |> Entity.set prim
        |> Entity.set {T = matrix_transform}
        |> Entity.set matrix_transform
        |> Entity.set {count = particles_len * 7 - 700}

    // create relation between a and b -> paticles depend on model
    relate model particles (HasParticles()) 

    if path.Contains("animated") then
        let animation =
            entity()
            |> Entity.set {idx = 0; dt = 0.; is_reversed = false; is_active = true; is_looped = true}
            |> Entity.set {animation_active = true; prev_key = false}
        
        relate model animation (HasAnimation())
)

// load the window
system OnLoad [] (fun _ -> window.Load())


let render_ply () =
    let models = Components.get<Model>()
    let meshes = Components.get<GLMesh>()
    let transforms = Components.get<Matrix4>()
    
    let shader = Shaders.get("model_shader")

    shader.Use()
    shader.SetMatrix4("view", camera.GetViewMatrix())
    shader.SetMatrix4("projection", camera.GetProjectionMatrix())
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

    for e in meshes.Entities do
        shader.SetMatrix4("model", transforms[e])
        Helpers.drawMesh models[e] meshes[e]
  

let render_particles () =
    let models = Components.get<Model>()
    let prims = Components.get<GLPrim>()
    let transforms = Components.get<Matrix4>()
    let lens = Components.get<SliceLen>()
    
    if particles_on then 
        let particles = Shaders.get("particles_shader")
        particles.Use()
        particles.SetMatrix4("view", camera.GetViewMatrix())
        particles.SetMatrix4("projection", camera.GetProjectionMatrix())

        for e in prims.Entities do
            particles.SetMatrix4("model", transforms[e])
            Helpers.drawPrim_sliced (lens[e].count) prims[e]


// Update animations system
system OnUpdate [typeof<ValueAnimation>] (fun q -> 
    let animts = Components.get<ValueAnimation>()
    let transforms = Components.get<Matrix4>()
    let matrices = Components.get<DefaultTransform>()

    if animation_on && q.Count > 0 then       
        let time = window.ElapsedTime * 0.4   // the animation is too fast, slow it down a bit
        
        for e in q do
            let anim = &animts[e]
            let m_ent = Relation.get<HasAnimation> In e
            let p_ent = Relation.get<HasParticles> Out m_ent

            let mutable animation_m = Helpers.animationTransform gltf.Value time &anim
            let a_transform = Unsafe.As<System.Numerics.Matrix4x4, Matrix4>(&animation_m)  // cast to Matrix4
        
            transforms[m_ent] <- a_transform * matrices[m_ent].T
            transforms[p_ent] <- a_transform * matrices[p_ent].T
)

// update the keys input
system OnUpdate [] (fun _ ->
    let e = window.ElapsedTime
    let input = window.KeyboardState

    let s = &Components.get<State>()[state]
    let camera_speed = 1.5f
    let sensitivity = 0.2f
    
    if input.IsKeyDown(Keys.W) && not wireframe_prev then
        wireframe_prev <- true
        wireframe_on <- not wireframe_on
        if wireframe_on then GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line)
        if not wireframe_on then GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill)
    elif not (input.IsKeyDown(Keys.W)) then
        wireframe_prev <- false

    if input.IsKeyDown(Keys.M) && not model_prev then
        model_prev <- true
        model_on <- not model_on
    elif not (input.IsKeyDown(Keys.M)) then
        model_prev <- false

    if input.IsKeyDown(Keys.K) && not animation_prev then
        animation_prev <- true
        animation_on <- not animation_on        
    elif not (input.IsKeyDown(Keys.K)) then
        animation_prev <- false

    if input.IsKeyDown(Keys.P) && not particles_prev then
        particles_prev <- true
        particles_on <- not particles_on
    elif not (input.IsKeyDown(Keys.P)) then
        particles_prev <- false

    if input.IsKeyDown(Keys.Escape) then
        window.Close()
        Systems.quit()
    if input.IsKeyDown(Keys.Up) then camera.Position <- camera.Position + camera.Front * camera_speed * (float32 e)
    if input.IsKeyDown(Keys.Down) then camera.Position <- camera.Position - camera.Front * camera_speed * (float32 e)
    if input.IsKeyDown(Keys.Right) then camera.Position <- camera.Position + camera.Right * camera_speed * (float32 e)
    if input.IsKeyDown(Keys.Left) then camera.Position <- camera.Position - camera.Right * camera_speed * (float32 e)
    if input.IsKeyDown(Keys.Space) then camera.Position <- camera.Position + camera.Up * camera_speed * (float32 e)
    if input.IsKeyDown(Keys.LeftShift) then camera.Position <- camera.Position - camera.Up * camera_speed * (float32 e)
    
    if input.IsKeyDown(Keys.R) && not record_prev then
        record_prev <- true
        record <- not record
        if record then printfn "recording stared" else printfn "recording paused"
    elif not (input.IsKeyDown(Keys.R)) then
        record_prev <- false
    
    let mouse = window.MouseState
    if s.first_move then
        s.last_pos <- Vector2(mouse.X, mouse.Y)
        s.first_move <- false
    else
        let dx = mouse.X - s.last_pos.X
        let dy = mouse.Y - s.last_pos.Y
        s.last_pos <- Vector2(mouse.X, mouse.Y)
        camera.Yaw <- camera.Yaw + dx * sensitivity
        camera.Pitch <- camera.Pitch - dy * sensitivity
)

// invoke when window renders frame
system OnRender [typeof<Model>] (fun q -> 
    window.Update(fun _ ->
        GL.Clear(ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit)
        if model_on then
            render_ply ()
        render_particles ()
        window.Context.SwapBuffers()
    )

    total_time <- total_time + window.ElapsedTime 
    // printfn "%g" (window.ElapsedTime)
    if record && (total_time > 0.16) then
        capture_frame frames window
        total_time <- total_time - 0.16
        let struct(i,j) = Console.GetCursorPosition()
        printfn "frame %d captured" (Seq.length frames)
        Console.SetCursorPosition(i,j)
)


// clear all resources
system OnExit [] (fun _ ->
    let models = Components.get<Model>().Entries
    for model in models do
        model.Dispose()

    let meshes = Components.get<GLMesh>().Entries
    for mesh in meshes do
        GL.DeleteVertexArray(mesh.vao)
        GL.DeleteBuffer(mesh.vbo)
        GL.DeleteBuffer(mesh.ebo)

    let prims = Components.get<GLPrim>().Entries
    for prim in prims do
        GL.DeleteVertexArray(prim.vao)
        GL.DeleteBuffer(prim.vbo)

    Shaders.unload()
    window.Dispose()
)

system OnExit [] (fun _ ->
    if frames.Count > 0 then
        printfn "wnd exists, calling 'create_video_from_frames'"
        create_video_from_frames "./capture.mp4" frames window
        printfn "process finished running on %d frames" (Seq.length frames)
)
    
Systems.progress()

