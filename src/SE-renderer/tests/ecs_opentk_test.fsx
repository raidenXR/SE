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

let [<Literal>] N = 200
let [<Literal>] L = 10
let [<Literal>] k = 2
let [<Literal>] max_iter = 50
let [<Literal>] ss = "../../../resources/shaders/"

type IsPoints = struct end
type UpdateColors = struct end
type IsTexture = struct end

let mutable time_elapsed = 0.

type [<Struct>] Temperature   = Temperature of float
type [<Struct>] Thickness     = Thickness of float
type [<Struct>] Concentration = Concentration of float
type [<Struct>] PDFsolve = PDFsolve of bool

let mutable solver_update = true
let mutable solver_update_prev = false
let mutable pause = true
let mutable pause_prev = false

// prefab example
// let is_some_prefab = prefab4 10uy 20. 90 5.f
let is_electrolyte_cv = prefab2 (Temperature 300.) (Concentration 3.2)
let is_electrode_cv = prefab2 (Temperature 600.) (Thickness (3.2e-6))

let frames = ResizeArray<IVideoFrame>(1000)

let inline (!) (u:Octree.Node<'T>) = Octree.valueof u
let inline Tf32 (Temperature T) = T
let pos = Octree.center

let octree_to_buffer (tree:Octree.Root<Entity>) (colorbar:Colorbar) (mesh:Mesh) =
    let T = Components.get<Temperature>()
    tree.Iteri (fun i u ->
        match u with
        | Octree.Internal | Octree.Boundary ->
            let vertices = mesh.vertices.AsSpan()
            if (i*mesh.L+6) >= vertices.Length then printfn "i: %d, tree_len: %d" i (tree.GetCount())
            let p = Octree.center u
            let c = colorbar[Tf32 T[!u]]
            vertices[i*mesh.L + 0] <- p.X
            vertices[i*mesh.L + 1] <- p.Y
            vertices[i*mesh.L + 2] <- p.Z
            vertices[i*mesh.L + 3] <- c.X
            vertices[i*mesh.L + 4] <- c.Y
            vertices[i*mesh.L + 5] <- c.Z
            vertices[i*mesh.L + 6] <- c.W
            
        | Octree.External -> ()
    )


// rotate mesh for testing
let rotation =
    System.Numerics.Quaternion.CreateFromYawPitchRoll(2.f, 4.f, 3.f)
    |> System.Numerics.Matrix4x4.CreateFromQuaternion

let tree =
    System.Environment.GetCommandLineArgs()[2]
    |> RGeometry.load_model
    |> Octree.ofMesh<Entity> N k

let tree' = tree.Copy()

    
let colorbar = new Colorbar(Colormap.Jet, 280., 620.)
let tree_len = tree.GetCount()


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

system PostRender [] (fun _ ->
    let wnd = SE_Window.Shared
    let input = wnd.KeyboardState
    if input.IsKeyDown(Keys.Escape) then wnd.Close()
    if input.IsKeyDown(Keys.Escape) then Systems.quit()
    if input.IsKeyDown(Keys.P) && not pause_prev then
        pause_prev <- true
        pause <- not pause
        match pause with
        | true -> Systems.pause()
        | false -> Systems.unpause()
    elif not (input.IsKeyDown(Keys.P)) then
        pause_prev <- false
    if input.IsKeyDown(Keys.B) && not solver_update_prev then
        solver_update_prev <- true
        solver_update <- not solver_update
    elif not (input.IsKeyDown(Keys.B)) then
        solver_update_prev <- false
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

    printfn "resouces initialization load"
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
    |> set (Matrix4.CreateScale(10.f))
    |> set (colorbar.AsTexture(150.f, 600.f))
    |> ignore
    
    printfn "vertex_buffer initialization load, count: %d" tree_len
)

// run this in parallel and update the vertex_buffer, once the mesh is ready
system PostUpdate [] (fun _ ->
    "vertex_buffer"
    |> Entity.fetch 
    |> Entity.get<Mesh>
    |> octree_to_buffer tree colorbar
    |> ignore
)

let mutable n = 0
system PostUpdate [] (fun _ ->
    n <- n + 1
    if n >= max_iter then
        Systems.pause()
        n <- 0
        printfn "n_max_iter --paused"
)

// run this in parallel and update the vertex_buffer, once the mesh is ready
// or use SE_WINDOW dedicated thread for rendering, And all the rendering functions...
system OnUpdate [] (fun _ ->
    if solver_update then 
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
                // T[tree'[pos u].Value] <- Temperature(
                    // Random.Shared.NextDouble() * 300. + 300.
                // )
            | _ -> ()
        )
)

system PostUpdate [] (fun _ ->
    let T = Components.get<Temperature>()    
    
    tree'.IterParallel 4 (fun u ->
        T[tree[pos u].Value] <- T[!u]    
    )
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

system PostUpdate [] (fun _ ->
    if solver_update then
        time_elapsed <- time_elapsed + 1.
        if time_elapsed > 1. then
            printfn "time elapsed, should trigger observer"
            time_elapsed <- time_elapsed - 1.

            Entity.fetch "vertex_buffer" |> Entity.add<UpdateColors> |> ignore        
)

// trigger update on VB after n-time internal
observer OnAdd [typeof<UpdateColors>] (fun q ->
    if solver_update then
        printfn "running oberser on add"

        for e in q do
            let mesh = e |> Entity.get<Mesh>
            let vbuf = e |> Entity.get<VertexBuffer> 
            octree_to_buffer tree colorbar mesh
            VertexBuffer.update vbuf mesh
        
            e |> Entity.remove<UpdateColors> |> ignore
)


let mutable capture_countdown = 20

system PostRender [] (fun _ ->
    let wnd = SE_Window.Shared
    if wnd.IsRecording then
        capture_countdown <- capture_countdown - 1
        // if capture_countdown <= 0 then
        VideoCapture.capture_frame frames wnd
        capture_countdown <- 20
)

system OnExit [] (fun _ ->
    VideoCapture.create_video_from_frames "capture.mp4" frames (SE_Window.Shared)
)

Systems.progress()

