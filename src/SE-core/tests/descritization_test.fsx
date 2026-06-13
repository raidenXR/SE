#load "../src/unsafe.fs"
#load "../src/trees.fs"
#load "../src/gnuplot.fs"

open System
open SE
open SE.Core
open GridGeneration2D
open Plotting

let path = "coordinates_dense.dat"
let output = "mesh_vertices.dat"
let quadout = "quad_vertices.dat"

let N = 8*20

let domain = read_from_file path
let (v_min,v_max) = bounds domain
let quadtree = Quadtree2.Root<double>(N, v_min, v_max)

let stencil =
    bitstencil domain N
    |> fill_bitstencil N v_min v_max

let mutable total_get_bits = 0
for i in 0..N-1 do
    for j in 0..N-1 do
        let v = to_cartesian_system i j N v_min v_max
        if stencil[i*N+j] then
            total_get_bits <- total_get_bits + 1
            quadtree[double v.X, double v.Y] <- 0.
printfn "total_get_bits: %d" total_get_bits

let count_full = quadtree.GetCount()
printfn "quadtree filled: done!, total_count: %d" count_full


quadtree.Trim(2)
quadtree.Trim(2)
quadtree.Trim(0)
let count_trimmed = quadtree.GetCount()
printfn "quadtree trimmed: done!, trimmed_count: %d" count_trimmed

quadtree.WriteRects(quadout)
// quadtree.WritePoints(quadout)
printfn "quadtree.Write() done!"
// NativeArray2D.delete stencil

// exit 0


// Gnuplot()
// |> Gnuplot.datablockXY (domain |> Array.map (fun v -> double v.X)) (domain |> Array.map (fun v -> double v.Y)) "coordinates"
// |>> "unset key"
// |>> "plot $coordinates using 1:2 with points lc rgb 'blue' lw 4, \\"
// |>> $"'{output}' using 1:2 with lines lc rgb 'black'"
// |> Gnuplot.run
// |> ignore

Gnuplot()
|> Gnuplot.datablockXY (domain |> Array.map (fun v -> double v.X)) (domain |> Array.map (fun v -> double v.Y)) "coordinates"
|>> "unset key"
|>> "plot $coordinates using 1:2 with lines lc rgb 'black' lw 2, \\"
// |>> $"'{quadout}' using 1:2 with points lc rgb 'black'"
|>> $"'{quadout}' using 1:2 with lines lc rgb 'black'"
|> Gnuplot.run
|> ignore

Console.ReadKey()


