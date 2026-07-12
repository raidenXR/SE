open System
open SE.Core
open SE.ECS
open SE.Renderer

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open BenchmarkDotNet.Jobs
open FSharp.Data.UnitSystems.SI

[<SimpleJob>]
type OctreeBenchmarks() =
    let path = "bun_zipper.ply"
    let gltf: option<GLTF.Deserializer> = None
    let N = 500
    let L = 10

    let mesh =
        match gltf with
        | Some gltf -> gltf.ReadMeshF(0)
        | None -> RGeometry.load_ply_unmanaged (path, 0.55f, 0.55f, 0.53f, 1.0f)

    let (v_min,v_max) = GridGeneration3D.bounds_SIMD (mesh.vertices.AsSpan()) L

    let stencil = 
        // GridGeneration3D.bitstencil vertices indices v_min v_max N
        System.Collections.BitArray(N*N*N)
        |> GridGeneration3D.assign_voxels_SIMD (mesh.vertices.AsSpan()) (mesh.indices.AsSpan()) L N 
        |> GridGeneration3D.fill_bitstencil N

    let N' = N
    let valueof = Octree.valueof
    let kindof  = Octree.kindof

    let tree_1 =
        stencil
        |> Octree.ofStencil<double> N 5 v_min v_max
        |> Octree.init 0.00

    do
        if N <> N' then failwith "N cannot differ"
        tree_1.Add <- (+)
        tree_1.Div <- (/)

    let tree_2 =
        stencil
        |> Octree.ofStencil<double> N 5 v_min v_max
        |> Octree.init 0.00

    do
        if N <> N' then failwith "N cannot differ"
        tree_2.Add <- (+)
        tree_2.Div <- (/)

        
    let tree_3 =
        stencil
        |> Octree.ofStencil<double> N 5 v_min v_max
        |> Octree.init 0.00

    do
        if N <> N' then failwith "N cannot differ"
        tree_3.Add <- (+)
        tree_3.Div <- (/)


    let tree_4 =
        stencil
        |> Octree.ofStencil<double> N 5 v_min v_max
        |> Octree.init 0.00

    do
        if N <> N' then failwith "N cannot differ"
        tree_4.Add <- (+)
        tree_4.Div <- (/)

    [<Benchmark>]
    member this.Iter1 () =
        tree_1.IterParallel 1 (fun node ->
            match (kindof node) with
            | Octree.Internal ->
                let c = Octree.center node
                let x = double c.X
                let y = double c.Y
                let z = double c.Z
                tree_1[0,0,0] <- 150.0
                
            | Octree.Boundary ->
                tree_1[0,0,0] <- 300.

            | Octree.External -> ()                            
        )
        // tree.Update(_trim, _dense, _set)
        // Quadtree.morph tree.Root tree'.Root (+) (/)

    [<Benchmark>]
    member this.Iter2 () =
        let dx = tree_2.dX
        let dy = tree_2.dY
        let dz = tree_2.dZ
        tree_2.IterParallel 2 (fun node ->
            match (kindof node) with
            | Octree.Internal ->
                let c = Octree.center node
                let x = double c.X
                let y = double c.Y
                let z = double c.Z
                tree_2[0,0,0] <- 150.0
                
            | Octree.Boundary ->
                tree_2[0,0,0] <- 300.

            | Octree.External -> ()                            
        )

    [<Benchmark>]
    member this.Iter3 () =
        let dx = tree_3.dX
        let dy = tree_3.dY
        let dz = tree_3.dZ
        tree_3.IterParallel 4 (fun node ->
            match (kindof node) with
            | Octree.Internal ->
                let c = Octree.center node
                let x = double c.X
                let y = double c.Y
                let z = double c.Z
                tree_3[0,0,0] <- 150.0
                
            | Octree.Boundary ->
                tree_3[0,0,0] <- 300.

            | Octree.External -> ()                            
        )

    [<Benchmark>]
    member this.Iter4 () =
        let dx = tree_4.dX
        let dy = tree_4.dY
        let dz = tree_4.dZ
        tree_2.IterParallel 8 (fun node ->
            match (kindof node) with
            | Octree.Internal ->
                let c = Octree.center node
                let x = double c.X
                let y = double c.Y
                let z = double c.Z
                tree_4[0,0,0] <- 150.0
                
            | Octree.Boundary ->
                tree_4[0,0,0] <- 300.

            | Octree.External -> ()                            
        )

BenchmarkRunner.Run<OctreeBenchmarks>() |> ignore



