#load "../src/serializers.fs"
#load "../src/numerical_methods.fs"

open SE.Serializers
open SE.Mathematics
open Matrix

let a = new Matrix(5, 5, [|
    4.; 6.; 8.; 10.; 0.
    4.; 6.; 8.; 10.; 0.
    4.; 6.; 8.; 10.; 0.
    4.; 6.; 8.; 10.; 0.
    3.; 3.; 8.; 90.; 0.
|])
printfn "A:\n%s" (string a)

do
    use i = identity a
    let t = trace a
    printfn "trace: %g\nIdentity:\n%s" t (string i)

do
    let x = new Vector([|1.; 1.; 4.; 5.; 3.|])
    let y = new Vector([|9.; 8.; 3.|])
    let z = new Vector([|0.; 4.; 3.|])
    use xy = Vector.diadic x y
    use i = identity a
    use b = 4. * a + 3. * i - a 
    use c = x * b
    use d = b * x
    // printfn "%s" (string b)
    printfn "operators_matrix\n%s" (string b)
    printfn "operators_vector\n%s" (string c)
    printfn "operators_vector\n%s" (string d)    
    printfn "diadic\n%s" (string xy)    
    printfn "cross\n%s" (string (Vector.cross y z))    

do
    use t = transpose a
    let r = rank t
    let n = nul t
    printfn "rank: %d, nul: %d" r n
    printfn "transpose\n%s" (string t)
    
do
    let s = new Matrix(6,6, [|
        4.; 5.; 1.; 2.; 4.; 6.
        3.; 9.; 1.; 2.; 9.; 6.
        1.; 1.; 3.; 2.; 1.; 1.
        4.; 4.; 4.; 1.; 9.; 6.
        1.; 9.; 3.; 2.; 9.; 4.
        1.; 1.; 3.; 2.; 1.; 1.
    |])
    printfn "determinant\n%g" (determinant s)
    printfn "inverse\n%s" (string (inverse s))

do
    let b = new Vector([|6.; 1.; 5.; 9.; 7|])
    printfn "gauss_elimination\n%s" (string (Solvers.Gauss_elimination a b))
    printfn "gauss_seidel\n%s" (string (Solvers.GaussSeidel a b))
    printfn "Jacobi\n%s" (string (Solvers.Jacobi a b 620))

    let d = determinant a
    printfn "determinant: %g" d

do
    let (L,U) = Solvers.Cholesky_decomposition a
    printfn "L:\n%s" (string L)    
    printfn "U:\n%s" (string U)    
