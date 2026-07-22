#nowarn "632"
// #load "../../SE-core/src/unsafe.fs"
// #load "../../SE-core/src/core.fs"
#r "../bin/Debug/net10.0/SE-core.dll"
#r "../bin/Debug/net10.0/SE-renderer.dll"
// #r "../bin/Debug/net10.0/SE-physics.dll"
#load "../src/physics.fs"

open SE
open SE.Core
open SE.ECS
open SE.Plotting
open SE.Renderer
open SE.Physics

open System
open System.Numerics
open FSharp.Data.UnitSystems.SI.UnitSymbols
open KineticsDSL

type E' =
    | H2 = 1
    | O2 = 2
    | H2O = 3
    | C = 4
    | CO = 5
    | CO2 = 6

let MR = Map [
    E'.H2, 2.<g/mol>
    E'.O2, 32.<g/mol>
    E'.H2O, 18.<g/mol>
    E'.C, 12.<g/mol>
    E'.CO, 28.<g/mol>
    E'.CO2, 46.<g/mol>
]

let DeltaH = Map [
    E'.H2, -102.<J>
    E'.O2, -320.<J>
    E'.H2O, 180.<J>
    E'.C, -120.<J>
    E'.CO, 280.<J>
    E'.CO2, 460.<J>
]

type Time = struct end
type Time' = struct end

