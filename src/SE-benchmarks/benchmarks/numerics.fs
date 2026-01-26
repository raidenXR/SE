open System
open System.Numerics
open SE.Core
open SE.Numerics

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open BenchmarkDotNet.Jobs
open FSharp.Data.UnitSystems.SI


[<SimpleJob>]
type NumericsBenchmarks() =
    let r = Random.Shared
    let vv_0 = [|for i in 1..10 -> 0.5 + r.NextDouble()|]
    let mv_0 = [|for i in 1..100 -> 0.5 + r.NextDouble()|]
    let vv_1 = [|for i in 1..20 -> 0.5 + r.NextDouble()|]
    let mv_1 = [|for i in 1..400 -> 0.5 + r.NextDouble()|]
    let vv_2 = [|for i in 1..30 -> 0.5 + r.NextDouble()|]
    let mv_2 = [|for i in 1..900 -> 0.5 + r.NextDouble()|]
    let vv_4 = [|for i in 1..100 -> 0.5 + r.NextDouble()|]
    let mv_4 = [|for i in 1..10000 -> 0.5 + r.NextDouble()|]
    let vv_5 = [|for i in 1..1000 -> 0.5 + r.NextDouble()|]
    let mv_5 = [|for i in 1..1000000 -> 0.5 + r.NextDouble()|]

    [<Benchmark>]
    member this.GE100 () =
        use m = new Matrix(10,10,mv_0)
        use v = new Vector(vv_0)
        use x = Solvers.GaussElimination m v
        ignore x
        
    [<Benchmark>]
    member this.GE400 () =
        use m = new Matrix(20,20,mv_1)
        use v = new Vector(vv_1)
        use x = Solvers.GaussElimination m v
        ignore x


    [<Benchmark>]
    member this.GE900 () =
        use m = new Matrix(30,30,mv_2)
        use v = new Vector(vv_2)
        use x = Solvers.GaussElimination m v
        ignore x

    [<Benchmark>]
    member this.GE10000 () =
        use m = new Matrix(100,100,mv_4)
        use v = new Vector(vv_4)
        use x = Solvers.GaussElimination m v
        ignore x

    [<Benchmark>]
    member this.GE1000000 () =
        use m = new Matrix(1000,1000,mv_5)
        use v = new Vector(vv_5)
        use x = Solvers.GaussElimination m v
        ignore x

BenchmarkRunner.Run<NumericsBenchmarks>() |> ignore



