// #r "../bin/Debug/net10.0/SE-renderer.dll"
// #r "../bin/Debug/net10.0/SE-core.dll"
#r "../bin/Release/net10.0/SE-renderer.dll"
#r "../bin/Release/net10.0/SE-core.dll"
#r "nuget: OpenTK, 4.9.4"
#r "nuget: SkiaSharp, 2.88.6"
#r "nuget: ImGui.NET, 1.91.6.1"
#r "nuget: FFMPegCore, 5.4.0"

#load "./ecs_opentkx_fns.fsx"

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

open Ecs_opentkx_fns

let [<Literal>] N = 260
let [<Literal>] L = 10
let [<Literal>] k = 3
let [<Literal>] ss = "../../../resources/shaders/"

let mutable time_elapsed = 0.
let mutable color_edit = System.Numerics.Vector3.Zero
let mutable solver_update = true
let mutable solver_update_prev = false
let mutable pause = true
let mutable pause_prev = false
let mutable update_bool = true

let sw =
    System.Diagnostics.Stopwatch()

let frames =
    ResizeArray<IVideoFrame>(1000)

let path =
    System.Environment.GetCommandLineArgs()[2]

do
    sw.Start()
    
let rotation =
    match path with
    | GLTF.IsTxt -> System.Numerics.Quaternion.CreateFromYawPitchRoll(2.f, 2.f, 1.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
    | GLTF.IsPly -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.f, 0.f, 0.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
    | GLTF.IsGltf -> System.Numerics.Quaternion.CreateFromYawPitchRoll(2.f, 4.f, 3.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
    | _ -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.f, 0.f, 0.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        

let scale =
    match path with
    | GLTF.IsTxt -> 0.2f
    | GLTF.IsPly -> 10.f
    | GLTF.IsGltf -> 5.f
    | _ -> 1.f
      
let tree =
    path
    |> RGeometry.load_model
    |> RGeometry.tranform rotation
    |> Octree.ofMesh<Entity> N k

let tree' = tree.Copy()
let colorbar_jet  = new Colorbar(Colormap.Jet, 280., 620.)
let colorbar_gray = new Colorbar(Colormap.Gray, 280., 620.)
let models_count = ResizeArray<int*int>()

    
let max_iter = max 50 (tree.GetInternalCount() / 1000)
let mutable n = 0

do
    sw.Stop()
    printfn "stopwatch: %d s" (sw.Elapsed.Seconds)
    sw.Reset()

// -----------------------
// SYSTEMS
// -----------------------

// UI
SE_UI.Shared.OnRender (fun _ ->
    let camera = SE_Window.Shared.Camera
    let mutable p = vec3(camera.Position)
    let mutable v = vec3(camera.GetView())

    let viewport_size = ImGui.GetMainViewport().Size

    // Position at left edge
    ImGui.SetNextWindowPos(System.Numerics.Vector2(0.f, 0.f))
    ImGui.SetNextWindowSize(System.Numerics.Vector2(280.f, 160.f))

    ImGui.Begin("Panel") |> ignore
    ImGui.SetWindowFontScale(1.2f)
    // ImGui.Text(
    //     match Systems.IsPaused with
    //     | true -> "paused"
    //     | false -> "solving"
    // )
    ImGui.Text($"iter: {n}/{max_iter}")
    // ImGui.InputFloat3("pos:  ", &p) |> ignore
    // ImGui.InputFloat3("view: ", &v) |> ignore
    for i in 0..models_count.Count-1 do
        ImGui.Text($"octree[{i}]: {models_count[i]}")
    ImGui.End()
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
    colorbar_jet.Dispose()
    colorbar_gray.Dispose()
    VideoCapture.create_video_from_frames ".gif" frames SE_Window.Shared

    sw.Stop()
    printfn "stopwatch: %d s" (sw.Elapsed.Seconds)
)

// initialize octree entities
system OnLoad [] (fun _ ->
    sw.Start()
    let L = 7
    let tree_len = tree.GetInternalCount()
    let mutable vertices = NativeArray.create<float32> (tree_len*L)
    let indices  = NativeArray.empty<uint32>()

    let mutable i = 0

    load_resources tree ss

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
    octree_to_buffer tree colorbar_jet mesh
    
    entity()
    |> Entity.singleton "vertex_buffer"
    |> Entity.add<IsPoints>
    |> set mesh
    |> set (VertexBuffer.create VT2 mesh)
    |> set (Matrix4.CreateScale(scale))
    |> set (colorbar_jet.AsTexture(0.8f, 0.0f, 120.f, 460.f))
    |> ignore
    
    printfn "vertex_buffer initialization load, count: %d" tree_len

    models_count.Add(tree_len, tree.GetCount())

    load_multiple_meshes N k colorbar_jet scale models_count
)

system PostLoad [] (fun _ ->
    Systems.pause()
    sw.Stop()
    printfn "stopwatch: %d s" (sw.Elapsed.Seconds)
    sw.Reset()
)

system PreUpdate [] (fun _ ->
    if n >= max_iter then
        printfn "n_max_iter --paused"
        Systems.quit()
)

// run this in parallel and update the vertex_buffer, once the mesh is ready
// or use SE_WINDOW dedicated thread for rendering, And all the rendering functions...
system OnUpdate [] (fun _ ->
    if update_bool then
        update_bool <- false
        task_new (fun _ ->
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
            // |> ignore      
            |> Entity.get<Mesh>
            |> octree_to_buffer tree colorbar_jet
            
            update_bool <- true
        ) |> ignore
)

system PreRender [typeof<Mesh>;typeof<VertexBuffer>] (fun q ->
    let mesh = Components.get<Mesh>()
    let vbuf = Components.get<VertexBuffer>()
    
    for e in q do
        if Entity.has<UpdateColors> e then
        // if e.Has<UpdateColors>() then
            VertexBuffer.update vbuf[e] mesh[e]
        
            e |> Entity.remove<UpdateColors> |> ignore        
)

// system PreRender [typeof<Mesh>] (fun q ->
//     update_colors tree colorbar_jet q
// )

system OnRender [] (fun _ ->
    let wnd = SE_Window.Shared
    wnd.Update (fun _ -> GL.Clear(ClearBufferMask.ColorBufferBit ||| ClearBufferMask.DepthBufferBit))
    SE_UI.Shared.OnRenderFrame(SE_Window.Shared)
)
system OnRender [typeof<Mesh>; typeof<VertexBuffer>; typeof<IsPoints>] draw_meshes

system OnRender [typeof<Texture>] draw_textures

system PostRender [] (fun _ ->
    let wnd = SE_Window.Shared
    wnd.Update (wnd.Context.SwapBuffers)        
)

system OnValidate [] (fun _ ->
    if SE_Window.Shared.IsRecording then
        if update_bool then
            VideoCapture.capture_frame frames SE_Window.Shared
            n <- n + 1
)

system OnValidate [] (fun _ ->
    let wnd = SE_Window.Shared
    let input = wnd.KeyboardState
    if input.IsKeyDown(Keys.Escape) then wnd.Close()
    if input.IsKeyDown(Keys.Escape) then Systems.quit()
    if input.IsKeyDown(Keys.P) && not pause_prev then
        sw.Start()
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

Systems.progress()

