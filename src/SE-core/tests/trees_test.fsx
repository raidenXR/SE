#r "nuget: SkiaSharp, 2.88.6"
#r "../bin/Debug/net10.0/SE-core.dll"
// #r "../bin/Release/net10.0/SE-core.dll"

open SE
open SE.Core
open System.Numerics
open System
open GridGeneration2D
open Plotting
open SkiaSharp

open Quadtree

// WARNING!!! the current API implementation requires 
// from the uset to implement 4 functions
// trim, dense, set_value fro the dynamic refinements,coarse for the tree
// and a custom indexer for the local coordinates of the nodes

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

let valueof = Quadtree.valueof

let _trim node =
    match node with
    | Quadtree.Node (_,c,_,_,_,_) when Quadtree.is_quadant node ->
        let v0 = valueof c[0]
        let v1 = valueof c[1]
        let v2 = valueof c[2]
        let v3 = valueof c[3]
        let d = 0.01
        abs(v0 - v1) < d || abs(v0 - v2) < d || abs(v1 - v3) < d
    | _ -> false
        
let _dense node =
    match node with
    | Quadtree.Node (_,c,_,_,_,_) when Quadtree.is_quadant node ->
        let v0 = valueof c[0]
        let v1 = valueof c[1]
        let v2 = valueof c[2]
        let v3 = valueof c[3]
        let d = 0.1
        abs(v0 - v1) > d || abs(v0 - v2) > d || abs(v1 - v3) > d
    | _ -> false
        
let _set (node:Quadtree.Node<double>) =
    match node with
    | Quadtree.Node (_,c,_,_,_,_) when Quadtree.is_quadant node ->
        let v0 = valueof c[0]
        let v1 = valueof c[1]
        let v2 = valueof c[2]
        let v3 = valueof c[3]
        (v0 + v1 + v2 + v3) / 4.0
    | _ -> -100.0
    
// get a domain from some hand-drawn image
let (stencil,N,v_min,v_max) = get_pixels 800 "cool_image.png"

// create a quadtree over the domain
let quadtree =
    stencil
    |> Quadtree.ofStencil<double> N 5 v_min v_max
    |> Quadtree.init 0.00

// analytical solutions
let pi = System.Math.PI
let inline u'(x,y,k) = sin(pi*k*x) * sin(pi*k*y)
let inline g(x, y,k) = -2. * pi**2 * sin(pi*k*x) * sin(pi*k*y) - sin(pi*k*x)**3. * sin(pi*k*y)**3.

let DX = quadtree.dX
let DY = quadtree.dY

// add indexer
type Node<'T> with
    member this.Item
        with get(i:int,j:int) =
            match (Quadtree.iterate i j DX DY this) with
            | Leaf (_,v,_,_,_,_) -> v.Value.Value
            | _ -> failwith "should always traverse to a leaf node"

        and set(i:int,j:int) value =
            match (Quadtree.iterate i j DX DY this) with
            | Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome value
            | _ -> failwith "should always traverse to a leaf node"
            
#time
// run 300 iteration to 'solve' the PDE
for n in 1..10 do
    printfn "iter: %d" n
    quadtree.Root |> Quadtree.iter (fun u ->
        match (Quadtree.ofState DX DY u) with
        | Quadtree.Internal ->  // apply the Partial Differential equation
            let (Quadtree.Leaf (_,_,_,_,v_min,v_max)) = u
            let dv = v_max - v_min
            let c = v_min + (v_max - v_min) / 2f
            let x = double c.X
            let y = double c.Y 
            let dx = (double dv.X)**2
            let dy = (double dv.Y)**2        
            u[0,0] <- x + y / double(v_max.X + v_max.Y)
            // u[0,0] <- dx*(u[-1,0].Value + u[1,0].Value) + dy*(u[0,-1].Value + u[0,1].Value)
            // u[0,0] <- u[-1,0] + 0.1*System.Random.Shared.NextDouble()
            // u[0,0] <- (1./(-2.*(dx+dy))) * g(x,y,1) - dx * u[0,1].Value - dx*u[0,-1].Value - dy*u[1,0].Value - dy*u[-1,0].Value

            
        | Quadtree.Boundary ->  // apply dirichlet conditions
            u[0,0] <- 0.00  
        
        | Quadtree.External -> // do nothing, ignore external nodes
            ()
    )
    quadtree.Update(_trim, _dense, _set)
    printfn "quadtree.count: %d" (quadtree.GetCount()) 
    printfn "quadtree.total_count: %d" (quadtree.GetTotalCount()) 
