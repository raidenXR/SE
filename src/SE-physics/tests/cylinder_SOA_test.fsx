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
open SE.Plotting
open System
open System.Runtime.InteropServices
open System.Runtime.CompilerServices
open FSharp.NativeInterop
open OctreeFlatten

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
type [<Struct>] Velocity = {u:Vector3}

type [<Measure>] L  

type [<Struct>] Phi = {p:double<V>}
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
    Colormap.Jet, new Colorbar(Colormap.Jet, 0., 0.5)
    Colormap.Gray, new Colorbar(Colormap.Gray, 0., 0.5)
]

let mesh_names =
    ResizeArray<string>()

// mutable global variables
let mutable update_count = 0
let mutable update_bool = true
let mutable wireframe_on = false

let inline vec3 (v:Vector3) = System.Numerics.Vector3(v.X, v.Y, v.Z)

// rendering trait
let solid = prefab3 (IBody()) UpdateColors (Matrix4.CreateScale scale)
let fluid = prefab3 (IPoints()) UpdateColors (Matrix4.CreateScale scale)

let descretized (tree:Octree<Entity>) e = e |> set ITree |> Entity.addRef tree |> set (tree.GetCount())
let visual VT mesh e = e |> set (VertexBuffer.create VT mesh) |> set mesh |> set true
let mref = Entity.getRef<Octree<Entity>>

