#load "../src/core.fs"

open System
open System.Numerics
open System.Runtime.InteropServices
open System.Runtime.CompilerServices
open FSharp.NativeInterop

open SE.Core

type Entity = uint32

type [<Struct>] ValueFlag = struct end


let has_linear (ids:Span<Entity>) (id:Entity) (i:byref<int>) =
    let count = ids.Length
    let mutable j = 0
    let mutable r = false
    while j < count && not r do
        r <- ids[j] = id
        j <- if r then j else j + 1
    i <- i + j
    r

let has_linear_SIMD (ids:Span<Entity>) (id:Entity) (i:byref<int>) =
    let size_t = Vector<Entity>.Count
    let count = ids.Length / size_t
    // let p = &MemoryMarshal.GetReference(ids)
    let p = &ids.GetPinnableReference()

    let d = Vector<Entity>(id)
    let mutable r = false
    while i < count && not r do
        let v = Unsafe.As<Entity,Vector<Entity>>(&Unsafe.Add(&p, i*size_t))
        r <- Vector.EqualsAny(v,d)
        i <- if r then i else i + 1
    i <- i * size_t
    has_linear (ids.Slice(i)) id &i

let ids = [|for i in 1..100 -> uint32 i|]
let id: Entity = 67u

let mutable i = -1
let b = has_linear_SIMD ids id &i

printfn "has: %b, at idx: %d" b i

let _size = 1000
do
    for i in 0.._size - 1 do
        let A = 
            entity()
            |> Entity.set (300.0)
            |> Entity.set (i)
            |> Entity.add<ValueFlag>

        let B =
            entity()
            |> Entity.set (Vector3.UnitY)

        relate A B (ValueFlag())

    for i in 1.._size do
        let components = Components.get<float>()
        let idx = Random.Shared.Next(components.Count)
        let e  = components.Entities[idx]
        Relation.has<ValueFlag> Out e |> ignore

        let components = Components.get<Vector3>()
        let idx = Random.Shared.Next(components.Count)
        let e  = components.Entities[idx]
        Relation.has<ValueFlag> In e |> ignore

    printfn "finished loop successfully"
