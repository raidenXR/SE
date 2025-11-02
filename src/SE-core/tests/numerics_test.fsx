#load "../src/numerics.fs"

open System
open System.Buffers
open SE.Numerics
open Integration
open Differentiation

let max_in = 200
let vmin, vmax = 0., 1.
let ME = 2.71828


do
    let f x = exp(-x)
    let result = gaussint 80 vmin vmax f
    printfn "%d %g" 80 (abs(result - 1. + 1. / ME))

// do
//     for i in 3..2..max_in do
//         let result = gaussint i vmin vmax f
//         printfn "%d %g" i (abs(result - 1. + 1. / ME))

do
    let f x = exp(-x)
    printfn "central: %g" (central 4. 0.0001 f)
    printfn "forward: %g" (forward 4. 0.0001 f)
    printfn "extrapolated: %g" (extrapolated 4. 0.0001 f)

do
    let f t (y:array<float>) (freturn:array<float>) =
        freturn[0] <- y[1]
        freturn[1] <- -100. * y[0] - 2. * y[1] + 10. * sin(3. * t)

    let rk4 = ODE.RK4 f 
    for struct(a,b) in rk4 do printfn "y[0]: %g, y[1]: %g" a b
