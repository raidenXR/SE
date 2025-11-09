#load "../src/gnuplot.fs"
#load "../src/linearalg.fs"

open SE.Plotting
open SE.Numerics
open Matrix


let a = Matrix.random 8 8 0.3
// let I = identity A
// let a = A + (16. * I)
// printfn "column: %g" (columnSumAbs a 4)
// printfn "row: %g" (rowSumAbs a 3)
// let c = a.ColumnVector(3)
printfn "original_matrix\n%s" (string a)
// printfn "%s" (string c)

// do
//     use x = Vector.random a.N 0.5
//     use y = Vector.random 3 0.3
//     use z = Vector.random 3 0.6
//     use i = identity a
//     use b = 4. * a + 3. * i - a 
//     // inplace
//     use w = Matrix.copy a
//     w += a
//     w *= 4.
//     w -= a
//     printfn "operators_matrix\n%s" (string b)
//     printfn "operators_vector\n%s" (string (x * a))
//     printfn "operators_vector\n%s" (string (a * x))    
//     printfn "diadic\n%s" (string (Vector.diadic y z))    
//     printfn "diadic\n%s" (string (Vector.diadic z y))    
//     printfn "cross\n%s" (string (Vector.cross y z))    
//     printfn "cross\n%s" (string (Vector.cross z y))    

// do
//     use t = transpose a
//     let r = rank t
//     let n = nul t
//     printfn "rank: %d, nul: %d" r n
//     printfn "transpose\n%s" (string t)
    
// do
//     use I = identity a
//     printfn "%s" (string (inverse I))

// do
//     use _a = inverse a
//     printfn "eigenvalues:\n%s" (string (eigenvalues a))
//     printfn "norm: %g, inv norm: %g" (L2 a) (L2 _a)
//     printfn "condition_number: %g" (conditionNumber a)
//     printfn "inverse:\n%s" (string _a)

// do
//     // use s = Matrix.random 6 6 10.
//     printfn "determinant\n%g" (determinant a)
//     printfn "inverse\n%s" (string (inverse a))

do
    use b = Vector.random a.N 2.
    // printfn "diagonal\n%s" (string (diagonal a))
    // printfn "LU_solve\n%s" (string (Solvers.LUSolve a b))
    // printfn "gauss_elimination\n%s" (string (Solvers.GaussElimination a b))
    printfn "Jacobi\n%s" (string (Solvers.Jacobi a b))
    // printfn "gauss_seidel\n%s" (string (Solvers.GaussSeidel a b))

    printfn "eigenvalues: %s" (string (eigenvalues a))
    printfn "determinant: %g" (determinant a)

// do
//     let (L,U) = Decomposition.LU a
//     printfn "L:\n%s" (string L)    
//     printfn "U:\n%s" (string U)    
//     printfn "recomposition\n%s" (string (L * U))
//     printfn "original_matrix\n%s" (string a)

// do
//     use n = new Matrix(3,3)
//     use v = new Vector(3)
//     use x = Vector.undefined 3
//     use y = Matrix.undefined 3 3

//     let w = new Vector([|1.;2.;3.;4.|])
//     let h = new Matrix(2,2, [|
//         4.; 6.
//         9.; 3.
//     |])

//     printfn "new vec, is_pooled: %b:\n%s" (v.IsPooled) (string v)
//     printfn "new mat, is_pooled: %b:\n%s" (n.IsPooled) (string n)
    
//     printfn "new vec, is_pooled: %b:\n%s" (w.IsPooled) (string w)
//     printfn "new mat, is_pooled: %b:\n%s" (h.IsPooled) (string h)
    
//     printfn "undefined vec, is_pooled: %b:\n%s" (x.IsPooled) (string x)
//     printfn "undefined mat, is_pooled: %b:\n%s" (y.IsPooled) (string y)


// do
    // use v = Vector.init 10 (fun x -> 0.1 * x + 0.2 * x * x)
    // let pi = System.Math.PI
    // printfn "f initialized vec: %s" (string v)
    // use m = Matrix.init 111 111 (fun x y -> sin(pi * 0.01 * x)  + sin(pi * 0.01 * y))
    // m.SaveAsGrid(Txt, "output/m_dat.txt", (0.,1.), (0.,1.))
    // Gnuplot()
    // |>> "set title 'test contour'"
    // |>> "set grid"
    // |>> "set dgrid3d"
    // |>> "splot 'output/m_dat.txt' with lines"
    // |> Gnuplot.run
    // |> ignore

    // ignore (System.Console.ReadKey())
    

