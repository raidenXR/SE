#r "../bin/Debug/net10.0/SE-core.dll"

open System
open System.Numerics
open SE
open SE.ECS
open SE.Core
open SE.Plotting

type [<Struct>] Temperature = {T:double}
type [<Struct>] Pressure = {P:double}
type [<Struct>] Velocity = {vx:double; vy:double}

let N = 400
let L = 10
let v_min = -Vector2.One
let v_max = Vector2.One

let mutable n_iter = 0

let bits = System.Collections.BitArray(N*N)
// let quadtree = lazy (Quadtree.ofStencil<Entity> N 0 v_min v_max bits)
let quadtree = lazy (Quadtree.Root<Entity>(N, 4, v_min, v_max))
let stopwatch = System.Diagnostics.Stopwatch()
 
system OnLoad [] (fun _ ->    
    // for i in 0..N-1 do
    //     for j in 0..N-1 do
    //         bits[i*N+j] <- true
    
    for i in 0..N-1 do
        for j in 0..N-1 do
            // let v = GridGeneration2D.to_cartesian_system i j N v_min v_max
            let x = double j / double N - 0.05 * System.Random.Shared.NextDouble()
            let y = double i / double N - 0.05 * System.Random.Shared.NextDouble()
            let ent =
                entity()
                |> Entity.set {T=100.}
                |> Entity.set {vx=0.; vy=0.}
            
            quadtree.Value.Put(x,y,ValueSome ent)            

    stopwatch.Start()
)

system OnLoad [] (fun _ ->
    let T = Components.get<Temperature>()
    let u = quadtree.Value
    let dx = u.dX
    let dy = u.dY

    u.Root |> Quadtree.iter (fun node ->
        u.CurrentNode <- node
        match (Quadtree.kindof dx dy node) with
        | Quadtree.Boundary -> T[u[0,0]] <- {T = 300.0}            
        | _ -> ()
    )
)

system OnUpdate [] (fun _ ->
    let T = Components.get<Temperature>()
    let P = Components.get<Pressure>()
    let u = quadtree.Value
    let dx = u.dX
    let dy = u.dY

    u.Root |> Quadtree.iter (fun node ->
        u.CurrentNode <- node

        match (Quadtree.kindof dx dy node) with
        | Quadtree.Internal -> 
            let T_e = T[u[0,0]].T
            let T_h = T[u[0,1]].T
            let T_f = T[u[1,0]].T
            let T_d = T[u[0,-1]].T
            let T_b = T[u[-1,0]].T
            let (Quadtree.Leaf (_,_,_,_,v1,v2)) = node
            let DX = double (v2 - v1).X
            let DY = double (v2 - v1).Y
            ()

            // T[u[0,0]] <- {T = (T_f + T_b + T_h + T_d - 2.*T_e) / double(DX*DY)}     
            T[u[0,0]] <- {T = (T_f + T_h - 8.*T_e) / double(DX*DY)}     
            // T[u[0,0]] <- {T = 500.0}

        | Quadtree.Boundary ->
            T[u[0,0]] <- {T = 300.0}
            
        | Quadtree.External -> ()
    )
)

system OnUpdate [] (fun _ ->
    if n_iter > N then Systems.quit()
    n_iter <- n_iter + 1
)

system OnExit [] (fun _ ->
    stopwatch.Stop()
    let temp = Components.get<Temperature>()
    let sb = System.Text.StringBuilder(1024*1024)
    let u = quadtree.Value

    u.Root |> Quadtree.iter (fun node ->
        let (Quadtree.Leaf (_,v,_,_,_,_)) = node
        let c = Quadtree.center node
        let t = temp[v.Value.Value].T
        ignore (sb.AppendLine($"{c.X}  {c.Y}  {t}"))
    )
    let points = string sb
    sb.Clear() |> ignore

    Quadtree.write_rects_to_sb (u.Root) sb
    let rects = string sb

    printfn "stopwatch.elapsed: %A" (stopwatch.Elapsed)
    printfn "nodes.count: %d" (u.GetCount())
    
    Gnuplot()
    |> Gnuplot.datablockString points "Temp"
    |> Gnuplot.datablockString rects "Rects"
    |>> "plot $Temp with points pt 5 lc palette lw 2, \\"
    |>> "$Rects with lines lc rgb 'black'"
    |> Gnuplot.run
    |> ignore

    System.Console.ReadKey() |> ignore
)

Systems.progress()


