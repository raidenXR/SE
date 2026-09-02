#r "../bin/Debug/net10.0/SE-renderer.dll"
#r "../bin/Debug/net10.0/SE-core.dll"
// #r "../bin/Release/net10.0/SE-renderer.dll"
// #r "../bin/Release/net10.0/SE-core.dll"
#r "nuget: OpenTK, 4.9.4"
#r "nuget: SkiaSharp, 2.88.6"
#r "nuget: ImGui.NET, 1.91.6.1"
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
open ImGuiNET
open FFMpegCore
open FFMpegCore.Pipes
open SE.Renderer.VideoCapture

open SE
open SE.Core
open SE.ECS
open SE.Spatial
open SE.Renderer

let [<Literal>] N = 260
let [<Literal>] L = 10
let [<Literal>] k = 3
let [<Literal>] ss = "../../../resources/shaders/"

type IsPoints = struct end
type UpdateColors = struct end
type IsTexture = struct end
type TextureString = struct end

let mutable time_elapsed = 0.
let mutable color_edit = System.Numerics.Vector3.Zero

type [<Struct>] Temperature   = Temperature of float
type [<Struct>] Thickness     = Thickness of float
type [<Struct>] Concentration = Concentration of float

let mutable solver_update = true
let mutable solver_update_prev = false
let mutable pause = true
let mutable pause_prev = false

// prefab example
let is_electrolyte_cv = prefab2 (Temperature 300.) (Concentration 3.2)
let is_electrode_cv = prefab2 (Temperature 600.) (Thickness (3.2e-6))

let frames = ResizeArray<IVideoFrame>(1000)

