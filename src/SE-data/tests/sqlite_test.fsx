#nowarn "524"
#r"../bin/Debug/net10.0/SE-core.dll"
#r "nuget: Microsoft.Data.Sqlite, 10.0.12"
#load "../src/data.fs"

open SE
open SE.Core
open SE.Spatial
open SE.ECS
open SE.Data
open System
open System.Collections
open Microsoft.Data.Sqlite

let N = 16
let k = 3
let L = 10
let _bits = [|1;1;1;1;2;2;2;2;3;3;3;3;4;4;4;4|] |> Array.map (fun x -> byte x)
let vertices = _bits |> Array.map (fun x -> float32 x * 2.f) |> NativeArray.ofSeq 
let indices  = _bits |> Array.map (fun x -> uint32 x * 3u) |> NativeArray.ofSeq
let bits = BitArray(_bits)

Serialize.stencils "testing.db" [|("body_1",N,bits)|]
let stencils = Deserialize.stencils "testing.db"

for (name,N,bits) in stencils do
    printfn "%s, %d" name N
    for i in 0..N-1 do
        for j in 0..7 do
            printf "%d" (if bits[i*8+j] then 1 else 0)
        printfn ""
    printfn "stencil^ \n"
    printfn "\n"



