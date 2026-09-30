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

open SE
open SE.Core
open SE.ECS
open SE.Spatial
open SE.Renderer

let [<Literal>] N = 430
let [<Literal>] L = 10
let [<Literal>] k = 2
let [<Literal>] ss = "../../../resources/shaders/"

// type [<Struct>] Enable = {is_enabled:bool}
type Enable = bool
type [<Struct>] Temperature = Temperature of float
type UpdateColors = struct end
type IsPoints = struct end

// type [<Struct>] UpdateCount = UpdateCount of int
// type [<Struct>] UpdateBool  = UpdateBool of bool

let mutable dtime = DateTime.Now
let dt_reset () =
    dtime <- DateTime.Now

let dt_print () =
    let t = DateTime.Now
    printfn "%d ms" (t - dtime).Milliseconds
    dtime <- t

let sw =
    System.Diagnostics.Stopwatch()

let path =
    "../../../resources/models/cell.gltf"

let mutable update_count = 0
let mutable update_bool = false
    
let rotation =
    match path with
    | GLTF.IsTxt -> System.Numerics.Quaternion.CreateFromYawPitchRoll(2.f, 2.f, 1.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
    | GLTF.IsPly -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.f, 0.f, 0.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
    | GLTF.IsGltf -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.1f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
    | _ -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.f, 0.f, 0.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        

let scale =
    match path with
    | GLTF.IsTxt -> 0.2f
    | GLTF.IsPly -> 10.f
    | GLTF.IsGltf -> 3.f
    | _ -> 1.f
      
let mesh_names = ResizeArray<string>()

let colorbars = Map[
    Colormap.Hot, new Colorbar(Colormap.Hot, 0., 100.)
    Colormap.Jet, new Colorbar(Colormap.Jet, 0., 100.)
    Colormap.Winter, new Colorbar(Colormap.Winter, 0., 100.)
    Colormap.Gray, new Colorbar(Colormap.Gray, 0., 100.)
    Colormap.Autumn, new Colorbar(Colormap.Autumn, 0., 100.)
]

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
    // ImGui.InputFloat3("view: ", &v) |> ignore
    ImGui.Text($"count: {update_count}")
    // ImGui.Text($"count: {Singletons.get<UpdateCount>()}")
    
    let mesh = Components.get<Enable>().Entries
    for i in 0..mesh_names.Count-1 do
        ImGui.Checkbox(mesh_names[i], &mesh[i]) |> ignore
    ImGui.End()
)

