#r "../bin/Debug/net10.0/SE-core.dll"
#r "../bin/Debug/net10.0/SE-renderer.dll"
// #r "../bin/Release/net10.0/SE-core.dll"
// #r "../bin/Release/net10.0/SE-renderer.dll"

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
let [<Literal>] k = 3
let [<Literal>] max_iter = 300
printfn "N: %d, k: %d, max_iter: %d" N k max_iter

let path =
    System.Environment.GetCommandLineArgs()[2]

// rotate mesh for testing
// let rotation =
//     Quaternion.CreateFromYawPitchRoll(2.f, 4.f, 3.f)
//     |> Matrix4x4.CreateFromQuaternion

let mesh =
    RGeometry.load_model path
    // |> RGeometry.tranform rotation


#time
let tree0 = Octree.ofMesh<double> N k mesh
printfn "nodes.len:      %d" (tree0.GetCount())
printfn "nodes.total:    %d" (tree0.GetTotalCount())
printfn "internal.count: %d" (tree0.GetInternalCount())
printfn "boundary.count: %d" (tree0.GetBoundaryCount())
#time
printfn "TREE\n\n"
GC.Collect()

#time
let tree1 = OctreeFlatten.ofMesh<double> N k mesh
tree1.AssignFlags()
printfn "nodes.len:      %d" (tree1.GetCount())
printfn "nodes.total:    %d" (tree1.GetTotalCount())
printfn "internal.count: %d" (tree1.GetInternalCount())
printfn "boundary.count: %d" (tree1.GetBoundaryCount())
printfn "capacity: %d" (tree1.Capacity)
printfn "total_capacity %d" (tree1.BufferSize)
#time
printfn "TREE_flatten\n\n"
GC.Collect()

// exit 0

// #time
// let tree2 = OctreeFlatten.ofMesh_2<double> N k mesh
// printfn "nodes.len:      %d" (tree2.GetCount())
// printfn "nodes.total:    %d" (tree2.GetTotalCount())
// printfn "internal.count: %d" (tree2.GetInternalCount())
// printfn "boundary.count: %d" (tree2.GetBoundaryCount())
// printfn "capacity: %d" (tree1.Capacity)
// printfn "total_capacity %d" (tree1.BufferSize)
// #time
// printfn "TREE_flatten_2\n\n"
// GC.Collect()


#time
for _ in 1..100 do
    tree0.IterParallel 4 (fun u ->
        ()
    )
#time
GC.Collect()

#time
for _ in 1..100 do
    tree1.IterParallel (fun t ni ->
        ()
    )
#time
GC.Collect()

// #time
// for _ in 1..100 do
//     tree2.IterParallel (fun t ni ->
//         ()
//     )
// #time
GC.Collect()

exit 0

// mesh.vertices.Dispose()
// mesh.indices.Dispose()

// let pts = tree0.AsPoints()
// let xs = pts |> Array.map (fun v -> double v.X)
// let ys = pts |> Array.map (fun v -> double v.Y)
// let zs = pts |> Array.map (fun v -> double v.Z)
// let xb = bds |> Array.map (fun v -> double v.X)
// let yb = bds |> Array.map (fun v -> double v.Y)
// let zb = bds |> Array.map (fun v -> double v.Z)

Gnuplot()
|> Gnuplot.datablockVec3 (tree0.AsPoints()) "points"
// |> Gnuplot.datablockXYZ xb yb zb "bounds"
|>> "unset key"
|>> "set view equal xyz"
|>> "splot $points with points lc rgb 'black'"
// |>> "splot $points with points lc rgb 'red', \\"
// |>> "$bounds with points lc rgb 'black'"
|> Gnuplot.run
|> ignore

Gnuplot()
|> Gnuplot.datablockVec3 (tree1.AsPoints()) "points"
// |> Gnuplot.datablockXYZ xb yb zb "bounds"
|>> "unset key"
|>> "set view equal xyz"
|>> "splot $points with points lc rgb 'black'"
// |>> "splot $points with points lc rgb 'red', \\"
// |>> "$bounds with points lc rgb 'black'"
|> Gnuplot.run
|> ignore

Console.ReadKey()


