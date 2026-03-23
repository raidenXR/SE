#load "../src/unsafe.fs"
// #load "../src/geometry.fs"

open SE
open System
// open SE.Spatial
open System.Numerics

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
