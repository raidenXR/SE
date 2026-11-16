#r "../bin/Debug/net10.0/SE-renderer.dll"
#r "../bin/Debug/net10.0/SE-core.dll"
#r "../bin/Debug/net10.0/SE-data.dll"
// #r "../bin/Release/net10.0/SE-renderer.dll"
// #r "../bin/Release/net10.0/SE-core.dll"
#r "nuget: OpenTK, 4.9.4"
#r "nuget: SkiaSharp, 2.88.6"
#r "nuget: ImGui.NET, 1.91.6.1"
#r "nuget: FFMPegCore, 5.4.0"

open SE
open SE.Core
open SE.ECS
open SE.Spatial
open SE.Renderer
open SE.Data
open System

open FSharp.Data.UnitSystems.SI.UnitSymbols

open SkiaSharp
open ImGuiNET
open FFMpegCore
open FFMpegCore.Pipes

open OpenTK.Core
open OpenTK.Graphics
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics
open OpenTK.Windowing.Common
open OpenTK.Windowing.Common.Input
open OpenTK.Windowing.Desktop
open OpenTK.Windowing.GraphicsLibraryFramework

type [<Struct>] Temperature = {T:double<K>}
type [<Struct>] Potential = {p:double<V>}
type [<Struct>] Velocity = {v:Vector3}

type [<Measure>] L  

type [<Struct>] CH2 = {c:double<mol/L>}
type [<Struct>] CO2 = {c:double<mol/L>}
type [<Struct>] COH = {c:double<mol/L>}
type [<Struct>] CNa = {c:double<mol/L>}
type [<Struct>] I = {i:double<A/m^2>}
type [<Struct>] Ik = {ik:double<C m/L s>}
type [<Struct>] NH2 = {n: double<m mol/L s>}
type [<Struct>] NO2 = {n: double<m mol/L s>}
type [<Struct>] NOH = {n: double<m mol/L s>}
type [<Struct>] NNa = {n: double<m mol/L s>}

type UpdateColors = struct end
type ITree = struct end
type IPoints = struct end
type IBody = struct end

let [<Literal>] N = 200
let [<Literal>] L = 10
let [<Literal>] k = 2
let [<Literal>] ss = "../../../resources/shaders/"

let [<Literal>] Phi_s_anode = 0.5<V>  // ANODE
let [<Literal>] i0_anode = 1e-2<A/m^2>
let [<Literal>] a_anode = 0.5

let [<Literal>] Phi_s_cathode = 0.0<V>  // ANODE
let [<Literal>] i0_cathode = 1e-4<A/m^2>
let [<Literal>] a_cathode = 0.5
let [<Literal>] kappa = 0.5<S/m>

let [<Literal>] C_O2 = 0.0<mol/L>
let [<Literal>] C_H2 = 0.0<mol/L>
let [<Literal>] C_OH = 0.5<mol/L>
let [<Literal>] C_Na = 0.5<mol/L>
let [<Literal>] F = 96485.0<C/mol>
let [<Literal>] R = 8.31446261815324<J K^-1 mol^-1>
let [<Literal>] T = 298.0<K>

let path =
    "../../../resources/models/cylinder_cellFCStd.gltf"

let scale =
    match path with
    | GLTF.IsTxt -> 0.2f
    | GLTF.IsPly -> 10.f
    | GLTF.IsGltf -> 4.f
    | _ -> 1.f
      
let colorbars = Map[
    Colormap.Jet, new Colorbar(Colormap.Jet, 0., 12.)
    Colormap.Gray, new Colorbar(Colormap.Gray, 0., 12.)
]

let mesh_names =
    ResizeArray<string>()

// mutable global variables
let mutable update_count = 0
let mutable update_bool = false
let mutable wireframe_on = false

let inline vec3 (v:Vector3) = System.Numerics.Vector3(v.X, v.Y, v.Z)

// rendering trait
let solid = prefab3 (IBody()) UpdateColors (Matrix4.CreateScale scale)
let fluid = prefab3 (IPoints()) UpdateColors (Matrix4.CreateScale scale)
let mref = Entity.getRef<Octree.Root<Entity>>

