open System
open System.Numerics
open SE.Core

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open BenchmarkDotNet.Jobs

type [<Struct>] Position = Position of Vector3
type [<Struct>] Velocity = Velocity of Vector3
type [<Struct>] Rotation = Rotation of float
type [<Struct>] Temperature = Temperature of float


[<SimpleJob>]
type Benchmarks() = 
    [<Params(10, 100, 1000)>]
    member val size = 0 with get, set

    [<Benchmark>]
    member this.CreateEntities () =
        Components.clearAll ()
        Entity.reset()
        for i in 0..this.size do
            entity ()
            |> Entity.add<Position>
            |> Entity.add<Velocity>
            |> Entity.add<Temperature>
            |> ignore      
            

    [<Benchmark>]
    member this.SetEntities () =
        Components.clearAll ()
        Entity.reset()
        for i in 0..this.size do
            entity ()
            |> Entity.set (Position (Vector3(0.0f, 0.4f, 0.5f)))
            |> Entity.set (Velocity (Vector3(5f, 6f, 7f)))
            |> Entity.set (Temperature 90.0)
            |> ignore      


    [<Benchmark>]
    member this.Queries () =
        Components.clearAll ()
        Entity.reset()
        for i in 0..10 do
            entity ()
            |> Entity.set (Position (Vector3(0.0f, 0.4f, 0.5f)))
            |> Entity.set (Velocity (Vector3(5f, 6f, 7f)))
            |> Entity.set (Temperature 90.0)
            |> ignore      

        for i in 0..100 do
            entity ()
            |> Entity.set (Position (Vector3(0.0f, 0.4f, 0.5f)))
            |> Entity.set (Velocity (Vector3(5f, 6f, 7f)))
            |> ignore      

        
        for i in 0..1000 do
            entity ()
            |> Entity.set (Temperature 90.0)
            |> ignore      

        for i in 0..100 do
            entity ()
            |> Entity.set (Rotation 90.0)
            |> ignore      
            
        let q0 = query [typeof<Temperature>]
        let q1 = query [typeof<Position>; typeof<Velocity>]
        let q2 = query [typeof<Rotation>]
        () // return unit
        
BenchmarkRunner.Run<Benchmarks>() |> ignore
