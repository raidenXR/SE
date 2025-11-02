#nowarn "9"
#nowarn "51"
namespace SE.Numerics
open SE.Serializers
open System
open System.Buffers
open FSharp.NativeInterop

type Vector(n:int, values:array<float>) =
    let buffer = if values = null then ArrayPool<float>.Shared.Rent(n) else values
    let is_from_arraypool = if values = null then true else false
    let mutable entries = ArraySegment(buffer, 0, n)
    let mutable is_disposed = false
    let clear () = Array.Clear(entries.Array)
    do
        if is_from_arraypool then clear ()

    new(n:int) = new Vector(n, null) 

    new(values:array<float>) = new Vector(values.Length, values)

    interface IDisposable with
        member this.Dispose() =
            if is_from_arraypool then
                if not is_disposed then ArrayPool<float>.Shared.Return(buffer)
                // printfn "vector got disposed"
                is_disposed <- true                    

    member this.Length = n
    member this.Entries = entries

    member this.Dispose() = (this :> IDisposable).Dispose()

    member this.Clear() = clear ()

    member this.Item
        with get(i:int) = entries[i - 1]
        and set(i:int) value = entries[i - 1] <- value

    override this.ToString() =
        let _v = entries.ToArray()
        sprintf "%A" _v


    static member (+) (a:Vector, b:Vector) =
        assert (a.Length = b.Length)
        let c = new Vector(a.Length)
        for i = 1 to a.Length do
            c[i] <- a[i] + b[i]
        c

    static member (-) (a:Vector, b:Vector) =
        assert (a.Length = b.Length)
        let c = new Vector(a.Length)
        for i = 1 to a.Length do
            c[i] <- a[i] - b[i]
        c
    
    static member (*) (s:float, v:Vector) =
        let c = new Vector(v.Length)
        for i = 1 to v.Length do
            c[i] <- s * v[i]
        c
    
    static member (/) (v:Vector, s:float) =
        let c = new Vector(v.Length)
        for i = 1 to v.Length do
            c[i] <- v[i] / s
        c
    
    static member (*) (v:Vector, s:float) =
        let c = new Vector(v.Length)
        for i = 1 to v.Length do
            c[i] <- s * v[i]
        c

    static member (*) (a:Vector, b:Vector) =
        let mutable sum = 0.
        for i = 1 to a.Length do
            sum <- sum + a[i] * b[i]
        sum

    static member (+=) (a:Vector, b:Vector) =
        assert (a.Length = b.Length)
        for i = 1 to a.Length do
            a[i] <- a[i] + b[i]

    static member (-=) (a:Vector, b:Vector) =
        assert (a.Length = b.Length)
        for i = 1 to a.Length do
            a[i] <- a[i] - b[i]

    static member ( *= ) (a:Vector, s:float) =
        for i = 1 to a.Length do
            a[i] <- a[i] * s

    static member (/=) (a:Vector, s:float) =
        for i = 1 to a.Length do
            a[i] <- a[i] / s

    static member (==) (a:Vector, b:Vector) =
        for i = 1 to a.Length do
            a[i] <- b[i]

    