let inline (!) (u:Octree.Node<'T>) = Octree.valueof u
let inline Tf32 (Temperature T) = T
let inline vec3 (v:Vector3) = System.Numerics.Vector3(v.X, v.Y, v.Z)
let pos = Octree.center

let octree_to_buffer (tree:Octree.Root<Entity>) (colorbar:Colorbar) (mesh:Mesh) =
    let T = Components.get<Temperature>()
    let mutable i = 0
    tree.Iteri (fun _ u ->
        match u with
        // | Octree.Internal | Octree.Boundary ->
        | Octree.Internal ->
            let vertices = mesh.vertices.AsSpan()
            if (i*mesh.L+6) >= vertices.Length then printfn "i: %d, tree_len: %d" i (tree.GetInternalCount())
            let p = pos u
            let c = colorbar[Tf32 T[!u]]
            vertices[i*mesh.L + 0] <- p.X
            vertices[i*mesh.L + 1] <- p.Y
            vertices[i*mesh.L + 2] <- p.Z
            vertices[i*mesh.L + 3] <- c.X
            vertices[i*mesh.L + 4] <- c.Y
            vertices[i*mesh.L + 5] <- c.Z
            vertices[i*mesh.L + 6] <- c.W
            i <- i + 1
            
        | _ -> ()
    )

let path =
    System.Environment.GetCommandLineArgs()[2]
    
let rotation =
    match path with
    | _ when path.Contains(".txt") -> System.Numerics.Quaternion.CreateFromYawPitchRoll(2.f, 2.f, 1.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
    | _ when path.Contains(".ply") -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.f, 0.f, 0.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
    | _ when path.Contains(".gltf") -> System.Numerics.Quaternion.CreateFromYawPitchRoll(2.f, 4.f, 3.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
    | _ -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.f, 0.f, 0.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        

let scale =
    match path with
    | _ when path.Contains(".txt") -> 0.2f
    | _ when path.Contains(".ply") -> 10.f
    | _ when path.Contains(".gltf") -> 5.f
    | _ -> 1.f
      
let tree =
    path
    |> RGeometry.load_model
    |> RGeometry.tranform rotation
    |> Octree.ofMesh<Entity> N k

let tree' = tree.Copy()

    
let colorbar = new Colorbar(Colormap.Jet, 280., 620.)
// let tree_len = tree.GetCount()
let tree_len = tree.GetInternalCount()
printfn "total: %d, leafs: %d" (tree.GetCount()) (tree.GetInternalCount())
let max_iter = max 50 (tree_len / 1000)
let mutable n = 0

// UI
SE_UI.Shared.OnRender (fun _ ->
    let camera = SE_Window.Shared.Camera
    let mutable p = vec3(camera.Position)
    let mutable v = vec3(camera.GetView())

    let viewport_size = ImGui.GetMainViewport().Size

    // Position at left edge
    ImGui.SetNextWindowPos(System.Numerics.Vector2(0.f, 0.f))
    ImGui.SetNextWindowSize(System.Numerics.Vector2(280.f, 140.f))

    ImGui.Begin("Panel") |> ignore
    ImGui.SetWindowFontScale(1.2f)
    ImGui.InputFloat3("pos:  ", &p) |> ignore
    ImGui.InputFloat3("view: ", &v) |> ignore
    ImGui.Text($"count: {tree_len}")
    ImGui.Text($"iter: {n}/{max_iter}")
    ImGui.End()
)


// clear on exit
system OnExit [] (fun _ ->
    SE_Window.Shared.Dispose()
    SE_UI.Shared.OnClosed()
    colorbar.Dispose()
)

// clear all resources
system OnExit [] (fun _ ->
    for vb in Components.get<VertexBuffer>().Entries do
        VertexBuffer.delete vb        
        
    for mesh in Components.get<Mesh>().Entries do
        mesh.Dispose()
        
    for texture in Components.get<Texture>().Entries do
        Texture.delete texture

    Shaders.unload()
)

system PreRender [] (fun _ ->
    let wnd = SE_Window.Shared
    let input = wnd.KeyboardState
    if input.IsKeyDown(Keys.Escape) then wnd.Close()
    if input.IsKeyDown(Keys.Escape) then Systems.quit()
    if input.IsKeyDown(Keys.P) && not pause_prev then
        pause_prev <- true
        pause <- not pause
        match pause with
        | true ->
            Systems.pause()
        | false ->
            wnd.IsRecording <- true
            Systems.unpause()
    elif not (input.IsKeyDown(Keys.P)) then
        pause_prev <- false
)

system PostLoad [] (fun _ -> Systems.pause())

// load window and resources
system OnLoad [] (fun _ ->
    let p = Octree.center (tree.Root)
    
    let wnd = SE_Window.Shared
    wnd.Camera.Position <- Vector3(p.X, p.Y, p.Z)
    wnd.Camera.Speed <- 2.f
    wnd.CursorState <- CursorState.Grabbed

    wnd.Load()

    Shaders.load [
        "m_shader", ss + "shader.vert", ss + "shader.frag"
        "p_shader", ss + "particles.vert", ss + "particles.frag"
        "t_shader", ss + "string_text.vert", ss + "string_text.frag"
    ]

    GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f)
    GL.Enable(EnableCap.DepthTest)
    GL.Enable(EnableCap.ProgramPointSize)
    GL.Enable(EnableCap.Blend)
    GL.BlendEquation(BlendEquationMode.FuncAdd)

    // Required for Skia's premultiplied-alpha pixels
    GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha)

    SE_UI.Shared.OnLoad(wnd) |> ignore

    printfn "resouces initialization load"
)

system PreRender [] (fun _ ->
    let wnd = SE_Window.Shared
    wnd.Update (fun _ -> GL.Clear(ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit))
    SE_UI.Shared.OnRenderFrame(SE_Window.Shared)
)

// system OnRender [] (fun _ ->
// )

system PostRender [] (fun _ ->
    let wnd = SE_Window.Shared
    wnd.Update (SE_Window.Shared.Context.SwapBuffers)        
)

// initialize octree entities
system OnLoad [] (fun _ ->
    let L = 7
    let mutable vertices = NativeArray.create<float32> (tree_len*L)
    let indices  = NativeArray.empty<uint32>()

    let mutable i = 0

    tree.Iter (fun u ->
        match u with
        | Octree.Internal & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> is_electrolyte_cv |> ValueSome
        | Octree.Boundary & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> is_electrode_cv |> ValueSome
        | _ -> ()
    )

    tree'.Iter (fun u ->
        match u with
        | Octree.Internal & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> is_electrolyte_cv |> ValueSome
        | Octree.Boundary & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> is_electrode_cv |> ValueSome
        | _ -> ()
    )

    let mesh = {vertices = vertices; indices = indices; L = L}
    octree_to_buffer tree colorbar mesh
    
    entity()
    |> Entity.singleton "vertex_buffer"
    |> Entity.add<IsPoints>
    |> set mesh
    |> set (VertexBuffer.create VT2 mesh)
    |> set (Matrix4.CreateScale(scale))
    |> set (colorbar.AsTexture(0.8f, 0.0f, 120.f, 460.f))
    |> ignore
    
    printfn "vertex_buffer initialization load, count: %d" tree_len
)

// system OnLoad [] (fun _ ->
//     let camera = SE_Window.Shared.Camera
//     let c0 = sprintf "pos:  %A" (camera.Position)
//     let c1 = sprintf "view: %A" (camera.GetView())
//     let c2 = sprintf "count: %d" tree_len
    
//     entity()
//     |> Entity.add<TextureString>
//     |> set (Texture.string c0 -0.98f 0.8f)
//     |> Entity.singleton "camera_pos"
//     |> ignore
    
//     entity()
//     |> Entity.add<TextureString>
//     |> set (Texture.string c1 -0.98f 0.65f)
//     |> Entity.singleton "camera_view"
//     |> ignore

//     entity()
//     |> Entity.add<TextureString>
//     |> set (Texture.string c2 -0.98f 0.5f)
//     |> Entity.singleton "nodes_count"
//     |> ignore

