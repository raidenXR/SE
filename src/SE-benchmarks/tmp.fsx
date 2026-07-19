#r "../bin/Release/net10.0/SE-core.dll"
#r "../bin/Release/net10.0/SE-renderer.dll"

open System
open System.Numerics
open SE.Core
open SE.ECS
open SE.Renderer
open SE.Plotting

open System.Runtime.CompilerServices
open System.Runtime.InteropServices

let path = "../bun_zipper.ply"
let gltf: option<GLTF.Deserializer> = None
let N = 500
let L = 10

let mesh =
    match gltf with
    | Some gltf -> gltf.ReadMeshF(0)
    | None -> RGeometry.load_ply_unmanaged (path, 0.55f, 0.55f, 0.53f, 1.0f)


let tree1 = OctreeExperimental.ofSurface<double> N L 4 (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
let tree2 = Octree.ofSurface<double> N L 4 (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())

#time
tree1.IterParallel 4 (fun node ->
    // ()
    match node with
    | OctreeExperimental.Internal -> node.value <- ValueSome 150.0
    | OctreeExperimental.Boundary -> node.value <- ValueSome 300.0
    | OctreeExperimental.External -> ()
)
#time

#time
tree2.IterParallel 4 (fun node ->
    // ()
    match node with
    | Octree.Internal -> node |> function | Octree.Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome 150.0 | _ -> ()
    | Octree.Boundary -> node |> function | Octree.Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome 300.0 | _ -> ()
    | Octree.External -> ()
)
#time

#time
printfn "octree_experimental.count:       %d" (tree1.GetCount())
printfn "octree_experimental.count_total: %d" (tree1.GetTotalCount())
printfn "octree_experimental.internal:    %d" (tree1.GetInternalCount())
printfn "octree_experimental.boundary:    %d" (tree1.GetBoundaryCount())
#time

#time
printfn "\n"
printfn "octree.count:       %d" (tree2.GetCount())
printfn "octree.count_total: %d" (tree2.GetTotalCount())
printfn "octree.internal:    %d" (tree2.GetInternalCount())
printfn "octree.boundary:    %d" (tree2.GetBoundaryCount())
#time

exit 0

let points = ResizeArray<Vector3>(1000)
let bounds = ResizeArray<Vector3>(1000)

tree1.IterParallel 1 (fun node ->
    match node with
    | OctreeExperimental.Internal -> points.Add(OctreeExperimental.center node)
    | OctreeExperimental.Boundary -> bounds.Add(OctreeExperimental.center node)
    | _ -> ()
)

let pts = points.ToArray()
let bds = bounds.ToArray()
// let pts = tree.AsPoints()
let xs = pts |> Array.map (fun v -> double v.X)
let ys = pts |> Array.map (fun v -> double v.Y)
let zs = pts |> Array.map (fun v -> double v.Z)
let xb = bds |> Array.map (fun v -> double v.X)
let yb = bds |> Array.map (fun v -> double v.Y)
let zb = bds |> Array.map (fun v -> double v.Z)

printfn "nodes.len: %d" (tree1.GetCount())
printfn "internal.count: %d" (tree1.GetInternalCount())
printfn "boundary.count: %d" (tree1.GetBoundaryCount())
printfn "internal.count: %d" (points.Count)
printfn "boundary.count: %d" (bounds.Count)

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


