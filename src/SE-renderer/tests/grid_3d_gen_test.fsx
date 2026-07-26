#r "../bin/Debug/net10.0/SE-core.dll"
#r "../bin/Debug/net10.0/SE-renderer.dll"

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

let path = System.Environment.GetCommandLineArgs()[2]
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

#time
let tree_soa = OctreeSOA_2.ofSurface<double> N L k (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
printfn "nodes.len:      %d" (tree_soa.GetCount())
printfn "nodes.total:    %d" (tree_soa.GetTotalCount())
printfn "internal.count: %d" (tree_soa.GetInternalCount())
printfn "boundary.count: %d" (tree_soa.GetBoundaryCount())
#time
printfn "TREE_SOA\n\n"

// exit 0

#time
let tree = Octree.ofSurface<double> N L k (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
printfn "nodes.len:      %d" (tree.GetCount())
printfn "nodes.total:    %d" (tree.GetTotalCount())
printfn "internal.count: %d" (tree.GetInternalCount())
printfn "boundary.count: %d" (tree.GetBoundaryCount())
#time
printfn "TREE\n\n"


mesh.vertices.Dispose()
mesh.indices.Dispose()

exit 0

let points = ResizeArray<Vector3>(1000)
let bounds = ResizeArray<Vector3>(1000)

// #time
// tree.Iter (fun node ->
//     match node with
//     | Octree.Internal -> points.Add(Octree.center node)
//     | Octree.Boundary -> bounds.Add(Octree.center node)
//     | _ -> ()
// )
// #time

// #time
// tree_soa.Iter (fun id ->
//     match struct(id,tree_soa) with
//     | OctreeSOA_2.Internal -> points.Add(OctreeSOA_2.center tree_soa id)
//     | OctreeSOA_2.Boundary -> bounds.Add(OctreeSOA_2.center tree_soa id)
//     | _ -> ()
// )
// #time

let pts = points.ToArray()
let bds = bounds.ToArray()
// let pts = tree.AsPoints()
let xs = pts |> Array.map (fun v -> double v.X)
let ys = pts |> Array.map (fun v -> double v.Y)
let zs = pts |> Array.map (fun v -> double v.Z)
let xb = bds |> Array.map (fun v -> double v.X)
let yb = bds |> Array.map (fun v -> double v.Y)
let zb = bds |> Array.map (fun v -> double v.Z)

// exit 0

Gnuplot()
|> Gnuplot.datablockXYZ xs ys zs "points"
|> Gnuplot.datablockXYZ xb yb zb "bounds"
|>> "unset key"
|>> "set view equal xyz"
|>> "splot $points with points lc rgb 'red', \\"
|>> "$bounds with points lc rgb 'black'"
|> Gnuplot.run
|> ignore

Console.ReadKey()


