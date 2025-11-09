#r "../bin/Debug/net9.0/SE-core.dll"
// #load "../src/linearalg.fs"
// #load "../src/numerics.fs"
// #load "../src/fem.fs"

open System
open SE.Numerics
open SE.FEM


let fs1 = System.IO.File.CreateText("output/fem_solution.txt")
let fs2 = System.IO.File.CreateText("output/fem_solution.dat")
let container = new Container()
container.Initialize()
container.SetZeroBoundaryCondition(3, 5, 6)
// container.Solve_FEM()
container.GetActualSolution(fs1)
container.GetViewSolution(fs2)
fs1.Close()
fs2.Close()
container.Dispose()
