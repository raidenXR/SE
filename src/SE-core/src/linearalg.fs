#nowarn "9"
#nowarn "51"
namespace SE.Numerics
open System
open System.Buffers
open System.Runtime.CompilerServices


type FmtType = | Txt | Csv


type Vector(n:int, values:array<float>, is_from_arraypool:bool) =
    let buffer =
        match values with
        | null ->
            let buf = ArrayPool<float>.Shared.Rent(n)
            Array.Clear(buf)
            buf
        | _ -> values
    let mutable entries = ArraySegment(buffer, 0, n)
    let mutable is_disposed = false

    new(n:int) = new Vector(n, null, true) 
    new(values:array<float>) = new Vector(values.Length, values, false)

    interface IDisposable with
        member this.Dispose() =
            if is_from_arraypool then
                if not is_disposed then ArrayPool<float>.Shared.Return(buffer)
                // printfn "vector got disposed"
                is_disposed <- true                    

    member this.N = n
    member this.Entries = entries
    member this.IsPooled = is_from_arraypool

    member this.Dispose() = (this :> IDisposable).Dispose()

    member this.Clear() = Array.Clear(buffer)

    member this.Item
        with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get(i:int) = buffer[i - 1]
        and  [<MethodImpl(MethodImplOptions.AggressiveInlining)>] set(i:int) value = buffer[i - 1] <- value

    member this.GetSlice(startIdx, endIdx) =
        let s = defaultArg startIdx 1
        let e = defaultArg endIdx n
        ReadOnlySpan(buffer, s - 1, e - (s - 1))        

    override this.ToString() =
        let _v = entries.ToArray()
        sprintf "%A" _v

    member this.Span(i:int, len:int) = ReadOnlySpan(buffer, i - 1, len)

    static member (+) (a:Vector, b:Vector) =
        assert (a.N = b.N)
        let c = new Vector(a.N)
        for i = 1 to a.N do
            c[i] <- a[i] + b[i]
        c

    static member (-) (a:Vector, b:Vector) =
        assert (a.N = b.N)
        let c = new Vector(a.N)
        for i = 1 to a.N do
            c[i] <- a[i] - b[i]
        c
    
    static member (*) (s:float, v:Vector) =
        let c = new Vector(v.N)
        for i = 1 to v.N do
            c[i] <- s * v[i]
        c
    
    static member (/) (v:Vector, s:float) =
        let c = new Vector(v.N)
        for i = 1 to v.N do
            c[i] <- v[i] / s
        c
    
    static member (*) (v:Vector, s:float) =
        let c = new Vector(v.N)
        for i = 1 to v.N do
            c[i] <- s * v[i]
        c

    static member (*) (a:Vector, b:Vector) =
        let mutable sum = 0.
        for i = 1 to a.N do
            sum <- sum + a[i] * b[i]
        sum

    static member (+=) (a:Vector, b:Vector) =
        assert (a.N = b.N)
        for i = 1 to a.N do
            a[i] <- a[i] + b[i]

    static member (-=) (a:Vector, b:Vector) =
        assert (a.N = b.N)
        for i = 1 to a.N do
            a[i] <- a[i] - b[i]

    static member ( *= ) (a:Vector, s:float) =
        for i = 1 to a.N do
            a[i] <- a[i] * s

    static member (/=) (a:Vector, s:float) =
        for i = 1 to a.N do
            a[i] <- a[i] / s

    static member (==) (a:Vector, b:Vector) =
        for i = 1 to a.N do
            a[i] <- b[i]

    
