#load "../src/thermal.fs"

open SE.Physics

let (v1,v2) = Thermal.volume ()

printfn "%A" v2
