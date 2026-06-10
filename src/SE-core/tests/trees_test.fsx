#load "../src/unsafe.fs"
#load "../src/trees.fs"

open SE
open SE.Core
open System.Numerics

// let octree = Octree<double>(100, Vector3.Zero, Vector3.One, (fun a b -> a - b > 0.4))
let N = 400
let octree = Octree<double>(N, Vector3.Zero, Vector3.One * float32 N)
let dx = octree.dX
let dy = octree.dY
let dz = octree.dZ
printfn "dx: %g, dy: %g, dz: %g" dx dy dz

// octree[0.4, 0.4, 0.4] <- 653.63

// let d = octree[0.4, 0.4, 0.4]
// printfn "node: %g" d

#time
for ix in 1..N-1 do
    for iy in 1..N-1 do
        for iz in 1..N-1 do
            // let x = float ix
            let y = System.Random.Shared.NextDouble() * double iy
            let z = System.Random.Shared.NextDouble() * double iz
            let x = double N - System.Random.Shared.NextDouble() * double ix
            // let y = float N - float iy
            // let z = float N - float iz
            // printfn "x: %g, y: %g, z: %g" x y z
            octree[x,y,z] <- System.Random.Shared.NextDouble()
            // octree[x,y,z] <- 0.0
#time
            
printfn "count: %d, %gMB" (octree.GetCount()) (double(octree.GetCount()) * 120. / 1024. / 1024.) 
printfn "max_level: %d" (octree.MaxLevel) 

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


