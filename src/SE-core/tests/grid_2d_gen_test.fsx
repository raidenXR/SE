#r "nuget: SkiaSharp, 2.88.6"
#r "../bin/Debug/net10.0/SE-core.dll"
// #r "../bin/Release/net10.0/SE-core.dll"

open SE
open SE.Core
open System.Numerics
open System
open GridGeneration2D
open Plotting

open Quadtree

open SkiaSharp

let get_pixels (N:int) (path:string) =
    let is_black (p:SKColor) =
        p.Blue < 80uy && p.Green < 80uy && p.Red < 80uy

    use image = SKImage.FromEncodedData(path)
    use bitmap = SKBitmap.FromImage(image)
    let w = bitmap.Width
    let h = bitmap.Height
    let stencil = System.Collections.BitArray(N*N)

    let mutable x_min = double N
    let mutable y_min = double N
    let mutable x_max = 0.0
    let mutable y_max = 0.0
    let mutable total_pixels = 0

    for j in 0..w-1 do
        for i in 0..h-1 do
            if is_black (bitmap.GetPixel(i,j)) then
                let ii = int(double i * double N / double h)
                let jj = int(double j * double N / double w)
                stencil[(N-jj)*N+ii] <- true
                x_min <- min (double j) x_min
                y_min <- min (double i) y_min
                x_max <- max (double j) x_max
                y_max <- max (double i) y_max
                total_pixels <- total_pixels + 1

    printfn "N: %d, total_pixels: %d" N total_pixels
    (stencil, N, Vector2(float32 x_min, float32 y_min), Vector2(float32 x_max, float32 y_max))
    

let N = 1000


// let path = "domains/coordinates_dense.dat"
// let points =
//     seq {
//         for line in (System.IO.File.ReadAllLines(path)) do
//             let dat = line.Split(',')
//             if dat.Length >= 2 then
//                 let v0 = double dat[1]
//                 let v1 = double dat[2]
//                 yield Vector2(float32 v0, float32 v1)        
//     }
//     |> Array.ofSeq
// let tree = Quadtree.ofBoundaries<double> N 4 points

// let path = "domains/random_domain.png"
let path = "domains/random_domain_2.png"
// let path = "domains/random_domain_3.png"
let (_stencil,N',v_min,v_max) = get_pixels N path
let tree = Quadtree.ofBits<double> N 4 v_min v_max _stencil

let pts = tree.AsPoints()
let xs = pts |> Array.map (fun v -> double v.X)
let ys = pts |> Array.map (fun v -> double v.Y)

// let xx = points |> Array.map (fun v -> double v.X)
// let yy = points |> Array.map (fun v -> double v.Y)

let sb = System.Text.StringBuilder(1024)
Quadtree.write_rects_to_sb tree.Root sb
let rects = string sb
// sb.Clear()
// Quadtree.write_rects_to_sb bounds_tree.Root sb
// let bounds_rects = string sb
// sb.Clear()

// printfn "bounds_tree.count: %d" (bounds_tree.GetCount())
printfn "quads_tree.count:  %d" (tree.GetCount())

// let norms = Quadtree.normals N v_min v_max points
// for n in norms do
    // ignore (sb.AppendLine($"{n.X}  {n.Y}  {n.Z}  {n.W}"))

Gnuplot()
|> Gnuplot.datablockXY xs ys "points"
// |> Gnuplot.datablockXY xx yy "bounds"
|> Gnuplot.datablockString rects "rects"
// |> Gnuplot.datablockString bounds_rects "bounds_rects"
// |> Gnuplot.datablockString (string sb) "norms"
|>> "set size ratio -1"
|>> "unset key"
// |>> "plot $bounds with lines lw 2 lc rgb 'black', \\"
// |>> "$norms with vectors lw 2 lc rgb 'red', \\"
// |>> "$points with points lw 1 lc rgb 'black', \\"
|>> "plot $rects with lines lw 1 lc rgb 'black', \\"
// |>> "$bounds_rects with lines lw 1 lc rgb 'black'"
|> Gnuplot.run
|> ignore

Console.ReadKey()


