#nowarn "9"
#nowarn "51"
namespace SE.Mathematics
open SE.Serializers
open System
open System.Buffers
open FSharp.NativeInterop

type Vector(n:int, values:array<float>) =
    let buffer = if values = null then ArrayPool<float>.Shared.Rent(n) else values
    let is_from_arraypool = if values = null then true else false
    let mutable entries = ArraySegment(buffer, 0, n)
    let mutable is_disposed = false

    new(n:int) = new Vector(n, null) 

    new(values:array<float>) = new Vector(values.Length, values)

    interface IDisposable with
        member this.Dispose() =
            if is_from_arraypool then
                if not is_disposed then ArrayPool<float>.Shared.Return(buffer)
                is_disposed <- true                    

    member this.Length = n
    member this.Entries = entries

    member this.Dispose() = (this :> IDisposable).Dispose()

    member this.Clear() =
        for i in 0..n - 1 do entries[i] <- 0.

    member this.Item
        with inline get(i:int) = entries[i - 1]
        and inline set(i:int) value = entries[i - 1] <- value

    override this.ToString() =
        let _v = entries.ToArray()
        sprintf "%A" _v


    
type Matrix(m:int, n:int, values:array<float>) =
    let buffer = if values = null then ArrayPool<float>.Shared.Rent(m * n) else values
    let is_from_arraypool = if values = null then true else false
    let mutable entries = ArraySegment(buffer, 0, m * n)
    let mutable is_disposed = false

    new(m:int, n:int) = new Matrix(m, n, null) 

    interface IDisposable with
        member this.Dispose() =
            if is_from_arraypool then
                if not is_disposed then ArrayPool<float>.Shared.Return(buffer)
                is_disposed <- true                    

    member this.M = m
    member this.N = n
    member this.Entries = entries

    member this.Dispose() = (this :> IDisposable).Dispose()

    member this.Clear() =
        for i in 0..(m * n) - 1 do entries[i] <- 0.

    member this.Item
        with inline get(i:int, j:int) = entries[(i - 1) * n + (j - 1)]
        and inline set(i:int, j:int) value = entries[(i - 1) * n + (j - 1)] <- value

    override this.ToString() =
        let _h = [for i in 1..n -> $"n{i}"]
        let _v = [
            for i = 1 to m do
                let _r = Array.zeroCreate<float> n
                for j = 1 to n do
                    _r[j - 1] <- entries[(i - 1) * n + (j - 1)]
                yield (_r |> Array.map (fun x -> string x) |> List.ofArray)
        ]
        string (Ascii.table _h _v (Ascii.AsciiBuilder()))

    static member (+) (a:Matrix, b:Matrix) =
        let c = new Matrix(a.M, a.N)
        for i = 1 to a.M do
            for j = 1 to a.N do
                c[i,j] <- a[i,j] + b[i,j]
        c        

    static member (-) (a:Matrix, b:Matrix) =
        let c = new Matrix(a.M, a.N)
        for i = 1 to a.M do
            for j = 1 to a.N do
                c[i,j] <- a[i,j] - b[i,j]
        c        

    static member (*) (s, m:Matrix) =
        let r = new Matrix(m.M, m.N)
        for i in 1..m.M do
            for j in 1..m.N do
                r[i,j] <- s * m[i,j]
        r
        
    static member (*) (a:Matrix, b:Matrix) =
        assert (a.N = b.M)
        let c = new Matrix(a.M, b.N)
        for i in 1..a.M do
            for j in 1..b.N do
                let mutable sum = 0.
                for k = 1 to a.N do
                    sum <- sum + a[i,k] * b[k,j]
                c[i,j] <- sum 
        c

    static member (*) (a:Matrix, x:Vector) =
        assert (a.N = x.Length)
        let y = new Vector(a.M)
        for i = 1 to a.M do
            let mutable sum = 0.
            for j = 1 to a.N do
                sum <- sum + a[i,j] * x[j]
            y[i] <- sum
        y

    static member (*) (x:Vector, a:Matrix) =
        assert (a.M = x.Length)
        let y = new Vector(a.N)
        for j = 1 to a.N do
            let mutable sum = 0.
            for i = 1 to a.M do
                sum <- sum + a[i,j] * x[i]
            y[j] <- sum
        y


