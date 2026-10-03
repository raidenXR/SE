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

open SE
open SE.Core
open SE.ECS
open SE.Spatial
open SE.Renderer

let [<Literal>] N = 200
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

let _trim node =
    let T = Components.get<Temperature>()
    match node with
    | Octree.FilledBranch & Octree.Node (_,c,_,_,_,_) ->
        let d = 0.3
        let T0 = Tf32 T[!c[0]]
        let T1 = Tf32 T[!c[1]]
        let T2 = Tf32 T[!c[2]]
        let T3 = Tf32 T[!c[3]]
        let T4 = Tf32 T[!c[4]]
        let T5 = Tf32 T[!c[5]]
        let T6 = Tf32 T[!c[6]]
        let T7 = Tf32 T[!c[7]]
        let b = abs(T0-T1) < d || abs(T0-T2) < d || abs(T2-T3) < d || abs(T3-T4) < d || abs(T4-T5) < d || abs(T5-T6) < d || abs(T6-T7) < d
        if b then printfn "_trim run"
        b
    | _ -> false
        
let _dense node =
    let T = Components.get<Temperature>()
    match node with
    | Octree.FilledBranch & Octree.Node (_,c,_,_,_,_) ->
        let d = 1.
        let T0 = Tf32 T[!c[0]]
        let T1 = Tf32 T[!c[1]]
        let T2 = Tf32 T[!c[2]]
        let T3 = Tf32 T[!c[3]]
        let T4 = Tf32 T[!c[4]]
        let T5 = Tf32 T[!c[5]]
        let T6 = Tf32 T[!c[6]]
        let T7 = Tf32 T[!c[7]]
        let b = abs(T0-T1) > d || abs(T0-T2) > d || abs(T2-T3) > d || abs(T3-T4) > d || abs(T4-T5) > d || abs(T5-T6) > d || abs(T6-T7) > d
        if b then printfn "_dense run"
        b
    | _ -> false
        
let _set node =
    let T = Components.get<Temperature>()
    match node with
    | Octree.FilledBranch & Octree.Node (_,c,_,_,_,_) ->
        let mutable t = 0.
        for ci in c do
            t <- t + Tf32 T[!ci] 
            Entity.remove !ci |> ignore

        entity()
        |> set (Temperature(t/8.))
    | _ -> failwith "_set SHOULD apply only on quadants" 
    
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
    // | GLTF.IsGltf -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.1f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
    | _ -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.f, 0.f, 0.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        

let scale =
    match path with
    | GLTF.IsTxt -> 0.2f
    | GLTF.IsPly -> 10.f
    | GLTF.IsGltf -> 3.f
    | _ -> 1.f
      
let mesh_names = ResizeArray<string>()

// rendering trait
let solid = prefab3 (IBody()) true (Matrix4.CreateScale scale)
let fluid = prefab4 (IPoints()) true UpdateColors (Matrix4.CreateScale scale)

let descretized (tree:Octree.Root<Entity>) e = e |> set ITree |> Entity.addRef tree

let colorbars = Map[
    Colormap.Hot, new Colorbar(Colormap.Hot, 0., 100.)
    Colormap.Jet, new Colorbar(Colormap.Jet, 0., 100.)
    Colormap.Winter, new Colorbar(Colormap.Winter, 0., 100.)
    Colormap.Gray, new Colorbar(Colormap.Gray, 0., 100.)
    Colormap.Autumn, new Colorbar(Colormap.Autumn, 0., 100.)
]

