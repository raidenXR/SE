open System
open SE.Core
open SE.Spatial
open SE.ECS
open SE.Renderer

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open BenchmarkDotNet.Jobs
open FSharp.Data.UnitSystems.SI

[<SimpleJob>]
type OctreeBenchmarks() =
    let [<Literal>] N = 200
    let [<Literal>] L = 10
    let [<Literal>] k = 2
    let [<Literal>] ss = "../../../resources/shaders/"
    // let [<Literal>] path = "../../../resources/models/bun_zipper.ply"
    let [<Literal>] path = "./bun_zipper.ply"
    let (!) (u:Octree.Node<'T>) = Octree.valueof u
    let pos = Octree.center

    let rotation =
        match path with
        | GLTF.IsTxt -> System.Numerics.Quaternion.CreateFromYawPitchRoll(2.f, 2.f, 1.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
        | GLTF.IsPly -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.f, 0.f, 0.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
        | GLTF.IsGltf -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.1f, 0.2f, 0.1f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        
        | _ -> System.Numerics.Quaternion.CreateFromYawPitchRoll(0.f, 0.f, 0.f) |> System.Numerics.Matrix4x4.CreateFromQuaternion        

    let scale =
        match path with
        | GLTF.IsTxt -> 0.2f
        | GLTF.IsPly -> 10.f
        | GLTF.IsGltf -> 3.f
        | _ -> 1.f

    let meshes = [|
        path |> RGeometry.load_model
        path |> RGeometry.load_model |> RGeometry.tranform rotation        
    |]

    let (v_min,v_max) =
        let mutable (v_min,v_max) = GridGeneration3D.bounds_SIMD (meshes[0].vertices.AsSpan()) meshes[0].L
        for mesh in meshes do
            let (_v_min,_v_max) = GridGeneration3D.bounds_SIMD (mesh.vertices.AsSpan()) mesh.L
            v_min <- System.Numerics.Vector3.Min(v_min, _v_min)        
            v_max <- System.Numerics.Vector3.Max(v_max, _v_max)        
        (v_min,v_max)

    let trees =
        let trees =
            meshes
            |> Array.Parallel.map (fun mesh ->                
                let vertices = mesh.vertices.AsSpan()
                let indices = mesh.indices.AsSpan()
                let (_v_min,_v_max) = GridGeneration3D.bounds_SIMD vertices L
            
                
                let bits = Octree.fill_scanlines N L v_min v_max vertices indices (System.Collections.BitArray(N*N*N))
                Octree.ofStencil<Entity> N k v_min v_max bits
            )
        for tree in trees do
            tree.Iter (fun u ->
                match u with
                | Octree.Internal & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> set 5 |> set 90.9 |> set true  |> ValueSome
                | Octree.Boundary & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> set 8 |> set 10.9 |> set false |> ValueSome
                | _ -> ()
            )
        trees


    [<Benchmark>]
    member this.BuildOctrees () =
        let meshes = [|
            path |> RGeometry.load_model
            path |> RGeometry.load_model |> RGeometry.tranform rotation        
        |]

        let (v_min,v_max) =
            let mutable (v_min,v_max) = GridGeneration3D.bounds_SIMD (meshes[0].vertices.AsSpan()) meshes[0].L
            for mesh in meshes do
                let (_v_min,_v_max) = GridGeneration3D.bounds_SIMD (mesh.vertices.AsSpan()) mesh.L
                v_min <- System.Numerics.Vector3.Min(v_min, _v_min)        
                v_max <- System.Numerics.Vector3.Max(v_max, _v_max)        
            (v_min,v_max)

        let trees =
            meshes
            |> Array.Parallel.map (fun mesh ->                
                let vertices = mesh.vertices.AsSpan()
                let indices = mesh.indices.AsSpan()
                let (_v_min,_v_max) = GridGeneration3D.bounds_SIMD vertices L
            
                
                let bits = Octree.fill_scanlines N L v_min v_max vertices indices (System.Collections.BitArray(N*N*N))
                Octree.ofStencil<Entity> N k v_min v_max bits
            )

        for tree in trees do
            tree.Iter (fun u ->
                match u with
                | Octree.Internal & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> set 5 |> set 90.9 |> set true  |> ValueSome
                | Octree.Boundary & Octree.Leaf(_,v,_,_,_,_) -> v.Value <- entity() |> set 8 |> set 10.9 |> set false |> ValueSome
                | _ -> ()
            )

    [<Benchmark>]
    member this.MapClosest() =
        let T = Components.get<int>()
        trees[0].IterParallel 4 (fun u ->
            match u with
            | Octree.Boundary & Octree.Leaf (_,v,_,_,_,_) ->
                match trees[1].MapTo(pos u) with
                | Octree.Leaf (_,v,_,_,_,_) ->
                    let idx = v.Value.Value
                    T[idx] <- Math.Clamp(T[idx] + 1, 0, 100)
                | _ -> ()
            | _ -> ()
        )    
        

    [<Benchmark>]
    member this.SolveOctree () =
        let T = Components.get<double>()    

        trees[0].IterParallel 4 (fun u ->
            match u with
            | Octree.Internal ->
                let i  = u[-1,0,0]
                let i' = u[+1,0,0]
                let j  = u[0,-1,0]
                let j' = u[0,+1,0]
                let l  = u[0,0,-1]
                let l' = u[0,0,+1]

                let x1 = double (pos u - pos i).X
                let y1 = double (pos u - pos j).Y
                let z1 = double (pos u - pos l).Z
    
                let x2 = double (pos i' - pos u).X
                let y2 = double (pos j' - pos u).Y
                let z2 = double (pos l' - pos u).Z
                
                T[(trees[1][pos u]).Value] <- 
                    (2./(x1*(x1+x2))* T[!i] + 2./(x2*(x1+x2))*T[!i']  +
                    2./(y1*(y1+y2)) * T[!j] + 2./(y2*(y1+y2))*T[!j']  +
                    2./(z1*(z1+z2)) * T[!l] + 2./(z2*(z1+z2))*T[!l']) /
                    (2./(x1*x2) + 2./(y1*y2) + 2./(z1*z2))
            | _ -> ()
        )
        

    // let tree_1 = Octree.ofSurface<double> N L 4 (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
    // let tree_2 = Octree.ofSurface<double> N L 4 (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
    // let tree_3 = OctreeExperimental.ofSurface<double> N L 4 (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
    // let tree_4 = OctreeExperimental.ofSurface<double> N L 4 (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())

    // do
    //     tree_1.Add <- (+)
    //     tree_1.Div <- (/)
    //     tree_2.Add <- (+)
    //     tree_2.Div <- (/)
    //     tree_3.Add <- (+)
    //     tree_3.Div <- (/)
    //     tree_4.Add <- (+)
    //     tree_4.Div <- (/)

    // [<Benchmark>]
    // member this.Iter2 () =
    //     tree_1.IterParallel 2 (fun node ->
    //         match node with
    //         | Octree.Internal ->
    //             let c = Octree.center node
    //             let x = double c.X
    //             let y = double c.Y
    //             let z = double c.Z
    //             tree_1[0,0,0] <- 150.0
                
    //         | Octree.Boundary ->
    //             tree_1[0,0,0] <- 300.

    //         | Octree.External -> ()                            
    //     )

    // [<Benchmark>]
    // member this.Iter4 () =
    //     tree_2.IterParallel 4 (fun node ->
    //         match node with
    //         | Octree.Internal ->
    //             let c = Octree.center node
    //             let x = double c.X
    //             let y = double c.Y
    //             let z = double c.Z
    //             tree_2[0,0,0] <- 150.0
                
    //         | Octree.Boundary ->
    //             tree_2[0,0,0] <- 300.

    //         | Octree.External -> ()                            
    //     )

    // [<Benchmark>]
    // member this.IterExperimental2 () =
    //     tree_3.IterParallel 2 (fun node ->
    //         match node with
    //         | OctreeExperimental.Internal ->
    //             let c = OctreeExperimental.center node
    //             let x = double c.X
    //             let y = double c.Y
    //             let z = double c.Z
    //             node.value <- ValueSome 150.0
                
    //         | OctreeExperimental.Boundary ->
    //             node.value <- ValueSome 300.

    //         | OctreeExperimental.External -> ()                            
    //     )

    // [<Benchmark>]
    // member this.IterExperimental4 () =
    //     tree_4.IterParallel 4 (fun node ->
    //         match node with
    //         | OctreeExperimental.Internal ->
    //             let c = OctreeExperimental.center node
    //             let x = double c.X
    //             let y = double c.Y
    //             let z = double c.Z
    //             node.value <- ValueSome 150.0
                
    //         | OctreeExperimental.Boundary ->
    //             node.value <- ValueSome 300.

    //         | OctreeExperimental.External -> ()                            
    //     )

    // [<Benchmark>]
    // old methods is deprecated
    // member this.SurfaceOld () =
        // let tree = Octree.ofSurfaceOLD N L 4 (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
        // ignore tree

    // [<Benchmark>]
    // member this.SurfaceNew () =
    //     let tree = Octree.ofSurface N L 4 (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
    //     ignore tree

BenchmarkRunner.Run<OctreeBenchmarks>() |> ignore



