#r "../bin/Debug/net9.0/SE-core.dll"
// #load "../src/linearalg.fs"
// #load "../src/numerics.fs"
// #load "../src/fem.fs"

open System
open SE.Numerics
open SE.FEM
open SE.Numerics.PtrOperations

let par = Parameters()
let nodes = new NodeArray(par)
let elements = new ElementArray(par, nodes)
let solver = (new Matrix(nodes.N, nodes.N), new Vector(nodes.N), new Vector(nodes.N))

nodes.FillGlobalIndexes()

// fill indices & set locations & bondary condition
for i = 1 to par.ni do
    for j = 1 to par.nj do
        for k = 1 to par.nk do
            let x = (float i) * (float par.ni) / 100.
            let y = (float j) * (float par.nj) / 30.
            let z = (float k) * (float par.nk) / 200.
            nodes.SetInitialLocation(i,j,k, x, y, z)

            if (i = 1) then nodes.SetZeroBoundaryCondition(i,j,k)
            if (j = 1) then nodes.SetZeroBoundaryCondition(i,j,k)
            
            // let g = nodes[i,j,k].global_index
            // let (gx, gy, gz) = (g[0], g[1], g[2])
            // let p = !!nodes[i,k,k].xyz
            // printfn "global_indices: <%d, %d, %d>,   pos: <%g, %g, %g>" gx gy gz p.x p.y p.z

elements.InitializeAllElements()

let solver_result_to_elementarray () =
    let (_,_,solution_b) = solver
    for i = 0 to par.ni - 1 do
        for j = 0 to par.nj - 1 do
            for k = 0 to par.nk - 1 do
                let local_element = elements.Elems[i,j,k]
                for r = 1 to 24 do
                    let index = int32 (local_element.GetGlobalIndex(r))
                    if index > -1 then
                        local_element.Dq[r] <- solution_b[index]
                        
let fs1 = System.IO.File.CreateText("output/fem_solution.txt")
let fs2 = System.IO.File.CreateText("output/fem_solution.dat")

fs1.Close()
fs2.Close()
nodes.Dispose()
