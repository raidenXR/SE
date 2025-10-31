#load "../src/core.fs"
#load "../src/gnuplot.fs"

open System
open SE.Core
open SE.Plotting
open FSharp.Data.UnitSystems.SI.UnitSymbols

// components
type [<Struct>] Temperature   = {mutable temp:  float<K>}
type [<Struct>] Molarity      = {mutable moles: float<mol>}
type [<Struct>] Mass = {mutable mass: float<kg>}
type [<Struct>] Stoichiometry = {coeff: float}
type [<Struct>] MolecularMass = {MR: float<kg mol^-1>}

// tags
type Reactant = struct end
type Product = struct end

// variables
let mutable time = 0.0<s>
let mutable temp = 300.0<K>
let mutable equilibrium = false


// static fields
let dt = 1.0<s>
let reaction_rate = 0.01
let values = System.Collections.Generic.Dictionary<Entity,ResizeArray<float>>()
// let gnuplot = Gnuplot(true, false, Some "output/output.png")
let gnuplot = Gnuplot()
let dr = Array.zeroCreate<array<float>> 2
let dp = Array.zeroCreate<array<float>> 2
let dr_names = Array.zeroCreate<string> 2
let dp_names = Array.zeroCreate<string> 2


let resetline f =
    let struct(a,b) = Console.GetCursorPosition()
    f
    let struct(i,j) = Console.GetCursorPosition()
    let y = j - b
    let k = min b (abs (y - 1))
    Console.SetCursorPosition(0, b - 1)


// create elements
system OnLoad [] (fun _ ->
    let A = entity_tagged "C2H6"
            |> Entity.add<Reactant>
            |> Entity.set {coeff = 1}
            |> Entity.set {moles = 4.3<mol>}
            |> Entity.set {mass = 0.7<kg>}
            |> Entity.set {MR = 34.123<kg mol^-1>}

    let B = entity_tagged "O2"
            |> Entity.add<Reactant>
            |> Entity.set {coeff = 5}
            |> Entity.set {moles = 6.3<mol>}
            |> Entity.set {mass = 0.7<kg>}
            |> Entity.set {MR = 34.123<kg mol^-1>}

    let C = entity_tagged "H2O"
            |> Entity.add<Product>
            |> Entity.set {coeff = 3}
            |> Entity.set {moles = 0.0<mol>}
            |> Entity.set {mass = 0.0<kg>}
            |> Entity.set {MR = 34.123<kg mol^-1>}
    
    let D = entity_tagged "CO2"
            |> Entity.add<Product>
            |> Entity.set {coeff = 2}
            |> Entity.set {moles = 0.0<mol>}
            |> Entity.set {mass = 0.0<kg>}
            |> Entity.set {MR = 34.123<kg mol^-1>}

    gnuplot
    |>>  "set title 'Gnuplot A'"
    |>> "array Molecule[4]"
    |>> "Molecule[1] = 'C2H6'"
    |>> "Molecule[2] = 'O2'"
    |>> "Molecule[3] = 'H2O'"
    |>> "Molecule[4] = 'CO2'"
    |>> "plot 0"
    |> Gnuplot.run
    |> ignore
)

system OnUpdate [typeof<Reactant>] (fun q ->
    let mols = Components.span<Molarity> q
    let stoich = Components.span<Stoichiometry> q
    let mass = Components.span<Mass> q
    let mrs = Components.span<MolecularMass> q
    let mutable n = 0

    for i in 0..q.Count - 1 do
        let r = q[i]
        let mr = mrs[i].MR
        let coeff = stoich[i].coeff
        let r_mol = &mols[i]
        let r_mass = &mass[i]
        let dm = reaction_rate * r_mol.moles * coeff
        if not (values.ContainsKey(r)) then values.Add(r, ResizeArray<float>())

        equilibrium <- equilibrium || if abs(r_mol.moles - dm) < 0.0<mol> then true else false
        // if equilibrium then 
        //     printfn "%s : dm: %g" (Entity.sprintf r) dm
        if not equilibrium then
            r_mol.moles <- r_mol.moles - dm
            r_mass.mass <- r_mass.mass - r_mol.moles * mr
            values[r].Add(float r_mol.moles)
            
            dr[n] <- values[r].ToArray()
            dr_names[n] <- Entity.sprintf r
            n <- n + 1
)

system OnUpdate [typeof<Product>] (fun q ->
    let mols = Components.span<Molarity> q
    let stoich = Components.span<Stoichiometry> q
    let mass = Components.span<Mass> q
    let mrs = Components.span<MolecularMass> q
    let mutable n = 0

    if not equilibrium then
        for i in 0..q.Count - 1 do
            let p = q[i]
            let mr = mrs[i].MR
            let coeff = stoich[i].coeff
            let p_mol = &mols[i]
            let p_mass = &mass[i]
            let dm = reaction_rate * coeff
            // printfn "%s : dm: %g" (Entity.sprintf p) dm
            if not (values.ContainsKey(p)) then values.Add(p, ResizeArray<float>())
            p_mol.moles <- p_mol.moles + dm * 1.0<mol>
            p_mass.mass <- p_mass.mass + p_mol.moles * mr
            values[p].Add(float p_mol.moles)

            dp[n] <- values[p].ToArray()
            dp_names[n] <- Entity.sprintf p
            n <- n + 1   

    let data = [| dr[0]; dr[1]; dp[0]; dp[1] |]
    // printfn "%A" data
    let data_names = [|dr_names[0]; dr_names[1]; dp_names[0]; dp_names[1]|]
    gnuplot
    |> Gnuplot.stringArray data_names "Molecule" 
    |> Gnuplot.datablockN data "Data"
    |>> $"plot for [i = 1:4] $Data using i title Molecule[i] w lines lw 2"
    |> ignore
)

system OnUpdate [] (fun _ ->
    time <- time + dt
    resetline (printfn "time: %g" time)
    if time >= 60.0<s> then Systems.quit()    
    if equilibrium then Systems.quit()

    System.Threading.Thread.Sleep(int (dt * 1000.0))
)

system OnExit [] (fun _ ->
    let reactants = query [typeof<Reactant>]
    let products  = query [typeof<Product>]

    let moles = Components.get<Molarity>()
    let mass  = Components.get<Mass>()

    for r in reactants do
        printfn "%s: mol: %g, m: %g" (Entity.sprintf r) (moles[r].moles) (mass[r].mass)

    for p in products do
        printfn "%s: mol: %g, m: %g" (Entity.sprintf p) (moles[p].moles) (mass[p].mass)

    // gnuplot.Run()
    gnuplot.Close()
)


Systems.progress()

// printfn "%A" [dr[0]; dr[1]; dp[0]; dp[1]]
Console.ReadKey()
