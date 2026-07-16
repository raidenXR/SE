#r "nuget: SkiaSharp, 2.88.6"
#r "../bin/Debug/net10.0/SE-core.dll"

open System
open SE.Plotting
open SkiaSharp
open System.Numerics

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
    

let N = 180
type Point = { X: float; Y: float }

type Edge = {
    YMin: float
    YMax: float
    X: float
    DXDy: float
}

let createEdge (p1: Point) (p2: Point) : Edge option =
    let dy = p2.Y - p1.Y
    
    if dy = 0.0 then None
    else
        let ymin = min p1.Y p2.Y
        let ymax = max p1.Y p2.Y
        let x = if p1.Y < p2.Y then p1.X else p2.X
        let dxdy = (p2.X - p1.X) / dy
        
        Some {
            YMin = ymin
            YMax = ymax
            X = x
            DXDy = dxdy
        }

let buildEdgeTable (vertices: Point list) : Map<int, Edge list> =
    let n = List.length vertices
    let edges =
        List.mapi (fun i v ->
            let next = vertices.[(i + 1) % n]
            createEdge v next
        ) vertices
        |> List.choose id
    
    edges
    |> List.groupBy (fun e -> int e.YMin)
    |> Map.ofList

let compareEdgesByX (e1: Edge) (e2: Edge) : int =
    if e1.X < e2.X then -1
    elif e1.X > e2.X then 1
    else 0

let scanlineFill (vertices: Point list) : (int * int) list =
    if List.length vertices < 3 then []
    else
        let yValues = List.map (fun p -> p.Y) vertices
        let ymin = int (List.minBy id yValues)
        let ymax = int (List.maxBy id yValues)
        let edgeTable = buildEdgeTable vertices
        
        let rec processScanlines (y: int) (aet: Edge list) (filledPixels: (int * int) list) =
            if y > ymax then
                List.rev filledPixels
            else
                // Add new edges from edge table for this scanline
                let newEdges = Map.tryFind y edgeTable |> Option.defaultValue []
                let aet' = List.append aet newEdges |> List.sortWith compareEdgesByX
                
                // Remove edges where YMax <= current Y
                let aet'' = List.filter (fun e -> e.YMax > float y) aet'
                
                // Fill scanline by pairing X intersections
                let rec pairAndFill edges acc =
                    match edges with
                    | [] -> List.rev acc
                    | [_] -> List.rev acc
                    | x1 :: x2 :: rest ->
                        let x1i = int (ceil x1.X)
                        let x2i = int (floor x2.X)
                        let pixels = [for x in x1i .. x2i -> (x, y)]
                        pairAndFill rest (List.append pixels acc)
                
                let filledThisScanline = pairAndFill aet'' []
                
                // Update X for next scanline
                let aet''' = aet'' |> List.map (fun e -> { e with X = e.X + e.DXDy })
                
                processScanlines (y + 1) aet''' (List.append filledThisScanline filledPixels)
        
        processScanlines ymin [] []

// Example usage
let triangle = [
    { X = 10.0; Y = 0.0 }
    { X = 30.0; Y = 20.0 }
    { X = 0.0; Y = 20.0 }
]

let path = "domains/random_domain.png"
let (_stencil,N',v_min,v_max) = get_pixels N path
let (vs,bounds_tree,stencil) =
    _stencil
    |> SE.Core.Quadtree.fill_scanlines_from_bits N v_min v_max
let points = vs
            |> Array.map (fun v -> {X = double v.X; Y = double v.Y})
            |> List.ofArray
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

// let _points = []
// let (v_min,v_max) = SE.Core.GridGeneration2D.bounds points
// let (vs,bounds_tree,stencil) =
//     System.Collections.BitArray(N*N)
//     |> SE.Core.Quadtree.fill_raycast N v_min v_max points

// exit 0
let _points =
    vs
    |> Array.map (fun v -> {X = double v.X; Y = double v.Y})
    |> List.ofArray
printfn "points.len: %d, _points.len: %d" (points.Length) (_points.Length)

let filledPixels = scanlineFill _points
let I_min = filledPixels |> List.map fst |> List.min
let J_min = filledPixels |> List.map snd |> List.min
let I_max = filledPixels |> List.map fst |> List.max
let J_max = filledPixels |> List.map snd |> List.max

let lerp p pmin pmax = (double p - double pmin) / (double pmax - double pmin)

// let sx x = double (N * x) / double J_max
// let sy y = double (N * y) / double I_max

printfn "I_min: %d, J_min: %d" I_min J_min
printfn "I_max: %d, J_max: %d" I_max J_max

let bits = System.Collections.BitArray(N*N)
for (x,y) in filledPixels do
    let j = double (N-1) * (lerp x I_min I_max) |> int
    let i = double (N-1) * (lerp y J_min J_max) |> int 
    ()
    // printfn "x: %d, y: %d, i,j: %d, %d" x y i j
    bits[i*N+j] <- true

// printfn "Filled pixels: %d" (List.length filledPixels)


// let v_min = Vector2.Zero
// let v_max = Vector2.One
let tree = SE.Core.Quadtree.ofStencil<double> N 3 v_min v_max bits
let pts = tree.AsPoints()
let xs = pts |> Array.map (fun v -> double v.X)
let ys = pts |> Array.map (fun v -> double v.Y)
let sb = System.Text.StringBuilder(1024)
SE.Core.Quadtree.write_rects_to_sb tree.Root sb
let rects = string sb
printfn "quads_tree.count:  %d" (tree.GetCount())

Gnuplot()
|> Gnuplot.datablockXY xs ys "points"
|> Gnuplot.datablockString rects "rects"
|>> "set size ratio -1"
|>> "unset key"
// |>> "plot $bounds with lines lc rgb 'black', \\"
// |>> "$norms with vectors lw 2 lc rgb 'red', \\"
|>> "plot $points with points lw 1 lc rgb 'black', \\"
|>> "$rects with lines lw 1 lc rgb 'black', \\"
// |>> "$bounds_rects with lines lw 1 lc rgb 'black'"
|> Gnuplot.run
|> ignore

Console.ReadKey()

