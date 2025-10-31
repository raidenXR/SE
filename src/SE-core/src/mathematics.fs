#nowarn "9"
#nowarn "51"
namespace SE.Mathematics
open System
open System.Buffers
open FSharp.NativeInterop
  

type [<Struct>] varray2<'T when 'T: unmanaged> =
    val mutable a: 'T
    val mutable b: 'T

    member x.Length with get() = 2
    member x.Item 
        with inline get(i:int) = NativePtr.get (&&x.a) i
        and inline set(i:int) value = NativePtr.set (&&x.a) i value
        
type [<Struct>] varray3<'T when 'T: unmanaged> =
    val mutable a: 'T
    val mutable b: 'T
    val mutable c: 'T

    member x.Length with get() = 3
    member x.Item 
        with inline get(i:int) = NativePtr.get (&&x.a) i
        and inline set(i:int) value = NativePtr.set (&&x.a) i value
            
type [<Struct>] varray4<'T when 'T: unmanaged> =
    val mutable a: 'T
    val mutable b: 'T
    val mutable c: 'T
    val mutable d: 'T

    member x.Length with get() = 4
    member x.Item 
        with inline get(i:int) = NativePtr.get (&&x.a) i
        and inline set(i:int) value = NativePtr.set (&&x.a) i value

// type [<Struct; StructLayout(LayoutKind.Explicit, Size = 6 * sizeof<'T>)>] varray6<'T when 'T: unmanaged> =
type [<Struct>] varray6<'T when 'T: unmanaged> =
    val mutable a: 'T
    val mutable b: 'T
    val mutable c: 'T
    val mutable d: 'T
    val mutable e: 'T
    val mutable f: 'T

    member x.Length with get() = 6
    member x.Item 
        with inline get(i:int) = NativePtr.get (&&x.a) i
        and inline set(i:int) value = NativePtr.set (&&x.a) i value


module PtrOperations =
    // add more ptr related operations
    let inline stackalloc<'a when 'a: unmanaged> (length: int): Span<'a> =
      let p = NativePtr.stackalloc<'a> length |> NativePtr.toVoidPtr
      Span<'a>(p, length)


module Integration =
    let gauss npts job a b (x:Span<float>) (w:Span<float>) =
        let mutable m = 0
        let mutable t, t1, pp, p1, p2, p3, xi = 0.,0.,0.,0.,0.,0.,0.
        let eps = 3e-14
        m <- (npts + 1) / 2
        for i in 1..m do
            t <- cos(Math.PI * (float i - 0.25) / (float npts + 0.5))
            t1 <- 1
            while abs(t - t1) >= eps do
                p1 <- 1.
                p2 <- 0.
                for j in 1..npts do
                    p3 <- p2
                    p2 <- p1
                    p1 <- (2. * float j - 1.) * t * p2 - (float j - 1.) * p3 / (float j)
                pp <- float npts * (t * p1 - p2) / (t * t - 1.)
                t1 <- t
                t <- t1 - p1 / pp
            x[i - 1] <- -t
            x[npts - i] <- t
            w[i - 1] <- 2. / ((1. - t * t) * pp * pp)
            w[npts - i] <- w[i - 1]
        if job = 0 then
            for i in 0..npts - 1 do
                x[i] <- x[i] * (b - a) / 2. + (b + a) / 2.
                w[i] <- w[i] * (b - a) / 2.
        if job = 1 then
            for i in 0..npts - 1 do
                xi <- x[i]
                x[i] <- a * b * (1. + xi) / (b + a - (b - a) * xi)
                w[i] <- w[i] * 2. * a * b * b / ((b + a - (b - a) * xi) * (b + a - (b - a) * xi))
        if job = 2 then
            for i in 0..npts - 1 do
                xi <- x[i]
                x[i] <- (b * xi + b + a + a) / (1. - xi)
                w[i] <- w[i] * 2. * (a + b) / ((1. - xi) * (1. - xi))


    let gaussint no _min _max f =
        let mutable quadra = 0.
        let pool = ArrayPool<float>.Shared
        let w = pool.Rent(2001)
        let x = pool.Rent(2001)
        gauss no 0 _min _max (x.AsSpan(0, 2001)) (w.AsSpan(0, 2001))
        for n in 0..no - 1 do
            quadra <- quadra + f(x[n]) * w[n]
        pool.Return(w)
        pool.Return(x)
        quadra


module Differentiation =
    let forward x (h:float) f = (f(x + h) - f(x)) / h

    let central x h f = (f(x + h / 2.) - f(x - h / 2.)) / h        

    let extrapolated x h f = (f(x + h / 4.) - f(x - h / 4.)) / (h / 2.)

    let def2nd x h f = (f(x + h) + f(x - h) - 2. * f(x)) / (h * h)


module TrialAndSearching =
    let newtonRaphson = ()


module ODE =
    open PtrOperations
    
    let RK4 f =
        let mutable h, t = 0., 0.
        let ydumb = Array.zeroCreate<float> 2
        let y = Array.zeroCreate<float> 2
        let freturn = Array.zeroCreate<float> 2
        let k1 = Array.zeroCreate<float> 2
        let k2 = Array.zeroCreate<float> 2
        let k3 = Array.zeroCreate<float> 2
        let k4 = Array.zeroCreate<float> 2
        let mutable a, b = 0. , 10.
        let mutable i, n = 0, 100
        let output = ResizeArray()

        y[0] <- 3.
        y[1] <- -5.
        h <- (b - a) / float n
        t <- a

        while t < b do
            if (t + h) > b then h <- b - t

            f t y freturn
            k1[0] <- h * freturn[0]
            k1[1] <- h * freturn[1]
            for i in 0..1 do ydumb[i] <- y[i] + k1[i] / 2.
            
            f (t + h / 2.) ydumb freturn
            k2[0] <- h * freturn[0]
            k2[1] <- h * freturn[1]
            for i in 0..1 do ydumb[i] <- y[i] + k2[i] / 2.
           
            f (t + h / 2.) ydumb freturn
            k3[0] <- h * freturn[0]
            k3[1] <- h * freturn[1]
            for i in 0..1 do ydumb[i] <- y[i] + k3[i] / 2.

            f (t + h) ydumb freturn
            k4[0] <- h * freturn[0]
            k4[1] <- h * freturn[1]
            for i in 0..1 do y[i] <- y[i] + (k1[i] + 2. * (k2[i] + k3[i]) + k4[i]) / 6.
            t <- t + h

            // yield y[0]
            // yield y[1]
            output.Add(struct(y[0],y[1]))

        output

        