let grad<'T> (conv:'T->double) (t:Octree<Entity>) ni =
    let C = Components.get<'T>()
    
    let u  = t[ni,  0,0,0]
    let i  = t[ni, -1,0,0]
    let i' = t[ni, +1,0,0]
    let j  = t[ni, 0,-1,0]
    let j' = t[ni, 0,+1,0]
    let l  = t[ni, 0,0,-1]
    let l' = t[ni, 0,0,+1]

    let x1 = double (u.Center - i.Center).X
    let y1 = double (u.Center - j.Center).Y
    let z1 = double (u.Center - l.Center).Z

    let x2 = double (i'.Center - u.Center).X
    let y2 = double (j'.Center - u.Center).Y
    let z2 = double (l'.Center - u.Center).Z

    let Cx = (-x2)/(x1*(x1+x2))*conv C[i.value] + (x2-x1)/(x1*x2)*conv C[u.value] + x1/(x2*(x1 + x2))*conv C[i'.value]
    let Cy = (-y2)/(y1*(y1+y2))*conv C[j.value] + (y2-y1)/(y1*y2)*conv C[u.value] + y1/(y2*(y1 + y2))*conv C[j'.value]
    let Cz = (-z2)/(z1*(z1+z2))*conv C[l.value] + (z2-z1)/(z1*z2)*conv C[u.value] + z1/(z2*(z1 + z2))*conv C[l'.value]
    Cx + Cy + Cz

let octreeToBuffer<'T> (colorbar:Colorbar) conv e =
    let mesh = Entity.get<Mesh> e
    let tree = Entity.getRef<Octree<Entity>> e
    let T = Components.get<'T>()
    let L = mesh.L
    match L with
    | 7 ->
        let vertices = mesh.vertices.AsSpan()
        let mutable i = 0
        for u in tree.Nodes do
            if u.flag = Flag.Leaf then
                let p = u.Center
                let c = colorbar[conv T[u.value]]
                vertices[i*L + 0] <- p.X
                vertices[i*L + 1] <- p.Y
                vertices[i*L + 2] <- p.Z
                vertices[i*L + 3] <- c.X
                vertices[i*L + 4] <- c.Y
                vertices[i*L + 5] <- c.Z
                vertices[i*L + 6] <- c.W
                i <- i + 7        
    | 10 ->
        let vertices = mesh.vertices.AsSpan()
        let v = &MemoryMarshal.GetReference(vertices)
        let len = mesh.vertices.Length / L
        for i in 0..len-1 do
            let p = Unsafe.As<float32,Numerics.Vector3>(&Unsafe.Add(&v, i*L))
            let n = Unsafe.As<float32,Numerics.Vector3>(&Unsafe.Add(&v, i*L + 3))
            let u = tree.MapTo(p)
            let c = match u.flag with
                    | Flag.Leaf -> colorbar[conv T[u.value]]
                    | _ -> Unsafe.As<float32,Numerics.Vector4>(&Unsafe.Add(&v, i*L + 6))

            vertices[i*L + 1] <- p.Y
            vertices[i*L + 0] <- p.X
            vertices[i*L + 2] <- p.Z
            vertices[i*L + 3] <- n.X
            vertices[i*L + 4] <- n.Y
            vertices[i*L + 5] <- n.Z
            vertices[i*L + 6] <- c.X
            vertices[i*L + 7] <- c.Y
            vertices[i*L + 8] <- c.Z
            vertices[i*L + 9] <- c.W
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
    while not update_bool do
        Threading.Thread.SpinWait(100)
    
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

            VertexBuffer.update_sliced vbuf[e] (7 * Entity.get<int> e) mesh[e]        
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
            VertexBuffer.draw_sliced v[e] (7 * Entity.get<int> e) m[e]
)

system PostRender [] (fun _ ->
    let wnd = SE_Window.Shared
    wnd.Update (wnd.Context.SwapBuffers)        
)


system OnLoad [] (fun _ ->
    let wnd = SE_Window.Shared
    use gltf = new GLTF.Deserializer(path)

    let meshes =
        gltf.ReadMeshesParallel()
    
    let (v_min,v_max) =
        meshes
        |> RGeometry.meshes_bounds
    
    let tree = 
        meshes[0]
        |> ofMesh<Entity> N k
        
    printfn "v_min: %A" v_min
    printfn "v_max: %A" v_max

    singleton "electrolyte"
    |> fluid |> visual VT2 (Mesh.points(N*N*N*7)) |> descretized tree |> set Colormap.Jet
    |> ignore
        
    singleton "anode"
    |> solid |> visual VT1 meshes[1] |> set Colormap.Gray
    |> ignore
        
    singleton "cathode"
    |> solid |> visual VT1 meshes[2] |> set Colormap.Gray
    |> ignore

    mesh_names.Add(sprintf "body_%d: %d/ %d" 0 (tree.GetInternalCount()) (tree.GetCount()))
    mesh_names.Add(sprintf "body_%d:" 1)
    mesh_names.Add(sprintf "body_%d:" 2)

    entity() |> set (colorbars[Colormap.Jet].AsTexture(0.8f, 0.0f, 120.f, 460.f)) |> ignore
    entity() |> set (colorbars[Colormap.Gray].AsTexture(0.8f, -0.8f, 120.f, 460.f)) |> ignore    

    let p = tree.Root.Center    
    wnd.Camera.Position <- Vector3(p.X, p.Y, p.Z)
    wnd.Camera.Speed <- wnd.Camera.Speed * 0.1f

    tree.Iter (fun t i -> t[i].value <- entity())
)

// guess potential and concetrations
system PostLoad [] (fun _ ->
    let e0 = singleton "electrolyte"
    let e1 = singleton "anode"
    let e2 = singleton "cathode"
    let t0 = mref e0
    let bits = t0.Stencil
    let v_min = t0.Vmin
    let v_max = t0.Vmax

    printfn "count: %d, internal: %d, boundary: %d" (t0.GetCount()) (t0.GetInternalCount()) (t0.GetBoundaryCount())

    for i in 0..N-1 do
        for j in 0..N-1 do
            for l in 0..3 do
                if bits[i*N*N + j*N + l] then
                    let p = GridGeneration3D.to_cartesian_system i j l N v_min v_max
                    t0.Insert(p)
    t0.AssignFlags()
                    
    e0
    |> set (t0.GetCount())
    |> ignore
    
    t0.Iter (fun t ni ->
        t[ni].value
        |> set {CH2.c = 0.0<mol/L>}
        |> set {CO2.c = 0.5e-3<mol/L>}
        |> set {COH.c = 0.5<mol/L>}
        |> set {CNa.c = 0.5<mol/L>}
        |> set {Phi.p = 0.0<V>}
        |> set {Ik.ik = 0.0<C m/L s>}
        |> set {I.i = 0.0<A/m^2>}
        |> set {Velocity.u = Vector3.Zero}
        |> ignore
    )
    
    octreeToBuffer<Phi> colorbars[Colormap.Jet] (fun p -> double p.p) e0
    VertexBuffer.update (Entity.get<VertexBuffer> e0) (Entity.get<Mesh> e0)          
)

system PostLoad [] (fun _ ->
    printfn "count: %d" (Components.get<Phi>().Count)
    printfn "count: %d" (Components.get<CH2>().Count)
    
    let e0 = singleton "electrolyte"
    let t0 = mref e0
    printfn "count: %d, internal: %d, boundary: %d" (t0.GetCount()) (t0.GetInternalCount()) (t0.GetBoundaryCount())
)

// compute the overpotential
let overpotential () =
    let e0 = singleton "electrolyte"
    let e1 = singleton "anode"
    let e2 = singleton "cathode"
    let t0 = mref e0
    let bits = t0.Stencil
    let v_min = t0.Vmin
    let v_max = t0.Vmax

    let Eeq = 0.0<V>
    let phi = Components.get<Phi>()

    for i in 0..N-1 do
        for j in 0..N-1 do
            let l = 0
            if bits[i*N*N + j*N + l] then
                let p = GridGeneration3D.to_cartesian_system i j l N v_min v_max
                let u = t0[p]

                if u.bounds = Bounds.Boundary then
                    let eta = Phi_s_anode - phi[u.value].p - Eeq
                    let i_HER = i0_anode * (exp((a_anode * F * eta)/(R * T)))
                    let N_H2 = -i_HER/(2.*F)
                    let N_OH = i_HER/F

                    u.value
                    |> set {i = i_HER}
                    |> set {p = eta}
                    |> ignore        


// evaluate the Butler-Volmer current density
let Butler_Volmer () = ()
    // let e0 = singleton "electrolyte"
    // let e1 = singleton "anode"
    // let e2 = singleton "cathode"

    // let t0 = mref e0
    // let t1 = mref e1
    // let t2 = mref e2
    
    // let Phi = Components.get<Potential>() 
    // t1.Iter (fun t i ->
    //     let u = t[i]
    //     match u.bounds with
    //     | Bounds.Boundary ->
    //         let eta = Phi[u.value].p
    //         let i_HER = i0_anode * (exp((a_anode * F * eta)/(R * T)))
    //         let v = t0.MapTo(u.Center)

    //         let N_H2 = -i_HER/(2.*F)
    //         let N_OH = i_HER/F

    //         v.value
    //         |> set {i = i_HER}
    //         |> ignore            

    //     | _ -> ()        
    // )

// Convert this current into flux boundary conditions.
system OnUpdate [] (fun _ ->
    ()
)

// Solve the Nernst-Planck transport equations.
let Nerst_Planck () =
    let e0 = singleton "electrolyte"
    let e1 = singleton "anode"
    let e2 = singleton "cathode"
    let t0 = mref e0
    let bits = t0.Stencil
    let v_min = t0.Vmin
    let v_max = t0.Vmax
    
    let phi = Components.get<Phi>() 
    let cH2 = Components.get<CH2>()
    let cO2 = Components.get<CO2>()
    let cOH = Components.get<COH>()
    let cNa = Components.get<CNa>()

    let nodes = t0.Nodes

    for I in 0..N-1 do
        for J in 0..N-1 do
            let L = 1
            if bits[I*N*N + J*N + L] then
                let p = GridGeneration3D.to_cartesian_system I J L N v_min v_max
                let i = t0.Traverse(p)
                let u = nodes[i] 

                if u.bounds = Bounds.Internal then
                    let N_H2 =
                        let D = 1.0<m^2/s>
                        let z = 1.0                
                        let diffusion = -(1.0<mol/(m L)> * grad<CH2> (fun x -> double(x.c)) t0 i) * D
                        let migration = -((z * D * F)/(R*T)) * cH2[u.value].c * (1.0<V/m> * grad<Phi> (fun x -> double(x.p)) t0 i) 
                        let convection = cH2[u.value].c * 0.0<m/s>
                        diffusion + migration + convection

                    let N_O2 =
                        let D = 1.0<m^2/s>
                        let z = 2.0                
                        let diffusion = -(1.0<mol/(m L)> * grad<CO2> (fun x -> double(x.c)) t0 i) * D
                        let migration = -((z * D * F)/(R*T)) * cO2[u.value].c * (1.0<V/m> * grad<Phi> (fun x -> double(x.p)) t0 i) 
                        let convection = cO2[u.value].c * 0.0<m/s>
                        diffusion + migration + convection

                    let N_OH =
                        let D = 1.0<m^2/s>
                        let z = 2.0                
                        let diffusion = -(1.0<mol/(m L)> * grad<COH> (fun x -> double(x.c)) t0 i) * D
                        let migration = -((z * D * F)/(R*T)) * cOH[u.value].c * (1.0<V/m> * grad<Phi> (fun x -> double(x.p)) t0 i) 
                        let convection = cOH[u.value].c * 0.0<m/s>
                        diffusion + migration + convection

                    let N_Na =
                        let D = 1.0<m^2/s>
                        let z = 2.0                
                        let diffusion = -(1.0<mol/(m L)> * grad<CNa> (fun x -> double(x.c)) t0 i) * D
                        let migration = -((z * D * F)/(R*T)) * cNa[u.value].c * (1.0<V/m> * grad<Phi> (fun x -> double(x.p)) t0 i) 
                        let convection = cNa[u.value].c * 0.0<m/s>
                        diffusion + migration + convection

                    let i_e = F * (1.*N_H2 + 2.*N_O2 + 1.*N_OH + 2.*N_Na)
                    u.value
                    |> set {ik = i_e}
                    |> ignore


// solve nabla i_e = 0
let solve_current_density () =
    let e0 = singleton "electrolyte"
    let e1 = singleton "anode"
    let e2 = singleton "cathode"
    let t0 = mref e0

    let ik = Components.get<Ik>()

    t0.IterParallel (fun t ni ->
        match t[ni].bounds with
        | Bounds.Internal -> 
            let u  = t[ni,  0,0,0]
            let i  = t[ni, -1,0,0]
            let i' = t[ni, +1,0,0]
            let j  = t[ni, 0,-1,0]
            let j' = t[ni, 0,+1,0]
            let l  = t[ni, 0,0,-1]
            let l' = t[ni, 0,0,+1]

            let x1 = double (u.Center - i.Center).X
            let y1 = double (u.Center - j.Center).Y
            let z1 = double (u.Center - l.Center).Z

            let x2 = double (i'.Center - u.Center).X
            let y2 = double (j'.Center - u.Center).Y
            let z2 = double (l'.Center - u.Center).Z
            ()

            ik[u.value] <- { ik = 
                ((x1*x2)/(x2-x1)) * ((x2/(x1*(x1+x2)))*ik[i.value].ik - (x1/(x2*(x1+x2)))*ik[i'.value].ik) +
                ((y1*y2)/(y2-y1)) * ((y2/(y1*(y1+y2)))*ik[j.value].ik - (y1/(y2*(y1+y2)))*ik[j'.value].ik) +
                ((z1*z2)/(z2-z1)) * ((z2/(z1*(z1+z2)))*ik[l.value].ik - (z1/(z2*(z1+z2)))*ik[l'.value].ik) 
            }
            
        | Bounds.Boundary -> ()
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

            octreeToBuffer<Phi> colorbars[Entity.get<Colormap> e0] (fun v -> double v.p) e0
            let electrolyte_count = t0.GetCount()
            e0 |> Entity.add<UpdateColors> |> set electrolyte_count |> ignore

            update_bool <- true
            update_count <- update_count + 1
        ) |> ignore
)

system OnExit [] (fun _ ->
    let e0 = singleton "electrolyte"
    let e1 = singleton "anode"
    let e2 = singleton "cathode"
    let t0 = mref e0
    let bits = t0.Stencil
    let v_min = t0.Vmin
    let v_max = t0.Vmax

    let ik = Components.get<Ik>()
    let values = ResizeArray<double>()

    let i = N/2
    let j = N/2
    for l in 0..N-1 do
        if bits[i*N*N + j*N + l] then
            let p = GridGeneration3D.to_cartesian_system i j l N v_min v_max
            let u = t0[p]
            if Entity.has<Phi> u.value then
                let v = double ik[u.value].ik
                values.Add(v)
                printfn "ik: %g" v

    printfn "values.count: %d" (values.Count)

    Gnuplot()
    |> Gnuplot.datablockX (values.ToArray()) "points"
    |>> "set term pngcairo size 800,600"
    |>> "set out 'plot.png'"
    |>> "plot $points"
    |> Gnuplot.run
    |> ignore
)

Systems.progress ()

