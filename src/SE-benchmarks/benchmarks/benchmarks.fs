open System
open System.Numerics
open SE.Core

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open BenchmarkDotNet.Jobs
open FSharp.Data.UnitSystems.SI

type [<Struct>] Position = Position of Vector3
type [<Struct>] Velocity = Velocity of Vector3
type [<Struct>] Rotation = Rotation of float
type [<Struct>] Temperature = Temperature of float
type [<Struct>] TemperaturT = {temp:float}


[<SimpleJob>]
type Benchmarks() =
    let _size = 1000
    let float_array = Array.zeroCreate<float> (_size)
    do
        Components.clearAll ()
        Entity.reset()
        for i in 0.._size - 1 do
            entity ()
            |> Entity.set (300.0)
            |> Entity.set {temp = 300.0}
            |> Entity.set (Temperature 300.0)
            |> ignore

    
    // [<Params(100, 1000)>]
    // member val size = 0 with get, set

    [<Benchmark>]
    member this.RegularArray () =
        let r = Random.Shared.NextDouble() * 10.0 - 5.0
        for i in 0..float_array.Length - 1 do
            float_array[i] <- float_array[i] + r 

    [<Benchmark>]
    member this.ComputePrimitive () =
        let r = Random.Shared.NextDouble() * 10.0 - 5.0
        let components = Components.get<float>()
        let entries = components.Entries
        let entities = components.Entities
        for e in entities do
            let x = &components[e]
            x <- x + r

    [<Benchmark>]
    member this.ComputePrimitiveiSequential () =
        let r = Random.Shared.NextDouble() * 10.0 - 5.0
        let components = Components.get<float>()
        let entries = components.Entries
        let entities = components.Entities
        for i in 0..entries.Length - 1 do
            let x = &entries[i]
            x <- x + r
            
    [<Benchmark>]
    member this.ComputeRecord () =
        let r = Random.Shared.NextDouble() * 10.0 - 5.0
        let components = Components.get<TemperaturT>()
        let entries = components.Entries
        let entities = components.Entities
        for e in entities do
            let x = &components[e]
            x <- {temp = x.temp + r}

    [<Benchmark>]
    member this.ComputeRecordSequential () =
        let r = Random.Shared.NextDouble() * 10.0 - 5.0
        let components = Components.get<TemperaturT>()
        let entries = components.Entries
        let entities = components.Entities
        for i in 0..entries.Length - 1 do
            let x = &entries[i]
            x <- {temp = x.temp + r}

    [<Benchmark>]
    member this.ComputeDURead () =
        let r = Random.Shared.NextDouble() * 10.0 - 5.0
        let components = Components.get<Temperature>()
        for e in components.Entities do
            let (Temperature x) = components[e]
            ignore x        

    [<Benchmark>]
    member this.ComputeDU () =
        let r = Random.Shared.NextDouble() * 10.0 - 5.0
        let components = Components.get<Temperature>()
        let entries = components.Entries
        let entities = components.Entities
        for e in entities do
            let x = &components[e]
            let (Temperature t) = x
            x <- Temperature (t + r)
            
    [<Benchmark>]
    member this.ComputeDUSequential () =
        let r = Random.Shared.NextDouble() * 10.0 - 5.0
        let components = Components.get<Temperature>()
        let entries = components.Entries
        let entities = components.Entities
        for i in 0..entries.Length - 1 do
            let x = &entries[i]
            let (Temperature t) = x
            x <- Temperature (t + r)

    [<Benchmark>]
    member this.CreateEntities () =
        for i in 0.._size - 1 do
            entity ()
            |> Entity.add<Position>
            |> Entity.add<Velocity>
            |> Entity.add<Temperature>
            |> ignore      
            

    [<Benchmark>]
    member this.SetEntities () =
        for i in 0.._size - 1 do
            entity ()
            |> Entity.set (Position (Vector3(0.0f, 0.4f, 0.5f)))
            |> Entity.set (Velocity (Vector3(5f, 6f, 7f)))
            |> Entity.set (Temperature 90.0)
            |> ignore      

    [<Benchmark>]
    member this.RunContains () =
        let tempT = Components.get<TemperaturT>()
        let ids = tempT.Entities
        let r = Random.Shared
        for i in 0.._size - 1 do
            let e = ids[r.Next(ids.Count - 1)]
            let mutable k = -1
            let b = tempT.Contains(e)
            ()


    // [<Benchmark>]
    // member this.Queries () =
    //     for i in 0..10 do
    //         entity ()
    //         |> Entity.set (Position (Vector3(0.0f, 0.4f, 0.5f)))
    //         |> Entity.set (Velocity (Vector3(5f, 6f, 7f)))
    //         |> Entity.set (Temperature 90.0)
    //         |> ignore      

    //     for i in 0..100 do
    //         entity ()
    //         |> Entity.set (Position (Vector3(0.0f, 0.4f, 0.5f)))
    //         |> Entity.set (Velocity (Vector3(5f, 6f, 7f)))
    //         |> ignore      

        
    //     for i in 0..1000 do
    //         entity ()
    //         |> Entity.set (Temperature 90.0)
    //         |> ignore      

    //     for i in 0..100 do
    //         entity ()
    //         |> Entity.set (Rotation 90.0)
    //         |> ignore      
            
    //     let q0 = query [typeof<Temperature>]
    //     let q1 = query [typeof<Position>; typeof<Velocity>]
    //     let q2 = query [typeof<Rotation>]
    //     () // return unit
        
BenchmarkRunner.Run<Benchmarks>() |> ignore
