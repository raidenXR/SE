#load "../src/serializers.fs"
#load "../src/linearalg.fs"

open SE.Serializers
open SE.Numerics
open Matrix


let a = Matrix.random 5 5 0.3
printfn "column: %g" (columnSumAbs a 4)
printfn "row: %g" (rowSumAbs a 3)
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
    
do
    use I = identity a
    printfn "%s" (string (inverse I))

do
    use _eigenvalues = Matrix.eigenvalues a
    let norm = Matrix.L2 a
    printfn "norm: %g" norm
    printfn "eigenvalues:\n%s" (string _eigenvalues)

// do
//     // use s = Matrix.random 6 6 10.
//     printfn "determinant\n%g" (determinant a)
//     printfn "inverse\n%s" (string (inverse a))

// do
//     use b = Vector.random a.N 12.
//     printfn "gauss_elimination\n%s" (string (Solvers.Gauss_elimination a b))
//     printfn "gauss_seidel\n%s" (string (Solvers.GaussSeidel a b))
//     printfn "Jacobi\n%s" (string (Solvers.Jacobi a b 300))

//     printfn "determinant: %g" (determinant a)

// do
//     let (L,U) = Decomposition.LU a
//     printfn "L:\n%s" (string L)    
//     printfn "U:\n%s" (string U)    
//     printfn "recomposition\n%s" (string (L * U))
//     printfn "original_matrix\n%s" (string a)

