#r "nuget: SkiaSharp, 2.88.6"
// #r "../bin/Debug/net10.0/SE-core.dll"
#r "../bin/Release/net10.0/SE-core.dll"

open SE
open SE.Core
open System.Numerics
open System
open GridGeneration2D
open Plotting
open SkiaSharp

let get_pixels (path:string) =
    let is_black (p:SKColor) =
        p.Blue < 80uy && p.Green < 80uy && p.Red < 80uy

    use image = SKImage.FromEncodedData(path)
    use bitmap = SKBitmap.FromImage(image)
    let w = bitmap.Width
    let h = bitmap.Height
    let N = max w h
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
    (stencil, N, Vector2(float32 x_min, float32 y_min), Vector2(float32 x_max, float32 y_max))


let _trim node =
    match node with
    | Quadtree.Node (_,c,_,_,_,_) when Quadtree.is_quadant node ->
        let v0 = Quadtree.get_value c[0]
        let v1 = Quadtree.get_value c[1]
        let v2 = Quadtree.get_value c[2]
        let v3 = Quadtree.get_value c[3]
        abs(v0 - v1) < 0.1 || abs(v0 - v2) < 0.1 || abs(v1 - v3) < 0.01
    | _ -> false
        
let _dense node =
    match node with
    | Quadtree.Node (_,c,_,_,_,_) when Quadtree.is_quadant node ->
        let v0 = Quadtree.get_value c[0]
        let v1 = Quadtree.get_value c[1]
        let v2 = Quadtree.get_value c[2]
        let v3 = Quadtree.get_value c[3]
        abs(v0 - v1) > 0.5 || abs(v0 - v2) > 0.5 || abs(v1 - v3) > 0.05
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
    

// get a domain from some hand-drawn image
let (stencil,N,v_min,v_max) = get_pixels "evil_vilain.png"

// create a quadtree over the domain
let quadtree =
    stencil
    |> Quadtree.ofStencil<double> N 5 v_min v_max
    |> Quadtree.init 0.00

// analytical solutions
let pi = System.Math.PI
let inline u'(x,y,k) = sin(pi*k*x) * sin(pi*k*y)
let inline g(x, y,k) = -2. * pi**2 * sin(pi*k*x) * sin(pi*k*y) - sin(pi*k*x)**3. * sin(pi*k*y)**3.

#time
// run 300 iteration to 'solve' the PDE
for n in 1..300 do
    printfn "itern: %d" n
    quadtree.Root |> Quadtree.iter (fun u ->
        match u with
        | Quadtree.IsInternalNode ->  // apply the Partial Differential equation
            let (Quadtree.Leaf (_,_,_,_,v_min,v_max)) = u
            let dv = v_max - v_min
            let c = v_min + (v_max - v_min) / 2f
            let x = double c.X
            let y = double c.Y 
            let dx = (double dv.X)**2
            let dy = (double dv.Y)**2        
            u[0,0] <- (1./(-2.*(dx+dy))) * g(x,y,1) - dx * u[0,1].Value - dx*u[0,-1].Value - dy*u[1,0].Value - dy*u[-1,0].Value

            ()
            
        | Quadtree.IsBoundaryNode ->  // apply dirichlet conditions
            u[0,0] <- 0.00  
        
        | Quadtree.IsExternal -> // do nothing, ignore external nodes
            ()
    )
    quadtree.Update(_trim, _dense, _set)
// ^^ It fails miserably to converge, test some other PDE

// print size of the quadtree
printfn "quadtree.count: %d" (quadtree.GetCount()) 
printfn "quadtree.total_count: %d" (quadtree.GetTotalCount()) 
#time
        

// FIX the gnuplot code to plot with polygons the domain, not points !!!
// let points = quadtree.AsPolygons(fun d -> float32 d)
let points = quadtree.AsPoints()
let xs = points |> Array.map (fun v -> double v.X)
let ys = points |> Array.map (fun v -> double v.Y)
let zs = quadtree.GetValues()
        
Gnuplot()
|>> "set size ratio -1"
|>> "unset key"
// |>> "set view map"
// |> Gnuplot.datablockPolygons2 points "centers"
|> Gnuplot.datablockXYZ xs ys zs "centers"
|>> "set palette model RGB"
|>> "plot $centers using 1:2:3 with points pt 5 palette"
// |>> "splot $centers using 1:2:3 with polygons fc palette"
// |>> "plot $centers using 1:2:3 with filledcurves closed fc palette"
// using 1:2:(column(4)) with filledcurves closed fillcolor palette z
|> Gnuplot.run
|> ignore


Console.ReadKey()