// load resources
system OnLoad [] (fun _ ->
    let wnd = SE_Window.Shared
    wnd.Camera.Speed <- 1.f
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
    sw.Restart()
    dt_reset()
    use gltf = new GLTF.Deserializer(path)
    let meshes = gltf.ReadMeshesParallel()
    // let meshes = [|meshes[2]; meshes[3]; meshes[5]|]
    let wnd = SE_Window.Shared
    sw.Stop()
    printfn "read_meshes: %d ms" (sw.Elapsed.Milliseconds)
    dt_print ()

    sw.Restart()
    dt_reset()

    meshes
    |> Array.iter (fun mesh -> RGeometry.tranform rotation mesh |> ignore)
    
    // define ALL the octrees on the same volume (x,y,z)
    let mutable (v_min,v_max) = GridGeneration3D.bounds_SIMD (meshes[0].vertices.AsSpan()) meshes[0].L
    for mesh in meshes do
        let (_v_min,_v_max) = GridGeneration3D.bounds_SIMD (mesh.vertices.AsSpan()) mesh.L
        v_min <- System.Numerics.Vector3.Min(v_min, _v_min)        
        v_max <- System.Numerics.Vector3.Max(v_max, _v_max)        
    
    printfn "v_min: %A" v_min
    printfn "v_max: %A" v_max

    let trees =
        meshes
        |> Array.Parallel.map (fun mesh ->                
            let vertices = mesh.vertices.AsSpan()
            let indices = mesh.indices.AsSpan()
            let (_v_min,_v_max) = GridGeneration3D.bounds_SIMD vertices L
            
            if (v_min.X > _v_min.X) || (v_min.Y > _v_min.Y) || (v_min.Z > _v_min.Z) then
                Console.ForegroundColor <- ConsoleColor.Red
                printfn "node_v_min: %A" _v_min
                Console.ResetColor()
                
            if (v_max.X < _v_max.X) || (v_max.Y < _v_max.Y) || (v_max.Z < _v_max.Z) then
                Console.ForegroundColor <- ConsoleColor.Red
                printfn "node_v_max: %A" _v_max
                Console.ResetColor()
                
            let bits = Octree.fill_scanlines N L v_min v_max vertices indices (System.Collections.BitArray(N*N*N))
            Octree.ofStencil<Entity> N k v_min v_max bits
        )

    // Fix the Electrolyte Control Volume
    let electrolyte_mesh = Seq.item 5 meshes
    let electrolyte_tree = Seq.item 5 trees
    let electrodes_tree = Seq.item 2 trees
    let tubes_tree = Seq.item 3 trees
    // let electrolyte_mesh = Seq.item 2 meshes
    // let electrolyte_tree = Seq.item 2 trees
    // let electrodes_tree = Seq.item 0 trees
    // let tubes_tree = Seq.item 1 trees
    let vertices = electrolyte_mesh.vertices.AsSpan()
    let indices  = electrolyte_mesh.indices.AsSpan()
    let L = electrolyte_mesh.L 
    let bits = electrodes_tree.Stencil.Or(tubes_tree.Stencil)
    
    trees[5] <- Octree.ofStencil<Entity> N k v_min v_max (electrolyte_tree.Stencil.And(bits.Not()))
    // trees[2] <- Octree.ofStencil<Entity> N k v_min v_max (electrolyte_tree.Stencil.And(bits.Not()))

    trees
    |> Array.iteri (fun i tree ->
        mesh_names.Add(sprintf "body_%d: %d/%d" (i+1) (tree.GetInternalCount()) (tree.GetCount()))
    )

    sw.Stop()
    printfn "create_trees: %d ms" (sw.Elapsed.Milliseconds)
    dt_print()
        
    sw.Restart()
    dt_reset()
    let mutable i = 0
    for tree in trees do
        // assign entities to leafs
        tree.Iter (fun u ->
            match u with
            | Octree.Internal & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> set (Temperature(5.)) |> ValueSome 
            | Octree.Boundary & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> set (Temperature(5.)) |> ValueSome 
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
        i <- i + 1
        entity()
        |> Entity.add<IsPoints>
        |> Entity.add<UpdateColors>
        |> Entity.addRef tree
        |> Entity.singleton $"body_{i}"
        |> set points
        |> set (VertexBuffer.create VT2 points)
        |> set (Matrix4.CreateScale(scale))
        |> set true
        |> set Colormap.Jet
        |> ignore

        let p = Octree.center (tree.Root)    
        wnd.Camera.Position <- Vector3(p.X, p.Y, p.Z)
        

    let entities = Components.get<IsPoints>().Entities
    entities[2] |> set (colorbars[Colormap.Jet].AsTexture(0.8f, 0.0f, 120.f, 460.f)) |> ignore
    entities[3] |> set (colorbars[Colormap.Gray].AsTexture(0.8f, -0.8f, 120.f, 460.f)) |> ignore
    // entities[0] |> set (colorbars[Colormap.Jet].AsTexture(0.8f, 0.0f, 120.f, 460.f)) |> ignore
    // entities[1] |> set (colorbars[Colormap.Gray].AsTexture(0.8f, -0.8f, 120.f, 460.f)) |> ignore

    sw.Stop()
    printfn "load_trees: %d ms" (sw.Elapsed.Milliseconds)
    dt_print()
)

system PostLoad [] (fun _ ->
    let P = Components.get<Colormap>().Entities
    P[0] |> set Colormap.Winter |> ignore
    P[1] |> set Colormap.Winter |> ignore
    P[2] |> set Colormap.Gray |> ignore
    P[3] |> set Colormap.Gray |> ignore
    P[4] |> set Colormap.Winter |> ignore
    P[5] |> set Colormap.Jet |> ignore
    // P[0] |> set Colormap.Winter |> ignore
    // P[1] |> set Colormap.Gray |> ignore
    // P[2] |> set Colormap.Jet |> ignore


    let E = Components.get<Enable>().Entries
    E[0] <- false
    E[1] <- false
    E[2] <- false
    E[3] <- false
    E[4] <- false
    E[5] <- true
    // E[0] <- false
    // E[1] <- false
    // E[2] <- true
)

// controls
system OnValidate [] (fun _ ->
    let wnd = SE_Window.Shared

    if wnd.Pressed Keys.Escape then
        wnd.Close()
        Systems.quit()

    if wnd.Pressed Keys.P then
        sw.Restart()
        Systems.unpause()
        update_bool <- true
        wnd.IsRecording <- true

    let mesh = Components.get<Enable>().Entries
    mesh[0] <- if wnd.Pressed Keys.D1 then not mesh[0] else mesh[0]
    mesh[1] <- if wnd.Pressed Keys.D2 then not mesh[1] else mesh[1]
    mesh[2] <- if wnd.Pressed Keys.D3 then not mesh[2] else mesh[2]
    mesh[3] <- if wnd.Pressed Keys.D4 then not mesh[3] else mesh[3]
    mesh[4] <- if wnd.Pressed Keys.D5 then not mesh[4] else mesh[4]
    mesh[5] <- if wnd.Pressed Keys.D6 then not mesh[5] else mesh[5]
    // mesh[0] <- if wnd.Pressed Keys.D1 then not mesh[0] else mesh[0]
    // mesh[1] <- if wnd.Pressed Keys.D2 then not mesh[1] else mesh[1]
    // mesh[2] <- if wnd.Pressed Keys.D3 then not mesh[2] else mesh[2]
    
    wnd.KeysCache()
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
        
    VideoCapture.export ".gif" SE_Window.Shared

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
            VideoCapture.frame SE_Window.Shared

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

system OnUpdate [] (fun _ ->
    if update_bool then
        update_bool <- false
        task_new (fun _ ->
            let T = Components.get<Temperature>()
            let electrolyte = Entity.fetch "body_6"
            let electrodes  = Entity.fetch "body_3"

            let electrolyte_tree = Entity.getRef<Octree.Root<Entity>> electrolyte
            let electrodes_tree  = Entity.getRef<Octree.Root<Entity>> electrodes

            electrodes_tree.IterParallel 4 (fun u ->
                match u with
                | Octree.Boundary & Octree.Leaf (_,v,_,_,_,_) ->
                    match electrolyte_tree.MapTo(pos u) with
                    | Octree.Leaf (_,v,_,_,_,_) ->
                        let idx = v.Value.Value
                        T[idx] <- Temperature(Math.Clamp(Tf32 T[idx] + 1., 0., 100.))
                    | _ -> ()
                | _ -> ()
            )    

            octree_to_buffer<Temperature> electrolyte_tree colorbars[Entity.get<Colormap> electrolyte] (Entity.get<Mesh> electrolyte) T Tf32

            electrolyte
            |> Entity.add<UpdateColors>
            |> ignore

            update_bool <- true
            update_count <- update_count + 1
        ) |> ignore
)

system OnValidate [] (fun _ ->
    if update_count > 100 then
        Systems.quit()
)

Systems.progress()


