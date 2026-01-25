open System
open SE.Renderer

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open BenchmarkDotNet.Jobs
open FSharp.Data.UnitSystems.SI

[<SimpleJob>]
type GeometryBenchmarks() =
    let path = "bun_zipper.ply"
    let struct(vertices,indices) = Geometry.load_ply_unmanaged (path, 0.55f, 0.55f, 0.55f, 1.0f)
    let struct(v_min,v_max) = Geometry.bounds_SIMD (vertices.AsSpan()) 10
    let model = new ValueModel(vertices, indices, [3;3;4])
    
    let (vertices_m,indices_m) = Geometry.load_ply (path, 0.55f, 0.55f, 0.55f, 1.0f)
    let model_m = new Model(vertices_m, indices_m, [3;3;4])

    let N = 100
    let voxels = Array3D.zeroCreate<bool> N N N
    let mutable voxels_native = new NativeArray3D<bool>(N,N,N)
    let particles = new ValueModel(new NativeArray<float32>(N*N*N*7), new NativeArray<uint32>(0), [3;4])

    // [<Params(20,50,100)>]
    // member val N = 0 with get,set
    // member val voxels = Array3D.zeroCreate<bool> N N N with get,set
    // member val voxels_native = new NativeArray3D<bool>(N,N,N)

    [<Benchmark>]
    member this.bounds_simple () =
        Geometry.bounds (vertices.AsSpan()) N 10 |> ignore 

    [<Benchmark>]
    member this.bounds_SIMD () =
        Geometry.bounds_SIMD (vertices.AsSpan()) 10 |> ignore

    [<Benchmark>]
    member this.assign_voxels_managed () =
        Geometry.assign_voxels model_m N voxels |> ignore

    [<Benchmark>]
    member this.assign_voxels_simple () =
        Geometry.assign_voxels_from_valuemodel model N voxels |> ignore

    [<Benchmark>]
    member this.assign_voxels_SIMD () =
        Geometry.assign_voxels_SIMD model N &voxels_native |> ignore


    [<Benchmark>]
    member this.assign_particles_simple () =
        Geometry.assign_particles_unmanaged model particles voxels N 

    [<Benchmark>]
    member this.assign_particles_SIMD () =
        Geometry.assign_particles_SIMD(v_min, v_max, &voxels_native, particles.Vertices, N, particles.L)



BenchmarkRunner.Run<GeometryBenchmarks>() |> ignore
    

