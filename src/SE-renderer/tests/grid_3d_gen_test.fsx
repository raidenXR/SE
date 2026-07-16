#r "../bin/Debug/net10.0/SE-core.dll"
#r "../bin/Debug/net10.0/SE-renderer.dll"

open SE
open SE.Core
open SE.Renderer
open SE.Plotting
open System
open System.Numerics


// let path = "../models/animated_object.gltf"
let path = "../models/bun_zipper.ply"
// let gltf: option<GLTF.Deserializer> = Some (new GLTF.Deserializer(path))
let gltf: option<GLTF.Deserializer> = None
let N = 200
let L = 10

let mesh =
    match gltf with
    | Some gltf -> gltf.ReadMeshF(0)
    | None -> RGeometry.load_ply_unmanaged (path, 0.55f, 0.55f, 0.53f, 1.0f)

let tree = Octree.ofSurface<double> N L 4 (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
let points = ResizeArray<Vector3>(1000)
let bounds = ResizeArray<Vector3>(1000)

tree.IterParallel 1 (fun node ->
    match node with
    | Octree.Internal -> points.Add(Octree.center node)
    | Octree.Boundary -> bounds.Add(Octree.center node)
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

printfn "nodes.len: %d" (tree.GetCount())
// printfn "internal.count: %d" (tree.GetInternalCount())
// printfn "boundary.count: %d" (tree.GetBoundaryCount())
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