// define custome operator to excract the concentration of element from control_volume cell
let ( --> ) (cv:Entity) (s:E') = Relation.ofValue Out cv s (=)

let inline center node = 
    let p = Octree.center node
    (double p.X, double p.Y, double p.Z)

// let relate_w cv value s = relate cv s value

let [<Literal>] N = 50
let [<Literal>] L = 10
let [<Literal>] k = 2
let [<Literal>] dt = 0.1<s>

let tree = 
    let mesh = RGeometry.load_ply_unmanaged("../bun_zipper.ply", 0.55f, 0.55f, 0.53f, 1.f)
    let oc = Octree.ofSurface<Entity> N L k (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
    mesh.indices.Dispose()
    mesh.vertices.Dispose()
    oc

let tree' = tree.Copy()

let mutable n_iter = 100
let points = ResizeArray<Vector4>(10000)


printfn "tree.count:    %d" (tree.GetCount())
printfn "tree.internal: %d" (tree.GetInternalCount())
printfn "tree.boundary: %d" (tree.GetBoundaryCount())

// exit 0

let mixture   = [5.0; 4.1; 0.8; 0.09; 0.004; 0.02]  // mean values of gas-composition mixture
let variances = [0.5; 0.05; 0.3; 0.6; 0.8; 0.1] // vaurinaces on each cell for the mixture


// computes initial mixture concentrations distribution on cells
let initialize_concentrations (cv:Entity) = 
    let cell_composition =
        (mixture,variances)
        ||> List.zip 
        |> List.mapi (fun i (x,y) -> x + y * (0.5 - Random.Shared.NextDouble()))
        |> List.mapi (fun i x -> Math.Clamp(x, 0.0, mixture[i] + variances[i]))
        |> List.map (fun x -> x * 1.<mol/m^3>)

    let h2  = entity_tagged "H2"  |> set {mol = 15.0<mol/m^3>}
    let o2  = entity_tagged "O2"  |> set {mol = 5.0<mol/m^3>}
    let h2o = entity_tagged "H2O" |> set {mol = 0.5<mol/m^3>}
    let c   = entity_tagged "C"   |> set {mol = 15.0<mol/m^3>}
    let co  = entity_tagged "CO"  |> set {mol = 0.0<mol/m^3>}
    let co2 = entity_tagged "CO2" |> set {mol = 0.0<mol/m^3>}

    relate cv h2 E'.H2   // attach the species to each control volume
    relate cv o2 E'.O2 
    relate cv h2o E'.H2O
    relate cv c E'.C
    relate cv co E'.CO
    relate cv co2 E'.CO2


// initialize trees
system OnLoad [] (fun _ ->
    // cannot run in parallel, it creates new entities
        tree.Iter (fun node ->
            match node with
            | Octree.Leaf (_,v,_,_,v_min,v_max) & Octree.Internal ->
                let dv = v_max - v_min
                let p  = v_min + (v_max + v_min) / 2.f
                let cv = entity_tagged "CV"
                            |> Entity.add<ControlVolume> |> Entity.add<Time>
                            |> set {T = 300.0<K>} |> set {R = 8.314<J/mol K>} |> set p
                            |> set {vol = abs(double(dv.X * dv.Y * dv.Z)) * 1.<m^3>}
                v.Value <- ValueSome cv
                initialize_concentrations cv

            | Octree.Leaf (_,v,_,_,v_min,v_max) & Octree.Boundary ->
                let dv = v_max - v_min
                let p  = v_min + (v_max + v_min) / 2.f
                let cv = entity_tagged "CV"
                            |> Entity.add<ControlVolume> |> Entity.add<Time>
                            |> set {T = 600.0<K>} |> set {R = 8.314<J/mol K>} |> set p
                            |> set {vol = abs(double(dv.X * dv.Y * dv.Z)) * 1.<m^3>}
                v.Value <- ValueSome cv
                initialize_concentrations cv
        
            | _ -> ()
        )

        tree'.Iter (fun node ->
            match node with
            | Octree.Leaf (_,v,_,_,v_min,v_max) & (Octree.Internal | Octree.Boundary) ->
                let dv = v_max - v_min
                let p  = v_min + (v_max + v_min) / 2.f
                let cv = entity_tagged "CV"
                            |> Entity.add<ControlVolume> |> Entity.add<Time'>
                            |> set {T = 300.0<K>} |> set {R = 8.314<J/mol K>} |> set p
                            |> set {vol = abs(double(dv.X * dv.Y * dv.Z)) * 3.<m^3>}
                v.Value <- ValueSome cv
            | _ -> ()
        )
)

// // compute chemical kinetics
system OnUpdate [typeof<Time>; typeof<ControlVolume>] (fun q ->    
    let T = Components.get<Temperature>()
    let V = Components.get<Volume>()
    let c = Components.get<Concentration>()

    // runs for every control volume in Quadtree/Octree
    parallel_for q (fun cv -> 
        let H2  = cv --> E'.H2   
        let O2  = cv --> E'.O2   
        let H2O = cv --> E'.H2O          
        let C   = cv --> E'.C   
        let CO  = cv --> E'.CO   
        let CO2 = cv --> E'.CO2   

        let reactions = [
            // 2.0**H2 ++ 1.0**O2 <=> [2.0**H2O]  // H2 + O2 = H2O
            // 2.0**C  ++ 1.0**O2 <=> [2.0**CO] 
            // 1.0**C  ++ 1.0**O2 <=> [1.0**CO2] 
            // 1.0**CO ++ 0.5**O2 <=> [1.0**CO2]
            [2.,H2; 1.,O2] <=> [2.,H2O]
            [2.,C;  1.,O2] <=> [2.,CO]
            [1.,C;  1.,O2] <=> [1.,CO2]
            [1.,CO; 0.5,O2] <=> [1.,CO2]
        ]

        let pressure = 101325<Pa>
        let A = 70000.0<J/mol>  // activation energy
        let k' = 1e8             // pre-expotnetial factor
        // calculate the H and Cp after reaction
        // let mutable H = 0.<J/mol>
        let mutable Cp = 1000.<J/mol K>
        // let mutable m = 0.<mol/m^3>

        let species = stackalloc 6
        let deltah  = stackalloc 6
        do
            species[0] <- H2
            species[1] <- O2
            species[2] <- H2O
            species[3] <- C
            species[4] <- CO
            species[5] <- CO2
            deltah[0] <- DeltaH[E'.H2]
            deltah[1] <- DeltaH[E'.O2]
            deltah[2] <- DeltaH[E'.H2O]
            deltah[3] <- DeltaH[E'.C]
            deltah[4] <- DeltaH[E'.CO]
            deltah[5] <- DeltaH[E'.CO2]

        let dh = measure_H_DSL cv species deltah V[cv].vol
        integrate_step_DSL k' A cv reactions dt
        T[cv].T <- update_temperature_DSL T[cv].T dh Cp
    )
)


// compute Temperature - heat transfer over time
system OnUpdate [typeof<Time'>; typeof<ControlVolume>] (fun _ ->
    let T = Components.get<Temperature>()    

    tree'.IterParallel 4 (fun node ->
        match node with
        | Octree.Internal ->
            let (x,y,z) = center node
            let t  = tree[x,y,z].Value
            let t' = tree'[x,y,z].Value
            let dT = 1.<K/s> * Numerics.derivative2 node (fun n -> double T[(Octree.valueof n)].T)
            let a = 2700.0  // temp transfer capacity coefficient whatever
            T[t'] <- {T = dt * a * dT + T[t].T}
        | _ -> ()
    )

    // copy old tree
    tree.IterParallel 4 (fun node ->
        match node with
        | Octree.Internal | Octree.Boundary ->
            let (x,y,z) = center node
            let t  = tree[x,y,z].Value
            let t' = tree'[x,y,z].Value
            T[t] <- T[t']
        | _ -> ()
    )
)

// plot each time frame
system OnUpdate [typeof<Time>; typeof<ControlVolume>] (fun q ->
    let T = Components.get<Temperature>()
    let C = Components.get<Concentration>()
    let V = Components.get<Vector3>()
    n_iter <- n_iter + 1
    
    points.Clear()
    for cv in q do
        points.Add(Vector4(V[cv], float32 T[cv].T))
        
    let pts = points.ToArray()
    let xs = pts |> Array.map (fun v -> double v.X)
    let ys = pts |> Array.map (fun v -> double v.Y)
    let zs = pts |> Array.map (fun v -> double v.Z)
    let ws = pts |> Array.map (fun v -> double v.W)

    Gnuplot()
    |> Gnuplot.datablockXYZW xs ys zs ws "points"
    |>> "set terminal pngcairo size 800,800"
    |>> $"set output 'frames/frame_{n_iter}.png'"
    |>> "unset key"
    // |>> "set view 60,30,30,1"
    |>> "set view equal xyz"
    |>> "splot $points using 1:2:3:4 with points lc palette "
    |> Gnuplot.run
    |> ignore
)

Systems.progress_N (Some 20) true

printfn "systems exited"


// Console.ReadKey()