//     for e in Components.get<Texture>().Entities do
//         Entity.printfn e
// )

system PreUpdate [] (fun _ ->
    n <- n + 1
    if n >= max_iter then
        Systems.pause()
        n <- 0
        printfn "n_max_iter --paused"
        Systems.quit()
)

// run this in parallel and update the vertex_buffer, once the mesh is ready
// or use SE_WINDOW dedicated thread for rendering, And all the rendering functions...
system OnUpdate [] (fun _ ->
    let T = Components.get<Temperature>()    

    tree.IterParallel 4 (fun u ->
        match u with
        | Octree.Internal ->
            let i  = u[-1,0,0]
            let i' = u[+1,0,0]
            let j  = u[0,-1,0]
            let j' = u[0,+1,0]
            let l  = u[0,0,-1]
            let l' = u[0,0,+1]

            let x1 = double (pos u - pos i).X
            let y1 = double (pos u - pos j).Y
            let z1 = double (pos u - pos l).Z
        
            let x2 = double (pos i' - pos u).X
            let y2 = double (pos j' - pos u).Y
            let z2 = double (pos l' - pos u).Z

            T[tree'[pos u].Value] <- Temperature(
                (2./(x1*(x1+x2))*Tf32(T[!i]) + 2./(x2*(x1+x2))*Tf32(T[!i'] ) +
                2./(y1*(y1+y2)) *Tf32(T[!j]) + 2./(y2*(y1+y2))*Tf32(T[!j'] ) +
                2./(z1*(z1+z2)) *Tf32(T[!l]) + 2./(z2*(z1+z2))*Tf32(T[!l'])) /
                (2./(x1*x2) + 2./(y1*y2) + 2./(z1*z2))
            )
        | _ -> ()
    )
    
    tree'.IterParallel 4 (fun u ->
        T[tree[pos u].Value] <- T[!u]    
    )

    "vertex_buffer"
    |> Entity.fetch
    |> Entity.add<UpdateColors>
    |> ignore
)

// system PreRender [typeof<TextureString>] (fun _ ->
//     let textures = Components.get<Texture>()
//     let camera = SE_Window.Shared.Camera
//     let e0 = Entity.fetch "camera_pos"
//     let e1 = Entity.fetch "camera_view"
//     let e2 = Entity.fetch "nodes_count"
    
//     let cp = camera.Position
//     let cv = camera.GetView()
//     Texture.update (textures[e0]) $"pos:{cp.X:F3},{cp.Y:F3},{cp.Z:F3}"
//     Texture.update (textures[e1]) $"max_iter:{max_iter}"
//     Texture.update (textures[e2]) $"count:{tree_len}"
// )

system PreRender [typeof<Mesh>] (fun q ->
    for e in q do
        if Entity.has<UpdateColors> e then
            let mesh = Entity.get<Mesh> e
            let vbuf = Entity.get<VertexBuffer> e
            octree_to_buffer tree colorbar mesh
            VertexBuffer.update vbuf mesh
            
            e |> Entity.remove<UpdateColors> |> ignore
)

system OnRender [typeof<Mesh>; typeof<VertexBuffer>; typeof<IsPoints>] (fun q ->
    let m = Components.get<Mesh>()
    let t = Components.get<Matrix4>()
    let v = Components.get<VertexBuffer>()

    let camera = SE_Window.Shared.Camera
    let shader = Shaders.get("p_shader")

    shader.Use()
    shader.SetMatrix4("view", camera.GetViewMatrix())
    shader.SetMatrix4("projection", camera.GetProjectionMatrix())

    for e in q do
        shader.SetMatrix4("model", t[e])
        VertexBuffer.draw v[e] m[e]
)

system OnRender [typeof<Texture>] (fun q ->
    let t = Components.get<Texture>()
    let shader = Shaders.get("t_shader")

    for e in t.Entities do
        Texture.draw t[e] shader
)

// system PostUpdate [typeof<IsPoints>] (fun q ->
//     for e in q do
//         e |> Entity.add<UpdateColors> |> ignore
// )

// trigger update on VB after n-time internal
// observer OnAdd [typeof<UpdateColors>] (fun q ->
//     for e in q do
//         let mesh = e |> Entity.get<Mesh>
//         let vbuf = e |> Entity.get<VertexBuffer> 
//         octree_to_buffer tree colorbar mesh
//         VertexBuffer.update vbuf mesh

//         e |> Entity.remove<UpdateColors> |> ignore
// )


let mutable capture_countdown = 20

system PostRender [] (fun _ ->
    if SE_Window.Shared.IsRecording then
        VideoCapture.capture_frame frames SE_Window.Shared
)

system OnExit [] (fun _ ->
    VideoCapture.create_video_from_frames ".gif" frames SE_Window.Shared
)

Systems.progress()

