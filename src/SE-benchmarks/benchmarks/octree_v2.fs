
open System
open SE.Core
open SE.Spatial
open SE.ECS

open System.Numerics
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
    let [<Literal>] path = "./bun_zipper.ply"
    let (!) (u:Octree.Node<'T>) = Octree.valueof u
    let pos = Octree.center

    let mesh = SE.Renderer.RGeometry.load_model path
    let vertices = mesh.vertices.AsSpan()
    let indices = mesh.indices.AsSpan()
    
    let (v_min, v_max) = GridGeneration3D.bounds_SIMD vertices L
    let bits = Octree.fill_scanlines N mesh.L v_min v_max vertices indices (System.Collections.BitArray(N*N*N)) 

    // let vmin = Vector3.Zero
    // let vmax = Vector3.One
    let rnd = System.Random.Shared
    // let vs  = Array.init 300_000 (fun _ -> float32(rnd.NextDouble()) * Vector3.One)

    // let tree_old = new Octree.Root<uint>(N, k, vmin, vmax)
    // let tree_new = new OctreeFlat.Octree<uint>(N, k, vmin, vmax)
    let tree = Octree.ofStencil<uint> N k v_min v_max bits
    let tree_flatten = OctreeFlatten.ofStencil<uint> N k v_min v_max bits
    do
        tree.Iter (fun u ->
            match u with
            | Octree.Boundary & Octree.Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome (entity() |> set true |> set 10 |> set 0.1 |> set 10u)
            | Octree.Internal & Octree.Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome (entity() |> set false |> set 40 |> set 0.4 |> set 20u)
            | _ -> ()
        )
        
        tree_flatten.Iter (fun t i ->
            let u = &t[i]
            match u.bounds with
            | OctreeFlatten.Bounds.Boundary -> u.value <- entity() |> set true |> set 10 |> set 0.1 |> set 10u
            | OctreeFlatten.Bounds.Internal -> u.value <- entity() |> set false |> set 40 |> set 0.4 |> set 20u
            | _ -> ()            
        )
    
    let vs = tree.AsPoints()
    let rs  = Array.init (vs.Length) (fun _ ->
        let x = v_min.X + float32(rnd.NextDouble()) * (v_max.X - v_min.X) 
        let y = v_min.Y + float32(rnd.NextDouble()) * (v_max.Y - v_min.Y) 
        let z = v_min.Z + float32(rnd.NextDouble()) * (v_max.Z - v_min.Z) 
        Vector3.Clamp(Vector3(x,y,z), v_min, v_max)
    )

    
    [<Benchmark>]
    member this.BuildOctree () =
        let tree = Octree.ofStencil<uint> N k v_min v_max bits
        tree.Iter (fun u ->
            match u with
            | Octree.Boundary & Octree.Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome 10u
            | Octree.Internal & Octree.Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome 20u
            | _ -> ()
        )
        
    [<Benchmark>]
    member this.BuildFlatOctree () =
        let tree = OctreeFlatten.ofStencil<uint> N k v_min v_max bits
        tree.Iter (fun t i ->
            let u = &t[i]
            match u.bounds with
            | OctreeFlatten.Bounds.Internal -> u.value <- 10u
            | OctreeFlatten.Bounds.External -> u.value <- 20u
            | _ -> ()
        )
       

    [<Benchmark>]
    member this.MapClosestOctree() =
        for r in rs do
            match tree.MapTo r with            
            | Octree.Leaf _ & Octree.Boundary -> ()
            | Octree.Leaf _ & Octree.Internal -> ()
            | _ -> ()
      
    [<Benchmark>]
    member this.MapClosestOctreeFlat() =
        for r in rs do
            let idx = tree_flatten.MapTo(r)
            match tree_flatten[idx].bounds with
            | OctreeFlatten.Bounds.Boundary -> ()
            | OctreeFlatten.Bounds.Internal -> ()
            | _ -> ()

    // [<Benchmark>]
    // member this.InitOctree() =
    //     tree.Iter (fun u ->
    //         match u with
    //         | Octree.Boundary & Octree.Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome (entity() |> set true |> set 10 |> set 0.1 |> set 10u)
    //         | Octree.Internal & Octree.Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome (entity() |> set false |> set 40 |> set 0.4 |> set 20u)
    //         | _ -> ()
    //     )

      
    // [<Benchmark>]
    // member this.InitOctreeFlat() =
    //     tree_flatten.Iter (fun i ->
    //         let u = &tree_flatten[i]
    //         match u.bounds with
    //         | OctreeFlatten.Bounds.Boundary -> u.value <- entity() |> set true |> set 10 |> set 0.1 |> set 10u
    //         | OctreeFlatten.Bounds.Internal -> u.value <- entity() |> set false |> set 40 |> set 0.4 |> set 20u
    //         | _ -> ()            
    //     )

    // [<Benchmark>]
    // member this.UpdateOctree() =
    //     tree.Update((fun _ -> false), (fun _ -> true), (fun _ -> 0u))
      
    // [<Benchmark>]
    // member this.UpdateOctreeFlatten() =
    //     tree_flatten.Update((fun _ -> false), (fun _ -> true), (fun _ -> 0u))
      

    [<Benchmark>]
    member this.IterTree() =
        tree.Iter (fun u ->
            let B = Components.get<bool>()
            let I = Components.get<int>()
            let F = Components.get<double>()
            match u with
            | Octree.Leaf (_,v,_,_,_,_) & Octree.Boundary ->
                let b = B[v.Value.Value]
                let i = I[v.Value.Value]
                let f = F[v.Value.Value]
                ()
            | _ -> ()
        )

    [<Benchmark>]
    member this.IterTreeFlatten() =
        tree_flatten.Iter (fun t i ->
            let B = Components.get<bool>()
            let I = Components.get<int>()
            let F = Components.get<double>()
            let u = t[i]
            let b = B[u.value]
            let i = I[u.value]
            let f = F[u.value]
            ()
        )

    [<Benchmark>]
    member this.IterTreeParallel() =
        tree.IterParallel 4 (fun u ->
            let B = Components.get<bool>()
            let I = Components.get<int>()
            let F = Components.get<double>()
            match u with
            | Octree.Leaf (_,v,_,_,_,_) & Octree.Internal ->
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

                F[tree[Octree.center u].Value] <- 
                    (2./(x1*(x1+x2))*F[!i] + 2./(x2*(x1+x2))*F[!i'] +
                    2./(y1*(y1+y2)) *F[!j] + 2./(y2*(y1+y2))*F[!j'] +
                    2./(z1*(z1+z2)) *F[!l] + 2./(z2*(z1+z2))*F[!l']) /
                    (2./(x1*x2) + 2./(y1*y2) + 2./(z1*z2))
            | _ -> ()
        )

    [<Benchmark>]
    member this.IterTreeFlattenParallel() =
        tree_flatten.IterParallel (fun t ni ->
            let B = Components.get<bool>()
            let I = Components.get<int>()
            let F = Components.get<double>()
            let u = t[ni]
            match u.bounds with
            | OctreeFlatten.Bounds.Internal -> 
                let i  = t[ni, -1,0,0]
                let i' = t[ni, +1,0,0]
                let j  = t[ni, 0,-1,0]
                let j' = t[ni, 0,+1,0]
                let l  = t[ni, 0,0,-1]
                let l' = t[ni, 0,0,+1]

                let x1 = double (u.Center - i.Center).X
                let y1 = double (u.Center - j.Center).Y
                let z1 = double (u.Center - l.Center).Z

                let x2 = double (i'.Center - u.Center).X
                let y2 = double (j'.Center - u.Center).Y
                let z2 = double (l'.Center - u.Center).Z

                F[t[u.Center].value] <- 
                    (2./(x1*(x1+x2))*F[!!i] + 2./(x2*(x1+x2))*F[!!i'] +
                    2./(y1*(y1+y2)) *F[!!j] + 2./(y2*(y1+y2))*F[!!j'] +
                    2./(z1*(z1+z2)) *F[!!l] + 2./(z2*(z1+z2))*F[!!l']) /
                    (2./(x1*x2) + 2./(y1*y2) + 2./(z1*z2))
            | _ -> ()
        )


BenchmarkRunner.Run<OctreeBenchmarks>() |> ignore