let descretized (tree:Octree.Root<Entity>) e = e |> set ITree |> Entity.addRef tree |> set (tree.GetCount())
let visual VT mesh e = e |> set (VertexBuffer.create VT mesh) |> set mesh |> set true

let grad<'T> (conv:'T->double) (u:Octree.Node<Entity>) =
    let pos = Octree.center
    let C = Components.get<'T>()
    let (!) = Octree.valueof
    
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

    let Cx = (-x2)/(x1*(x1+x2))*conv C[!i] + (x2-x1)/(x1*x2)*conv C[!u] + x1/(x2*(x1 + x2))*conv C[!i']
    let Cy = (-y2)/(y1*(y1+y2))*conv C[!j] + (y2-y1)/(y1*y2)*conv C[!u] + y1/(y2*(y1 + y2))*conv C[!j']
    let Cz = (-z2)/(z1*(z1+z2))*conv C[!l] + (z2-z1)/(z1*z2)*conv C[!u] + z1/(z2*(z1 + z2))*conv C[!l']
    Cx + Cy + Cz


// let grad<'T> conv (u:Octree.Node<Entity>) =
//     let pos = Octree.center
//     let (!!) = Octree.valueof 
//     let C = Components.get<'T>()
    
//     let i  = u[-1,0,0]
//     let i' = u[+1,0,0]
//     let j  = u[0,-1,0]
//     let j' = u[0,+1,0]
//     let l  = u[0,0,-1]
//     let l' = u[0,0,+1]

//     let x1 = double (pos u - pos i).X
//     let y1 = double (pos u - pos j).Y
//     let z1 = double (pos u - pos l).Z

//     let x2 = double (pos i' - pos u).X
//     let y2 = double (pos j' - pos u).Y
//     let z2 = double (pos l' - pos u).Z

//     let Cx = (-x2)/(x1*(x1+x2))*C[!!i] + (x2-x1)/(x1*x2)*C[!!u] + x1/(x2*(x1 + x2))*C[!!i']
//     let Cy = (-y2)/(y1*(y1+y2))*C[!!j] + (y2-y1)/(y1*y2)*C[!!u] + y1/(y2*(y1 + y2))*C[!!j']
//     let Cz = (-z2)/(z1*(z1+z2))*C[!!l] + (z2-z1)/(z1*z2)*C[!!u] + z1/(z2*(z1 + z2))*C[!!l']
//     Cx + Cy + Cz

// let grad2<'T> conv (u:Octree.Node<Entity>) =
//     let pos = Octree.center
//     let (!!) = Octree.valueof 
//     let C = Components.get<'T>()
    
//     let i  = u[-1,0,0]
//     let i' = u[+1,0,0]
//     let j  = u[0,-1,0]
//     let j' = u[0,+1,0]
//     let l  = u[0,0,-1]
//     let l' = u[0,0,+1]

//     let x1 = double (pos u - pos i).X
//     let y1 = double (pos u - pos j).Y
//     let z1 = double (pos u - pos l).Z

//     let x2 = double (pos i' - pos u).X
//     let y2 = double (pos j' - pos u).Y
//     let z2 = double (pos l' - pos u).Z

//     let Cx = 2./(x1*(x1+x2))*C[!!i] - 2./(x1*x2)*C[!!i'] + 2./(x2*(x1+x2))*C[!!i']
//     let Cy = 2./(y1*(y1+y2))*C[!!j] - 2./(y1*y2)*C[!!j'] + 2./(y2*(y1+y2))*C[!!j']
//     let Cz = 2./(z1*(z1+z2))*C[!!l] - 2./(z1*z2)*C[!!l'] + 2./(z2*(z1+z2))*C[!!l']
//     Cx + Cy + Cz


// let diffusion (D:double) (u:Octree.Node<Entity>) =
//     -D * (grad<Concentration> () u)

// let migration z U (u:Octree.Node<Entity>) =
//     let c = Components.get<Concentration>()
//     let (!!) = Octree.valueof 
//     -z * U * F * c[!!u].C * (grad<Potential> () u)

// let overpotential (u:Octree.Node<Entity>) =
//     1.e-5

// let Buttler_Volmer_Anode a_a (u:Octree.Node<Entity>) =
//     let i0 = 1.e-5
//     let T = 25.<K> + 273.<K>
//     let eta = overpotential u
//     let i = i0 * (exp((a_a * F * eta)/(R * T)))
//     i
    

// // Laplace
// let Laplace (tree:Octree.Root<Entity>) (tree':Octree.Root<Entity>) =
//     let potentials = Components.get<Potential>()
//     let pos = Octree.center
//     let (!!) = Octree.valueof 

//     tree.IterParallel 4 (fun u ->
//         match u with
//         | Octree.Internal ->
//             let i  = u[-1,0,0]
//             let i' = u[+1,0,0]
//             let j  = u[0,-1,0]
//             let j' = u[0,+1,0]
//             let l  = u[0,0,-1]
//             let l' = u[0,0,+1]

//             let x1 = double (pos u - pos i).X
//             let y1 = double (pos u - pos j).Y
//             let z1 = double (pos u - pos l).Z

//             let x2 = double (pos i' - pos u).X
//             let y2 = double (pos j' - pos u).Y
//             let z2 = double (pos l' - pos u).Z

//             potentials[tree'[pos u].Value] <- { V =
//                 (2./(x1*(x1+x2))*potentials[!!i].V + 2./(x2*(x1+x2))*potentials[!!i'].V  +
//                 2./(y1*(y1+y2)) *potentials[!!j].V + 2./(y2*(y1+y2))*potentials[!!j'].V  +
//                 2./(z1*(z1+z2)) *potentials[!!l].V + 2./(z2*(z1+z2))*potentials[!!l'].V ) /
//                 (2./(x1*x2) + 2./(y1*y2) + 2./(z1*z2))
//             }

//             // V[tree'[pos u].Value].V <- 
//         | _ -> ()
//     )

//     tree'.IterParallel 4 (fun u -> potentials[tree[pos u].Value] <- potentials[!!u])
    

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
    
    let mesh = Components.get<bool>().Entries
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

// controls
system OnValidate [] (fun _ ->
    let wnd = SE_Window.Shared

    if wnd.Pressed Keys.Escape then
        wnd.Close()
        Systems.quit()

    if wnd.Pressed Keys.P then
        Systems.unpause()
        update_bool <- true
        wnd.IsRecording <- true

    let mesh = Components.get<bool>().Entries
    // for i in (int Keys.D1)..(int Keys.D1 + mesh.Length-1) do
        // mesh[i] <- if wnd.Pressed (enum<Keys> (i + int Keys.D1)) then not mesh[i] else mesh[i]
    mesh[0] <- if wnd.Pressed Keys.D1 then not mesh[0] else mesh[0]
    mesh[1] <- if wnd.Pressed Keys.D2 then not mesh[1] else mesh[1]
    mesh[2] <- if wnd.Pressed Keys.D3 then not mesh[2] else mesh[2]

    wireframe_on <- if wnd.Pressed Keys.W then not wireframe_on else wireframe_on
    if wireframe_on then GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Line)
    if not wireframe_on then GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill)
    
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
        
    // VideoCapture.export ".gif" SE_Window.Shared
)


system PreRender [typeof<Mesh>; typeof<VertexBuffer>; typeof<bool>; typeof<IBody>] (fun q ->
    let mesh = Components.get<Mesh>()
    let vbuf = Components.get<VertexBuffer>()
    let enabled = Components.get<bool>()    

    for e in q do
        if Entity.has<UpdateColors> e && enabled[e] then
            // VideoCapture.frame SE_Window.Shared

            VertexBuffer.update vbuf[e] mesh[e]        
            e |> Entity.remove<UpdateColors> |> ignore        
)

system PreRender [typeof<Mesh>; typeof<VertexBuffer>; typeof<bool>; typeof<IPoints>] (fun q ->
    let mesh = Components.get<Mesh>()
    let vbuf = Components.get<VertexBuffer>()
    let enabled = Components.get<bool>()    

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

// render solid volumes
system OnRender [typeof<Mesh>; typeof<VertexBuffer>; typeof<IBody>] (fun q ->
    let m = Components.get<Mesh>()
    let t = Components.get<Matrix4>()
    let v = Components.get<VertexBuffer>()

    let camera = SE_Window.Shared.Camera
    let shader = Shaders.get("s_shader")
    let enable = Components.get<bool>()

    shader.Use()
    shader.SetMatrix4("view", camera.GetViewMatrix())
    shader.SetMatrix4("projection", camera.GetProjectionMatrix())

    for e in q do
        if enable[e] then
            shader.SetMatrix4("model", t[e])
            VertexBuffer.draw v[e] m[e]
)

// render points
system OnRender [typeof<Mesh>; typeof<VertexBuffer>; typeof<IPoints>] (fun q ->
    let m = Components.get<Mesh>()
    let t = Components.get<Matrix4>()
    let v = Components.get<VertexBuffer>()

    let camera = SE_Window.Shared.Camera
    let shader = Shaders.get("p_shader")
    let enable = Components.get<bool>()

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

let (!) (node:Octree.Node<'T>) = Octree.valueof node
let pos = Octree.center


system OnLoad [] (fun _ ->
    let wnd = SE_Window.Shared
    use gltf = new GLTF.Deserializer(path)

    let meshes =
        gltf.ReadMeshesParallel()
    
    let (v_min,v_max) =
        meshes
        |> RGeometry.meshes_bounds
    
    let trees = 
        meshes
        |> Array.zip3 [|N; N; N|] [|k; k; k|]
        |> Octree.ofMeshes<Entity> v_min v_max

    for t in trees do
        printfn "count: %d, internal: %d, boundary: %d" (t.GetCount()) (t.GetInternalCount()) (t.GetBoundaryCount())
        
    printfn "v_min: %A" v_min
    printfn "v_max: %A" v_max

    singleton "electrolyte"
    |> fluid |> visual VT2 (Mesh.points(N*N*N*7)) |> descretized trees[0] |> set Colormap.Jet
    |> ignore
        
    singleton "anode"
    |> solid |> visual VT1 meshes[1] |> descretized trees[1] |> set Colormap.Gray
    |> ignore
        
    singleton "cathode"
    |> solid |> visual VT1 meshes[2] |> descretized trees[2] |> set Colormap.Gray
    |> ignore

    meshes
    |> Array.iteri (fun i m -> mesh_names.Add(sprintf "body_%d: %d" i (trees[i].GetCount())))

    entity() |> set (colorbars[Colormap.Jet].AsTexture(0.8f, 0.0f, 120.f, 460.f)) |> ignore
    entity() |> set (colorbars[Colormap.Gray].AsTexture(0.8f, -0.8f, 120.f, 460.f)) |> ignore    

    let p = Octree.center (trees[2].Root)    
    wnd.Camera.Position <- Vector3(p.X, p.Y, p.Z)

    
    trees[0].Iter (fun (Octree.Leaf(_,v,_,_,_,_)) -> v.Value <- entity() |> ValueSome)
    trees[1].Iter (fun (Octree.Leaf(_,v,_,_,_,_)) -> v.Value <- entity() |> ValueSome)
    trees[2].Iter (fun (Octree.Leaf(_,v,_,_,_,_)) -> v.Value <- entity() |> ValueSome)
)

system PostLoad [] (fun _ ->
    let e0 = singleton "electrolyte"
    let e1 = singleton "anode"
    let e2 = singleton "cathode"
    
    let t0 = mref e0
    let t1 = mref e1
    let t2 = mref e2

    t0.Iter (fun u ->
        match u with
        | Octree.Leaf (_,v,_,_,_,_) ->
            v.Value.Value
            |> set {CH2.c = 0.0<mol/L>}
            |> set {CO2.c = 0.5e-3<mol/L>}
            |> set {COH.c = 0.5<mol/L>}
            |> set {CNa.c = 0.5<mol/L>}
            |> set {p = 0.0<V>}
            |> set {ik = 0.0<C m/L s>}
            |> ignore
        | _ -> ()
    )
    
    t1.Iter (fun u -> match u with Octree.Leaf (_,v,_,_,_,_) -> v.Value.Value |> set {p = 0.5<V>} |> ignore | _ -> ())
    t2.Iter (fun u -> match u with Octree.Leaf (_,v,_,_,_,_) -> v.Value.Value |> set {p = 0.0<V>} |> ignore | _ -> ())
    
    Helpers.octreeToBuffer<Potential> colorbars[Colormap.Jet] (fun p -> double p.p) e0
    Helpers.octreeToBuffer<Potential> colorbars[Colormap.Gray] (fun p -> double p.p) e1
    Helpers.octreeToBuffer<Potential> colorbars[Colormap.Gray] (fun p -> double p.p) e2

    VertexBuffer.update (Entity.get<VertexBuffer> e0) (Entity.get<Mesh> e0)          
)

system PostLoad [] (fun _ ->
    printfn "count: %d" (Components.get<Potential>().Count)
    printfn "count: %d" (Components.get<CH2>().Count)
)

// compute the overpotential
let overpotential () =
    let e0 = singleton "electrolyte"
    let e1 = singleton "anode"
    let e2 = singleton "cathode"

    let t0 = mref e0
    let t1 = mref e1
    let t2 = mref e2

    let Eeq = 0.0<V>
    let Phi = Components.get<Potential>()
    
    t1.Iter (fun u ->
        match u with
        | Octree.Boundary ->
            match t0.MapTo(pos u) with
            | Octree.Leaf _ as v ->
                let eta = Phi_s_anode - Phi[!v].p - Eeq
                let i_HER = i0_anode * (exp((a_anode * F * eta)/(R * T)))
                let N_H2 = -i_HER/(2.*F)
                let N_OH = i_HER/F

                !u
                |> set {i = i_HER}
                |> ignore

                !v
                |> set {p = eta}
                |> ignore
            | _ -> ()
        | _ -> ()
    )

// Solve the Nernst-Planck transport equations.
let Nerst_Planck () =
    let e0 = singleton "electrolyte"
    let e1 = singleton "anode"
    let e2 = singleton "cathode"

    let t0 = mref e0
    let t1 = mref e1
    let t2 = mref e2
    
    let Phi = Components.get<Potential>() 
    let cH2 = Components.get<CH2>()
    let cO2 = Components.get<CO2>()
    let cOH = Components.get<COH>()
    let cNa = Components.get<CNa>()

    t1.Iter (fun u ->
        match u with
        | Octree.Boundary ->
            match t0.MapTo(pos u) with
            | Octree.Internal & Octree.Leaf _ as v ->
                let N_H2 =
                    let D = 1.0<m^2/s>
                    let z = 1.0                
                    let diffusion = -(1.0<mol/(m L)> * grad<CH2> (fun x -> double(x.c)) v) * D
                    let migration = -((z * D * F)/(R*T)) * cH2[!v].c * (1.0<V/m> * grad<Potential> (fun x -> double(x.p)) v) 
                    let convection = cH2[!v].c * 0.0<m/s>
                    diffusion + migration + convection

                let N_O2 =
                    let D = 1.0<m^2/s>
                    let z = 2.0                
                    let diffusion = -(1.0<mol/(m L)> * grad<CO2> (fun x -> double(x.c)) v) * D
                    let migration = -((z * D * F)/(R*T)) * cO2[!v].c * (1.0<V/m> * grad<Potential> (fun x -> double(x.p)) v) 
                    let convection = cO2[!v].c * 0.0<m/s>
                    diffusion + migration + convection

                let N_OH =
                    let D = 1.0<m^2/s>
                    let z = 2.0                
                    let diffusion = -(1.0<mol/(m L)> * grad<COH> (fun x -> double(x.c)) v) * D
                    let migration = -((z * D * F)/(R*T)) * cOH[!v].c * (1.0<V/m> * grad<Potential> (fun x -> double(x.p)) v) 
                    let convection = cOH[!v].c * 0.0<m/s>
                    diffusion + migration + convection

                let N_Na =
                    let D = 1.0<m^2/s>
                    let z = 2.0                
                    let diffusion = -(1.0<mol/(m L)> * grad<CNa> (fun x -> double(x.c)) v) * D
                    let migration = -((z * D * F)/(R*T)) * cNa[!v].c * (1.0<V/m> * grad<Potential> (fun x -> double(x.p)) v) 
                    let convection = cNa[!v].c * 0.0<m/s>
                    diffusion + migration + convection

                let i_e = F * (1.*N_H2 + 2.*N_O2 + 1.*N_OH + 2.*N_Na)
                !v
                |> set {ik = i_e}
                |> ignore

            | Octree.Boundary & Octree.Leaf _ as v' ->
                let v = v'[0,0,+1]
                let N_H2 =
                    let D = 1.0<m^2/s>
                    let z = 1.0                
                    let diffusion = -(1.0<mol/(m L)> * grad<CH2> (fun x -> double(x.c)) v) * D
                    let migration = -((z * D * F)/(R*T)) * cH2[!v].c * (1.0<V/m> * grad<Potential> (fun x -> double(x.p)) v) 
                    let convection = cH2[!v].c * 0.0<m/s>
                    diffusion + migration + convection

                let N_O2 =
                    let D = 1.0<m^2/s>
                    let z = 2.0                
                    let diffusion = -(1.0<mol/(m L)> * grad<CO2> (fun x -> double(x.c)) v) * D
                    let migration = -((z * D * F)/(R*T)) * cO2[!v].c * (1.0<V/m> * grad<Potential> (fun x -> double(x.p)) v) 
                    let convection = cO2[!v].c * 0.0<m/s>
                    diffusion + migration + convection

                let N_OH =
                    let D = 1.0<m^2/s>
                    let z = 2.0                
                    let diffusion = -(1.0<mol/(m L)> * grad<COH> (fun x -> double(x.c)) v) * D
                    let migration = -((z * D * F)/(R*T)) * cOH[!v].c * (1.0<V/m> * grad<Potential> (fun x -> double(x.p)) v) 
                    let convection = cOH[!v].c * 0.0<m/s>
                    diffusion + migration + convection

                let N_Na =
                    let D = 1.0<m^2/s>
                    let z = 2.0                
                    let diffusion = -(1.0<mol/(m L)> * grad<CNa> (fun x -> double(x.c)) v) * D
                    let migration = -((z * D * F)/(R*T)) * cNa[!v].c * (1.0<V/m> * grad<Potential> (fun x -> double(x.p)) v) 
                    let convection = cNa[!v].c * 0.0<m/s>
                    diffusion + migration + convection

                let i_e = F * (1.*N_H2 + 2.*N_O2 + 1.*N_OH + 2.*N_Na)
                !v
                |> set {ik = i_e}
                |> ignore
            | _ -> ()

        | _ -> ()
        
    )


// solve nabla i_e = 0
let solve_current_density () =
    let e0 = singleton "electrolyte"
    let e1 = singleton "anode"
    let e2 = singleton "cathode"

    let t0 = mref e0
    let t1 = mref e1
    let t2 = mref e2
    let ik = Components.get<Ik>()

    t0.IterParallel 4 (fun u ->
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
            ()

            ik[!u] <- {ik = 
                ((x1*x2)/(x2-x1)) * ((x2/(x1*(x1+x2)))*ik[!i].ik - (x1/(x2*(x1+x2)))*ik[!i'].ik) +
                ((y1*y2)/(y2-y1)) * ((y2/(y1*(y1+y2)))*ik[!j].ik - (y1/(y2*(y1+y2)))*ik[!j'].ik) +
                ((z1*z2)/(z2-z1)) * ((z2/(z1*(z1+z2)))*ik[!l].ik - (z1/(z2*(z1+z2)))*ik[!l'].ik) 
            }
            
        | Octree.Boundary -> ()
        | _ -> ()
    )

system OnUpdate [] (fun _ ->
    if update_count > 100 then Systems.quit()

    if update_bool then
        update_bool <- false
        task_new (fun _ ->
            let e0 = singleton "electrolyte"
            let e1 = singleton "anode"
            let e2 = singleton "cathode"

            let t0 = mref e0
            let t1 = mref e1
            let t2 = mref e2

            try
                overpotential ()
            with
                | e -> printfn "overpotential %s" e.Message
            try
                Nerst_Planck ()
            with
                | e -> printfn "Nerst-Planck %s" e.Message
            try
                solve_current_density ()
            with
                | e -> printfn "Solve %s" e.Message

            Helpers.octreeToBuffer<Potential> colorbars[Entity.get<Colormap> e0] (fun p -> double p.p) e0
            let electrolyte_count = t0.GetCount()
            e0 |> Entity.add<UpdateColors> |> set electrolyte_count |> ignore

            update_bool <- true
            update_count <- update_count + 1
        ) |> ignore
)

Systems.progress()

