#r "nuget: SkiaSharp, 2.88.6"
#r "../bin/Debug/net10.0/SE-core.dll"
// #r "../bin/Release/net10.0/SE-core.dll"

open SE
open SE.Spatial
open SE.Core
open System.Numerics
open System
open GridGeneration2D
open Plotting
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

// create a quadtree over the domain
let (stencil1,N1,v_min1,v_max1) = get_pixels 400 "keyframes_domain/cool_image_01.png"
let (stencil2,N2,v_min2,v_max2) = get_pixels 400 "keyframes_domain/cool_image_02.png"
let N = 400
let v_min = Vector2.Zero
let v_max = Vector2.One * 1000.f

let quadtree =
    stencil1
    |> Quadtree.ofStencil<double> N 3 v_min v_max

quadtree.Iter (fun u ->
    match u with
    | (Quadtree.Internal | Quadtree.Boundary) & (Quadtree.Leaf (_,v,_,_,_,_)) -> v.Value <- ValueSome 0.0
    | _ -> ()
)


// create a quadtree over the domain
let quadtree' =
    // for i in 0..N1-1 do
    //     for j in 0..N1-1 do
    //         let idx = i*N1 + j
    //         stencil2[idx] <- if stencil1[idx] then false else stencil2[idx]
    stencil2.And(quadtree.Stencil.Not())
    // stencil2
    |> Quadtree.ofStencil<double> N 3 v_min v_max

quadtree'.Iter (fun u ->
    match u with
    | (Quadtree.Internal | Quadtree.Boundary) & (Quadtree.Leaf (_,v,_,_,_,_)) -> v.Value <- ValueSome 0.0
    | _ -> ()
)

quadtree.Iter (fun u  ->
    // match u with
    // | Quadtree.Internal -> quadtree'.RemoveF(Quadtree.center u)
    // | _ -> ()
    ()
)

printfn "internal: %d, count: %d" (quadtree.GetInternalCount()) (quadtree.GetCount())
printfn "internal: %d, count: %d" (quadtree'.GetInternalCount()) (quadtree'.GetCount())

let sb =
    let sb= System.Text.StringBuilder(1024*1024)
    Quadtree.write_rects_to_sb quadtree'.Root sb
    sb

let (x,y) =
    quadtree.AsPoints()
    |> Array.map (fun v -> (double v.X, double v.Y))
    |> Array.unzip    


Gnuplot()
|>> "set size ratio -1"
|>> "unset key"
|>> "set title 'Descritized Swallow COPY (Quadtrees)' tc rgb 'white'"
|>> "set cbtics textcolor rgb 'white'"
|>> "set xtics textcolor rgb 'white'"
|>> "set ytics textcolor rgb 'white'"
|>> "set object 1 rectangle from screen 0,0 to screen 1,1 fillcolor rgbc 'black' behind"
|>> "set style fill noborder"
|>> "set palette defined (0 'navy', 1 'blue', 2 'cyan', 3 'green', 4 'yellow', 5 'orange', 6 'red')"
// |>> $"set cbrange[{Array.min zs_copy}:{Array.max zs_copy}]"
// |>> "set view map"
|> Gnuplot.datablockString (string sb) "grid1"
|> Gnuplot.datablockXY x y "grid2"
|>> "plot $grid1 using 1:2 with lines lc rgb 'white', \\"
|>> "$grid2 using 1:2 with points lc rgb 'yellow'"
|> Gnuplot.run
|> ignore


Console.ReadKey()