let octree_to_buffer<'T> (tree:Octree.Root<Entity>) (colorbar:Colorbar) (mesh:Mesh) convert =
    let T = Components.get<'T>()
    let L = mesh.L
    // let mutable i = 0
    match L with
    | 7 ->
        tree.Iteri (fun i u ->
            let vertices = mesh.vertices.AsSpan()
            let p = pos u
            let c = colorbar[convert T[!u]]
            vertices[i*L + 0] <- p.X
            vertices[i*L + 1] <- p.Y
            vertices[i*L + 2] <- p.Z
            vertices[i*L + 3] <- c.X
            vertices[i*L + 4] <- c.Y
            vertices[i*L + 5] <- c.Z
            vertices[i*L + 6] <- c.W
            // match u with
            // | Octree.Internal | Octree.Boundary ->
            // // | Octree.Internal ->
            //     let vertices = mesh.vertices.AsSpan()
            //     // if (i*mesh.L+6) >= vertices.Length then printfn "i: %d, tree_len: %d" i (tree.GetInternalCount())
            //     let p = pos u
            //     let c = colorbar[convert values[!u]]
            //     vertices[i*mesh.L + 0] <- p.X
            //     vertices[i*mesh.L + 1] <- p.Y
            //     vertices[i*mesh.L + 2] <- p.Z
            //     vertices[i*mesh.L + 3] <- c.X
            //     vertices[i*mesh.L + 4] <- c.Y
            //     vertices[i*mesh.L + 5] <- c.Z
            //     vertices[i*mesh.L + 6] <- c.W
            //     // i <- i + 1            
            // | _ -> ()
        )
    | 10 ->
        let vertices = mesh.vertices.AsSpan()
        let len = mesh.vertices.Length / L
        for i in 0..len-1 do
            match tree.MapTo(double vertices[i*L+0], double vertices[i*L+1], double vertices[i*L+2]) with
            | Octree.Leaf _ as u ->
                let c = colorbar[convert T[!u]]
                vertices[i*L + 6] <- c.X
                vertices[i*L + 7] <- c.Y
                vertices[i*L + 8] <- c.Z
                vertices[i*L + 9] <- c.W
                // v[i*L+9] <- 0.55f
                // FSharp.NativeInterop.NativePtr.write c_ptr (Vector4(c.X, c.Y, c.Z, c.W))
                // printfn "color set"
            | _ -> ()
    | _ ->
        failwith "Not valid mesh.L value"
            

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
    
    let meshes =
        gltf.ReadMeshesParallel()
        |> Array.map (fun mesh -> RGeometry.tranform rotation mesh)
    
    let (v_min,v_max) =
        meshes
        |> RGeometry.meshes_bounds

    let trees = 
        [|meshes[2]; meshes[3]; meshes[5]|]
        |> Array.zip3 [|N; N; N|] [|k; k; k|]
        |> Octree.ofMeshes v_min v_max

    printfn "v_min: %A" v_min
    printfn "v_max: %A" v_max

    // Fix the Electrolyte Control Volume
    let electrolyte_tree = trees[2]
    let electrodes_tree  = trees[0]
    let tubes_tree       = trees[1]
    let bits = electrodes_tree.Stencil.Or(tubes_tree.Stencil)    
    trees[2] <- Octree.ofStencil<Entity> N k v_min v_max (electrolyte_tree.Stencil.And(bits.Not()))

    // init electrolyte tree
    trees[2].Iter (fun u ->
        match u with
        | Octree.Internal & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> set (Temperature(5.)) |> ValueSome 
        | Octree.Boundary & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> set (Temperature(5.)) |> ValueSome 
        | _ -> ()
    )
    trees[0].Iter (fun u ->
        match u with
        | Octree.Internal & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> set (Voltage(60.)) |> ValueSome 
        | Octree.Boundary & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> set (Voltage(60.)) |> ValueSome 
        | _ -> ()
    )

    mesh_names.Add(sprintf "body_%d:" (0+1)) 
    mesh_names.Add(sprintf "body_%d:" (1+1)) 
    mesh_names.Add(sprintf "body_%d: %d/%d" (2+1) (trees[0].GetInternalCount()) (trees[0].GetCount()))
    mesh_names.Add(sprintf "body_%d: %d/%d" (3+1) (trees[1].GetInternalCount()) (trees[1].GetCount()))
    mesh_names.Add(sprintf "body_%d:" (4+1))
    mesh_names.Add(sprintf "body_%d: %d/%d" (5+1) (trees[2].GetInternalCount()) (trees[2].GetCount()))
    

    let body1 = entity() |> solid |> Entity.singleton "body_1" |> set (VertexBuffer.create VT1 meshes[0]) |> set meshes[0]
    let body2 = entity() |> solid |> Entity.singleton "body_2" |> set (VertexBuffer.create VT1 meshes[1]) |> set meshes[1]

    let body3 =
        entity()
        |> solid
        |> descretized trees[0]
        |> Entity.singleton "body_3"
        |> set (VertexBuffer.create VT1 meshes[2])
        |> set meshes[2]
        |> set Colormap.Gray
    
    let body4 =
        entity()
        |> solid
        |> descretized trees[1]
        |> Entity.singleton "body_4"
        |> set (VertexBuffer.create VT1 meshes[3])
        |> set meshes[3]
        
    let body5 = entity() |> solid |> Entity.singleton "body_5" |> set (VertexBuffer.create VT1 meshes[4]) |> set meshes[4]
    
    let points = {
        // vertices = NativeArray.create<float32>(trees[2].GetCount()*7)
        vertices = NativeArray.create<float32>(N*N*N*7)
        indices = NativeArray.empty<uint32>()
        L = 7
    }
    
    octree_to_buffer<Temperature> trees[2] colorbars[Colormap.Jet] points Tf32
    let c = 1.f / float32 update_count
    RGeometry.colorfill (c, c, c, 1.f) meshes[2] |> ignore
    // octree_to_buffer<Voltage> trees[0] colorbars[Colormap.Gray] meshes[2] (fun (Voltage v) -> v)

    let body6 =
        entity()
        |> fluid
        |> descretized trees[2]
        |> Entity.singleton "body_6"
        |> set (VertexBuffer.create VT2 points)
        |> set points
        |> set (trees[2].GetCount())
        |> set Colormap.Jet

    let p = Octree.center (trees[2].Root)    
    wnd.Camera.Position <- Vector3(p.X, p.Y, p.Z)
    
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
    E[3] <- false
    E[4] <- false
    E[5] <- true

    // C[0] |> Entity.get<Mesh> |> RGeometry.colorfill (0.5f, 0.5f, 0.5f, 1.0f) |> VertexBuffer.update (Entity.get<VertexBuffer> C[0])
    // C[1] |> Entity.get<Mesh> |> RGeometry.colorfill (0.6f, 0.3f, 0.4f, 1.0f) |> VertexBuffer.update (Entity.get<VertexBuffer> C[1])
    // C[2] |> Entity.get<Mesh> |> RGeometry.colorfill (0.5f, 0.3f, 0.4f, 1.0f) |> VertexBuffer.update (Entity.get<VertexBuffer> C[2])
    // C[3] |> Entity.get<Mesh> |> RGeometry.colorfill (0.4f, 0.5f, 0.5f, 0.2f) |> VertexBuffer.update (Entity.get<VertexBuffer> C[3])
    // C[4] |> Entity.get<Mesh> |> RGeometry.colorfill (0.8f, 0.7f, 0.8f, 0.2f) |> VertexBuffer.update (Entity.get<VertexBuffer> C[4])

    entity() |> set (colorbars[Colormap.Jet].AsTexture(0.8f, 0.0f, 120.f, 460.f)) |> ignore
    entity() |> set (colorbars[Colormap.Gray].AsTexture(0.8f, -0.8f, 120.f, 460.f)) |> ignore    

    let tree = "body_6" |> Entity.fetch |> Entity.getRef<Octree.Root<Entity>>
    let tree' = tree.Copy()

    tree'.Iter (fun u ->
        match u with
        | Octree.Internal & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> set (Temperature(5.)) |> ValueSome 
        | Octree.Boundary & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> set (Temperature(5.)) |> ValueSome 
        | _ -> ()
    )
    // Singletons.set tree'
    entity()
    |> Entity.singleton "tree'"
    |> Entity.addRef tree'
    |> ignore

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
    
    wnd.KeysCache()
)

