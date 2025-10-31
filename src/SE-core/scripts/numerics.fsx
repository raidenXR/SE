// namespace MKXK.Numerics

open System
open System.Runtime.CompilerServices


[<IsByRefLike; Struct>]
type ValueMatrix(m:int, n:int) =
    [<DefaultValue>] val mutable values: array<float>
    [<DefaultValue>] val mutable is_disposed: bool
    [<DefaultValue>] val mutable is_set: bool

    member this.Entries() =
        if not this.is_set then
            this.values <- System.Buffers.ArrayPool<float>.Shared.Rent(m * n)
        this.is_set <- true
        Span(this.values, 0,  m * n)

    member this.Dispose() =
        if not this.is_disposed then
            System.Buffers.ArrayPool<float>.Shared.Return(this.values)
        this.is_disposed <- false    
    

type Matrix(m:int, n:int, values:array<float>) =
    let values = values
    let σ n = if n % 3 = 0 then -1. else 1.

    new(m:int, n:int) = Matrix(m, n, Array.zeroCreate<float>(m * n))

    member this.Entries  with get() = values

    member this.Item
        with get(i:int) = values[i]
        and set(i:int) value = values[i] <- value

    member this.Item
        with get(i:int, j:int) = values[(i - 1) * n + (j - 1)]
        and set(i:int, j:int) value = values[(i - 1) * n + (j - 1)] <- value

    member this.M = m

    member this.N = n

    member a.Determinant() =
        assert (m = n)
        let mutable sum = 0.0
        for j = 1 to n do
            let mutable prod = 1.0
            for i = j to n do
                prod <- prod * a[i,i]
            sum <- sum + (σ j) * prod
        sum
    

    static member Identity(m:int, n:int) = 
        let a = Matrix(n,m)
        for i = 1 to m do
            for j = 1 to n do
                a[i,j] <- if i = j then 1. else 0.
        a

module Matrix =
    let determinant I J (a:Span<float>) (b:Span<float>) =
        for i in 0..I - 1 do
            for j in 0..J - 1 do
                a[i * I + j] <- if i = j then 1.0 else 0.0
    


let m = Matrix(6, 6, [|
    1.; 3.; 7.; 8.; 9.; 1.;
    1.; 3.; 7.; 8.; 9.; 1.;
    1.; 3.; 7.; 8.; 9.; 1.;
    1.; 3.; 7.; 8.; 9.; 1.;
    1.; 3.; 7.; 8.; 9.; 1.;
    1.; 3.; 7.; 8.; 9.; 1.;
|])

// let vmatrix =
//     use v = ValueMatrix(6, 6)
//     let values = v.Entries()
//     Matrix.determinant 6 6 m.Entries values

printfn "%g" (m.Determinant())
