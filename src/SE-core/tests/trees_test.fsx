// #load "../src/unsafe.fs"
// #load "../src/grid_generation.fs"
// #load "../src/trees.fs"
// #load "../src/gnuplot.fs"
#r "../bin/Debug/net10.0/SE-core.dll"

open SE
open SE.Core
open System.Numerics
open System
open GridGeneration2D
open Plotting

// let octree = Octree<double>(100, Vector3.Zero, Vector3.One, (fun a b -> a - b > 0.4))
let N = 1000

let path = Environment.GetCommandLineArgs()[2]
let domains = read_from_file_multiple path
let (v_min,v_max) = total_bounds domains

// let stencil =
//     let stencil = System.Collections.BitArray(N*N)
//     for domain in domains do
//         bitstencil_overwrite domain stencil N v_min v_max |> ignore
//     fill_bitstencil N v_min v_max stencil
        
let stencil =
    bitstencil domains[0] N
    |> fill_bitstencil N v_min v_max 

// let (v0_min,v0_max) = bounds domains[0]
// let mutable v_min = v0_min
// let mutable v_max = v0_max

    
// fill_bitstencil N v_min v_max stencil
// let quadtree = Quadtree.Root<double>(N, 4, v_min, v_max)

// #time
// // let quadtree = Quadtree2.Root<double>(N, 4, -1.5f * Vector2.One, 1.5f * Vector2.One)
// let mutable total_get_bits = 0
// for i in 0..N-1 do
//     for j in 0..N-1 do
//         let v = to_cartesian_system i j N v_min v_max
//         if stencil[i*N+j] then
//             total_get_bits <- total_get_bits + 1
//             quadtree[double v.X, double v.Y] <- 0.
// printfn "total_get_bits: %d" total_get_bits

// let count_full = quadtree.GetCount()
// printfn "quadtree filled: done!, total_count: %d" count_full

// #time
// let v_min = Vector2.Zero
// let v_max = Vector2.One
// let stencil = System.Collections.BitArray(N*N)
// let dv = (v_max - v_min) / (float32 N)
// for i in 0..N-1 do
//     for j in 0..N-1 do
//         if stencil[i*N+j] then
//             let x = double j * double dv.X
//             let y = double i * double dv.Y
        // let (i,j) = to_stencil_system N (Vector2(float32 x, float32 y)) v_min v_max

let quadtree =
    stencil
    |> Quadtree.ofStencil<double> N 5 v_min v_max
    |> Quadtree.init 0.00

// printfn "quadtree.max_level: %d" (quadtree.MaxLevel) 

// assign values
for i in 0..N-1 do
    for j in 0..N-1 do
        if stencil[i*N+j] then
            let x = double j * double quadtree.dX
            let y = double i * double quadtree.dY
            let z = exp (-(x**2.) - (y**2))
            // quadtree[x,y] <- z
            // quadtree.Put(x,y,ValueSome z)
            ()


let _trim = (fun a b -> abs(a - b) < 0.01)
let _dense = (fun a b -> abs(a - b) > 0.1)
// Quadtree.update quadtree _trim _dense

printfn "quadtree.count: %d" (quadtree.GetCount()) 
printfn "quadtree.total_count: %d" (quadtree.GetTotalCount()) 
// #time
        

let points = quadtree.AsPoints()
let xs = points |> Array.map (fun v -> double v.X)
let ys = points |> Array.map (fun v -> double v.Y)
let zs = quadtree.GetValues()
        
Gnuplot()
|>> "set size ratio -1"
|>> "unset key"
|> Gnuplot.datablockXYZ xs ys zs "centers"
|>> "set palette model RGB"
|>> "plot $centers using 1:2:3 with points palette"
|> Gnuplot.run
|> ignore


Console.ReadKey()


