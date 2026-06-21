#r "nuget: SkiaSharp, 2.88.6"
#r "../bin/Debug/net10.0/SE-core.dll"
// #load "../src/unsafe.fs"
// #load "../src/trees.fs"
// #load "../src/gnuplot.fs"


open System
open System.Numerics
open SE
open SE.Core
open SkiaSharp
open GridGeneration2D
open Plotting

let path = Environment.GetCommandLineArgs()[2]
let ouput = path[0..path.Length-5] + "_output.dat"
let quadout = path[0..path.Length-5] + "_quad.dat"
// let path = "coordinates_dense.dat"
// let output = "mesh_vertices.dat"
// let quadout = "quad_vertices.dat"

let mutable N = 8*80


let is_black (p:SKColor) =
    p.Blue < 80uy && p.Green < 80uy && p.Red < 80uy

let get_pixels (path:string)  =
    use image = SKImage.FromEncodedData(path)
    use bitmap = SKBitmap.FromImage(image)
    let w = bitmap.Width
    let h = bitmap.Height
    N <- max w h
    printfn "N: %d" N
    let stencil = System.Collections.BitArray(N*N)

    let mutable x_min = double N
    let mutable y_min = double N
    let mutable x_max = 0.0
    let mutable y_max = 0.0
    let mutable total_pixels = 0

    for j in 0..w-1 do
        for i in 0..h-1 do
            if is_black (bitmap.GetPixel(i,j)) then
                stencil[(N-j)*w+i] <- true
                x_min <- min (double j) x_min
                y_min <- min (double i) y_min
                x_max <- max (double j) x_max
                y_max <- max (double i) y_max
                total_pixels <- total_pixels + 1

    printfn "total_pixels: %d" total_pixels
    (stencil, Vector2(float32 x_min, float32 y_min), Vector2(float32 x_max, float32 y_max))

    
let (stencil,v_min,v_max) = get_pixels path
fill_bitstencil N v_min v_max stencil

// let domains = read_from_file_multiple path
// let (v_min,v_max) = total_bounds domains
// let stencil = System.Collections.BitArray(N*N)

// for domain in domains do
//     ignore (bitstencil_overwrite domain stencil N v_min v_max)
// fill_bitstencil N v_min v_max stencil

#time
let quadtree = Quadtree.Root<double>(N, 5, v_min, v_max)
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
printfn "quadtree dx: %g, dy: %g" quadtree.dX quadtree.dY
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

// domains |> Array.iteri (fun i domain->
//     gnu
//     |> Gnuplot.datablockXY (domain |> Array.map (fun v -> double v.X)) (domain |> Array.map (fun v -> double v.Y)) $"coordinates{i}"
//     |> ignore
// )

// gnu |>> "plot $coordinates0 using 1:2 with lines lc rgb 'black' lw 2, \\" |> ignore

// domains |> Array.iteri (fun i domain->
//     gnu
//     |>> $"$coordinates{i} using 1:2 with points lc rgb 'black' lw 2, \\"
//     |> ignore
// )

gnu
|>> "set size ratio -1"
// |>> "set term pngcairo size 800,800"
// |>> "set output 'image2.png'"
// |>> $"'{quadout}' using 1:2 with points lc rgb 'black'"
// |>> $"plot '{path}' binary filetype=png with rgbimage, \\"
|>> "plot \\"
|>> $"'{quadout}' using 1:2 with lines lc rgb 'black'"
|> Gnuplot.run
|> ignore

Console.ReadKey()


