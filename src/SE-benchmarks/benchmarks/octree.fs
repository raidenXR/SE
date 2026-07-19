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

    // let (v_min,v_max) = GridGeneration3D.bounds_SIMD (mesh.vertices.AsSpan()) L

    // let stencil = 
    //     // GridGeneration3D.bitstencil vertices indices v_min v_max N
    //     System.Collections.BitArray(N*N*N)
    //     |> GridGeneration3D.assign_voxels_SIMD (mesh.vertices.AsSpan()) (mesh.indices.AsSpan()) L N 
    //     |> GridGeneration3D.fill_bitstencil N

    let N' = N
    let valueof = Octree.valueof
    // let kindof  = Octree.kindof

    let tree_1 = Octree.ofSurface<double> N L 4 (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
        // stencil
        // |> Octree.ofStencil<double> N 5 v_min v_max
        // |> Octree.init 0.00

    do
        if N <> N' then failwith "N cannot differ"
        tree_1.Add <- (+)
        tree_1.Div <- (/)

    let tree_2 = Octree.ofSurface<double> N L 4 (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
        // stencil
        // |> Octree.ofStencil<double> N 5 v_min v_max
        // |> Octree.init 0.00

    do
        if N <> N' then failwith "N cannot differ"
        tree_2.Add <- (+)
        tree_2.Div <- (/)

        
    let tree_3 = OctreeExperimental.ofSurface<double> N L 4 (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
        // stencil
        // |> Octree.ofStencil<double> N 5 v_min v_max
        // |> Octree.init 0.00

    do
        if N <> N' then failwith "N cannot differ"
        tree_3.Add <- (+)
        tree_3.Div <- (/)


    let tree_4 = OctreeExperimental.ofSurface<double> N L 4 (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
        // stencil
        // |> Octree.ofStencil<double> N 5 v_min v_max
        // |> Octree.init 0.00

    do
        if N <> N' then failwith "N cannot differ"
        tree_4.Add <- (+)
        tree_4.Div <- (/)

    [<Benchmark>]
    member this.Iter2 () =
        tree_1.IterParallel 2 (fun node ->
            match node with
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
    member this.Iter4 () =
        tree_2.IterParallel 4 (fun node ->
            match node with
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
    member this.IterExperimental2 () =
        tree_3.IterParallel 2 (fun node ->
            match node with
            | OctreeExperimental.Internal ->
                let c = OctreeExperimental.center node
                let x = double c.X
                let y = double c.Y
                let z = double c.Z
                node.value <- ValueSome 150.0
                
            | OctreeExperimental.Boundary ->
                node.value <- ValueSome 300.

            | OctreeExperimental.External -> ()                            
        )

    [<Benchmark>]
    member this.IterExperimental4 () =
        tree_4.IterParallel 4 (fun node ->
            match node with
            | OctreeExperimental.Internal ->
                let c = OctreeExperimental.center node
                let x = double c.X
                let y = double c.Y
                let z = double c.Z
                node.value <- ValueSome 150.0
                
            | OctreeExperimental.Boundary ->
                node.value <- ValueSome 300.

            | OctreeExperimental.External -> ()                            
        )

BenchmarkRunner.Run<OctreeBenchmarks>() |> ignore