module Vector =
    let dot (x:Vector) (y:Vector) =
        assert (x.Length = y.Length)
        let mutable s = 0.
        for i = 1 to x.Length do
            s <- s + x[i] * y[i]
        s

    let cross (a:Vector) (b:Vector) =
        assert (a.Length = b.Length && a.Length = 3)
        new Vector([|
            a[2] * b[3] - a[3] * b[2]
            a[3] * b[1] - a[1] * b[3]
            a[1] * b[2] - a[2] * b[1]
        |])

    let diadic (x:Vector) (y:Vector) =
        let a = new Matrix(x.Length, y.Length)
        for i = 1 to a.M do
            for j = 1 to a.N do
                a[i,j] <- x[i] * y[j]
        a

    let copy (v:Vector) =
        let a = new Vector(v.Length)
        for i = 1 to a.Length do
            a[i] <- v[i]
        a

    let zeroes (n:int) =
        let v = new Vector(n)
        for i = 1 to n do
            v[i] <- 0.
        v


module Matrix =
    let (!+) (a:Matrix) (b:Matrix) =
        for i = 1 to a.M do
            for j = 1 to a.N do
                a[i,j] <- a[i,j] + b[i,j]
        

    let (!-) (a:Matrix) (b:Matrix) =
        for i = 1 to a.M do
            for j = 1 to a.N do
                a[i,j] <- a[i,j] - b[i,j]
        
        
    let (!*) s (m:Matrix) =
        for i in 1..m.M do
            for j in 1..m.N do
                m[i,j] <- s * m[i,j]
        

    let transpose (m:Matrix) =
        let t = new Matrix(m.N, m.M)
        for i = 1 to t.M do
            for j = 1 to t.N do
                t[i,j] <- m[j,i]
        t 

    let nul (m:Matrix) =
        let mutable n = 0
        for i = 1 to m.M do
            let mutable is_empty = m[i, 1] = 0.
            for j = 1 to m.N do
                is_empty <- is_empty && m[i,j] = 0.
            n <- if is_empty then n + 1 else n

        for j = 1 to m.N do
            let mutable is_empty = m[1,j] = 0.
            for i = 1 to m.M do
                is_empty <- is_empty && m[i,j] = 0.
            n <- if is_empty then n + 1 else n        
        n

    let rank (m:Matrix) = m.N - (nul m)

    let identity (m:Matrix) =
        assert (m.M = m.N)
        let I = new Matrix(m.M, m.N)
        for i = 1 to m.M do
            for j = 1 to m.N do
                I[i,j] <- if i = j then 1.0 else 0.0  
        I

    let trace (m:Matrix) =
        assert (m.M = m.N)
        let mutable sum = 0.
        for i = 1 to m.M do
            for j = 1 to m.N do
                sum <- if i = j then sum + m[i,j] else sum
        sum

    let copy (m:Matrix) =
        let a = new Matrix(m.M, m.N)
        for i = 1 to m.M do
            for j = 1 to m.N do
                a[i,j] <- m[i,j]
        a

    let zeroes m n =
        let a = new Matrix(m, n)
        for i = 1 to a.M do
            for j = 1 to a.N do
                a[i,j] <- 0.
        a
        
    let rec determinant (m:Matrix) =
        assert (m.M = m.N)
        match m.N with
        | 0 -> Double.NaN
        | 1 -> m[1,1]
        | 2 -> m[1,1] * m[2,2] - m[1,2] * m[2,1]
        | 3 ->
            let a, b, c = m[1,1], m[1,2], m[1,3]
            let d, e, f = m[2,1], m[2,2], m[2,3]
            let g, h, i = m[3,1], m[3,2], m[3,3]
            a * (e * i - f * h) - b * (d * i - f * g) + c * (d * h - e * g)
        | _ ->
            let mutable res = 0.
            for col = 1 to m.N do
                use _m = new Matrix(m.M - 1, m.N - 1)
                for i = 2 to m.M do
                    let mutable subcol = 0
                    for j = 1 to m.N do
                        if col = j then
                            ()   // skip the current column
                        else
                            _m[i - 1, subcol + 1] <- m[i,j]  // fill the matrix
                            subcol <- subcol + 1
                let sign = if col % 2 = 0 then 1. else -1.
                res <- res + sign * m[1,col] * (determinant _m)
            res

    let inverse (m:Matrix) =
        let a = copy m
        printfn "copy\n%s" (string a)
        for i = 1 to a.N do
            // if a[i,i] < (1e-12) then printfn "dividing with a[%d,%d] = %g" i i a[i,i]; a[i,i] <- 1e-22  // check 
            let d = 1. / a[i,i]
            let mutable tt = -d
            for j = 1 to a.N do
                a[i,j] <- a[i,j] * tt
            for k = 1 to i - 1 do
                tt <- a[k,i]
                // if a[k,i] < (1e-12) then printfn "a[%d,%d]: %g, %g" k i a[k,i] tt  // check
                for j = 1 to i - 1 do
                    a[k,j] <- a[k,j] + tt * a[i,j]
                for j = i + 1 to a.N do
                    a[k,j] <- a[k,j] + tt * a[i,j]
                a[k,i] <- tt * d
            for k = i + 1 to a.N do
                tt <- a[k,i]
                // if a[k,i] < (1e-12) then printfn "a[%d,%d]: %g, %g" k i a[k,i] tt  // check
                for j = 1 to i - 1 do
                    a[k,j] <- a[k,j] + tt * a[i,j]
                for j = i + 1 to a.N do
                    a[k,j] <- a[k,j] + tt * a[i,j]
                a[k,i] <- tt * d
                // if a[k,i] < (1e-12) then printfn "tt: %g, d: %g" tt d  // check
            a[i,i] <- d
            // if a[i,i] < (1e-12) then printfn "a[%d,%d]: %g, %g" i i a[i,i] d  // check
        a



