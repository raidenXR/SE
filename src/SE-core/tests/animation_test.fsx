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
let kindof  = Quadtree.kindof

let _trim node =
    match node with
    | Quadtree.Node (_,c,_,_,_,_) when Quadtree.is_quadant node ->
        let v0 = valueof c[0]
        let v1 = valueof c[1]
        let v2 = valueof c[2]
        let v3 = valueof c[3]
        let d = 50.
        abs(v0 - v1) < d || abs(v0 - v2) < d || abs(v1 - v3) < d
    | _ -> false
        
let _dense node =
    match node with
    | Quadtree.Node (_,c,_,_,_,_) when Quadtree.is_quadant node ->
        let v0 = valueof c[0]
        let v1 = valueof c[1]
        let v2 = valueof c[2]
        let v3 = valueof c[3]
        let d = 100.
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
    | _ -> failwith "_set SHOULD apply only on quadants" 
    
    
let mutable v_min = Vector2(Single.MaxValue, Single.MaxValue)
let mutable v_max = Vector2(Single.MinValue, Single.MinValue)
let stencils  = ResizeArray<System.Collections.BitArray>()
let quadtrees = ResizeArray<Root<double>>()
let N = 400

for img in System.IO.Directory.EnumerateFiles("keyframes_domain") do
// for img in System.IO.Directory.EnumerateFiles("domains") do
    let (stencil,N',v_min',v_max') = get_pixels N img
    stencils.Add(stencil)
    v_min <- Vector2.Min(v_min, v_min')
    v_max <- Vector2.Max(v_max, v_max')
    if N <> N' then failwith "N cannot differ"

printfn "%A, %A" v_min v_max

v_min.X <- 0.f
v_min.Y <- 0.f
v_max.X <- 1.f
v_max.Y <- 1.f

// init quadtrees and set boundaries and 
for stencil in stencils do
    let T =
        stencil
        |> Quadtree.ofStencil<double> N 5 v_min v_max
        |> Quadtree.init 0.00

    T.Add <- (+)
    T.Div <- (/)

    let dx = T.dX
    let dy = T.dY
    T.Root |> Quadtree.iter (fun node ->
        T.CurrentNode <- node
        match (kindof dx dy node) with
        // match (kindof T) with
        | Quadtree.Boundary -> T[0,0] <- 300.0
        | _ -> ()
    )

    quadtrees.Add(T)
            
// let heat flow
let KAPPA =  210.
let SPH = 900.
let RHO = 2700.
let incrx = Math.PI / 100.0
let incry = Math.PI / 100.0
let den = 390.
let ten = 180.
let cc = sqrt(ten / den) 
let cprime = cc
let covercp = cc / cprime
let ratio = 0.5 * covercp * covercp

// initial condition: positions
do
    let T = quadtrees[0]
    T.Root |> Quadtree.iter (fun node ->
        T.CurrentNode <- node
        let c = Quadtree.center node
        let x = double c.X
        let y = double c.Y
        T[0,0] <- sin (2.*x) * sin(y)
        // T[0,0] <- 273.
    )

// // first time step
do
    let T' = quadtrees[0]
    let T = quadtrees[1]
    let dx = T.dX
    let dy = T.dY
    T.Root |> Quadtree.iter (fun node ->
        T.CurrentNode <- node
        match (kindof dx dy node) with
        // match (kindof T[i]) with
        | Quadtree.Internal ->  // apply the Partial Differential equation
            let c = Quadtree.center node
            let x = double c.X
            let y = double c.Y
            T'.MapTo(x,y)
            T[0,0] <- T'[0,0] + 0.5*ratio*(T'[1,0] + T'[-1,0] + T'[0,1] + T'[0,-1] - 4.0*T'[0,0])

        | Quadtree.Boundary ->  // apply dirichlet conditions
            T[0,0] <- 300.0
    
        | Quadtree.External -> // do nothing, ignore external nodes
            ()
    )
    

#time
for i in 2..quadtrees.Count-1 do
    let T = quadtrees
    let dx = T[i].dX
    let dy = T[i].dY
    let dr = sqrt(dx*dx + dy*dy) 
    let dt = 6.
    for n in 1..500 do
        T[i].Root |> Quadtree.iter (fun node ->
            T[i].CurrentNode <- node
            match (kindof dx dy node) with
            // match (kindof T[i]) with
            | Quadtree.Internal ->  // apply the Partial Differential equation
                let (Quadtree.Leaf (_,_,_,_,v_min,v_max)) = node
                let c = Quadtree.center node
                let x = double c.X
                let y = double c.Y
                T[i-2].MapTo(x,y)
                T[i-1].MapTo(x,y)
                T[i][0,0] <- 2.*T[i-1][0,0] - T[i-2][0,0] + ratio*(T[i-1][1,0] + T[i-1][-1,0] - 4.* T[i-1][0,0] + T[i-1][0,1] + T[i-1][0,-1])
                // T[0,0] <- (T[1,0] + T[-1,0] + T[0,1] + T[0,-1]) / 4.
                // T[i][0,0] <- T[i-1][0,0] + (KAPPA*dt/(SPH*RHO*double(dr*dr))) * (T[i-1][1,0] + T[i-1][-1,0] + T[i-1][0,1] + T[i-1][0,-1] - 4.*T[i-1][0,0])
            
            | Quadtree.Boundary ->  // apply dirichlet conditions
                T[i][0,0] <- 300.0
        
            | Quadtree.External -> // do nothing, ignore external nodes
                ()
        )
        T[i].Update(_trim, _dense, _set)

    Quadtree.morph (quadtrees[i-1].Root) (quadtrees[i-2].Root) (+) (/)
    Quadtree.morph (quadtrees[i-0].Root) (quadtrees[i-1].Root) (+) (/)
            
    printfn "quadtree.count: %d" (T[i].GetCount()) 
    printfn "quadtree.total_count: %d" (T[i].GetTotalCount()) 
#time


// let elements_copy = quadtree_2.AsPolygons(fun d -> float32 d)
// let points_copy = quadtree_2.AsPoints()
// let xs_copy = points_copy |> Array.map (fun v -> double v.X)
// let ys_copy = points_copy |> Array.map (fun v -> double v.Y)
// let zs_copy = quadtree_2.GetValues()
// let sb_copy = System.Text.StringBuilder(1024*1024)
// Quadtree.write_rects_to_sb quadtree_2.Root sb_copy
// // exit 0

let (z_min,z_max) =
    let mutable z_min = Double.MaxValue
    let mutable z_max = Double.MinValue
    for q in quadtrees do
        z_min <- min z_min (Array.min (q.GetValues()))
        z_max <- max z_max (Array.max (q.GetValues()))
    z_min,z_max
        

let mutable ii = 0
for T in quadtrees do
    let elements = T.AsPolygons(fun d -> float32 d)
    let sb = System.Text.StringBuilder(1024*1024)
    Quadtree.write_rects_to_sb T.Root sb
    let points = T.AsPoints()
    let xs = points |> Array.map (fun v -> double v.X)
    let ys = points |> Array.map (fun v -> double v.Y)

    Gnuplot()
    |>> "set terminal pngcairo size 800,800"
    |>> $"set output 'keyframes_descretized/swallow_{100 + ii}.png'"
    // |>> $"set output 'domains/domain_{100 + ii}.png'"
    |>> "set size ratio -1"
    |>> "unset key"
    |>> $"set xrange[{v_min.X}:{v_max.X}]"
    |>> $"set yrange[{v_min.Y}:{v_max.Y}]"
    |>> "set title 'Descritized Swallow (Quadtrees)' tc rgb 'white'"
    |>> "set cbtics textcolor rgb 'white'"
    |>> "set xtics textcolor rgb 'white'"
    |>> "set ytics textcolor rgb 'white'"
    |>> "set object 1 rectangle from screen 0,0 to screen 1,1 fillcolor rgbc 'black' behind"
    |>> "set style fill noborder"
    |>> "set palette defined (0 'navy', 1 'blue', 2 'cyan', 3 'green', 4 'yellow', 5 'orange', 6 'red')"
    |>> $"set cbrange[{z_min}:{z_max}]"
    |> Gnuplot.datablockPolygons2 elements "elements"
    |> Gnuplot.datablockString (string sb) "grid"
    |>> "plot $elements using 1:2:3 with filledcurves closed fc palette z"
    // |>> "$grid with lines lc rgb 'white'"
    // |>> "plot $centers with points lc rgb 'white'"
    |> Gnuplot.run
    |> ignore

    ii <- ii + 1


// convert -delay 20 -loop 0 *.png swallow_volume.gif
// ^^^ use this command to export frames to .gif