type Matrix(m:int, n:int, values:array<float>) =
    let buffer = if values = null then ArrayPool<float>.Shared.Rent(m * n) else values
    let is_from_arraypool = if values = null then true else false
    let mutable entries = ArraySegment(buffer, 0, m * n)
    let mutable is_disposed = false
    let clear () = Array.Clear(entries.Array)
    do
        if is_from_arraypool then clear ()

    new(m:int, n:int) = new Matrix(m, n, null) 

    interface IDisposable with
        member this.Dispose() =
            if is_from_arraypool then
                if not is_disposed then ArrayPool<float>.Shared.Return(buffer)
                // printfn "matrix got disposed"
                is_disposed <- true                    

    member this.M = m
    member this.N = n
    member this.Entries = entries

    member this.Dispose() = (this :> IDisposable).Dispose()

    member this.Clear() = clear ()

    member this.Item
        with get(i:int, j:int) = entries[(i - 1) * n + (j - 1)]
        and set(i:int, j:int) value = entries[(i - 1) * n + (j - 1)] <- value


    member this.RowVector(i:int) =
        let v = new Vector(m)
        for j = 1 to n do
            v[j] <- this[i,j]
        v

    member this.ColumnVector(j:int) =
        let v = new Vector(n)
        for i = 1 to m do
            v[i] <- this[i,j]
        v

    override this.ToString() =
        let _h = [for i in 1..n -> $"n{i}"]
        let _v = [
            for j = 1 to n do
                let x = this.ColumnVector(j)
                let s = [for y in x.Entries -> string y]
                let l = s |> List.maxBy (fun t -> t.Length)
                (x :> IDisposable).Dispose()
                yield l.Length
        ]
        let sb = System.Text.StringBuilder(4096)
        for j = 1 to n do
            ignore (sb.Append(" |-"))
            ignore (sb.Append("".PadRight(_v[j - 1], '-')))
        ignore (sb.AppendLine("-|"))
        for i = 1 to m do
            for j = 1 to n do
                ignore (sb.Append(" | "))
                let s = string this[i,j]
                ignore (sb.Append(s.PadRight(_v[j - 1])))
            ignore (sb.AppendLine(" |"))
        for j = 1 to n do
            ignore (sb.Append(" |-"))
            ignore (sb.Append("".PadRight(_v[j - 1], '-')))
        ignore (sb.AppendLine("-|"))
        string sb

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

    static member (+=) (a:Matrix, b:Matrix) =
        for i = 1 to a.M do
            for j = 1 to a.N do
                a[i,j] <- a[i,j] + b[i,j]
        
    static member (-=) (a:Matrix, b:Matrix) =
        for i = 1 to a.M do
            for j = 1 to a.N do
                a[i,j] <- a[i,j] - b[i,j]        
        
    static member ( *= ) (m:Matrix, s:float) =
        for i in 1..m.M do
            for j in 1..m.N do
                m[i,j] <- m[i,j] * s
        
    static member (/=) (m:Matrix, s:float) =
        for i in 1..m.M do
            for j in 1..m.N do
                m[i,j] <- m[i,j] / s
        

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
        v.Clear()
        v

    let random (n:int) (d:float) =
        let r = System.Random.Shared
        let v = new Vector(n)
        for i in 1..n do v[i] <- d + r.NextDouble()
        v

    let sum (v:Vector) =
        let mutable s = 0.
        for i = 1 to v.Length do
            s <- s + v[i]
        s

    let sumAbs (v:Vector) =
        let mutable s = 0.
        for i = 1 to v.Length do
            s <- s + abs v[i]
        s

    let L1 (v:Vector) =
        let mutable sum = 0.
        for i = 1 to v.Length do
            sum <- sum + abs v[i]
        sum

    let L2 (v:Vector) =
        let mutable sum = 0.
        for i = 1 to v.Length do
            sum <- sum + (v[i] * v[i])
        sqrt sum

    let Linf (v:Vector) =
        let mutable r = v[1]
        for i = 1 to v.Length do
            r <- max r (abs v[i])
        r


