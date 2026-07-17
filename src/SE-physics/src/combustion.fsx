#nowarn "632"
#r "../bin/Debug/net10.0/SE-core.dll"
#r "../bin/Debug/net10.0/SE-physics.dll"

open SE
open SE.ECS
open SE.Physics
open FSharp.Data.UnitSystems.SI.UnitSymbols

// All chemical species are defined as tags too.
type H2 = struct end
type O2 = struct end
type H2O = struct end

let strs = System.Collections.Generic.Dictionary<Entity,string>() // match species with their strings-names


system OnLoad [] (fun _ ->
    let h2 = entity()
                |> Entity.add<Reactant>
                |> Entity.set {MR = 2.0<g/mol>}
                |> Entity.set {mol = 15.0<mol/m^3>}
                |> Entity.set {dC = 0.0<mol/m^3 s>}

    let o2 = entity()
                |> Entity.add<Reactant>
                |> Entity.set {MR = 32.0<g/mol>}
                |> Entity.set {mol = 5.0<mol/m^3>}
                |> Entity.set {dC = 0.0<mol/m^3 s>}

    let h2o = entity()
                |> Entity.add<Product>
                |> Entity.set {MR = 18.0<g/mol>}
                |> Entity.set {mol = 0.5<mol/m^3>}
                |> Entity.set {dC = 0.0<mol/m^3 s>}

    let cv = entity()
                |> Entity.add<ControlVolume>
                |> Entity.set {T = 300.0<K>}
                |> Entity.set {R = 8.314<J/mol K>}
    // the id of the node on the tree
    // relate each spacial node, control volume
    // as a chemical reactor
    relate cv h2 (H2())  
    relate cv o2 (O2()) 
    relate cv h2o (H2O()) 

    strs.Add(h2,"H2")
    strs.Add(o2,"O2")
    strs.Add(h2o,"H2O")
    
    // relate h2  node_id (H2()) 
    // relate o2  node_id (O2()) 
    // relate h2o node_id (H2O()) 
    
)

// H2 + O2 -> H20
system OnUpdate [typeof<ControlVolume>] (fun q ->    
    let C = Components.get<Concentration>()
    let T = Components.get<Temperature>()
    let S = Components.get<Stoichimetry>()
    let R = Components.get<GasConstant>()
    let dC = Components.get<Derivative>()
    let dt = 0.01<s>

    // runs for every control volume in Quadtree/Octree
    for cv in q do
        let h2 = Relation.get<H2> Out cv
        let o2 = Relation.get<O2> Out cv
        let h2o = Relation.get<H2O> Out cv

        h2 |> Entity.set {N=2} |> ignore
        o2 |> Entity.set {N=1} |> ignore
        h2o |> Entity.set {N=2} |> ignore

        let A = 7000.0<J/mol>  // activation energy
        let k = 1e4             // pre-expotnetial factor
        
        let reactants = stackalloc 2 
        reactants[0] <- h2
        reactants[1] <- o2

        let products = stackalloc 1
        products[0] <- h2o

        Kinetics.calculate_derivatives A k cv reactants products         
        Kinetics.update_concentrations reactants products dt
        // Kinetics.integration_step ()  <-- this is not implemented yet AND the numerical results are not correct

        let H = 90.0<J/mol>
        let Cp = 100.0<J/mol K>
        T[cv].T <- Kinetics.update_temperature (T[cv].T) H Cp
)


system OnExit [typeof<Reactant>] (fun q ->
    let C = Components.get<Concentration>()
    
    for e in q do
        printfn "%s: %g<mol>" (strs[e]) (C[e].mol)    
)

system OnExit [typeof<Product>] (fun q ->
    let C = Components.get<Concentration>()
    
    for e in q do
        printfn "%s: %g<mol>" (strs[e]) (C[e].mol)    
)

system OnExit [typeof<ControlVolume>] (fun q ->
    let T = Components.get<Temperature>()

    for cv in q do
        printfn "Temperature: %g<K>" (T[cv].T)
)

Systems.progress_N (Some 12)

