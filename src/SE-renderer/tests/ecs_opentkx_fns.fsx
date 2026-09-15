// #r "../bin/Debug/net10.0/SE-renderer.dll"
// #r "../bin/Debug/net10.0/SE-core.dll"
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


type [<Struct>] Temperature   = Temperature of float
type [<Struct>] Thickness     = Thickness of float
type [<Struct>] Concentration = Concentration of float

type IsPoints = struct end
type UpdateColors = struct end
type IsTexture = struct end
type TextureString = struct end


let inline (!) (u:Octree.Node<'T>) = Octree.valueof u
let inline Tf32 (Temperature T) = T
let inline vec3 (v:Vector3) = System.Numerics.Vector3(v.X, v.Y, v.Z)
let pos = Octree.center

[<Extension>]
type EntityExtensions =
    [<Extension>]
    static member inline Has<'T>(e:Entity) = Entity.has<'T> e


// prefab example
let is_electrolyte_cv = prefab2 (Temperature 300.) (Concentration 3.2)
let is_electrode_cv = prefab2 (Temperature 600.) (Thickness (3.2e-6))
let is_electrode_2 = prefab (Temperature 370.)
let is_electrode_3 = prefab (Temperature 480.)

let from_yaw_pitch_roll = System.Numerics.Quaternion.CreateFromYawPitchRoll
let from_quaternion = System.Numerics.Matrix4x4.CreateFromQuaternion


let octree_to_buffer (tree:Octree.Root<Entity>) (colorbar:Colorbar) (mesh:Mesh) =
    let T = Components.get<Temperature>()
    let mutable i = 0
    tree.Iter (fun u ->
        match u with
        // | Octree.Internal | Octree.Boundary ->
        | Octree.Internal ->
            let vertices = mesh.vertices.AsSpan()
            if (i*mesh.L+6) >= vertices.Length then printfn "i: %d, tree_len: %d" i (tree.GetInternalCount())
            let p = pos u
            let c = colorbar[Tf32 T[!u]]
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

let load_resources (tree:SE.Spatial.Octree.Root<_>) ss =
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

    SE_UI.Shared.OnLoad(wnd) |> ignore

    printfn "resouces initialization load"
    

let load_multiple_meshes N k colorbar_jet (scale:float32) (models_count:ResizeArray<int*int>) = 
    let gltf_path = "../../../resources/models/electr_cell.gltf"
    let rotation =
        match gltf_path with
        | GLTF.IsTxt -> System.Numerics.Quaternion.CreateFromYawPitchRoll(2.f, 2.f, 1.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
        | GLTF.IsPly -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.f, 0.f, 0.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
        | GLTF.IsGltf -> System.Numerics.Quaternion.CreateFromYawPitchRoll(2.f, 4.f, 3.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
        | _ -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.f, 0.f, 0.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        

    use glrf = new GLTF.Deserializer(gltf_path)
    let models = glrf.ReadMeshes()
    let L = 7

    do // model 0
        let tree =
            models[0]
            |> RGeometry.tranform rotation
            |> Octree.ofMesh<Entity> N k
            
        let tree_len = tree.GetCount()
        let mutable vertices = NativeArray.create<float32> (tree_len*L)
        let indices  = NativeArray.empty<uint32>()

        let mutable i = 0

        tree.Iter (fun u ->
            match u with
            | Octree.Internal & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> is_electrode_2 |> ValueSome
            | Octree.Boundary & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> is_electrode_2 |> ValueSome
            | _ -> ()
        )

        let mesh = {vertices = vertices; indices = indices; L = L}
        models_count.Add(tree.GetInternalCount(), tree.GetCount())
        octree_to_buffer tree colorbar_jet mesh
    
        entity()
        |> Entity.add<IsPoints>
        |> set mesh
        |> set (VertexBuffer.create VT2 mesh)
        |> set (Matrix4.CreateScale(scale))
        |> ignore        
        
    do // model 1
        let tree =
            models[1]
            |> RGeometry.tranform rotation
            |> Octree.ofMesh<Entity> N k
            
        let tree_len = tree.GetCount()
        let mutable vertices = NativeArray.create<float32> (tree_len*L)
        let indices  = NativeArray.empty<uint32>()

        let mutable i = 0

        tree.Iter (fun u ->
            match u with
            | Octree.Internal & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> is_electrode_3 |> ValueSome
            | Octree.Boundary & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> is_electrode_3 |> ValueSome
            | _ -> ()
        )

        let mesh = {vertices = vertices; indices = indices; L = L}
        octree_to_buffer tree colorbar_jet mesh
        models_count.Add(tree.GetInternalCount(), tree.GetCount())
    
        entity()
        |> Entity.add<IsPoints>
        |> set mesh
        |> set (VertexBuffer.create VT2 mesh)
        |> set (Matrix4.CreateScale(scale))
        |> ignore        
    
let draw_meshes (q:Entities) = 
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

let draw_textures (q:Entities) =
    let t = Components.get<Texture>()
    let shader = Shaders.get("t_shader")

    for e in t.Entities do
        Texture.draw t[e] shader


let update_colors tree colorbar_jet (q:Entities) =
    for e in q do
        if Entity.has<UpdateColors> e then
            let mesh = Entity.get<Mesh> e
            let vbuf = Entity.get<VertexBuffer> e
            octree_to_buffer tree colorbar_jet mesh
            VertexBuffer.update vbuf mesh
            
            e |> Entity.remove<UpdateColors> |> ignore

let clear_resources () = 
    for vb in Components.get<VertexBuffer>().Entries do
        VertexBuffer.delete vb        
        
    for mesh in Components.get<Mesh>().Entries do
        mesh.Dispose()
        
    for texture in Components.get<Texture>().Entries do
        Texture.delete texture

    Shaders.unload()
    SE_Window.Shared.Dispose()
    SE_UI.Shared.OnClosed()

    
