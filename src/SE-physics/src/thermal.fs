namespace SE.Physics

module Thermal =

    let volume () =
        let r = 10.
        let s = 80.
        let len = 120.
        let ep = s / (2. * len)
        let theta = [|for i in -180.0..1.0..180.0 -> i|]
        let ys1 = [| for th in theta -> (1. - cos th) / 2. |]
        let ys2 = [| for th,s1 in (Array.zip theta ys1) -> s1 + (1. - sqrt(1. - ep**2 * (sin th)**2) / (2. * ep)) |]
        let vol1 = [| for s1 in ys1 -> 1. + (r-1.) * s1 |]
        let vol2 = [| for s2 in ys2 -> 1. + (r-1.) * s2 |]
        (vol1,vol2)
        
