#r "../bin/Debug/net10.0/SE-core.dll"
#r "../bin/Debug/net10.0/SE-renderer.dll"

open SE
open SE.Core
open SE.Renderer
open SE.Plotting
open System.Numerics


// let path = "../models/animated_object.gltf"
let path = "../models/bun_zipper.ply"
// let gltf: option<GLTF.Deserializer> = Some (new GLTF.Deserializer(path))
let gltf: option<GLTF.Deserializer> = None
let N = 100
let L = 10

let mesh =
    match gltf with
    | Some gltf -> gltf.ReadMeshF(0)
    | None -> RGeometry.load_ply_unmanaged (path, 0.55f, 0.55f, 0.53f, 1.0f)


let (v_min,v_max) = GridGeneration3D.bounds_SIMD (mesh.vertices.AsSpan()) L
printfn "%A, %A" v_min v_max


#time
let stencil = 
    // GridGeneration3D.bitstencil vertices indices v_min v_max N
    System.Collections.BitArray(N*N*N)
    |> GridGeneration3D.assign_voxels_SIMD (mesh.vertices.AsSpan()) (mesh.indices.AsSpan()) L N 
    |> GridGeneration3D.fill_bitstencil N
    
let octree =
    stencil
    // System.Collections.BitArray(N*N*N)
    // |> GridGeneration3D.assign_voxels_SIMD (mesh.vertices.AsSpan()) (mesh.indices.AsSpan()) L N 
    // |> GridGeneration3D.fill_bitstencil N
    |> Octree.ofStencil<double> N 5 v_min v_max
    |> Octree.init 0.

do
    let mutable _c = 0
    for i in 0..N-1 do
        for j in 0..N-1 do
            for k in 0..N-1 do
                if stencil[i*N*N+j*N+k] then _c <- _c + 1
    printfn "total_bits: %d" _c

printfn "nodes: %d" (octree.GetCount())
printfn "total_nodes: %d" (octree.GetTotalCount())
#time

mesh.vertices.Dispose()
mesh.indices.Dispose()
// exit 0

let sb = System.Text.StringBuilder(1024*1024)
let points = octree.AsPoints()
let rects  = octree.AsPolygons(fun d -> float32 d)

points |> Array.iter (fun v -> ignore (sb.AppendLine($"{v.X}  {v.Y}  {v.Z}")))

Gnuplot()
|> Gnuplot.datablockString (string sb) "centers"
// |>> "set size ratio -1"
|>> "set view equal xyz"
// |>> "splot $centers using 1:2:3 with polygons fc rgb 'black'"
|>> "splot $centers using 1:2:3 with points lc rgb 'black'"
|> Gnuplot.run
|> ignore

System.Console.ReadKey()


