#nowarn "632"
namespace SE.Physics
open SE.Core
open SE.ECS
open System
open System.Numerics
open FSharp.Data.UnitSystems.SI.UnitSymbols

[<Measure>] type g

[<Struct>] type MolecularWeight = {MR:double<g/mol>}
[<Struct>] type HeatOfFormation = {h:double<J/mol>}
[<Struct>] type HeatCapacity = {cp:double<J/mol K>}
[<Struct>] type Concentration = {mutable mol:double<mol/m^3>}
[<Struct>] type Derivative = {mutable dC:double<mol/m^3 s>}

[<Struct>] type Temperature = {mutable T:double<K>}
[<Struct>] type Pressure = {mutable P:double}
[<Struct>] type GasConstant = {R:double<J/mol K>}

[<Struct>] type Volume = {vol:double<m^3>}

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


module Numerics =

    let derivative1 (node:Octree.Node<'T>) (F:Octree.Node<'T> -> double) =
        let u = node[ 0,0,0]
        let a = node[-1,0,0]
        let b = node[+1,0,0]
        let c = node[0,-1,0]
        let d = node[0,+1,0]
        let f = node[0,0,-1]
        let g = node[0,0,+1]

        let xh1 = (Octree.center u).X - (Octree.center a).X |> double
        let xh2 = (Octree.center b).X - (Octree.center u).X |> double
        let yh1 = (Octree.center u).Y - (Octree.center c).Y |> double
        let yh2 = (Octree.center c).Y - (Octree.center u).Y |> double
        let zh1 = (Octree.center u).Z - (Octree.center f).Z |> double
        let zh2 = (Octree.center g).Z - (Octree.center u).Z |> double

        let x = ((xh2 / (xh1 * (xh1 + xh2))) * F(a)) +
                (((xh2 - xh1) / (xh1 * xh2)) * F(u)) -
                ((xh1 / (xh2 * (xh1 + xh2))) * F(b)) 
        let y = ((yh2 / (yh1 * (yh1 + yh2))) * F(a)) +
                (((yh2 - yh1) / (yh1 * yh2)) * F(u)) -
                ((yh1 / (yh2 * (yh1 + yh2))) * F(b)) 
        let z = ((zh2 / (zh1 * (zh1 + zh2))) * F(a)) +
                (((zh2 - zh1) / (zh1 * zh2)) * F(u)) -
                ((zh1 / (zh2 * (zh1 + zh2))) * F(b)) 
        x+y+z

        
    let derivative2 (node:Octree.Node<'T>) (F:Octree.Node<'T> -> double) =
        let u = node[ 0,0,0]
        let a = node[-1,0,0]
        let b = node[+1,0,0]
        let c = node[0,-1,0]
        let d = node[0,+1,0]
        let f = node[0,0,-1]
        let g = node[0,0,+1]

        let xh1 = (Octree.center u).X - (Octree.center a).X |> double
        let xh2 = (Octree.center b).X - (Octree.center u).X |> double
        let yh1 = (Octree.center u).Y - (Octree.center c).Y |> double
        let yh2 = (Octree.center c).Y - (Octree.center u).Y |> double
        let zh1 = (Octree.center u).Z - (Octree.center f).Z |> double
        let zh2 = (Octree.center g).Z - (Octree.center u).Z |> double

        let x = (2.0 / (xh1 * (xh1 + xh2))) * F(a) +
                (2.0 / (xh1 * xh2)) * F(u) -
                (2.0 / (xh2 * (xh1 + xh2))) * F(b) 
        let y = (2.0 / (yh1 * (yh1 + yh2))) * F(a) +
                (2.0 / (yh1 * yh2)) * F(u) -
                (2.0 / (yh2 * (yh1 + yh2))) * F(b) 
        let z = (2.0 / (zh1 * (zh1 + zh2))) * F(a) +
                (2.0 / (zh1 * zh2)) * F(u) -
                (2.0 / (zh2 * (zh1 + zh2))) * F(b) 
        x+y+z
        

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


    // let integrate_step_DSL A k cv reactions (dt:double<s>) (H:double<J/mol>) (cp:double<J/mol K>) =
    let integrate_step_DSL A k cv reactions (dt:double<s>) =
        let C = Components.get<Concentration>()
        let T = Components.get<Temperature>()
        
        // store initiali concentrations
        let y =
            let c = Concentrations()
            for e in C.Entities do
                if c.TryAdd(e, C[e].mol) then
                    ()
                else
                    failwith ("failed to add: " + (Entity.sprintf e))
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

        let species = Seq.concat [k1.Keys; k2.Keys; k3.Keys; k4.Keys] |> Seq.distinct

        for e in species do
        // for e in k1.Keys do
            C[e].mol <- y[e] + (dt / 6.) * ((k1[e] + 2.*k2[e] + 2.*k3[e] + k4[e]))

            if C[e].mol < 0.<mol/m^3> then
                C[e].mol <- 0.<mol/m^3>

        // T[cv].T <- Kinetics.update_temperature T[cv].T H cp

    let measure_H_DSL (cv:Entity) (species:Span<Entity>) (H:Span<double<J>>) (v:double<m^3>) =
        let C = Components.get<Concentration>()
        let mutable m = 0.<mol/m^3>
        let mutable h = 0.<J/mol>
        
        for i in 0..species.Length-1 do
            let e = species[i]
            m <- m + C[e].mol
            h <- h + H[i] / (m * v)    
        h
        
    let update_temperature_DSL (T:double<K>) (H:double<J/mol>) (cp:double<J/mol K>) =
        if cp > 0.0<J/mol K> then
            T + (H / cp)
        else
            T

    let ( ** ) (a:double) (e:Entity) = (a,e)
    let ( ++ ) (a:double*Entity) (b:double*Entity) = [a;b]
    // let (+) (a:double*Entity) (b:list<double*Entity>) = [a]@b
    // let (+) (a:list<double*Entity>) (b:list<double*Entity>) = a@b

    // let (<=>) (reactants:list<double*Entity>) (product:double*Entity) =
    //     // let c0 = List.unzip reactants |> fst
    //     // let r0 = List.unzip reactants |> snd
    //     // (c0,r0, fst product, snd product)
    //     (reactants,product)
            
    let (<=>) (reactants:list<double*Entity>) (products:list<double*Entity>) =
        (reactants, products)
            
            
