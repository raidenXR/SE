#nowarn "632"
namespace SE.Physics
open System
open FSharp.Data.UnitSystems.SI.UnitSymbols
open SE.ECS

[<Measure>] type g

[<Struct>] type MolecularWeight = {MR:double<g/mol>}
[<Struct>] type HeatOfFormation = {h:double<J/mol>}
[<Struct>] type HeatCapacity = {cp:double<J/mol K>}
[<Struct>] type Concentration = {mutable mol:double<mol/m^3>}
[<Struct>] type Derivative = {mutable dC:double<mol/m^3 s>}

[<Struct>] type Temperature = {mutable T:double<K>}
[<Struct>] type Pressure = {mutable P:double}
[<Struct>] type GasConstant = {R:double<J/mol K>}

[<Struct>] type PreExponentialFactor = {A:double}
[<Struct>] type ActivationEnergy = {E:double<J/mol>}
[<Struct>] type ReactionOrder = {n:int}

[<Struct>] type Stoichimetry = {N:int}

// TAGS --> structs with no fields
type Reactant = struct end
type Product  = struct end
type ControlVolume = struct end


module Tables = 
    let species = [
        "H", 1
        "He", 2
        "Ar", 3
    ]

module Kinetics =

    let rate_constant (A:double) (Ea:double) (R:double) (_T:double<K>) =
        let T = double _T
        A * exp(-Ea / (R * T))

    let update_temperature (T:double<K>) (H:double<J/mol>) (cp:double<J/mol K>) =
        if cp > 0.0<J/mol K> then
            T + (H / cp)
        else
            T

    let reaction_rate (_A:double<J/mol>) (E:double) (cv:Entity) (reactants:Span<Entity>) =
        let A = double _A
        let stoichiometry = Components.get<Stoichimetry>()
        let C = Components.get<Concentration>()
        let T = (Components.get<Temperature>()[cv]).T
        let R = (Components.get<GasConstant>()[cv]).R
        let mutable concentration_term = 1.0

        for r in reactants do
            concentration_term <- concentration_term * (pown (double C[r].mol) stoichiometry[r].N)

        concentration_term * (rate_constant A E (double R) T) * (1.0<mol/m^3 s>)
        

    let calculate_derivatives A k cv (reactants:Span<Entity>) (products:Span<Entity>) =
        let S = Components.get<Stoichimetry>()
        let C = Components.get<Concentration>()
        let dC = Components.get<Derivative>()
        let rate = reaction_rate A k cv reactants 
        
        for r in reactants do
            dC[r].dC <- dC[r].dC - (double(S[r].N) * rate)

        for p in products do
            dC[p].dC <- dC[p].dC + (double(S[p].N) * rate)


    let integration_step () =
        failwith "NOT IMPLEMENTED"


    let update_concentrations (reactants:Span<Entity>) (products:Span<Entity>) (dt:double<s>) =
        let C = Components.get<Concentration>()
        let dC = Components.get<Derivative>()
        
        for r in reactants do
           C[r].mol <- C[r].mol + (dC[r].dC * dt)
    
        for p in products do
           C[p].mol <- C[p].mol + (dC[p].dC * dt)
    
    // ignore that part, it will as a system for each different reaction
    // let derivatives (reactants:Entities) (products:Entities) =
        // ()
