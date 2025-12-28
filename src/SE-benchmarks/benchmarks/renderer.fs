open System
open SE.Renderer

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open BenchmarkDotNet.Jobs
open FSharp.Data.UnitSystems.SI

[<SimpleJob>]
type GeometryBenchmarks() =
    let path = "bun_zipper.ply"
    let (vertices,indices) = Geometry.load_ply (path, 0.55f, 0.55f, 0.55f, 1.0f)
    let (v_min,v_max) = Geometry.bounds vertices 10
    let bunny_model = Model(vertices, indices)
    do
        printfn "vertices.len: %d, indices.len: %d" (vertices.Length / 10) (indices.Length / 3)

    [<Params(20,50,100)>]
    member val N = 0 with get,set

    [<Benchmark>]
    member this.get_CV () =
        let v_range = Geometry.bounds vertices
        ignore (v_range)

    [<Benchmark>]
    member this.get_volume () =
        let (voxels,t) = Geometry.as_voxels bunny_model this.N
        let particles = Geometry.get_particles v_min v_max voxels t this.N 
        ignore particles


BenchmarkRunner.Run<GeometryBenchmarks>() |> ignore
    

