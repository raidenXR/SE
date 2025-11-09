#load "../src/numerics.fs"
#load "../src/linearalg.fs"

open System
open System.Buffers
open SE.Numerics
open Integration
open Differentiation

let max_in = 200
let vmin, vmax = 0., 1.
let ME = 2.71828

do
    // test dispose on loop
    for i in 1..8 do
        use v = new Vector(3)
        use m = new Matrix(3,3)
        ()

do
    // test varray3
    let v = varray3<float>(a = 4., b = 3., c = 9.)
    printfn "%g, %g, %g" v[0] v[1] v[2]

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
    ()
    // for struct(a,b) in rk4 do printfn "y[0]: %g, y[1]: %g" a b
