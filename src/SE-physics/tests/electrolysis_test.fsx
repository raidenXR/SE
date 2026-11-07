#r "../bin/Debug/net10.0/SE-renderer.dll"
#r "../bin/Debug/net10.0/SE-core.dll"
#r "../bin/Debug/net10.0/SE-data.dll"
// #r "../bin/Release/net10.0/SE-renderer.dll"
// #r "../bin/Release/net10.0/SE-core.dll"
#r "nuget: OpenTK, 4.9.4"
#r "nuget: SkiaSharp, 2.88.6"
#r "nuget: ImGui.NET, 1.91.6.1"
#r "nuget: FFMPegCore, 5.4.0"
#r "nuget: Microsoft.Data.Sqlite, 10.0.12"

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
open SE.Data

let [<Literal>] N = 300
let [<Literal>] L = 10
let [<Literal>] k = 3
let [<Literal>] ss = "../../../resources/shaders/"

// type [<Struct>] Enable = {is_enabled:bool}
type Enable = bool
type [<Struct>] Temperature = Temperature of float
type [<Struct>] Voltage = Voltage of float
type UpdateColors = struct end
type IPoints = struct end
type IBody = struct end
type ITree = struct end

let mutable dtime = DateTime.Now
let dt_reset () =
    dtime <- DateTime.Now

let dt_print () =
    let t = DateTime.Now
    printfn "%d ms" (t - dtime).Milliseconds
    dtime <- t