// ^^ It fails miserably to converge, test some other PDE

        
let quadtree_copy = quadtree.Copy()
printfn "quadtree.count: %d, copy.count: %d" (quadtree_copy.GetCount()) (quadtree_copy.GetCount())
printfn "quadtree.total_count: %d, copy.total_count: %d" (quadtree_copy.GetTotalCount()) (quadtree_copy.GetTotalCount())


for n in 1..10 do
    printfn "iter: %d" n
    quadtree_copy.Root |> Quadtree.iter (fun u ->
        match (Quadtree.ofState DX DY u) with
        | Quadtree.Internal ->  // apply the Partial Differential equation
            let (Quadtree.Leaf (_,_,_,_,v_min,v_max)) = u
            let dv = v_max - v_min
            let c = v_min + (v_max - v_min) / 2f
            let x = double c.X
            let y = double c.Y 
            let dx = (double dv.X)**2
            let dy = (double dv.Y)**2        
            u[0,0] <- u[-1,0] + 0.1*System.Random.Shared.NextDouble()
            
        | Quadtree.Boundary ->  // apply dirichlet conditions
            u[0,0] <- 5.00  
        
        | Quadtree.External -> // do nothing, ignore external nodes
            ()
    )
    quadtree_copy.Update(_trim, _dense, _set)
    printfn "quadtree.count: %d" (quadtree_copy.GetCount()) 
    printfn "quadtree.total_count: %d" (quadtree_copy.GetTotalCount()) 
// ^^ It fails miserably to converge, test some other PDE

// print size of the quadtree
#time
let elements = quadtree.AsPolygons(fun d -> float32 d)
let sb = System.Text.StringBuilder(1024*1024)
Quadtree.write_rects_to_sb quadtree.Root sb
let points = quadtree.AsPoints()
let xs = points |> Array.map (fun v -> double v.X)
let ys = points |> Array.map (fun v -> double v.Y)
let zs = quadtree.GetValues()

let elements_copy = quadtree_copy.AsPolygons(fun d -> float32 d)
let zs_copy = quadtree_copy.GetValues()
let sb_copy = System.Text.StringBuilder(1024*1024)
Quadtree.write_rects_to_sb quadtree_copy.Root sb_copy
// exit 0
        
Gnuplot()
|>> "set size ratio -1"
|>> "unset key"
|>> "set title 'Descritized Swallow (Quadtrees)' tc rgb 'white'"
|>> "set cbtics textcolor rgb 'white'"
|>> "set xtics textcolor rgb 'white'"
|>> "set ytics textcolor rgb 'white'"
|>> "set object 1 rectangle from screen 0,0 to screen 1,1 fillcolor rgbc 'black' behind"
|>> "set style fill noborder"
|>> "set palette defined (0 'navy', 1 'blue', 2 'cyan', 3 'green', 4 'yellow', 5 'orange', 6 'red')"
|>> $"set cbrange[{Array.min zs}:{Array.max zs}]"
// |>> "set view map"
// |> Gnuplot.datablockPolygons2 elements "elements"
|> Gnuplot.datablockString (string sb) "grid"
// |> Gnuplot.datablockXY xs ys "centers"
// |>> "plot $elements using 1:2:3 with filledcurves closed fc palette z"
|>> "plot $grid with lines lc rgb 'white'"
|> Gnuplot.run
|> ignore

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
|>> $"set cbrange[{Array.min zs_copy}:{Array.max zs_copy}]"
// |>> "set view map"
// |> Gnuplot.datablockPolygons2 elements_copy "elements"
|> Gnuplot.datablockString (string sb_copy) "grid"
// |> Gnuplot.datablockXY xs ys "centers"
// |>> "plot $elements using 1:2:3 with filledcurves closed fc palette z"
|>> "plot $grid with lines lc rgb 'white'"
|> Gnuplot.run
|> ignore


Console.ReadKey()