type Matrix(m:int, n:int, values:array<float>, is_from_arraypool:bool) =
    let buffer =
        match values with
        | null ->
            let buf = ArrayPool<float>.Shared.Rent(m * n)
            Array.Clear(buf)
            buf
        | _ -> values
    let mutable entries = ArraySegment(buffer, 0, m * n)
    let mutable is_disposed = false

    new(m:int, n:int) = new Matrix(m, n, null, true) 
    new(m:int, n:int, values:array<float>) = new Matrix(m, n, values, false)

    interface IDisposable with
        member this.Dispose() =
            if is_from_arraypool then
                if not is_disposed then ArrayPool<float>.Shared.Return(buffer)
                // printfn "matrix got disposed"
                is_disposed <- true                    

    member this.M = m
    member this.N = n
    member this.Entries = entries
    member this.IsPooled = is_from_arraypool

    member this.Dispose() = (this :> IDisposable).Dispose()

    member this.Clear() = Array.Clear(buffer)

    member this.Item
        with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get(i:int, j:int) = buffer[(i - 1) * n + (j - 1)]
        and  [<MethodImpl(MethodImplOptions.AggressiveInlining)>] set(i:int, j:int) value = buffer[(i - 1) * n + (j - 1)] <- value

    member this.GetSlice(i, startIdx, endIdx) =
        let s = defaultArg startIdx 1
        let e = defaultArg endIdx n 
        let offset = (i - 1) * n + (s - 1)
        ReadOnlySpan(buffer, offset, e - (s - 1))


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
        ignore (sb.Append(" "))
        for j = 1 to n do
            ignore (sb.Append("|-"))
            ignore (sb.Append("".PadRight(_v[j - 1] + 1, '-')))
        ignore (sb.AppendLine("|"))
        for i = 1 to m do
            for j = 1 to n do
                ignore (sb.Append(" | "))
                let s = string this[i,j]
                ignore (sb.Append(s.PadRight(_v[j - 1])))
            ignore (sb.AppendLine(" |"))
        ignore (sb.Append(" "))
        for j = 1 to n do
            ignore (sb.Append("|-"))
            ignore (sb.Append("".PadRight(_v[j - 1] + 1, '-')))
        ignore (sb.AppendLine("|"))
        string sb

    member this.SaveAs (fmt:FmtType, path:string) =
        use fs = System.IO.File.CreateText(path)
        match fmt with
        | Txt ->
            for i = 1 to m do
                for j = 1 to n do
                    fs.WriteLine($"{i}  {j}  {this[i,j]}")
        | Csv ->
            for i = 1 to m do
                for j = 1 to n do
                    fs.WriteLine($"{i},{j},{this[i,j]}")
        fs.Flush()
        fs.Close()

    member this.SaveAsGrid (fmt:FmtType, path:string, xrange:float * float, yrange: float * float) =
        use fs = System.IO.File.CreateText(path)
        let x_min, x_max = xrange
        let y_min, y_max = yrange
        let dx = (x_max - x_min) / (float n)
        let dy = (y_max - y_min) / (float m)
        match fmt with
        | Txt ->
            for i = 1 to m do
                for j = 1 to n do
                    fs.WriteLine($"{x_min + dx * float j}  {y_min + dy * float i}  {this[i,j]}")
        | Csv ->
            for i = 1 to m do
                for j = 1 to n do
                    fs.WriteLine($"{x_min + dx * float j},{y_min + dy * float i},{this[i,j]}")
        fs.Flush()
        fs.Close()

    member this.SetSymmetric (i:int, j:int, v:float) =
        assert (n = m)
        this[i,j] <- v
        this[j,i] <- v

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
        assert (a.N = x.N)
        let y = new Vector(a.M)
        for i = 1 to a.M do
            let mutable sum = 0.
            for j = 1 to a.N do
                sum <- sum + a[i,j] * x[j]
            y[i] <- sum
        y

    static member (*) (x:Vector, a:Matrix) =
        assert (a.M = x.N)
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

    static member (%) (a:Matrix, b:Matrix) =
        assert (a.N = b.N)
        let y = new Matrix(a.M, b.M)
        for i = 1 to a.M do
            for j = 1 to b.M do
                let mutable x = 0.
                for k = 1 to a.N do
                    x <- x + a[k,i] * b[k,j]
                y[i,j] <- x
        y
                
    static member (%) (m:Matrix, v:Vector) =
        assert (m.N = v.N)
        let y = new Vector(m.M)
        for i = 1 to m.M do
            let mutable x = 0.
            for k = 1 to m.N do
                x <- x + m[k,i] * v[k]
            y[i] <- x
        y
        