module Matrix =
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
        a.Clear()
        a
        
    let random (n:int) (m:int) (d:float) =
        let r = System.Random.Shared
        let a = new Matrix(m,n)
        for i = 1 to m do
            for j = 1 to n do a[i,j] <- d + r.NextDouble()
        a

    let columnSum (m:Matrix) (j:int) =
        use v = m.ColumnVector(j)
        Vector.sum v

    let columnSumAbs (m:Matrix) (j:int) =
        use v = m.ColumnVector(j)
        Vector.sumAbs v

    let rowSum (m:Matrix) (i:int) =
        use v = m.RowVector(i)
        Vector.sum v

    let rowSumAbs (m:Matrix) (i:int) =
        use v = m.RowVector(i)
        Vector.sumAbs v

    let private power_iteration (m:Matrix) (N:int) =
        // b_k = np.random.rand(A.shape[1])

        //     for _ in range(num_iterations):
        //         # calculate the matrix-by-vector product Ab
        //         b_k1 = np.dot(A, b_k)

        //         # calculate the norm
        //         b_k1_norm = np.linalg.norm(b_k1)

        //         # re normalize the vector
        //         b_k = b_k1 / b_k1_norm

        //     return b_k
        let b_k = m.RowVector(System.Random.Shared.Next(m.M + 1))
        for i = 1 to N do
            let b_k1 = m * b_k
            let b_k1_norm = Vector.L2 b_k1
            b_k1 /= b_k1_norm
            b_k == b_k1
            (b_k1 :> IDisposable).Dispose()
        b_k

    let eigenvalues (m:Matrix) = power_iteration m 130


    /// Norm inf
    let Linf (m:Matrix) =
        let mutable r = m[1,1]
        for i = 1 to m.M do
            r <- max r (rowSumAbs m i)
        r

    /// Norm L1
    let L1 (m:Matrix) =
        let mutable r = m[1,1]
        for j = 1 to m.N do
            r <- max r (columnSumAbs m j)
        r

    // Norm Frobenius
    let frobenious (m:Matrix) =
        let mutable sum = 0.
        for i = 1 to m.M do
            for j = 1 to m.N do
                sum <- sum + m[i,j] * m[i,j]
        sqrt sum

    /// Norm L2 
    let L2 (m:Matrix) =
        use _eigenvalues = power_iteration m 130
        let mutable l = abs _eigenvalues[1]
        for ln in _eigenvalues.Entries do
            l <- max l (abs ln)
        sqrt l

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
        assert (determinant m <> 0.)
        let a = copy m
        for i = 1 to a.N do
            let d = 1. / a[i,i]
            let mutable tt = -d
            for j = 1 to a.N do
                a[i,j] <- a[i,j] * tt
            for k = 1 to i - 1 do
                tt <- a[k,i]
                for j = 1 to i - 1 do
                    a[k,j] <- a[k,j] + tt * a[i,j]
                for j = i + 1 to a.N do
                    a[k,j] <- a[k,j] + tt * a[i,j]
                a[k,i] <- tt * d
            for k = i + 1 to a.N do
                tt <- a[k,i]
                for j = 1 to i - 1 do
                    a[k,j] <- a[k,j] + tt * a[i,j]
                for j = i + 1 to a.N do
                    a[k,j] <- a[k,j] + tt * a[i,j]
                a[k,i] <- tt * d
            a[i,i] <- d
        a

module Decomposition =
    let LU (m:Matrix) =
        assert (m.M = m.N)
        let n = m.N
        let l = new Matrix(n,n)
        let u = new Matrix(n,n)
        for i = 1 to n do
            // upper triangular
            for k = i to n do
                let mutable sum = 0.
                for j = 1 to i - 1 do
                    sum <- sum + (l[i,j] * u[j,k])
                u[i,k] <- m[i,k] - sum
            // lower triangular
            for k = i to n do
                if i = k then
                    l[i,i] <- 1
                else
                    let mutable sum = 0.
                    for j = 1 to i - 1 do
                        sum <- sum + (l[k,j] * u[j,i])
                    l[k,i] <- (m[k,i] - sum) / u[i,i]
        (l,u)                    
    
    let Cholesky (m:Matrix) =
        assert (m.M = m.N)
        let n = m.N
        let lower = new Matrix(n,n)
        for i = 1 to n do
            for j = 1 to i do
                let mutable sum = 0.
                if j = i then
                    for k = 1 to j - 1 do
                        sum <- sum + Math.Pow(lower[j,k], 2)
                    lower[j,j] <- sqrt (m[j,j] - sum)
                else
                    for k = 1 to j - 1 do
                        sum <- sum + (lower[i,k] * lower[j,k])
                    lower[i,j] <- (m[i,j] - sum) / lower[j,j]
        let upper = Matrix.transpose lower
        (lower,upper)


module Solvers =              
    let Jacobi (A:Matrix) (B:Vector) N =
        use a = Matrix.copy A
        use b = Vector.copy B
        let x = new Vector(b.Length)
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
        let x = new Vector(b.Length)
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
        let x = new Vector(b.Length)
        for j = 1 to a.N do
            let mutable d = b[j]
            for i = 1 to a.M do
                if j <> i then
                    d <- d - a[j,i] * x[i]
            x[j] <- d / a[j,j]
        x



        