let inline (!) (u:Octree.Node<'T>) = Octree.valueof u
let inline Tf32 (Temperature T) = T
let inline vec3 (v:Vector3) = System.Numerics.Vector3(v.X, v.Y, v.Z)
let pos = Octree.center

    
let sw =
    System.Diagnostics.Stopwatch()

let path =
    "../../../resources/models/cell.gltf"

let mutable update_count = 0
let mutable update_bool = false
let mutable wireframe_on = false
    
let rotation =
    match path with
    | GLTF.IsTxt -> System.Numerics.Quaternion.CreateFromYawPitchRoll(2.f, 2.f, 1.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
    | GLTF.IsPly -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.f, 0.f, 0.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
    // | GLTF.IsGltf -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.1f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
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
    Colormap.Jet, new Colorbar(Colormap.Jet, 280., 630.)
    Colormap.Winter, new Colorbar(Colormap.Winter, 0., 100.)
    Colormap.Gray, new Colorbar(Colormap.Gray, 0., 100.)
    Colormap.Autumn, new Colorbar(Colormap.Autumn, 0., 100.)
]

// rendering trait
let solid = prefab3 (IBody()) true (Matrix4.CreateScale scale)
let fluid = prefab4 (IPoints()) true UpdateColors (Matrix4.CreateScale scale)

let descretized (tree:Octree.Root<Entity>) e = e |> set ITree |> Entity.addRef tree |> set (tree.GetCount())
let visual VT mesh e = e |> set (VertexBuffer.create VT mesh) |> set mesh

let _trim = Octree._trim<Temperature> 0.5 Tf32
let _dense = Octree._dense<Temperature> 1.5 Tf32

let _set node =
    let T = SE.ECS.Components.get<Temperature>()
    match node with
    | Octree.Leaf (p,_,_,_,_,_) ->
        let c = Octree.children p
        let mutable t = 0.
        for ci in c do
            t <- t + Tf32 (T[!ci]) 
            SE.ECS.Entity.remove !ci |> ignore

        SE.ECS.Entity.create() |> SE.ECS.Entity.set (Temperature(t/8.))
    | _ -> failwith "set must exectute ONLY on Leafs"        
            

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
    ImGui.Text($"count: {update_count}")
    
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
        "s_shader", ss + "shader.vert", ss + "solid.frag"
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
    let wnd = SE_Window.Shared
    use gltf = new GLTF.Deserializer(path)
    
    let (meshes,trees,v_min,v_max) =
        if not (System.IO.File.Exists "testing.db") then
            let meshes =
                gltf.ReadMeshesParallel()
                // gltf.ReadMeshes()
                // |> Array.map (fun mesh -> RGeometry.tranform rotation mesh)
    
            let (v_min,v_max) =
                meshes
                |> RGeometry.meshes_bounds

            let trees = 
                [|meshes[2]; meshes[3]; meshes[5]|]
                |> Array.zip3 [|N; N; N|] [|k-2; k; k|]
                |> Octree.ofMeshes v_min v_max

            let names = [|"body_1"; "body_2"; "body_3"; "body_4"; "body_5"; "body_6"|]
            (names,meshes) ||> Array.zip |> Serialize.meshes "testing.db"

            let names' = [|"body_3"; "body_4"; "body_6"|]
            (names', [|N;N;N|], trees |> Array.map (fun t -> t.Stencil)) |||> Array.zip3 |> Serialize.stencils "testing.db"
            (meshes,trees,v_min,v_max)
        else
            let meshes = Deserialize.meshes "testing.db"
            let (v_min,v_max) =
                meshes
                |> RGeometry.meshes_bounds
            let trees = [|for (name,n,bits) in Deserialize.stencils "testing.db" -> Octree.ofStencil<Entity> n k v_min v_max bits|]
            (meshes,trees,v_min,v_max)
        
    printfn "v_min: %A" v_min
    printfn "v_max: %A" v_max

    // Fix the Electrolyte Control Volume
    let electrolyte_tree = trees[2]
    let electrodes_tree  = trees[0]
    let tubes_tree       = trees[1]
    let bits = electrodes_tree.Stencil.Or(tubes_tree.Stencil)    
    trees[2] <- Octree.ofStencil<Entity> N k v_min v_max (electrolyte_tree.Stencil.And(bits.Not()))

    // init electrolyte tree
    let tree2_t' = trees[2].Copy()
    trees[0].Iter (fun (Octree.Leaf(_,v,_,_,_,_)) -> v.Value <- entity() |> set (Voltage(5.)) |> ValueSome)
    trees[2].Iter (fun (Octree.Leaf(_,v,_,_,_,_)) -> v.Value <- entity() |> set (Temperature(300.)) |> ValueSome)
    tree2_t'.Iter (fun (Octree.Leaf(_,v,_,_,_,_)) -> v.Value <- entity() |> set (Temperature(300.)) |> ValueSome)
    singleton "tree'" |> Entity.addRef tree2_t' |> ignore

    // trees[0].Iter (fun u ->
    //     match u with
    //     | Octree.Boundary _ -> ()
    //     | Octree.Internal _ -> ()
    // )

    mesh_names.Add(sprintf "body_%d:" (0+1)) 
    mesh_names.Add(sprintf "body_%d:" (1+1)) 
    mesh_names.Add(sprintf "body_%d: %d/%d" (2+1) (trees[0].GetInternalCount()) (trees[0].GetCount()))
    mesh_names.Add(sprintf "body_%d: %d/%d" (3+1) (trees[1].GetInternalCount()) (trees[1].GetCount()))
    mesh_names.Add(sprintf "body_%d:" (4+1))
    mesh_names.Add(sprintf "body_%d: %d/%d" (5+1) (trees[2].GetInternalCount()) (trees[2].GetCount()))    

    let body1 = singleton "body_1" |> solid |> visual VT1 meshes[0]
    let body2 = singleton "body_2" |> solid |> visual VT1 meshes[1]
    let body3 = singleton "body_3" |> solid |> visual VT1 meshes[2] |> descretized trees[0] |> set Colormap.Gray    
    let body4 = singleton "body_4" |> solid |> visual VT1 meshes[3] |> descretized trees[1]       
    let body5 = singleton "body_5" |> solid |> visual VT1 meshes[4]
    let body6 = singleton "body_6" |> fluid |> visual VT2 (Mesh.points(N*N*N * 7))|> descretized trees[2] |> set Colormap.Jet

    let p = Octree.center (trees[2].Root)    
    wnd.Camera.Position <- Vector3(p.X, p.Y, p.Z)
    
    body3 |> Helpers.octreeToBuffer<Voltage> colorbars[Colormap.Gray] (fun (Voltage v) -> v)
    body6 |> Helpers.octreeToBuffer<Temperature> colorbars[Colormap.Jet] Tf32

    sw.Stop()
    printfn "create_trees: %d ms" (sw.Elapsed.Milliseconds)
)

system PostLoad [] (fun _ ->
    let ens = Components.get<Enable>()
    let E = ens.Entries
    let C = ens.Entities
    
    E[0] <- false
    E[1] <- false
    E[2] <- true
    E[3] <- true
    E[4] <- false
    E[5] <- true

    entity() |> set (colorbars[Colormap.Jet].AsTexture(0.8f, 0.0f, 120.f, 460.f)) |> ignore
    entity() |> set (colorbars[Colormap.Gray].AsTexture(0.8f, -0.8f, 120.f, 460.f)) |> ignore    
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

    wireframe_on <- if wnd.Pressed Keys.W then not wireframe_on else wireframe_on
    if wireframe_on then GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line)
    if not wireframe_on then GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill)
    
    wnd.KeysCache()
)

