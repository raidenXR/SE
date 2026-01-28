#nowarn "9"
namespace SE.Renderer
open System
open System.Numerics
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open FSharp.NativeInterop


[<AutoOpen>]
module UnsafeOps =
    let inline ( ** ) (ptr:nativeptr<_>) = NativePtr.read ptr

    let inline ( ~~ ) (ptr:nativeptr<_>) = NativePtr.toVoidPtr ptr

    let inline ( ++ ) (ptr:nativeptr<_>) offset = NativePtr.add ptr offset

    let inline stackalloc<'T when 'T:unmanaged> (n:int) = NativePtr.stackalloc<'T> n

    type nativeptr<'T when 'T: unmanaged> with
        member this.Item with inline get(idx) = NativePtr.get this idx and inline set(idx) value = NativePtr.set this idx value  


        
type [<Struct>] NativeArray<'T when 'T:unmanaged> =
    val mutable private ptr: voidptr
    val mutable private len: int
    val mutable private is_disposed: bool

    new(n:int) =
        {
            ptr = NativeMemory.AllocZeroed(unativeint(n * sizeof<'T>))
            len = n
            is_disposed = false
        }

    new(source:array<'T>) =
        let n = source.Length
        let size = n * sizeof<'T>
        use pta = fixed source
        let ptr = NativeMemory.AllocZeroed(unativeint size)
        System.Buffer.MemoryCopy(NativePtr.toVoidPtr pta, ptr, size, size)
        {
            ptr = ptr
            len = n
            is_disposed = false
        }

    /// Unfortunately slow copy from ResizeArray
    new(source:ResizeArray<'T>) =
        let n = source.Count
        let size = n * sizeof<'T>
        let ptr = NativeMemory.AllocZeroed(unativeint size)
        let pta = NativePtr.ofVoidPtr<'T> ptr
        for i in 0..n-1 do
            NativePtr.set pta i source[i]
        {
            ptr = ptr
            len = n
            is_disposed = false
        }

    interface IDisposable with
        member this.Dispose() =
            if not this.is_disposed then
                NativeMemory.Free(this.ptr)
            this.is_disposed <- true

    member this.Length with get() = this.len

    member this.BufferSize with get() = this.len * sizeof<'T>

    member this.Ptr with get() = this.ptr

    member this.ToInt() = this.ptr |> NativePtr.ofVoidPtr<'T> |> NativePtr.toNativeInt 

    member this.Dispose() =
        if not this.is_disposed then
            NativeMemory.Free(this.ptr)
        this.is_disposed <- true

    member this.AsSpan() = Span<'T>(this.ptr, this.len)

    member this.Item
        with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get (i:int) =
            let p = NativePtr.ofVoidPtr<'T> this.ptr
            NativePtr.get p i
        and [<MethodImpl(MethodImplOptions.AggressiveInlining)>] set (i:int) value = 
            let p = NativePtr.ofVoidPtr<'T> this.ptr
            NativePtr.set p i value


type [<Struct>] NativeArray2D<'T when 'T:unmanaged> =
    val mutable private ptr: voidptr
    val mutable private I: int
    val mutable private J: int
    val mutable private is_disposed: bool

    new(i:int, j:int) =
        {
            ptr = NativeMemory.AllocZeroed(unativeint((i*j) * sizeof<'T>))
            I = i
            J = j
            is_disposed = false
        }

    interface IDisposable with
        member this.Dispose() =
            if not this.is_disposed then
                NativeMemory.Free(this.ptr)
            this.is_disposed <- true

    member this.Length with get() = this.I * this.J
    
    member this.Ptr with get() = this.ptr
    
    member this.ToInt() = this.ptr |> NativePtr.ofVoidPtr<'T> |> NativePtr.toNativeInt 

    member this.Dispose() =
        if not this.is_disposed then
            NativeMemory.Free(this.ptr)
        this.is_disposed <- true

    member this.AsSpan() = Span<'T>(this.ptr, this.Length)

    member this.Item
        with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get (i:int, j:int) =
            let idx = i * this.J + j
            let p = NativePtr.ofVoidPtr<'T> this.ptr
            NativePtr.get p idx
        and [<MethodImpl(MethodImplOptions.AggressiveInlining)>] set (i:int, j:int) value = 
            let idx = i * this.J + j
            let p = NativePtr.ofVoidPtr<'T> this.ptr
            NativePtr.set p idx value


type [<Struct>] NativeArray3D<'T when 'T:unmanaged> =
    val mutable private ptr: voidptr
    val mutable private I: int
    val mutable private J: int
    val mutable private K: int
    val mutable private is_disposed: bool

    new(i:int, j:int, k:int) =
        {
            ptr = NativeMemory.AllocZeroed(unativeint((i*j*k) * sizeof<'T>))
            I = i
            J = j
            K = k
            is_disposed = false
        }

    interface IDisposable with
        member this.Dispose() =
            if not this.is_disposed then
                NativeMemory.Free(this.ptr)
            this.is_disposed <- true

    member this.Length with get() = this.I * this.J * this.K
    
    member this.Ptr with get() = this.ptr
    
    member this.ToInt() = this.ptr |> NativePtr.ofVoidPtr<'T> |> NativePtr.toNativeInt 

    member this.Dispose() =
        if not this.is_disposed then
            NativeMemory.Free(this.ptr)
        this.is_disposed <- true

    member this.AsSpan() = Span<'T>(this.ptr, this.Length)

    member this.Item
        with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get (i:int, j:int, k:int) =
                let idx = i * this.J * this.K + j * this.K + k
                let p = NativePtr.ofVoidPtr<'T> this.ptr
                NativePtr.get p idx
            and [<MethodImpl(MethodImplOptions.AggressiveInlining)>] set (i:int, j:int, k:int) value = 
                let idx = i * this.J * this.K + j * this.K + k
                let p = NativePtr.ofVoidPtr<'T> this.ptr
                NativePtr.set p idx value



        
        
