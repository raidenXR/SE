// #r "../bin/Debug/net10.0/SE-renderer.dll"
// #r "../bin/Debug/net10.0/SE-core.dll"
#r "../bin/Release/net10.0/SE-renderer.dll"
#r "../bin/Release/net10.0/SE-core.dll"
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

let [<Literal>] N = 300
let [<Literal>] L = 10
let [<Literal>] k = 4
let [<Literal>] max_iter = 300

type IsPoints = struct end

let path = System.Environment.GetCommandLineArgs()[2]
let gltf = if path.Contains(".gltf") then Some (new GLTF.Deserializer(path)) else None

// rotate mesh for testing
let rotation =
    System.Numerics.Quaternion.CreateFromYawPitchRoll(2.f, 4.f, 3.f)
    |> System.Numerics.Matrix4x4.CreateFromQuaternion

let mesh =
    match gltf with
    | _ when path.Contains(".txt") ->
        RGeometry.load_txt_unmanaged (path, 0.55f, 0.55f, 0.55f, 1.0f)
        |> RGeometry.tranform rotation
        
    | Some gltf ->
        gltf.ReadMeshF(0)
        |> RGeometry.tranform rotation
        
    | None ->
        RGeometry.load_ply_unmanaged (path, 0.55f, 0.55f, 0.53f, 1.0f)
        |> RGeometry.tranform rotation

let tree =
    Octree.ofSurface<Entity> N L k (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())

// clear on exit
system OnExit [] (fun _ ->
    if gltf.IsSome then gltf.Value.Dispose()

    mesh.vertices.Dispose()
    mesh.indices.Dispose()
)

// clear all resources
system OnExit [] (fun _ ->
    for vb in Components.get<VertexBuffer>().Entries do
        match vb with
        | VB1(vao,vbo,ebo) ->
            GL.DeleteVertexArray(vao)
            GL.DeleteBuffer(vbo)
            GL.DeleteBuffer(ebo)            
        | VB2(vao,vbo) ->
            GL.DeleteVertexArray(vao)
            GL.DeleteBuffer(vbo)

    for mesh in Components.get<MeshF>().Entries do
        mesh.vertices.Dispose()
        mesh.indices.Dispose()

    Shaders.unload()
    SE_Window.Shared.Dispose()
)


// load window
system OnLoad [] (fun _ ->
    let p = Octree.center (tree.Root)
    
    let wnd = SE_Window.Shared
    wnd.Camera.Position <- Vector3(p.X, p.Y, p.Z)
    wnd.Camera.Speed <- 2.f
    wnd.CursorState <- CursorState.Grabbed

    wnd.Load()

    Shaders.load [
        "model_shader", "../../../resources/shaders/shader.vert", "../../../resources/shaders/shader.frag"
        "particles_shader", "../../../resources/shaders/particles.vert", "../../../resources/shaders/particles.frag"
    ]

    GL.ClearColor(0.2f, 0.2f, 0.2f, 1.0f)
    GL.Enable(EnableCap.DepthTest)
    GL.Enable(EnableCap.ProgramPointSize)

    printfn "resouces initialization load"
)

system OnLoad [] (fun _ ->
    let len = tree.GetCount()
    let L = 7
    let mutable vertices = NativeArray.create<float32> (len*L)
    let indices  = NativeArray.empty<uint32>()
    let mesh = {vertices = vertices; indices = indices; L = L}

    let mutable i = 0

    tree.Iter (fun u ->
        match u with
        | Octree.Internal ->
            let p = Octree.center u
            let c = Vector4(0.5f, 0.5f, 0.6f, 1.0f)
            vertices[i+0] <- p.X
            vertices[i+1] <- p.Y
            vertices[i+2] <- p.Z
            vertices[i+3] <- c.X
            vertices[i+4] <- c.Y
            vertices[i+5] <- c.Z
            vertices[i+6] <- c.W
            i <- i + L
            
        | Octree.Boundary ->
            let p = Octree.center u
            let c = Vector4(0.6f, 0.4f, 0.7f, 1.0f)
            vertices[i+0] <- p.X
            vertices[i+1] <- p.Y
            vertices[i+2] <- p.Z
            vertices[i+3] <- c.X
            vertices[i+4] <- c.Y
            vertices[i+5] <- c.Z
            vertices[i+6] <- c.W
            i <- i + L

        | Octree.External -> ()            
    )

    let e0 =
        entity()
        |> Entity.add<IsPoints>
        |> set mesh
        |> set (VertexBuffer.create VT2 mesh)
        |> set (Matrix4.CreateScale(10.f))

    let e_tree = entity()

    relate e0 e_tree 0
    
    printfn "vertex_buffer initialization load, count: %d" len
)

system PreRender [] (fun _ ->
    SE_Window.Shared.Update (fun _ -> GL.Clear(ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit))
)
    
system PostRender [] (fun _ ->
    SE_Window.Shared.Update (SE_Window.Shared.Context.SwapBuffers)        
)

system OnRender [typeof<MeshF>; typeof<VertexBuffer>; typeof<IsPoints>] (fun q ->
    let m = Components.get<MeshF>()
    let t = Components.get<Matrix4>()
    let v = Components.get<VertexBuffer>()

    let camera = SE_Window.Shared.Camera
    let shader = Shaders.get("particles_shader")

    shader.Use()
    shader.SetMatrix4("view", camera.GetViewMatrix())
    shader.SetMatrix4("projection", camera.GetProjectionMatrix())

    for e in q do
        shader.SetMatrix4("model", t[e])
        VertexBuffer.draw v[e] m[e]
)

system OnUpdate [] (fun _ ->
    let wnd = SE_Window.Shared
    let input = wnd.KeyboardState
    if input.IsKeyDown(Keys.Escape) then wnd.Close()
    if input.IsKeyDown(Keys.Escape) then Systems.quit()
)

Systems.progress()

