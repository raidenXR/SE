#load "../src/unsafe.fs"
#load "../src/trees.fs"
// #r "../bin/Release/net10.0/SE-core.dll"

open SE
open SE.Core
open System.Numerics
open System
open GridGeneration2D

// let octree = Octree<double>(100, Vector3.Zero, Vector3.One, (fun a b -> a - b > 0.4))
let N = 8*200

let path = Environment.GetCommandLineArgs()[2]
let domains = read_from_file_multiple path
let stencil = BitArray(N*N)

let (v0_min,v0_max) = bounds domains[0]
let mutable v_min = v0_min
let mutable v_max = v0_max

for domain in domains do
    let (v1,v2) = bounds domain
    let (v_min',v_max') = bounds_union v1 v2 v_min v_max
    v_min <- v_min'
    v_max <- v_max'
    
fill_bitstencil N v_min v_max stencil
let quadtree = Quadtree2.Root<double>(N, 4, v_min, v_max)

#time
// let quadtree = Quadtree2.Root<double>(N, 4, -1.5f * Vector2.One, 1.5f * Vector2.One)
let mutable total_get_bits = 0
for i in 0..N-1 do
    for j in 0..N-1 do
        let v = to_cartesian_system i j N v_min v_max
        if stencil[i*N+j] then
            total_get_bits <- total_get_bits + 1
            quadtree[double v.X, double v.Y] <- 0.
printfn "total_get_bits: %d" total_get_bits

// let count_full = quadtree.GetCount()
// printfn "quadtree filled: done!, total_count: %d" count_full
// for ix in 1..N-1 do
//     for iy in 1..N-1 do
//             let x = float ix / float N - 0.5 * (Random.Shared.NextDouble())
//             let y = float iy / float N - 0.5 * (Random.Shared.NextDouble()) 
//             quadtree[x,y] <- System.Random.Shared.NextDouble()

printfn "quadtree.count: %d" (quadtree.GetCount()) 
printfn "quadtree.total_count: %d" (quadtree.GetTotalCount()) 
#time



// let octree = Octree<double>(N, Vector3.Zero, Vector3.One * float32 N)
// let octree_2 = Octree2.Root<double>(N, Vector3.Zero, Vector3.One * float32 N)

// let dx = octree.dX
// let dy = octree.dY
// let dz = octree.dZ

// let dx' = octree_2.dX
// let dy' = octree_2.dY
// let dz' = octree_2.dZ

// #time
// for ix in 1..N-1 do
//     for iy in 1..N-1 do
//         for iz in 1..N-1 do
//             // let x = float ix
//             let x = float ix
//             let y = float iy
//             let z = float iz
//             // let y = System.Random.Shared.NextDouble() * double iy
//             // let z = System.Random.Shared.NextDouble() * double iz
//             // let x = double N - System.Random.Shared.NextDouble() * double ix
//             octree_2[x,y,z] <- System.Random.Shared.NextDouble()
// #time
// printfn "octree_2: dx: %g, dy: %g, dz: %g" dx' dy' dz'
// printfn "octree_2: count: %d, %gMB" (octree_2.GetCount()) (double(octree_2.GetCount()) * 120. / 1024. / 1024.) 
// printfn "octree_2: max_level: %d" (octree_2.MaxLevel) 
// #time
// for ix in 1..N-1 do
//     for iy in 1..N-1 do
//         for iz in 1..N-1 do
//             // let x = float ix
//             let x = float ix
//             let y = float iy
//             let z = float iz
//             // let y = System.Random.Shared.NextDouble() * double iy
//             // let z = System.Random.Shared.NextDouble() * double iz
//             // let x = double N - System.Random.Shared.NextDouble() * double ix
//             let v = octree_2[x,y,z]
//             ignore v
// #time
// printfn "traversal on built tree ^^^^^"

