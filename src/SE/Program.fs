open SE
open SE.Core
open SE.Spatial
open SE.Renderer
open SE.Plotting
open System
open System.Numerics
open System.Runtime.InteropServices
open System.Runtime.CompilerServices


let [<Literal>] N = 300
let [<Literal>] L = 10
let [<Literal>] k = 5
let [<Literal>] max_iter = 200
printfn "N: %d, k: %d, max_iter: %d" N k max_iter

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

let points = ResizeArray<Vector3>(1000)
let bounds = ResizeArray<Vector3>(1000)

// tree.Iter (fun node ->
//     match node with
//     | Octree.Internal -> points.Add(Octree.center node)
//     | Octree.Boundary -> bounds.Add(Octree.center node)
//     | _ -> ()
// )

tree_soa.Iter (fun x t ->
    match struct(x,t) with
    | OctreeSOA_2.Internal -> points.Add(t.Center(x))
    | OctreeSOA_2.Boundary -> bounds.Add(t.Center(x))
    | _ -> ()
)

for i in 1..max_iter do
    tree_soa.Iter (fun x t -> 
        match struct(x,t) with
        | OctreeSOA_2.Internal ->
            let a = t[x, 0,0,0]
            let b = t[x,-1,0,0]
            let d = t[x,+1,0,0]
            let dx = double (t.Center(d) - t.Center(b)).X
            if t[d].IsSome then ignore (t[d].Value) else ()
            if t[a].IsSome then ignore (t[d].Value) else ()
            if t[b].IsSome then ignore (t[d].Value) else ()
        | _ -> () 
    )
printfn "TREE_SOA\n\n"

for i in 1..max_iter do
    tree.Iter (fun x ->
        match x with
        | Octree.Internal ->
            let a = x[ 0,0,0]
            let b = x[-1,0,0]
            let d = x[+1,0,0]
            let va = Octree.center a
            let vb = Octree.center b
            let vd = Octree.center d
            let dx = double (vd - vb).X
            ignore (tree[double vd.X, double vd.Y, double vd.Z])
            ignore (tree[double va.X, double va.Y, double va.Z])
            ignore (tree[double vb.X, double vb.Y, double vb.Z])
        | _ -> ()
    )
printfn "TREE\n\n"