// clear all resources
system OnExit [] (fun _ ->
    // for vb in Components.get<VertexBuffer>().Entries do
    //     VertexBuffer.delete vb        
        
    // for mesh in Components.get<Mesh>().Entries do
    //     mesh.Dispose()
        
    // for texture in Components.get<Texture>().Entries do
    //     Texture.delete texture

    // Shaders.unload()
    // SE_Window.Shared.Dispose()
    // SE_UI.Shared.OnClosed()
    // for pair in colorbars do
    //     pair.Value.Dispose()
        
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
            VideoCapture.frame SE_Window.Shared

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
            let b6 = Entity.fetch "body_6"
            let b3  = Entity.fetch "body_3"

            let b6_tree = Entity.getRef<Octree.Root<Entity>> b6
            let b3_tree  = Entity.getRef<Octree.Root<Entity>> b3
            let tree' = "tree'" |> Entity.fetch |> Entity.getRef<Octree.Root<Entity>>

            try
                b3_tree.IterParallel 4 (fun u ->
                    match u with
                    | Octree.Boundary & Octree.Leaf (_,v,_,_,_,_) ->
                        V[!u] <- Voltage(double update_count/ 100.)

                        match (b6_tree.MapTo(pos u)) with
                        | Octree.Leaf (_,v,_,_,_,_) as x ->
                            let idx = v.Value.Value
                            T[idx] <- Temperature(Math.Clamp(Tf32 T[idx] + 1., 0., 100.))
                            T[tree'[pos x].Value] <- T[idx]
                        | _ -> ()
                    | _ -> ()
                )    
                printfn "surface iter %d" update_count
            // octree_to_buffer<Temperature> electrolyte_tree colorbars[Colormap.Jet] (Entity.get<Mesh> electrolyte) Tf32
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

                        T[tree'[pos u].Value] <- Temperature(1.1*
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
                octree_to_buffer<Temperature> b6_tree colorbars[Colormap.Jet] (Entity.get<Mesh> b6) Tf32
                // octree_to_buffer<Voltage> b3_tree colorbars[Colormap.Gray] (Entity.get<Mesh> b3) (fun (Voltage v) -> Math.Clamp(v, 0., 90.))
                let c = 1.f / (float32 update_count + 1.f)
                RGeometry.colorfill (c, c, c, 1.f) (Entity.get<Mesh> b3) |> ignore
            with
                | _ as e -> printfn "copy_to_buffer: %s" e.Message 


            try
                b6_tree.Update(_trim, _dense, _set)
                tree'.Update(_trim, _dense, _set)

                let electrolyte_count = b6_tree.GetCount()
                printfn "electrolyte_count: %d" electrolyte_count

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


