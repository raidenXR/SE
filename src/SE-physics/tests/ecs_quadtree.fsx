#r "nuget: SkiaSharp, 2.88.6"
// #r "../bin/Debug/net10.0/SE-core.dll"
#r "../bin/Release/net10.0/SE-core.dll"
#r "../bin/Release/net10.0/SE-renderer.dll"

open SE
open SE.Core
open SE.ECS
open System.Numerics
open System
open System.Runtime.CompilerServices
open GridGeneration2D
open Plotting
open SkiaSharp
open SE.Renderer.Imaging

open Quadtree

// WARNING!!! the current API implementation requires 
// from the uset to implement 4 functions
// trim, dense, set_value fro the dynamic refinements,coarse for the tree
// and a custom indexer for the local coordinates of the nodes

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
let N = 600

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

[<Extension>]
type Extensions =
    [<Extension>]
    static member get(this:Quadtree.Node<double>, i:int,j:int) =
        match (Quadtree.iterate_node i j this) with
        | Quadtree.Leaf (_,v,_,_,_,_) -> v.Value.Value
        | Quadtree.Node (_,c,_,_,_,_) ->
            let mutable _n = 0
            let mutable _t = Operators.Unchecked.defaultof<double>
            iterate_sum (+) &_n &_t this
            (/) _t (double _n)                    
        | Quadtree.Empty ->                
            let mutable _n = 0
            let mutable _t = Operators.Unchecked.defaultof<double>
            iterate_sum (+) &_n &_t this    // if current node is EMPTY use the values of rest Leafs of quadant(?)
            (/) _t (double _n)                  

    [<Extension>]
    static member set(this:Quadtree.Node<double>, i:int,j:int, value:double) =
        match (Quadtree.iterate_node i j this) with
        | Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome value
        | _ -> failwith "set failed"
        // | _ -> Quadtree.morph root cached_node add div 


system OnLoad [] (fun _ ->
    for img in System.IO.Directory.EnumerateFiles("volume_keyframes") do
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
)

system OnLoad [] (fun _ ->
    // init quadtrees and set boundaries and 
    for stencil in stencils do
        let T =
            stencil
            |> Quadtree.ofStencil<double> N 5 v_min v_max
            |> Quadtree.init 0.00

        T.Add <- (+)
        T.Div <- (/)
        quadtrees.Add(T)
)


system OnUpdate [] (fun _ ->
    for i in 1..quadtrees.Count-1 do
        let tree' = quadtrees[i-1]
        let tree = quadtrees[i]
        let dx = tree.dX
        let dy = tree.dY
        tree.IterParallel (fun node ->
            match (kindof dx dy node) with
            | Quadtree.Internal ->
                let (Quadtree.Leaf (_,_,_,_,v_min,v_max)) = node
                let c = Quadtree.center node
                let x = double c.X
                let y = double c.Y
                let T = node
                let T' = Quadtree.traverse_map (Vector2(float32 x, float32 y)) tree'.Root
                // tree'.MapTo(x,y)
                T.set(0,0, 2.*T'.get(0,0) + ratio*(T'.get(1,0) + T'.get(-1,0) - 4.* T'.get(0,0) + T'.get(0,1) + T'.get(0,-1)))
                
            | Quadtree.Boundary ->
                // tree[0,0] <- System.Drawing.Color.Red.ToArgb() |> double
                node.set(0, 0, 300.)

            | Quadtree.External ->
                match node with
                | Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome (System.Drawing.Color.Yellow.ToArgb() |> double)
                | _ -> ()
                            
        )
        tree.Update(_trim, _dense, _set)
        Quadtree.morph tree.Root tree'.Root (+) (/)
)

system OnExit [] (fun _ ->
    for tree in quadtrees do
        let mutable is_internal = 0
        let mutable is_boundary = 0
        let mutable is_external = 0
        let dx = tree.dX
        let dy = tree.dY
        tree.Root |> Quadtree.iter (fun node ->
            match (kindof dx dy node) with
            | Quadtree.Internal ->
                is_internal <- is_internal + 1
                
            | Quadtree.Boundary ->
                is_boundary <- is_boundary + 1

            | Quadtree.External ->
                is_external <- is_external + 1
        )
        printfn "total_nodes: %d" (tree.GetTotalCount())
        printfn "leaf_nodes: %d" (tree.GetCount())
        printfn "is_iteranal: %d" is_internal
        printfn "is_external: %d" is_external
        printfn "is_boundary: %d\n\n" is_boundary
)


system OnExit [] (fun _ ->
    let (z_min,z_max) =
        let mutable z_min = Double.MaxValue
        let mutable z_max = Double.MinValue
        for q in quadtrees do
            z_min <- min z_min (Array.min (q.GetValues()))
            z_max <- max z_max (Array.max (q.GetValues()))
        z_min,z_max
        
    let mutable ii = 0
    for T in quadtrees do
        let sb = System.Text.StringBuilder(1024*1024)
        let points = T.AsPoints()
        let xs = points |> Array.map (fun v -> double v.X)
        let ys = points |> Array.map (fun v -> double v.Y)
        let zs = T.GetValues() |> Array.map(fun x -> x)

        Gnuplot()
        |>> "set terminal pngcairo size 800,800"
        |>> $"set output 'volume_descretized/volume_{100 + ii}.png'"
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
        |> Gnuplot.datablockXYZ xs ys zs "centers"
        |>> "plot $centers using 1:2:3 with points lc palette"
        |> Gnuplot.run
        |> ignore

        ii <- ii + 1
)

#time
Systems.progress_N (Some 100)
printfn "for N= %d" N
#time
printfn "\n"

// convert -delay 20 -loop 0 *.png swallow_volume.gif
// ^^^ use this command to export frames to .gif
