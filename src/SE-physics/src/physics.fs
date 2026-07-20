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

[<Flags>]
type Reaction =
    | Exothermic = 0
    | Endothermic = 1

module Kinetics =

    let rate_constant (A:double) (Ea:double<J/mol>) (R:double<J/mol K>) (T:double<K>) =
        A * exp(-Ea / (R * T))

    let update_temperature (T:double<K>) (H:double<J/mol>) (cp:double<J/mol K>) =
        if cp > 0.0<J/mol K> then
            T + (H / cp)
        else
            T

    let reaction_rate (A:double) (E:double<J/mol>) (cv:Entity) (reactants:Span<Entity>) =
        let stoichiometry = Components.get<Stoichimetry>()
        let C = Components.get<Concentration>()
        let T = (Components.get<Temperature>()[cv]).T
        let R = (Components.get<GasConstant>()[cv]).R
        let mutable concentration_term = 1.0

        for r in reactants do
            concentration_term <- concentration_term * (pown (double C[r].mol) stoichiometry[r].N)

        concentration_term * (rate_constant A E R T) * (1.0<mol/m^3 s>)
        

    let calculate_derivatives A k cv (reactants:Span<Entity>) (products:Span<Entity>) =
        let S = Components.get<Stoichimetry>()
        // let C = Components.get<Concentration>()
        let dC = Components.get<Derivative>()
        let rate = reaction_rate A k cv reactants 
        
        for r in reactants do
            dC[r].dC <- dC[r].dC - double(S[r].N) * rate

        for p in products do
            dC[p].dC <- dC[p].dC + double(S[p].N) * rate


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


module KineticsDSL = 

    // let set = Entity.set
    type Concentrations = System.Collections.Generic.Dictionary<Entity, double<mol/m^3>>
    type Derivatives = System.Collections.Generic.Dictionary<Entity, double<mol/m^3 s>>

    let reaction_rate_DSL (A:double) (E:double<J/mol>) (cv:Entity) (reactants:list<double*Entity>) =
        let C = Components.get<Concentration>()
        let T = (Components.get<Temperature>()[cv]).T
        let R = (Components.get<GasConstant>()[cv]).R
        let mutable concentration_term = 1.0

        for (a,r) in reactants do
            concentration_term <- concentration_term * Math.Pow(double C[r].mol, a)

        concentration_term * (Kinetics.rate_constant A E R T) * (1.0<mol/m^3 s>)
        

    let calculate_derivatives_DSL A k cv (reactions:list<list<double*Entity> * list<double*Entity>>) =
        let derivatives = System.Collections.Generic.Dictionary<Entity,double<mol/m^3 s>>()
        
        for (reactants,products) in reactions do
            for (_,r) in reactants do ignore (derivatives.TryAdd(r, 0.0<mol/m^3 s>))
            for (_,p) in products do  ignore (derivatives.TryAdd(p, 0.0<mol/m^3 s>))

        for (reactants,products) in reactions do
            let rate = reaction_rate_DSL A k cv reactants 
        
            for (a,r) in reactants do
                derivatives[r] <- derivatives[r] - a * rate 

            for (b,p) in products do
                derivatives[p] <- derivatives[p] + b * rate 

        derivatives
        


    let update_concentrations_DSL (concentrations:Concentrations) (derivatives:Derivatives) (dt:double<s>) =
        let C = Components.get<Concentration>()

        for e in derivatives.Keys do
            C[e].mol <- concentrations[e] + derivatives[e] * dt


    let integrate_step_DSL A k cv reactions (dt:double<s>) (H:double<J/mol>) (cp:double<J/mol K>) =
        let C = Components.get<Concentration>()
        let T = Components.get<Temperature>()
        
        // store initiali concentrations
        let y =
            let c = Concentrations()
            for e in C.Entities do
                c.TryAdd(e, C[e].mol) |> ignore
            c
        
        // k1
        let k1 = calculate_derivatives_DSL A k cv reactions

        // k2
        update_concentrations_DSL y k1 (dt / 2.)
        let k2 = calculate_derivatives_DSL A k cv reactions

        // k3
        update_concentrations_DSL y k2 (dt / 2.)
        let k3 = calculate_derivatives_DSL A k cv reactions
        
        // k4
        update_concentrations_DSL y k3 dt
        let k4 = calculate_derivatives_DSL A k cv reactions

        for e in C.Entities do
            C[e].mol <- y[e] + (dt / 6.) * ((k1[e] + 2.*k2[e] + 2.*k3[e] + k4[e]))

            if C[e].mol < 0.<mol/m^3> then
                C[e].mol <- 0.<mol/m^3>

        T[cv].T <- Kinetics.update_temperature T[cv].T H cp

        

    let (*) (a:double) (e:Entity) = (a,e)
    let (+) (a:double*Entity) (b:double*Entity) = [a;b]
    // let (+) (a:double*Entity) (b:list<double*Entity>) = [a]@b
    // let (+) (a:list<double*Entity>) (b:list<double*Entity>) = a@b

    // let (<=>) (reactants:list<double*Entity>) (product:double*Entity) =
    //     // let c0 = List.unzip reactants |> fst
    //     // let r0 = List.unzip reactants |> snd
    //     // (c0,r0, fst product, snd product)
    //     (reactants,product)
            
    let (<=>) (reactants:list<double*Entity>) (products:list<double*Entity>) =
        (reactants, products)
            
            
