#r "../bin/Debug/net9.0/SE-renderer.dll"
#r "nuget: OpenTK, 4.9.4"

open System
open SE.Renderer
open OpenTK.Mathematics
open System.IO


open SE.Renderer

printfn "NativeArray.size: %d" (sizeof<NativeArray<float32>>)
printfn "ValueModel.size: %d" (sizeof<ValueModel>)
printfn "ValueAnimation.size: %d" (sizeof<ValueAnimation>)

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





