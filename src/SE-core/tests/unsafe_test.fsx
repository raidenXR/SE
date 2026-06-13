#load "../src/unsafe.fs"
// #load "../src/geometry.fs"

open SE
open System
// open SE.Spatial
open System.Numerics

let v0 = NativeArray.create<byte> 10 
v0.SetBit(10, true)
v0.SetBit(20, true)
v0.SetBit(30, true)
v0.SetBit(50, true)
do
    let mutable total_bits = 0
    for i in 0..9 do
        total_bits <- total_bits + Bits.bits_count v0[i]

    for i in 0..79 do
        if v0.GetBit(i) then
            printfn "(%d) found true bit, 0b%B at %d" i (v0.GetByte(i)) (i%8)
            
    printfn "total_bits: %d" (total_bits)



let v1 = NativeArray2D.create<byte> 10 10
v1.SetBit(10,10, true)
v1.SetBit(20,11, true)
v1.SetBit(30,12, true)
v1.SetBit(50,15, true)

do
    let mutable total_bits = 0
    for i in 0..9 do
        for j in 0..9 do
            total_bits <- total_bits + Bits.bits_count v1[i,j]

    for i in 0..79 do
        for j in 0..79 do
            if v1.GetBit(i,j) then
                printfn "(%d,%d) found true bit, 0b%B at %d" i j (v1.GetByte(i,j)) (j%8)

    printfn "total_bits: %d" (total_bits)

// NativeArray.delete v0
// NativeArray2D.delete v1

printfn "%d" (sizeof<narray<Vector3>>)
// printfn "%d" (sizeof<nsparse<Vector3>>)
printfn "%d" (sizeof<narray2d<Vector3>>)
printfn "%d" (sizeof<narray3d<Vector3>>)
// printfn "%d" (sizeof<Voxels>)
// printfn "%d" (sizeof<Shape>)
// printfn "%d" (sizeof<Ray>)
// printfn "%d" (sizeof<CVBounds>)

let method () =
    use buffer = NativeArray.create<uint8> 100
    let slice = buffer.AsSpan()

    let mutable voxels = NativeArray3D.create<bool> 101 101 101
    for ix in 0..100 do
        for iy in 0..100 do
            for iz in 0..100 do
                voxels[ix,iy,iz] <- true   

    voxels.Dispose()


    for i in 0..buffer.Length-1 do
        slice[i] <- 90uy
    // buffer.Dispose()

    for i in 0..100 do
        use buf = NativeArray.rent<float> 100
        let s = buf.AsSpan()
        for i in 0..buffer.Length-1 do
            s[i] <- 0.34298

        // if buf.is_pooled then printfn "is_pooled"
        // buf.Dispose()
        // buf.Dispose()

    // (buffer :> IDisposable).Dispose()

    // buffer.len <- 90
    // buffer.Method(1000)
    printfn "len: %d" (buffer.Length)
    

method()