// printfn "\n"
// #time
// for ix in 1..N-1 do
//     for iy in 1..N-1 do
//         for iz in 1..N-1 do
//             let x = float ix
//             let y = float iy
//             let z = float iz
//             // let y = System.Random.Shared.NextDouble() * double iy
//             // let z = System.Random.Shared.NextDouble() * double iz
//             // let x = double N - System.Random.Shared.NextDouble() * double ix
//             octree[x,y,z] <- System.Random.Shared.NextDouble()
// #time            
// printfn "octree: dx: %g, dy: %g, dz: %g" dx dy dz
// printfn "octree: count: %d, %gMB" (octree.GetCount()) (double(octree.GetCount()) * 120. / 1024. / 1024.) 
// printfn "octree: max_level: %d" (octree.MaxLevel) 
// printfn "octree: max_level: %d" (octree.MaxLevel) 

// // ignore (octree[50.4,90.1,60.3])
// // ignore (octree[40.4,70.1,60.3])
// // ignore (octree[20.4,80.1,30.3])
// // ignore (octree[20.4,70.1,50.3])

// #time
// for ix in 1..N-1 do
//     for iy in 1..N-1 do
//         for iz in 1..N-1 do
//             // let x = float ix
//             let x = float ix
//             let y = float iy
//             let z = float iz
//             // let y = System.Random.Shared.NextDouble() * double iy
//             // let z = System.Random.Shared.NextDouble() * double iz
//             // let x = double N - System.Random.Shared.NextDouble() * double ix
//             let v = octree[x,y,z]
//             ignore v
// #time
// printfn "traversal on built tree ^^^^^"


// printfn "\nbit-narray1d"
// do
//     let bits1 = NativeArray.create<byte> 100
//     // System.Random.Shared.NextBytes(bits1.AsSpan())
//     for i in 0..800-1 do
//         if System.Random.Shared.NextDouble() < 0.5 then
//             bits1.SetBit(i, true)
//         else 
//             bits1.SetBit(i, false)

//     printfn "count: %d, 0b%B" (bits1.GetByte(10).BitCount()) (bits1.GetByte(10))    

//     printfn "bits get: %b, 0b%B" (bits1.GetBit(300)) (bits1.GetByte(300))

//     bits1.SetBit(300, not (bits1.GetBit(300)))
//     printfn "bits get: %b, 0b%B" (bits1.GetBit(300)) (bits1.GetByte(300))
//     NativeArray.delete bits1

// printfn "\nbit-narray2d"
// do
//     let bits2 = NativeArray2D.create<byte> 100 100
//     // System.Random.Shared.NextBytes(bits2.AsSpan())
//     for i in 0..800-1 do
//         for j in 0..800-1 do
//         if System.Random.Shared.NextDouble() < 0.5 then
//                 bits2.SetBit(i,j, true)
//             else 
//                 bits2.SetBit(i,j, false)

//     printfn "count: %d, 0b%B" (bits2.GetByte(10,10).BitCount()) (bits2.GetByte(10,10))    

//     printfn "bits get: %b, 0b%B" (bits2.GetBit(300,300)) (bits2.GetByte(300,300))

//     bits2.SetBit(300,300, not (bits2.GetBit(300,300)))
//     printfn "bits get: %b, 0b%B" (bits2.GetBit(300,300)) (bits2.GetByte(300,300))
//     NativeArray2D.delete bits2

// printfn "\nbit-narray3d"
// do
//     let bits3 = NativeArray3D.create<byte> 100 100 100
//     System.Random.Shared.NextBytes(bits3.AsSpan())
//     for i in 0..800-1 do
//         for j in 0..800-1 do
//             for k in 0..800-1 do
//             if System.Random.Shared.NextDouble() < 0.5 then
//                     bits3.SetBit(i,j,k, true)
//                 else 
//                     bits3.SetBit(i,j,k, false)

//     printfn "count: %d, 0b%B" (bits3.GetByte(10,10,10).BitCount()) (bits3.GetByte(10,10,10))    

//     printfn "bits get: %b, 0b%B" (bits3.GetBit(300,300,300)) (bits3.GetByte(300,300,300))

//     bits3.SetBit(300,300,300, not (bits3.GetBit(300,300,300)))
//     printfn "bits get: %b, 0b%B" (bits3.GetBit(300,300,300)) (bits3.GetByte(300,300,300))
//     NativeArray3D.delete bits3


