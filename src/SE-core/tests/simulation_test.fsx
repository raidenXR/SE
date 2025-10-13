#load "../src/Core.fs"
#load "../src/Gnuplot.fs"

open System
open SE.Core
open SE.Core.Plotting
open FSharp.Data.UnitSystems.SI.UnitSymbols

// components
type [<Struct>] Temperature   = {temp:  float<K>}
type [<Struct>] Molarity      = {moles: float<mol>}
type [<Struct>] Stoichiometry = {coeff: float}
type Mass = float<kg>
type Speed = float<m/s>

// tags
type Reactant = struct end
type Product = struct end

// variables
let mutable time = 0.0<s>
let mutable temp = 300.0<K>
let mutable equilibrium = false


// static fields
let dt = 1.0<s>
let reaction_rate = 0.05
let values = System.Collections.Generic.Dictionary<Entity,ResizeArray<float>>()
// let gnuplot = Gnuplot(true, true, None)
let gnuplot = Gnuplot()
let dr = Array.zeroCreate<array<float>> 2
let dp = Array.zeroCreate<array<float>> 2


let resetline f =
    let struct(a,b) = Console.GetCursorPosition()
    f
    let struct(i,j) = Console.GetCursorPosition()
    let y = j - b
    let k = min b (abs (y - 1))
    Console.SetCursorPosition(0, b - 1)


// create elements
system OnLoad [] (fun _ ->
    let A = entity ()
            |> Entity.add<Reactant>
            |> Entity.set {coeff = 2}
            |> Entity.set {moles = 4.3<mol>}
            |> Entity.set (0.1<kg>)
            |> Entity.set (0.00001<m/s>)

    let B = entity ()
            |> Entity.add<Reactant>
            |> Entity.set {coeff = 1}
            |> Entity.set {moles = 2.3<mol>}
            |> Entity.set (0.4<kg>)
            |> Entity.set (0.00001<m/s>)

    let C = entity ()
            |> Entity.add<Product>
            |> Entity.set {coeff = 1}
            |> Entity.set {moles = 0.0<mol>}
            |> Entity.set (0.0<kg>)
            |> Entity.set (0.00001<m/s>)
    
    let D = entity ()
            |> Entity.add<Product>
            |> Entity.set {coeff = 1}
            |> Entity.set {moles = 0.0<mol>}
            |> Entity.set (0.0<kg>)
            |> Entity.set (0.00001<m/s>)

    gnuplot
    |>>  "set title 'Gnuplot A'"
    |>> "plot 0"
    |> Gnuplot.run
    |> ignore
)

system OnUpdate [typeof<Reactant>] (fun q ->
    let molarity = Components.get<Molarity>()
    let stoichiometry = Components.get<Stoichiometry>()
    let mass_components = Components.get<float<kg>>()
    let mutable n = 0

    for r in q do
        let moles = molarity[r].moles
        let coeff = stoichiometry[r].coeff
        let mass = &mass_components[r]
        let dm = reaction_rate * moles * coeff
        if not (values.ContainsKey(r)) then values.Add(r, ResizeArray<float>())

        equilibrium <- equilibrium || if abs(moles - dm) < 0.09<mol> then true else false
        if not equilibrium then
            molarity[r] <- {moles = moles - dm}
            values[r].Add(float molarity[r].moles)
            mass <- molarity[r].moles * 0.7634234<kg mol^-1>
            
            dr[n] <- values[r].ToArray()
            n <- n + 1
)

system OnUpdate [typeof<Product>] (fun q ->
    let molarity = Components.get<Molarity>()
    let stoichiometry = Components.get<Stoichiometry>()
    let mass_components = Components.get<float<kg>>()
    let mutable n = 0

    if not equilibrium then
        for p in q do
            let moles = molarity[p].moles
            let mass = &mass_components[p]
            let m = &molarity[p]
            let coeff = stoichiometry[p].coeff
            let dm = reaction_rate * coeff * 1.0<mol>
            if not (values.ContainsKey(p)) then values.Add(p, ResizeArray<float>())
            m <- {moles = moles + dm}
            mass <- 0.453545<kg mol^-1> * m.moles
            // molarity[p] <- {moles = moles + dm}
            values[p].Add(float molarity[p].moles)

            dp[n] <- values[p].ToArray()
            n <- n + 1   

    let data = [| dr[0]; dr[1]; dp[0]; dr[1] |]
    gnuplot
    |> Gnuplot.datablockN data "Data"
    |>> "plot for [i = 1:4] $Data using i title 'element '.i w lines lw 2"
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
    let mass  = Components.get<float<kg>>()
    let speed = Components.get<float<m/s>>()

    for r in reactants do
        printfn "reactant %s: mol: %g, m: %g, speed: %g" (Entity.sprintf r) (moles[r].moles) (mass[r]) (speed[r])

    for p in products do
        printfn "product %s: mol: %g, m: %g, speed: %g" (Entity.sprintf p) (moles[p].moles) (mass[p]) (speed[p])

    // gnuplot.Close()
)


Systems.progress()

// printfn "%A" [dr[0]; dr[1]; dp[0]; dp[1]]
Console.ReadKey()
