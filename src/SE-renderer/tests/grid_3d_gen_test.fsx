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
let N = 50
let L = 10

let mesh =
    match gltf with
    | Some gltf -> gltf.ReadMeshF(0)
    | None -> RGeometry.load_ply_unmanaged (path, 0.55f, 0.55f, 0.53f, 1.0f)


let (v_min,v_max) = GridGeneration3D.bounds_SIMD (mesh.vertices.AsSpan()) L
let (vs,stencil) = 
    System.Collections.BitArray(N*N*N)
    |> Octree.fill_raycast N L v_min v_max (mesh.vertices.AsSpan()) (mesh.indices.AsSpan()) 


let tree = Octree.ofStencil<double> N 3 v_min v_max stencil
let pts = tree.AsPoints()
let xs = pts |> Array.map (fun v -> double v.X)
let ys = pts |> Array.map (fun v -> double v.Y)
let zs = pts |> Array.map (fun v -> double v.Z)

printfn "nodes.len: %d" (tree.GetCount())

Gnuplot()
|> Gnuplot.datablockXYZ xs ys zs "points"
|>> "set view equal xyz"
|>> "splot $points with points lc rgb 'black'"
|> Gnuplot.run
|> ignore

Console.ReadKey()


