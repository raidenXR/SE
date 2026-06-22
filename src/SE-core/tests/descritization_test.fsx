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


#time
let quadtree = Quadtree.ofStencil N 5 v_min v_max stencil

let mutable total_get_bits = 0
for i in 0..N-1 do
    for j in 0..N-1 do
        if quadtree.Stencil[i*N+j] then
            let v = to_cartesian_system i j N v_min v_max 
            total_get_bits <- total_get_bits + 1
            let x = double v.X
            let y = double v.Y
            // let x = double j * double quadtree.dX + double (v_min.X)  <-- this breaks traversal
            // let y = double i * double quadtree.dY + double (v_min.Y)  <-- this breaks traversal
            // ^^ probably something with the decimals from truncate ??
            let z = x + y - 0.5 * Random.Shared.NextDouble()
            quadtree[x, y] <- z


let _trim node =
    match node with
    | Quadtree.Node (_,c,_,_,_,_) when Quadtree.is_quadant node ->
        let v0 = Quadtree.get_value c[0]
        let v1 = Quadtree.get_value c[1]
        let v2 = Quadtree.get_value c[2]
        let v3 = Quadtree.get_value c[3]
        abs(v0 - v1) < 0.1 || abs(v0 - v2) < 0.1 || abs(v1 - v3) < 0.1
    | _ -> false
        
let _dense node =
    match node with
    | Quadtree.Node (_,c,_,_,_,_) when Quadtree.is_quadant node ->
        let v0 = Quadtree.get_value c[0]
        let v1 = Quadtree.get_value c[1]
        let v2 = Quadtree.get_value c[2]
        let v3 = Quadtree.get_value c[3]
        abs(v0 - v1) > 0.5 || abs(v0 - v2) > 0.5 || abs(v1 - v3) > 0.5
    | _ -> false
        
let _set (node:Quadtree.Node<double>) =
    match node with
    | Quadtree.Node (_,c,_,_,_,_) when Quadtree.is_quadant node ->
        let v0 = Quadtree.get_value c[0]
        let v1 = Quadtree.get_value c[1]
        let v2 = Quadtree.get_value c[2]
        let v3 = Quadtree.get_value c[3]
        (v0 + v1 + v2 + v3) / 4.0
    | _ -> -100.0
    
quadtree.Update(_trim, _dense, _set)

printfn "quadtree total_bits:  %d" total_get_bits
printfn "quadtree total_rank: %d" (quadtree.Rank)
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

// // assign values
// for i in 0..N-1 do
//     for j in 0..N-1 do
//         if stencil[i*N+j] then
//             let x = double j * double quadtree.dX
//             let y = double i * double quadtree.dY
//             let z = exp (-(x**2.) - (y**2))
//             // quadtree[x,y] <- z
//             quadtree.Put(x,y, ValueSome z)


let points = quadtree.AsPoints()
let xs = points |> Array.map (fun v -> double v.X)
let ys = points |> Array.map (fun v -> double v.Y)
let zs = quadtree.GetValues()

gnu
|>> "set size ratio -1"
|> Gnuplot.datablockXYZ xs ys zs "elements"
|>> "set palette model RGB"
// |>> "set term pngcairo size 800,800"
// |>> "set output 'image2.png'"
// |>> $"'{quadout}' using 1:2 with points lc rgb 'black'"
// |>> $"plot '{path}' binary filetype=png with rgbimage, \\"
|>> "plot \\"
|>> $"'{quadout}' using 1:2 with lines lc rgb 'black', \\"
|>> "$elements using 1:2:3 with points palette"
|> Gnuplot.run
|> ignore

Console.ReadKey()


