open System
open SE.Core
open SE.ECS
open SE.Renderer.Imaging

open BenchmarkDotNet.Attributes
open BenchmarkDotNet.Running
open BenchmarkDotNet.Jobs
open FSharp.Data.UnitSystems.SI

[<SimpleJob>]
type QuadtreeBenchmarks() =
    let path = "cool_image_00.png"
    let N = 1000

    let (stencil,N',v_min,v_max) = get_pixels N path

    let valueof = Quadtree.valueof
    let kindof  = Quadtree.kindof

    let tree_1 =
        stencil
        |> Quadtree.ofStencil<double> N 1 v_min v_max
        |> Quadtree.init 0.00

    do
        if N <> N' then failwith "N cannot differ"
        tree_1.Add <- (+)
        tree_1.Div <- (/)

    let tree_2 =
        stencil
        |> Quadtree.ofStencil<double> N 1 v_min v_max
        |> Quadtree.init 0.00

    do
        if N <> N' then failwith "N cannot differ"
        tree_2.Add <- (+)
        tree_2.Div <- (/)

        
    let tree_3 =
        stencil
        |> Quadtree.ofStencil<double> N 1 v_min v_max
        |> Quadtree.init 0.00

    do
        if N <> N' then failwith "N cannot differ"
        tree_3.Add <- (+)
        tree_3.Div <- (/)


    let tree_4 =
        stencil
        |> Quadtree.ofStencil<double> N 1 v_min v_max
        |> Quadtree.init 0.00

    do
        if N <> N' then failwith "N cannot differ"
        tree_4.Add <- (+)
        tree_4.Div <- (/)

    [<Benchmark>]
    member this.Iter1 () =
        let dx = tree_1.dX
        let dy = tree_1.dY
        tree_1.IterParallel 1 (fun node ->
            match (kindof dx dy node) with
            | Quadtree.Internal ->
                let c = Quadtree.center node
                let x = double c.X
                let y = double c.Y
                tree_1[0,0] <- 150.0
                
            | Quadtree.Boundary ->
                tree_1[0,0] <- 300.

            | Quadtree.External -> ()                            
        )
        // tree.Update(_trim, _dense, _set)
        // Quadtree.morph tree.Root tree'.Root (+) (/)

    [<Benchmark>]
    member this.Iter2 () =
        let dx = tree_2.dX
        let dy = tree_2.dY
        tree_2.IterParallel 2 (fun node ->
            match (kindof dx dy node) with
            | Quadtree.Internal ->
                let c = Quadtree.center node
                let x = double c.X
                let y = double c.Y
                tree_2[0,0] <- 150.0
                
            | Quadtree.Boundary ->
                tree_2[0,0] <- 300.

            | Quadtree.External -> ()                            
        )

    [<Benchmark>]
    member this.Iter3 () =
        let dx = tree_3.dX
        let dy = tree_3.dY
        tree_3.IterParallel 3 (fun node ->
            match (kindof dx dy node) with
            | Quadtree.Internal ->
                let c = Quadtree.center node
                let x = double c.X
                let y = double c.Y
                tree_3[0,0] <- 150.0
                
            | Quadtree.Boundary ->
                tree_3[0,0] <- 300.

            | Quadtree.External -> ()                            
        )

    [<Benchmark>]
    member this.Iter4 () =
        let dx = tree_4.dX
        let dy = tree_4.dY
        tree_4.IterParallel 4 (fun node ->
            match (kindof dx dy node) with
            | Quadtree.Internal ->
                let c = Quadtree.center node
                let x = double c.X
                let y = double c.Y
                tree_4[0,0] <- 150.0
                
            | Quadtree.Boundary ->
                tree_4[0,0] <- 300.

            | Quadtree.External -> ()                            
        )

BenchmarkRunner.Run<QuadtreeBenchmarks>() |> ignore


