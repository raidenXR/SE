// #r "../bin/Debug/net9.0/SE-renderer.dll"
// #r "nuget: OpenTK, 4.9.4"
#load "../src/unsafe.fs"
// #load "../src/geometry.fs"

open System
open SE.Renderer
open System.IO
open FSharp.NativeInterop
open SE.Renderer

printfn "NativeArray.size: %d" (sizeof<NativeArray<float32>>)
printfn "NativeArray2D.size: %d" (sizeof<NativeArray2D<float32>>)
printfn "NativeArray3D.size: %d" (sizeof<NativeArray3D<float32>>)
// printfn "ValueModel.size: %d" (sizeof<ValueModel>)
// printfn "ValueAnimation.size: %d" (sizeof<ValueAnimation>)

let A = [|for i in 1..100 -> float32 i|]
let V = new NativeArray<float32>(A)

let check_equality (a:array<_>) (b:NativeArray<_>) =
    let mutable i = 0
    while i < b.Length do
        let _a = a[i]
        let _b = b.AsSpan()[i]
        if _a <> _b then printfn "%A  ,   %A" _a _b 
        i <- i + 1

check_equality A V
V.Dispose()

do
    let values = NativeArrayPool.Shared.Rent<float>(100)
    printfn "values.len: %d" (values.Length)
    NativeArrayPool.Shared.Return<float>(values)



