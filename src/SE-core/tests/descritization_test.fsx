#load "../src/unsafe.fs"
#load "../src/trees.fs"
#load "../src/gnuplot.fs"

open System
open System.Numerics
open SE
open SE.Core
open GridGeneration2D
open Plotting

let path = Environment.GetCommandLineArgs()[2]
let ouput = path[0..path.Length-5] + "_output.dat"
let quadout = path[0..path.Length-5] + "_quad.dat"
// let path = "coordinates_dense.dat"
// let output = "mesh_vertices.dat"
// let quadout = "quad_vertices.dat"

let N = 8*80

// let domain = read_from_file path
let domains = read_from_file_multiple path
let (v_min,v_max) = total_bounds domains
let stencil = BitArray(N*N)

for domain in domains do
    ignore (bitstencil_overwrite domain stencil N v_min v_max)
fill_bitstencil N v_min v_max stencil

#time
let quadtree = Quadtree2.Root<double>(N, 5, v_min, v_max)
let mutable total_get_bits = 0
for i in 0..N-1 do
    for j in 0..N-1 do
        let v = to_cartesian_system i j N v_min v_max
        if stencil[i*N+j] then
            total_get_bits <- total_get_bits + 1
            quadtree[double v.X, double v.Y] <- 0.

printfn "quadtree total_bits:  %d" total_get_bits
printfn "quadtree total_leafs: %d" (quadtree.GetCount())
printfn "quadtree total_nodes: %d" (quadtree.GetTotalCount())
#time

quadtree.WriteRects(quadout)
// quadtree.WritePoints(quadout)

// exit 0


// Gnuplot()
// |> Gnuplot.datablockXY (domain |> Array.map (fun v -> double v.X)) (domain |> Array.map (fun v -> double v.Y)) "coordinates"
// |>> "unset key"
// |>> "plot $coordinates using 1:2 with points lc rgb 'blue' lw 4, \\"
// |>> $"'{output}' using 1:2 with lines lc rgb 'black'"
// |> Gnuplot.run
// |> ignore

let gnu =
    Gnuplot()
    |>> "unset key"

domains |> Array.iteri (fun i domain->
    gnu
    |> Gnuplot.datablockXY (domain |> Array.map (fun v -> double v.X)) (domain |> Array.map (fun v -> double v.Y)) $"coordinates{i}"
    |> ignore
)

gnu |>> "plot $coordinates0 using 1:2 with lines lc rgb 'black' lw 2, \\" |> ignore

domains |> Array.iteri (fun i domain->
    gnu
    |>> $"$coordinates{i} using 1:2 with points lc rgb 'black' lw 2, \\"
    |> ignore
)

gnu
// |>> $"'{quadout}' using 1:2 with points lc rgb 'black'"
|>> $"'{quadout}' using 1:2 with lines lc rgb 'black'"
|> Gnuplot.run
|> ignore

Console.ReadKey()


