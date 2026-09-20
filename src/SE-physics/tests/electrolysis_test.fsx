#r "../bin/Release/net10.0/SE-renderer.dll"
#r "../bin/Release/net10.0/SE-core.dll"
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

let [<Literal>] N = 130
let [<Literal>] L = 10
let [<Literal>] k = 2
let [<Literal>] ss = "../../../resources/shaders/"

// type [<Struct>] Enable = {is_enabled:bool}
type Enable = bool
type [<Struct>] Temperature = Temperature of float
type UpdateColors = struct end
type IsPoints = struct end

let sw =
    System.Diagnostics.Stopwatch()

let frames =
    ResizeArray<IVideoFrame>(1000)

let path =
    "../../../resources/models/cell.gltf"
    // System.Environment.GetCommandLineArgs()[2]

    
let rotation =
    match path with
    | GLTF.IsTxt -> System.Numerics.Quaternion.CreateFromYawPitchRoll(2.f, 2.f, 1.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
    | GLTF.IsPly -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.f, 0.f, 0.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
    | GLTF.IsGltf -> System.Numerics.Quaternion.CreateFromYawPitchRoll(1.f, 2.f, 2.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
    | _ -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.f, 0.f, 0.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        

let scale =
    match path with
    | GLTF.IsTxt -> 0.2f
    | GLTF.IsPly -> 10.f
    | GLTF.IsGltf -> 5.f
    | _ -> 1.f
      

let trees = new System.Collections.Generic.Dictionary<Entity,Octree.Root<Entity>>()
let mesh_names = ResizeArray<string>()
let ent_to_tree tree e = trees.Add(e,tree)

let colorbars = Map[
    Colormap.Hot, new Colorbar(Colormap.Hot, 0., 100.)
    Colormap.Jet, new Colorbar(Colormap.Jet, 0., 100.)
    Colormap.Winter, new Colorbar(Colormap.Winter, 0., 100.)
    Colormap.Gray, new Colorbar(Colormap.Gray, 0., 100.)
    Colormap.Autumn, new Colorbar(Colormap.Autumn, 0., 100.)
]

let keys = Array.create 512 false

let inline (!) (u:Octree.Node<'T>) = Octree.valueof u
let inline Tf32 (Temperature T) = T
let inline vec3 (v:Vector3) = System.Numerics.Vector3(v.X, v.Y, v.Z)
let pos = Octree.center

let octree_to_buffer<'T> (tree:Octree.Root<Entity>) (colorbar:Colorbar) (mesh:Mesh) (values:Components<'T>) convert =
    let mutable i = 0
    tree.Iter (fun u ->
        match u with
        | Octree.Internal | Octree.Boundary ->
        // | Octree.Internal ->
            let vertices = mesh.vertices.AsSpan()
            // if (i*mesh.L+6) >= vertices.Length then printfn "i: %d, tree_len: %d" i (tree.GetInternalCount())
            let p = pos u
            let c = colorbar[convert values[!u]]
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

SE_UI.Shared.OnRender (fun _ ->
    let camera = SE_Window.Shared.Camera
    let mutable p = vec3(camera.Position)
    let mutable v = vec3(camera.GetView())
    let viewport_size = ImGui.GetMainViewport().Size

    // Position at left edge
    ImGui.SetNextWindowPos(System.Numerics.Vector2(0.f, 0.f))
    ImGui.SetNextWindowSize(System.Numerics.Vector2(280.f, 260.f))

    ImGui.Begin("Panel") |> ignore
    ImGui.SetWindowFontScale(1.2f)
    ImGui.InputFloat3("pos:  ", &p) |> ignore
    ImGui.InputFloat3("view: ", &v) |> ignore
    
    let mesh = Components.get<Enable>().Entries
    for i in 0..mesh_names.Count-1 do
        ImGui.Checkbox(mesh_names[i], &mesh[i]) |> ignore
    ImGui.End()
)

// load resources
system OnLoad [] (fun _ ->
    let wnd = SE_Window.Shared
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

system OnLoad [] (fun _ ->
    sw.Start()
    use gltf = new GLTF.Deserializer(path)
    let meshes = gltf.ReadMeshes()
    let wnd = SE_Window.Shared
    sw.Stop()
    printfn "read_meshes: %d s" (sw.Elapsed.Seconds)
    sw.Reset()

    sw.Start()
    let mutable i = 0
    for mesh in meshes do
        let tree =
            mesh
            |> RGeometry.tranform rotation
            |> Octree.ofMesh<Entity> N k

        i <- i + 1
        mesh_names.Add(sprintf "body_%d: %d/%d" i (tree.GetInternalCount()) (tree.GetCount()))
        
        // assign entities to leafs
        tree.Iter (fun u ->
            match u with
            | Octree.Internal & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> set (Temperature(40.)) |> ValueSome 
            | Octree.Boundary & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> set (Temperature(80.)) |> ValueSome 
            | _ -> ()
        )

        let points = {
            vertices = NativeArray.create<float32>(tree.GetCount()*7)
            indices = NativeArray.empty<uint32>()
            L = 7
        }
        
        let T = Components.get<Temperature>()
        octree_to_buffer<Temperature> tree colorbars[Colormap.Gray] points T Tf32

        // assign entities to trees
        entity()
        |> Entity.add<IsPoints>
        |> Entity.add<UpdateColors>
        |> set points
        |> set (VertexBuffer.create VT2 points)
        |> set (Matrix4.CreateScale(scale))
        |> set true
        |> set Colormap.Jet
        |> ent_to_tree tree

        let p = Octree.center (tree.Root)    
        wnd.Camera.Position <- Vector3(p.X, p.Y, p.Z)
        

    let entities = Components.get<IsPoints>().Entities
    entities[2] |> set (colorbars[Colormap.Jet].AsTexture(0.8f, 0.0f, 120.f, 460.f)) |> ignore
    entities[3] |> set (colorbars[Colormap.Gray].AsTexture(0.8f, -0.8f, 120.f, 460.f)) |> ignore

    sw.Stop()
    printfn "load_trees: %d s" (sw.Elapsed.Seconds)
    sw.Reset()
)

system PostLoad [] (fun _ ->
    let P = Components.get<Colormap>().Entities
    P[0] |> set Colormap.Winter |> ignore
    P[1] |> set Colormap.Jet |> ignore
    P[2] |> set Colormap.Jet |> ignore
    P[3] |> set Colormap.Gray |> ignore
    P[4] |> set Colormap.Winter |> ignore
    P[5] |> set Colormap.Hot |> ignore
)
    
    
let inline pressed key (input:KeyboardState) = input.IsKeyDown(key) && not keys[int key]

// controls
system OnValidate [] (fun _ ->
    let wnd = SE_Window.Shared
    let input = wnd.KeyboardState

    if pressed Keys.Escape input then
        wnd.Close()
        Systems.quit()

    if pressed Keys.P input then
        sw.Start()
        Systems.unpause()
        wnd.IsRecording <- true

    let mesh = Components.get<Enable>().Entries
    mesh[0] <- if pressed Keys.D1 input then not mesh[0] else mesh[0]
    mesh[1] <- if pressed Keys.D2 input then not mesh[1] else mesh[1]
    mesh[2] <- if pressed Keys.D3 input then not mesh[2] else mesh[2]
    mesh[3] <- if pressed Keys.D4 input then not mesh[3] else mesh[3]
    mesh[4] <- if pressed Keys.D5 input then not mesh[4] else mesh[4]
    mesh[5] <- if pressed Keys.D6 input then not mesh[5] else mesh[5]

    keys[int Keys.Escape] <- input.IsKeyDown(Keys.Escape)
    keys[int Keys.P] <- input.IsKeyDown(Keys.P)
    keys[int Keys.D1] <- input.IsKeyDown(Keys.D1)
    keys[int Keys.D2] <- input.IsKeyDown(Keys.D2)
    keys[int Keys.D3] <- input.IsKeyDown(Keys.D3)
    keys[int Keys.D4] <- input.IsKeyDown(Keys.D4)
    keys[int Keys.D5] <- input.IsKeyDown(Keys.D5)
    keys[int Keys.D6] <- input.IsKeyDown(Keys.D6)
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
    SE_Window.Shared.Dispose()
    SE_UI.Shared.OnClosed()
    for pair in colorbars do
        pair.Value.Dispose()
    // VideoCapture.create_video_from_frames ".gif" frames SE_Window.Shared

    sw.Stop()
    printfn "stopwatch: %d s" (sw.Elapsed.Seconds)
)


system PreRender [typeof<Mesh>; typeof<VertexBuffer>; typeof<Enable>] (fun q ->
    let mesh = Components.get<Mesh>()
    let vbuf = Components.get<VertexBuffer>()
    let cbar = Components.get<Colormap>()
    let enabled = Components.get<Enable>()
    
    let T = Components.get<Temperature>()    

    for e in q do
        if Entity.has<UpdateColors> e && enabled[e] then
            octree_to_buffer trees[e] colorbars[cbar[e]] mesh[e] T Tf32

            VertexBuffer.update vbuf[e] mesh[e]        
            e |> Entity.remove<UpdateColors> |> ignore        
)

system OnRender [] (fun _ ->
    let wnd = SE_Window.Shared
    wnd.Update (fun _ -> GL.Clear(ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit))
    SE_UI.Shared.OnRenderFrame(SE_Window.Shared)
)

system OnRender [typeof<Texture>] (fun q ->
    let t = Components.get<Texture>()
    let shader = Shaders.get("t_shader")

    for e in q do
        Texture.draw t[e] shader
)

system OnRender [typeof<Mesh>; typeof<VertexBuffer>; typeof<IsPoints>] (fun q ->
    let m = Components.get<Mesh>()
    let t = Components.get<Matrix4>()
    let v = Components.get<VertexBuffer>()

    let camera = SE_Window.Shared.Camera
    let shader = Shaders.get("p_shader")
    let enable = Components.get<Enable>()

    shader.Use()
    shader.SetMatrix4("view", camera.GetViewMatrix())
    shader.SetMatrix4("projection", camera.GetProjectionMatrix())

    for e in q do
        if enable[e] then
            shader.SetMatrix4("model", t[e])
            VertexBuffer.draw v[e] m[e]
)

system PostRender [] (fun _ ->
    let wnd = SE_Window.Shared
    wnd.Update (wnd.Context.SwapBuffers)        
)

Systems.progress()


