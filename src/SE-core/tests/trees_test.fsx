// #load "../src/unsafe.fs"
// #load "../src/trees.fs"
#r "../bin/Release/net10.0/SE-core.dll"

open SE
open SE.Core
open System.Numerics

// let octree = Octree<double>(100, Vector3.Zero, Vector3.One, (fun a b -> a - b > 0.4))
let N = 200
let octree = Octree<double>(N, Vector3.Zero, Vector3.One * float32 N)
let octree_2 = Octree2.Root<double>(N, Vector3.Zero, Vector3.One * float32 N)

let dx = octree.dX
let dy = octree.dY
let dz = octree.dZ

let dx' = octree_2.dX
let dy' = octree_2.dY
let dz' = octree_2.dZ

#time
for ix in 1..N-1 do
    for iy in 1..N-1 do
        for iz in 1..N-1 do
            // let x = float ix
            let x = float ix
            let y = float iy
            let z = float iz
            // let y = System.Random.Shared.NextDouble() * double iy
            // let z = System.Random.Shared.NextDouble() * double iz
            // let x = double N - System.Random.Shared.NextDouble() * double ix
            octree_2[x,y,z] <- System.Random.Shared.NextDouble()
#time
printfn "octree_2: dx: %g, dy: %g, dz: %g" dx' dy' dz'
printfn "octree_2: count: %d, %gMB" (octree_2.GetCount()) (double(octree_2.GetCount()) * 120. / 1024. / 1024.) 
printfn "octree_2: max_level: %d" (octree_2.MaxLevel) 
#time
for ix in 1..N-1 do
    for iy in 1..N-1 do
        for iz in 1..N-1 do
            // let x = float ix
            let x = float ix
            let y = float iy
            let z = float iz
            // let y = System.Random.Shared.NextDouble() * double iy
            // let z = System.Random.Shared.NextDouble() * double iz
            // let x = double N - System.Random.Shared.NextDouble() * double ix
            let v = octree_2[x,y,z]
            ignore v
#time
printfn "traversal on built tree ^^^^^"

printfn "\n"
#time
for ix in 1..N-1 do
    for iy in 1..N-1 do
        for iz in 1..N-1 do
            let x = float ix
            let y = float iy
            let z = float iz
            // let y = System.Random.Shared.NextDouble() * double iy
            // let z = System.Random.Shared.NextDouble() * double iz
            // let x = double N - System.Random.Shared.NextDouble() * double ix
            octree[x,y,z] <- System.Random.Shared.NextDouble()
#time            
printfn "octree: dx: %g, dy: %g, dz: %g" dx dy dz
printfn "octree: count: %d, %gMB" (octree.GetCount()) (double(octree.GetCount()) * 120. / 1024. / 1024.) 
printfn "octree: max_level: %d" (octree.MaxLevel) 

// ignore (octree[50.4,90.1,60.3])
// ignore (octree[40.4,70.1,60.3])
// ignore (octree[20.4,80.1,30.3])
// ignore (octree[20.4,70.1,50.3])

#time
for ix in 1..N-1 do
    for iy in 1..N-1 do
        for iz in 1..N-1 do
            // let x = float ix
            let x = float ix
            let y = float iy
            let z = float iz
            // let y = System.Random.Shared.NextDouble() * double iy
            // let z = System.Random.Shared.NextDouble() * double iz
            // let x = double N - System.Random.Shared.NextDouble() * double ix
            let v = octree[x,y,z]
            ignore v
#time
printfn "traversal on built tree ^^^^^"


printfn "\nbit-narray1d"
do
    let bits1 = NativeArray.create<byte> 100
    // System.Random.Shared.NextBytes(bits1.AsSpan())
    for i in 0..800-1 do
        if System.Random.Shared.NextDouble() < 0.5 then
            bits1.SetBit(i, true)
        else 
            bits1.SetBit(i, false)

    printfn "count: %d, 0b%B" (bits1.GetByte(10).BitCount()) (bits1.GetByte(10))    

    printfn "bits get: %b, 0b%B" (bits1.GetBit(300)) (bits1.GetByte(300))

    bits1.SetBit(300, not (bits1.GetBit(300)))
    printfn "bits get: %b, 0b%B" (bits1.GetBit(300)) (bits1.GetByte(300))
    NativeArray.delete bits1

printfn "\nbit-narray2d"
do
    let bits2 = NativeArray2D.create<byte> 100 100
    // System.Random.Shared.NextBytes(bits2.AsSpan())
    for i in 0..800-1 do
        for j in 0..800-1 do
        if System.Random.Shared.NextDouble() < 0.5 then
                bits2.SetBit(i,j, true)
            else 
                bits2.SetBit(i,j, false)

    printfn "count: %d, 0b%B" (bits2.GetByte(10,10).BitCount()) (bits2.GetByte(10,10))    

    printfn "bits get: %b, 0b%B" (bits2.GetBit(300,300)) (bits2.GetByte(300,300))

    bits2.SetBit(300,300, not (bits2.GetBit(300,300)))
    printfn "bits get: %b, 0b%B" (bits2.GetBit(300,300)) (bits2.GetByte(300,300))
    NativeArray2D.delete bits2

printfn "\nbit-narray3d"
do
    let bits3 = NativeArray3D.create<byte> 100 100 100
    System.Random.Shared.NextBytes(bits3.AsSpan())
    for i in 0..800-1 do
        for j in 0..800-1 do
            for k in 0..800-1 do
            if System.Random.Shared.NextDouble() < 0.5 then
                    bits3.SetBit(i,j,k, true)
                else 
                    bits3.SetBit(i,j,k, false)

    printfn "count: %d, 0b%B" (bits3.GetByte(10,10,10).BitCount()) (bits3.GetByte(10,10,10))    

    printfn "bits get: %b, 0b%B" (bits3.GetBit(300,300,300)) (bits3.GetByte(300,300,300))

    bits3.SetBit(300,300,300, not (bits3.GetBit(300,300,300)))
    printfn "bits get: %b, 0b%B" (bits3.GetBit(300,300,300)) (bits3.GetByte(300,300,300))
    NativeArray3D.delete bits3


