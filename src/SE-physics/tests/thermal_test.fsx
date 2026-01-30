#load "../src/thermal.fs"

open SE.Physics

let (v1,v2) = Thermal.volume ()

// printfn "%A" v2


let idx = 
    let mutable i = 0
    let mutable b = false
    while not b do
        b <- i >= 10 || i < 0
        i <- i + if b then 0 else 1
    i

printfn "idx: %d" idx


