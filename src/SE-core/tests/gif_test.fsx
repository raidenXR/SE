#r "nuget: SkiaSharp, 2.88.6"
#r "../bin/Debug/net10.0/SE-core.dll"


open System
open System.Numerics
open SE
open SE.Core
open SkiaSharp
open GridGeneration2D
open Plotting


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


let mutable v1 = Vector2.Zero
let mutable v2 = Vector2.Zero
for fs in System.IO.Directory.EnumerateFiles("keyframes") do
    if fs.EndsWith("png") then
        let (stencil,v_min,v_max) = get_pixels fs
        let (_v1, _v2) = bounds_union v1 v2 v_min v_max
        v1 <- _v1
        v2 <- _v2
    
let mutable I = 1
for fs in System.IO.Directory.EnumerateFiles("keyframes") do
    if fs.EndsWith("png") then
        let (stencil,_v_min,_v_max) = get_pixels fs
        let v_min = v1
        let v_max = v2
        let quadtree = Quadtree.ofStencil N 5 v_min v_max stencil

        let mutable total_get_bits = 0
        for i in 0..N-1 do
            for j in 0..N-1 do
                let v = to_cartesian_system i j N v_min v_max
                if quadtree.Stencil[i*N+j] then
                    total_get_bits <- total_get_bits + 1
                    let x = double j * double quadtree.dX
                    let y = double i * double quadtree.dY
                    let z = x + y - 0.5 * Random.Shared.NextDouble()
                    quadtree.Put(double v.X, double v.Y, ValueSome z)


        printfn "quadtree total_bits:  %d" total_get_bits
        printfn "quadtree total_rank: %d" (quadtree.Rank)
        printfn "quadtree total_leafs: %d" (quadtree.GetCount())
        printfn "quadtree total_nodes: %d" (quadtree.GetTotalCount())
        printfn "quadtree dx: %g, dy: %g" quadtree.dX quadtree.dY

        let quadout = "keyframes_discrt/tmp.dat"
        quadtree.WriteRects(quadout)

        let points = quadtree.AsPoints()
        let xs = points |> Array.map (fun v -> double v.X)
        let ys = points |> Array.map (fun v -> double v.Y)
        let zs = quadtree.GetValues()

        Gnuplot()
        |>> "unset key"
        |>> "set size ratio -1"
        |> Gnuplot.datablockXYZ xs ys zs "elements"
        |>> "set palette model RGB"
        |>> "set xrange [0:1000]"
        |>> "set yrange [0:1000]"
        |>> "set term pngcairo size 800,800"
        |>> $"set output 'keyframes_discrt/image_{100 + I}.png'"
        |>> "plot \\"
        |>> $"'{quadout}' using 1:2 with lines lc rgb 'black', \\"
        |>> "$elements using 1:2:3 with points palette"
        |> Gnuplot.run
        |> ignore

        I <- I + 1