module Solvers =
    let Cholesky_decomposition (m:Matrix) =
        assert (m.M = m.N)
        let lower = Matrix.zeroes m.M m.N
        for i = 1 to m.M do
            for j = 1 to i do
                let mutable sum = 0.
                if j = i then
                    for k = 1 to j - 1 do
                        sum <- sum + (pown lower[j,k] 2)
                    lower[j,j] <- sqrt (m[j,j] - sum)
                else
                    for k = 1 to j - 1 do
                        sum <- sum + (lower[i,k] * lower[j,k])
                    lower[i,j] <- (m[i,j] - sum) / lower[j,j]
        let upper = Matrix.transpose lower
        (lower,upper)
                

    let Jacobi (A:Matrix) (B:Vector) N =
        use a = Matrix.copy A
        use b = Vector.copy B
        let x = Vector.zeroes b.Length
        for p = 1 to N do
            for i = 1 to a.M do
                let mutable sigma = 0.
                for j = 1 to a.N do
                    if (j <> i) then
                        sigma <- sigma + a[i,j] * x[j]
                x[i] <- (b[i] - sigma) / a[i,i]
        x
    
    let Gauss_elimination (A:Matrix) (B:Vector) =
        use a = Matrix.copy A
        use b = Vector.copy B
        let x = Vector.zeroes b.Length
        let forward_substitution () =
            for k = 1 to a.N - 1 do
                for i = k + 1 to a.N do
                    for j = k + 1 to a.N do
                        a[i,j] <- a[i,j] - a[k,j] * a[i,k] / a[k,k]  // matrix factorization
                    b[i] <- b[i] - b[k] * a[i,k] / a[k,k]

        let backward_substitution () =
            for i = a.N to 1 do
                let mutable sum = 0.
                for j = i + 1 to a.N do
                    sum <- sum + a[i,j] * x[j]
                x[i] <- (b[i] - sum) / a[i,i]

        forward_substitution ()
        backward_substitution ()                    
        x
        
    let GaussSeidel (A:Matrix) (B:Vector) =
        assert (A.M = A.N && A.M = B.Length)
        use a = Matrix.copy A
        use b = Vector.copy B
        let x = Vector.zeroes b.Length
        for j = 1 to a.N do
            let mutable d = b[j]
            for i = 1 to a.M do
                if j <> i then
                    d <- d - a[j,i] * x[i]
            x[j] <- d / a[j,j]
        x



        