module Vector =
    // let dot (x:Vector) (y:Vector) =
    //     assert (x.Length = y.Length)
    //     let mutable s = 0.
    //     for i = 1 to x.Length do
    //         s <- s + x[i] * y[i]
    //     s

    let dot (a:ReadOnlySpan<float>) (b:ReadOnlySpan<float>) =
        assert (a.Length = b.Length)
        let mutable s = 0.
        for i = 0 to a.Length - 1 do
            s <- s + a[i] * b[i]
        s

    let cross (a:Vector) (b:Vector) =
        assert (a.N = b.N && a.N = 3)
        new Vector([|
            a[2] * b[3] - a[3] * b[2]
            a[3] * b[1] - a[1] * b[3]
            a[1] * b[2] - a[2] * b[1]
        |])

    let diadic (x:Vector) (y:Vector) =
        let a = new Matrix(x.N, y.N)
        for i = 1 to a.M do
            for j = 1 to a.N do
                a[i,j] <- x[i] * y[j]
        a

    let copy (v:Vector) =
        let a = new Vector(v.N)
        for i = 1 to a.N do
            a[i] <- v[i]
        a

    let empty = new Vector(0, [||], false)

    let undefined (n:int) =
        let buffer = ArrayPool<float>.Shared.Rent(n)
        new Vector(n, buffer, true)

    let random (n:int) (d:float) =
        let r = System.Random.Shared
        let v = new Vector(n)
        for i in 1..n do v[i] <- d + r.NextDouble()
        v

    let init (n:int) f =
        let a = undefined n
        for i = 1 to n do
            a[i] <- f (float i) 
        a

    let sum (v:Vector) =
        let mutable s = 0.
        for i = 1 to v.N do
            s <- s + v[i]
        s

    let sumAbs (v:Vector) =
        let mutable s = 0.
        for i = 1 to v.N do
            s <- s + abs v[i]
        s

    let L1 (v:Vector) =
        let mutable sum = 0.
        for i = 1 to v.N do
            sum <- sum + abs v[i]
        sum

    let L2 (v:Vector) =
        let mutable sum = 0.
        for i = 1 to v.N do
            sum <- sum + (v[i] * v[i])
        sqrt sum

    let Linf (v:Vector) =
        let mutable r = v[1]
        for i = 1 to v.N do
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
        // for n in 1..(m.N + 1)..(m.M * m.N) do sum <- sum + m.Entries[n - 1]
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

    let diagonal (m:Matrix) =
        let a = new Matrix(m.M, m.N)
        for i = 1 to m.M do
            for j = 1 to m.N do
                a[i,j] <- if j = i then m[i,j] else 0.
        a

    let empty = new Matrix(0, 0, [||], false)        

    let undefined (m:int) (n:int) =
        let buffer = ArrayPool<float>.Shared.Rent(m * n)
        new Matrix(m, n, buffer, true)
        
    let random (n:int) (m:int) (d:float) =
        let r = System.Random.Shared
        let a = new Matrix(m,n)
        for i = 1 to m do
            for j = 1 to n do a[i,j] <- d + r.NextDouble()
        a

    let init (m:int) (n:int) f =
        let a = undefined m n
        for i = 1 to m do
            for j = 1 to n do
                a[i,j] <- f (float i) (float j)
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
        let r = System.Random.Shared.Next(m.M) + 1  // avoid 0 index or > m.M !!
        let b_k = m.RowVector(r)
        for i = 1 to N do
            use b_k1 = m * b_k
            let b_k1_norm = Vector.L2 b_k1
            b_k1 /= b_k1_norm
            b_k == b_k1
            // (b_k1 :> IDisposable).Dispose()
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

    let conditionNumber (a:Matrix) =
        use a_inv = inverse a
        (L2 a) * (L2 a_inv)
    

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
    let residuals (x:Vector) (b:Vector) =
        use r = x - b
        Vector.L2 r

    let Jacobi (a:Matrix) (b:Vector) =
        assert (a.M = a.N && a.M = b.N)
        let N = 1000  // iterations
        let x = new Vector(b.N)
        use x_k = Vector.copy x

        let mutable k = 1
        let mutable converged = false 
        while k < N && not converged do
            x_k.Clear()
            for i = 1 to a.M do
                let s1 = Vector.dot a[i, 1..i] x[1..i]
                let s2 = Vector.dot a[i, i + 1..] x[i + 1..]
                x_k[i] <- (b[i] - s1 - s2) / a[i,i]
                // if x_k[i] = x_k[i - 1] then
                //     converged <- true
            converged <- (residuals x x_k) < 1e-6
            k <- k + 1
            x == x_k
            // if not converged then
        x
    
    let GaussElimination (A:Matrix) (B:Vector) =
        assert (A.M = A.N && A.M = B.N)
        use a = Matrix.copy A
        use b = Vector.copy B
        let x = new Vector(b.N)

        let forward_substitution () =
            let eps = 1e-6
            let mutable ier = 0
            for k = 1 to a.N - 1 do
                if (abs a[k,k]) > eps then
                    for i = k + 1 to a.N do
                        if a[i,k] <> 0. then
                            let t = a[i,k] / a[k,k]
                            for j = k + 1 to a.N do
                                a[i,j] <- a[i,j] - t * a[k,j]
                            b[i] <- b[i] - t * b[k]
                else
                    ier <- -1

        let backward_substitution () =
            for i = a.N downto 1 do
                let mutable sum = 0.
                for j = i + 1 to a.N do
                    sum <- sum + a[i,j] * x[j]
                x[i] <- (b[i] - sum) / a[i,i]

        forward_substitution ()
        backward_substitution ()                    
        x
        
    let GaussSeidel (a:Matrix) (b:Vector) =
        assert (a.M = a.N && a.M = b.N)
        let N = 100  // iterations
        let x = new Vector(b.N)
        use x_k = Vector.copy x

        let mutable k = 1
        let mutable converged = false
        while k < N && not converged do
            x_k.Clear()
            for i = 1 to a.M do
                let s1 = Vector.dot (a[i, 1..i]) x_k[1..i]
                let s2 = Vector.dot (a[i, i + 1..]) x[i + 1..]
                x_k[i] <- (b[i] - s1 - s2) / a[i,i]
            converged <- (residuals x x_k) < 1e-6
            k <- k + 1
            x == x_k
            // if not converged then
        x
            

    let LUSolve (A:Matrix) (B:Vector) =
        assert (A.M = A.N && A.M = B.N)
        let (L,U) = Decomposition.LU A
        use L' = Matrix.inverse L
        use U' = Matrix.inverse U
        use _t = U' * L'        
        let x = _t * B        
        L.Dispose()
        U.Dispose()
        x


        