// system OnExit [typeof<Mesh>] (fun q ->
//     if not (System.IO.File.Exists "testing.db") then
//         let meshes = Components.get<Mesh>().Entries.ToArray()
//         let names = [|"body_1"; "body_2"; "body_3"; "body_4"; "body_5"; "body_6"|]
//         (names,meshes) ||> Array.zip |> Serialize.meshes "testing.db"

//         for e in q do
//             Serialize.meshes
// )

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
        
    // VideoCapture.export ".gif" SE_Window.Shared

    sw.Stop()
    printfn "stopwatch: %d s" (sw.Elapsed.Seconds)
)


system PreRender [typeof<Mesh>; typeof<VertexBuffer>; typeof<Enable>; typeof<IBody>] (fun q ->
    let mesh = Components.get<Mesh>()
    let vbuf = Components.get<VertexBuffer>()
    let enabled = Components.get<Enable>()    

    for e in q do
        if Entity.has<UpdateColors> e && enabled[e] then
            // VideoCapture.frame SE_Window.Shared

            VertexBuffer.update vbuf[e] mesh[e]        
            e |> Entity.remove<UpdateColors> |> ignore        
)

system PreRender [typeof<Mesh>; typeof<VertexBuffer>; typeof<Enable>; typeof<IPoints>] (fun q ->
    let mesh = Components.get<Mesh>()
    let vbuf = Components.get<VertexBuffer>()
    let enabled = Components.get<Enable>()    

    for e in q do
        if Entity.has<UpdateColors> e && enabled[e] then
            VideoCapture.frame SE_Window.Shared

            VertexBuffer.update_sliced vbuf[e] (Entity.get<int> e) mesh[e]        
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

system OnRender [typeof<Mesh>; typeof<VertexBuffer>; typeof<IBody>] (fun q ->
    let m = Components.get<Mesh>()
    let t = Components.get<Matrix4>()
    let v = Components.get<VertexBuffer>()

    let camera = SE_Window.Shared.Camera
    let shader = Shaders.get("s_shader")
    let enable = Components.get<Enable>()

    shader.Use()
    shader.SetMatrix4("view", camera.GetViewMatrix())
    shader.SetMatrix4("projection", camera.GetProjectionMatrix())

    for e in q do
        if enable[e] then
            shader.SetMatrix4("model", t[e])
            VertexBuffer.draw v[e] m[e]
)

// system OnRender [typeof<Mesh>; typeof<VertexBuffer>; typeof<IBody>] (fun q ->
//     let m = Components.get<Mesh>()
//     let t = Components.get<Matrix4>()
//     let v = Components.get<VertexBuffer>()

//     let camera = SE_Window.Shared.Camera
//     let shader = Shaders.get("m_shader")
//     let enable = Components.get<Enable>()

//     shader.Use()
//     shader.SetMatrix4("view", camera.GetViewMatrix())
//     shader.SetMatrix4("projection", camera.GetProjectionMatrix())
//     shader.SetVector3("viewPos", camera.Position)

//     shader.SetVector3("material.ambient", Vector3(1.0f, 0.5f, 0.31f))
//     shader.SetVector3("material.diffuse", Vector3(1.0f, 0.5f, 0.31f))
//     shader.SetVector3("material.specular", Vector3(0.5f, 0.5f, 0.5f))
//     shader.SetFloat("material.shininess", 32.0f)
    
//     shader.SetVector3("dirLight.direction", Vector3(-0.2f, -1.0f, -0.3f))
//     shader.SetVector3("dirLight.ambient", Vector3(0.05f, 0.05f, 0.05f))
//     shader.SetVector3("dirLight.diffuse", Vector3(0.4f, 0.4f, 0.4f))
//     shader.SetVector3("dirLight.specular", Vector3(0.5f, 0.5f, 0.5f))
    
//     shader.SetVector3("pointLight.position", Vector3(2.0f, 2.0f, 4.0f))
//     shader.SetVector3("pointLight.ambient", Vector3(0.05f, 0.05f, 0.05f))
//     shader.SetVector3("pointLight.diffuse", Vector3(0.8f, 0.8f, 0.8f))
//     shader.SetVector3("pointLight.specular", Vector3(1.0f, 1.0f, 1.0f))
//     shader.SetFloat("pointLight.constant", 1.0f)
//     shader.SetFloat("pointLight.linear", 0.09f)
//     shader.SetFloat("pointLight.quadratic", 0.032f)
    
//     shader.SetVector3("spotLight.position", camera.Position)
//     shader.SetVector3("spotLight.direction", camera.Front)
//     shader.SetVector3("spotLight.ambient", Vector3(0.0f, 0.0f, 0.0f))
//     shader.SetVector3("spotLight.diffuse", Vector3(1.0f, 1.0f, 1.0f))
//     shader.SetVector3("spotLight.specular", Vector3(1.0f, 1.0f, 1.0f))
//     shader.SetFloat("spotLight.constant", 1.0f)
//     shader.SetFloat("spotLight.linear", 0.09f)
//     shader.SetFloat("spotLight.quadratic", 0.032f)
//     shader.SetFloat("spotLight.cutOff", MathF.Cos(MathHelper.DegreesToRadians(12.5f)))
//     shader.SetFloat("spotLight.outerCutOff", MathF.Cos(MathHelper.DegreesToRadians(17.5f)))

//     for e in q do
//         if enable[e] then
//             shader.SetMatrix4("model", t[e])
//             VertexBuffer.draw v[e] m[e]
// )

system OnRender [typeof<Mesh>; typeof<VertexBuffer>; typeof<IPoints>] (fun q ->
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
            VertexBuffer.draw_sliced v[e] (Entity.get<int> e) m[e]
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
            let V = Components.get<Voltage>()
            let b6 = singleton "body_6"
            let b3 = singleton "body_3"

            let b6_tree = Entity.getRef<Octree.Root<Entity>> b6
            let b3_tree = Entity.getRef<Octree.Root<Entity>> b3
            let tree' = singleton "tree'" |> Entity.getRef<Octree.Root<Entity>>

            try
                b3_tree.IterParallel 4 (fun u ->
                    match u with
                    | Octree.Boundary | Octree.Internal ->
                        let (Voltage _v) = V[!u] 
                        V[!u] <- Voltage(_v + 1.)

                        match (b6_tree.MapTo(pos u)) with
                        | Octree.Leaf (_,v,_,_,_,_) as x ->
                            let idx = v.Value.Value
                            T[idx] <- Temperature(Tf32 T[idx] + 1.)
                            T[tree'[pos x].Value] <- T[idx]
                        | _ -> ()
                    | _ -> ()
                )    
            with
                | _ as e -> printfn "surface.iter: %s" e.Message

            try
                b6_tree.IterParallel 4 (fun u ->
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
            with
                | _ as e -> printfn "laplace.iter: %s" e.Message
   
            try
                tree'.IterParallel 4 (fun u ->
                    T[b6_tree[pos u].Value] <- T[!u]    
                )
            with
                | _ as e -> printfn "trees.copy: %s" e.Message

            try
                singleton "body_3" |> Helpers.octreeToBuffer<Voltage> colorbars[Colormap.Gray] (fun (Voltage v) -> v)
                singleton "body_6" |> Helpers.octreeToBuffer<Temperature> colorbars[Colormap.Jet] Tf32
            with
                | _ as e -> printfn "copy_to_buffer: %s" e.Message 


            try
                b6_tree.Update(_trim, _dense, _set)
                tree'.Update(_trim, _dense, _set)

                let electrolyte_count = b6_tree.GetCount()

                mesh_names[5] <- sprintf "body_%d: %d/%d" (5+1) (b6_tree.GetInternalCount()) (electrolyte_count)
                b6 |> Entity.add<UpdateColors> |> set electrolyte_count |> ignore
                b3 |> Entity.add<UpdateColors> |> ignore

                update_bool <- true
                update_count <- update_count + 1
            with
                | _ as e -> printfn "update n set failed: %s" e.Message 
        ) |> ignore
)

system OnValidate [] (fun _ ->
    if update_count > 100 then
        Systems.quit()
)

Systems.progress()


