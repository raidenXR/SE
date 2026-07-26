open SE
open SE.Core
open SE.Spatial
open SE.Renderer
open SE.Plotting
open System
open System.Numerics
open System.Runtime.InteropServices
open System.Runtime.CompilerServices


let [<Literal>] N = 500
let [<Literal>] L = 10
let [<Literal>] k = 5
printfn "N: %d, k: %d" N k

let path = "./bun_zipper.ply"
let gltf = if path.Contains(".gltf") then Some (new GLTF.Deserializer(path)) else None

// rotate mesh for testing
let rotation =
    Quaternion.CreateFromYawPitchRoll(2.f, 4.f, 3.f)
    |> Matrix4x4.CreateFromQuaternion

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


printfn "Flag: %d" (sizeof<OctreeSOA_2.Flag>)
printfn "NodeId:   %d" (sizeof<OctreeSOA_2.NodeId>)
printfn "Data<'T>: %d" (sizeof<OctreeSOA_2.Data<double>>)

let tree_soa = OctreeSOA_2.ofSurface<double> N L k (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
printfn "nodes.len:      %d" (tree_soa.GetCount())
printfn "nodes.total:    %d" (tree_soa.GetTotalCount())
printfn "internal.count: %d" (tree_soa.GetInternalCount())
printfn "boundary.count: %d" (tree_soa.GetBoundaryCount())
printfn "TREE_SOA\n\n"


let tree = Octree.ofSurface<double> N L k (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
printfn "nodes.len:      %d" (tree.GetCount())
printfn "nodes.total:    %d" (tree.GetTotalCount())
printfn "internal.count: %d" (tree.GetInternalCount())
printfn "boundary.count: %d" (tree.GetBoundaryCount())
printfn "TREE\n\n"


mesh.vertices.Dispose()
mesh.indices.Dispose()

printfn "done!"


