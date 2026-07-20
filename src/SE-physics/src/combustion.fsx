#nowarn "632"
#load "../../SE-core/src/unsafe.fs"
#load "../../SE-core/src/core.fs"
#load "./physics.fs"
// #r "../bin/Debug/net10.0/SE-core.dll"
// #r "../bin/Debug/net10.0/SE-physics.dll"

open SE
open SE.ECS
open SE.Physics
open FSharp.Data.UnitSystems.SI.UnitSymbols

open KineticsDSL

type Species =
    | H2 = 1
    | O2 = 2
    | H2O = 3
    | C = 4
    | CO = 5
    | CO2 = 6


system OnLoad [] (fun _ ->
    let h2 = entity_tagged "H2"
                |> Entity.add<Reactant>
                |> set Species.H2
                |> set {MR = 2.0<g/mol>}
                |> set {mol = 15.0<mol/m^3>}
                |> set {dC = 0.0<mol/m^3 s>}

    let o2 = entity_tagged "O2"
                |> Entity.add<Reactant>
                |> set Species.O2
                |> set {MR = 32.0<g/mol>}
                |> set {mol = 5.0<mol/m^3>}
                |> set {dC = 0.0<mol/m^3 s>}

    let h2o = entity_tagged "H2O"
                |> Entity.add<Product>
                |> set Species.H2O
                |> set {MR = 18.0<g/mol>}
                |> set {mol = 0.5<mol/m^3>}
                |> set {dC = 0.0<mol/m^3 s>}

    let c = entity_tagged "C"
                |> Entity.add<Reactant>
                |> set Species.C
                |> set {MR = 12.0<g/mol>}
                |> set {mol = 15.0<mol/m^3>}
                |> set {dC = 0.0<mol/m^3 s>}

    let co = entity_tagged "CO"
                |> Entity.add<Reactant>
                |> Entity.add<Product>
                |> set Species.CO
                |> set {MR = 28.0<g/mol>}
                |> set {mol = 0.0<mol/m^3>}
                |> set {dC = 0.0<mol/m^3 s>}

    let co2 = entity_tagged "CO2"
                |> Entity.add<Product>
                |> set Species.CO2
                |> set {MR = 40.0<g/mol>}
                |> set {mol = 0.0<mol/m^3>}
                |> set {dC = 0.0<mol/m^3 s>}

    let cv = entity_tagged "CV"
                |> Entity.add<ControlVolume>
                |> set {T = 300.0<K>}
                |> set {R = 8.314<J/mol K>}
    // the id of the node on the tree
    // relate each spacial node, control volume
    // as a chemical reactor
    
    relate cv h2 Species.H2   // attach the species to each control volume
    relate cv o2 Species.O2 
    relate cv h2o Species.H2O

    relate cv c Species.C
    relate cv co Species.CO
    relate cv co2 Species.CO2
)

// run the reactions on each control volume
system OnUpdate [typeof<ControlVolume>] (fun q ->    
    let T = Components.get<Temperature>()
    let pressure = 101325<Pa>
    let A = 70000.0<J/mol>  // activation energy
    let k = 1e8             // pre-expotnetial factor
    let dt = 0.01<s>
    let H = 0.<J/mol>
    let Cp = 1000.<J/mol K>

    // runs for every control volume in Quadtree/Octree
    for cv in q do
        let H2  = Relation.ofValue Out cv Species.H2 (=) 
        let O2  = Relation.ofValue Out cv Species.O2 (=) 
        let H2O = Relation.ofValue Out cv Species.H2O (=)        
        let C   = Relation.ofValue Out cv Species.C (=) 
        let CO  = Relation.ofValue Out cv Species.CO (=) 
        let CO2 = Relation.ofValue Out cv Species.CO2 (=) 

        let reactions = [
            2.*H2 + 1.*O2 <=> [2*H2O]  // H2 + O2 = H2O
            2.*C + 1.*O2 <=> [2.*CO] 
            1.*C + 1.*O2 <=> [1.*CO2] 
            1.*CO + 0.5*O2 <=> [1.*CO2]
        ]

        integrate_step_DSL k A cv reactions dt H Cp
)

system OnExit [typeof<ControlVolume>] (fun q ->
    let C = Components.get<Concentration>()
    let T = Components.get<Temperature>()
    let species = Components.get<Species>().Entities

    for cv in q do
        for e in species do
            if Relation.exists<Species> e cv then
                printfn "%s: %g<mol>" (Entity.sprintf e) (C[e].mol)    
                
        printfn "Temperature: %g<K>" (T[cv].T)
)

Systems.progress_N (Some 20)

