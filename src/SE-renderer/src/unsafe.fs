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


// ***********************************************************
// USE REGULAR ARRAYPOOL INSTEAD !!!!!
// ***********************************************************
// [<AllowNullLiteral>]
// type NativeArrayPool() as x =
//     let default_max_array_length = 1024 * 1024
//     let default_max_number_of_buckets = 50
//     let hash_map = new Collections.Generic.Dictionary<nativeint,nativeint>()

//     let min_array_len = 0x10
//     let max_array_len = 0x40000000    

//     let mutable is_initialized = false
//     let mutable _buckets: array<Bucket> = null

//     let null_ptr = ~~(NativePtr.nullPtr<int>)
//     let null_buffer = struct(nativeint 0, 0)

//     let alloc n =
//         let ptr = NativeMemory.Alloc(unativeint n) |> NativePtr.ofVoidPtr<int> |> NativePtr.toNativeInt
//         struct(ptr,n)
   
//     let selectBucketIndex bufferSize =
//         let x = (bufferSize - 1 ||| 15) - 3
//         int32 (log (float x))

//     let getMaxSizeForBucket binIndex =
//         let max_size = 16 <<< binIndex
//         assert (max_size >= 0)
//         max_size

//     let fst' (t:struct(nativeint * int)) =
//         let struct(a,_) = t
//         NativePtr.toVoidPtr (NativePtr.ofNativeInt<int> a)

//     let snd' (t:struct(nativeint * int)) =
//         let struct(_,b) = t
//         b

//     do
//         let pool_id = x.GetHashCode()
//         let max_buckets = selectBucketIndex max_array_len
//         _buckets <- Array.zeroCreate<Bucket> (max_buckets + 1)
//         for i in 0.._buckets.Length-1 do
//             _buckets[i] <- Bucket(getMaxSizeForBucket(i), default_max_array_length, pool_id)
//         is_initialized <- true
        

//     [<DefaultValue>] static val mutable private shared: NativeArrayPool

//     static member Shared with get() =
//         match NativeArrayPool.shared with
//         | null ->
//             NativeArrayPool.shared <- new NativeArrayPool()
//             NativeArrayPool.shared
//         | _ -> NativeArrayPool.shared
        

//     member this.Rent<'T>(_minimumLength:int) =
//         let minimumLength = _minimumLength * sizeof<'T>
//         if minimumLength = 0 then
//             Span<'T>(null_ptr, 0)
//         else
//             let mutable buffer = null_buffer
//             let mutable r = false
//             let index = selectBucketIndex(minimumLength)
//             if index < _buckets.Length then
//                 let max_buckets_to_try = 2
//                 let mutable i = index

//                 while i < _buckets.Length && i <> index + max_buckets_to_try && not r do
//                     let struct(ptr,len) = _buckets[i].Rent()
//                     r <- r || len > 0
//                     buffer <- if r then struct(ptr,len) else buffer
//                     i <- i + 1
//             if r then
//                 Span(fst' buffer, snd' buffer)
//             else
//                 let p = alloc _buckets[index].BufferLength
//                 Span(fst' p, snd' p)

//     member this.Return<'T>(span:Span<'T>) =
//         let len = span.Length * sizeof<'T>
//         if len = 0 then
//             ()

//         let bucket = selectBucketIndex (len)
//         let have_bucket = bucket < _buckets.Length
//         if have_bucket then
//             let ptr = &&MemoryMarshal.GetReference(span)
//             let p = NativePtr.toNativeInt ptr
//             _buckets[bucket].Return(struct(p,len))

        
// /// Used for internal need of NativeArrayPool
// and Bucket(bufferLength:int, numberOfBuffers:int, pool_id:int) = 
//     let buffer_len = bufferLength
//     let buffers = Array.zeroCreate<struct(nativeint * int)> numberOfBuffers
//     let mutable index = 0

//     let null_buffer = struct(nativeint 0, 0)

//     let alloc n =
//         let ptr = NativeMemory.Alloc(unativeint n) |> NativePtr.ofVoidPtr<int> |> NativePtr.toNativeInt
//         struct(ptr,n)
   
//     // new() = Bucket(128,)

//     member this.BufferLength = bufferLength

//     member this.Rent() =
//         let mutable buffer = null_buffer
//         let mutable allocate_buffer = false
        
//         if index < buffer_len then
//             buffer <- buffers[index] 
//             buffers[index] <- null_buffer
//             index <- index + 1
//             allocate_buffer <- buffer = null_buffer

//         if allocate_buffer then
//             buffer <- alloc buffer_len

//         buffer
                
                
//     member this.Return(_array:struct(nativeint * int)) =
//         let struct(ptr,len) = _array
//         if len <> buffer_len then failwith "Error: not from pool"

//         let returned = index <> 0
//         if returned then
//             index <- index - 1
//             buffers[index] <- _array

//         if not returned then
//             ()  // if log is enabled
//



