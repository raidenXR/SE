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
open SE.Renderer.VideoCapture

open SE
open SE.Core
open SE.ECS
open SE.Spatial
open SE.Renderer

let [<Literal>] N = 100
let [<Literal>] L = 10
let [<Literal>] k = 3
let [<Literal>] max_iter = 300
let [<Literal>] ss = "../../../resources/shaders/"

type IsPoints = struct end
type UpdateColors = struct end
type IsTexture = struct end

let mutable time_elapsed = 0.

// prefab example
// let is_some_prefab = prefab4 10uy 20. 90 5.f

let frames = ResizeArray<IVideoFrame>(1000)


// rotate mesh for testing
let rotation =
    System.Numerics.Quaternion.CreateFromYawPitchRoll(2.f, 4.f, 3.f)
    |> System.Numerics.Matrix4x4.CreateFromQuaternion

let tree =
    System.Environment.GetCommandLineArgs()[2]
    |> RGeometry.load_model
    |> Octree.ofMesh<Entity> N k
    
let colorbar = new Colorbar(Colormap.Jet, 0., 100.)
let tree_len = tree.GetCount()

let mutable particles_ent = 0u


// clear on exit
system OnExit [] (fun _ ->
    SE_Window.Shared.ExportVideo()
    SE_Window.Shared.Dispose()
    colorbar.Dispose()
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

    for mesh in Components.get<Mesh>().Entries do
        mesh.Dispose()

    for TXT1(vao,vbo,txt) in Components.get<Texture>().Entries do
        GL.DeleteVertexArray(vao)
        GL.DeleteBuffer(vbo)
        GL.DeleteTexture(txt)

    Shaders.unload()
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

    printfn "resouces initialization load"
)

system OnLoad [] (fun _ ->
    let L = 7
    // let particles = particles_array |> NativeArray.ofArray
    let vertices = Array.zeroCreate<float32> (tree_len*L)
    // let mutable vertices = NativeArray.create<float32> (tree_len*L)
    let indices  = NativeArray.empty<uint32>()

    let mutable i = 0
    // let c1 = Vector4(0.55f, 0.55f, 0.55f, 1.0f)
    // let c2 = Vector4(0.25f, 0.25f, 0.25f, 1.0f)

    tree.Iter (fun u ->
        match u with
        | Octree.Internal -> 
            let pos = Octree.center u
            let c1 = colorbar[float32(Random.Shared.Next(0,100))]
            vertices[i+0] <- pos.X
            vertices[i+1] <- pos.Y
            vertices[i+2] <- pos.Z
            vertices[i+3] <- c1.X
            vertices[i+4] <- c1.Y
            vertices[i+5] <- c1.Z
            vertices[i+6] <- 1.f
            i <- i + 7

        | Octree.Boundary ->
            let pos = Octree.center u
            let c2 = colorbar[float32(Random.Shared.Next(0,100))]
            vertices[i+0] <- pos.X
            vertices[i+1] <- pos.Y
            vertices[i+2] <- pos.Z
            vertices[i+3] <- c2.X
            vertices[i+4] <- c2.Y
            vertices[i+5] <- c2.Z
            vertices[i+6] <- 1.f
            i <- i + 7

        | Octree.External -> ()
    )

    let mesh = {vertices = (NativeArray.ofArray vertices); indices = indices; L = L}
    
    particles_ent <-
        entity()
        |> Entity.add<IsPoints>
        |> set mesh
        |> set (VertexBuffer.create VT2 mesh)
        |> set (Matrix4.CreateScale(10.f))

    // let e_tree = entity()

    // relate e0 e_tree 0
    
    printfn "vertex_buffer initialization load, count: %d" tree_len

    let e1 =
        entity()
        |> set (colorbar.AsTexture(150.f, 600.f))

    ()
)

system PreRender [] (fun _ ->
    SE_Window.Shared.Update (fun _ -> GL.Clear(ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit))
)
    
system PostRender [] (fun _ ->
    SE_Window.Shared.Update (SE_Window.Shared.Context.SwapBuffers)        
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

    for e in q do
        Texture.draw t[e] shader
)

system OnUpdate [] (fun _ ->
    let wnd = SE_Window.Shared
    let input = wnd.KeyboardState
    if input.IsKeyDown(Keys.Escape) then wnd.Close()
    if input.IsKeyDown(Keys.Escape) then Systems.quit()
)

system OnUpdate [] (fun _ ->
    time_elapsed <- time_elapsed + SE_Window.Shared.ElapsedTime
    if time_elapsed > 1. then
        printfn "time elapsed, should trigger observer"
        time_elapsed <- time_elapsed - 1.

        particles_ent |> Entity.add<UpdateColors> |> ignore        
)

// trigger update on VB after n-time internal
observer OnAdd [typeof<UpdateColors>] (fun q ->
    let vbs = Components.get<VertexBuffer>()
    let mesh = Components.get<Mesh>()[particles_ent]
    // match vbs[particles_ent] with
    // | VB1(vao,vbo,ebo) ->
    //     GL.DeleteVertexArray(vao)
    //     GL.DeleteBuffer(vbo)
    //     GL.DeleteBuffer(ebo)            
    // | VB2(vao,vbo) ->
    //     GL.DeleteVertexArray(vao)
    //     GL.DeleteBuffer(vbo)
        
    
    let vertices = mesh.vertices.AsSpan()
    let L = 7
    let mutable i = 0

    printfn "running oberser on add"
    
    let c1 = colorbar[float32(Random.Shared.Next(10,90))]
    let c2 = colorbar[float32(Random.Shared.Next(10,90))]


    let c = colorbar[float32(Random.Shared.Next(0,100))]
    // for i in 0..tree_len-1 do
    //     vertices[i*L+3] <- c.X
    //     vertices[i*L+4] <- c.Y
    //     vertices[i*L+5] <- c.Z
        
    
    tree.Iter (fun u ->
        let vertices = mesh.vertices.AsSpan()
        match u with
        | Octree.Internal ->
            let p = Octree.center u
            vertices[i+0] <- p.X
            vertices[i+1] <- p.Y
            vertices[i+2] <- p.Z
            vertices[i+3] <- c1.X
            vertices[i+4] <- c1.Y
            vertices[i+5] <- c1.Z
            vertices[i+6] <- c1.W
            i <- i + L
            
        | Octree.Boundary ->
            let p = Octree.center u
            vertices[i+0] <- p.X
            vertices[i+1] <- p.Y
            vertices[i+2] <- p.Z
            vertices[i+3] <- c2.X
            vertices[i+4] <- c2.Y
            vertices[i+5] <- c2.Z
            vertices[i+6] <- c2.W
            i <- i + L

        | Octree.External -> ()            
    )
    
    VertexBuffer.update vbs[particles_ent] mesh
    // vbs[particles_ent] <- VertexBuffer.create VT2 mesh

    for e in q do
        e |> Entity.remove<UpdateColors> |> ignore
)


let mutable capture_countdown = 20

system OnUpdate [] (fun _ ->
    let wnd = SE_Window.Shared
    if wnd.IsRecording then
        capture_countdown <- capture_countdown - 1
        if capture_countdown <= 0 then
            VideoCapture.capture_frame frames wnd
            capture_countdown <- 20
)

system OnExit [] (fun _ ->
    VideoCapture.create_video_from_frames "capture.mp4" frames (SE_Window.Shared)
)

Systems.progress()

