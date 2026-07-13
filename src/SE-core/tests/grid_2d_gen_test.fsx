#r "../bin/Debug/net10.0/SE-core.dll"
// #r "../bin/Release/net10.0/SE-core.dll"

open SE
open SE.Core
open System.Numerics
open System
open GridGeneration2D
open Plotting

open Quadtree

let valueof = Quadtree.valueof
let kindof  = Quadtree.kindof

let path = "domains/coordinates_dense.dat"
let points =
    seq {
        for line in (System.IO.File.ReadAllLines(path)) do
            let dat = line.Split(',')
            if dat.Length >= 2 then
                let v0 = double dat[1]
                let v1 = double dat[2]
                yield Vector2(float32 v0, float32 v1)        
    }
    |> Array.ofSeq

let N = 180
let (v_min,v_max) = GridGeneration2D.bounds points
let (vs,stencil) =
    System.Collections.BitArray(N*N)
    |> Quadtree.fill_raycast N v_min v_max points
// let stencil = GridGeneration2D.bitstencil points N |> GridGeneration2D.fill_bitstencil N 

let tree = Quadtree.ofStencil<double> N 3 v_min v_max stencil
let pts = tree.AsPoints()
let xs = pts |> Array.map (fun v -> double v.X)
let ys = pts |> Array.map (fun v -> double v.Y)
let xx = vs |> Array.map (fun v -> double v.X)
let yy = vs |> Array.map (fun v -> double v.Y)

Gnuplot()
|> Gnuplot.datablockXY xs ys "points"
|> Gnuplot.datablockXY xx yy "bounds"
|>> "set ratio -1"
|>> "plot $points with points lc rgb 'black', \\"
|>> "$bounds with points lw 2 lc rgb 'black'"
|> Gnuplot.run
|> ignore

Console.ReadKey()


