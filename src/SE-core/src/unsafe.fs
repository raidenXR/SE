#nowarn "9"
namespace SE

open System
open System.Numerics
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open FSharp.NativeInterop


[<AutoOpen>]
module UnsafeOps =
    let inline ( !! ) (ptr:nativeptr<_>) = NativePtr.read ptr

    let inline ( ~~ ) (ptr:nativeptr<_>) = NativePtr.toVoidPtr ptr

    let inline ( ++ ) (ptr:nativeptr<_>) offset = NativePtr.add ptr offset

    let inline ( -- ) (ptr:nativeptr<_>) offset = NativePtr.add ptr (-offset)

    let nullptr = IntPtr.Zero |> NativePtr.ofNativeInt<uint8> |> NativePtr.toVoidPtr

    /// stack allocates a Span<T>() with legth N
    let inline stackalloc<'T when 'T:unmanaged> (n:int) =
        let ptr = NativePtr.stackalloc<'T> n |> NativePtr.toVoidPtr
        Span<'T>(ptr, n * sizeof<'T>)

    let inline cast<'T when 'T:unmanaged> ptr = NativePtr.ofVoidPtr<'T> ptr

    let nint ptr = cast<byte> ptr |> NativePtr.toNativeInt 

    type nativeptr<'T when 'T: unmanaged> with
        member this.Item with inline get(idx) = NativePtr.get this idx and inline set(idx) value = NativePtr.set this idx value  


    type [<Struct; CustomComparison; CustomEquality>] Slice = {ptr:nativeint; len:int} with
        override this.Equals(other:obj) = this.len = (other :?> Slice).len

        member this.Equals(other:Slice) = this.ptr = other.ptr

        override this.GetHashCode() = int(this.ptr)

        interface IComparable with
            member this.CompareTo(other:obj) = failwith "Not Implemented"         

        interface IComparable<Slice> with
            member this.CompareTo(other:Slice) = this.len.CompareTo(other.len)           


    let internal pool = ResizeArray<Slice>()
    let internal ptrs = System.Collections.Generic.Dictionary<nativeint, int>()

    let alloc<'T> n =
        let len = n * sizeof<'T> 
        let ptr = NativeMemory.AllocZeroed (unativeint len) 
        ptrs.Add(nint ptr, len)
        ptr
        
    let rent<'T> n =
        match pool.Count with
        | 0 -> alloc<'T> n
        | _ -> 
            let mutable r = false
            let mutable i = 0
            while i < pool.Count && not r do
                r <- pool[i].len >= n * sizeof<'T>
                i <- i + if r then 0 else 1
            if r then
                let ptr = pool[i].ptr
                pool.RemoveAt(i)
                pool.Sort()
                ptr.ToPointer()
            else
                alloc<'T> n

    let inline free ptr = NativeMemory.Free ptr

    let return' ptr =
        let npt = nint ptr
        let len = ptrs[npt]
        pool.Add({ptr = npt; len = len})
        pool.Sort()
        
        
type [<Struct>] narray<'T when 'T:unmanaged> =
    val mutable private ptr: voidptr
    val mutable private len: int
    val mutable private is_disposed: bool
    val mutable private is_pooled: bool

    internal new(ptr:voidptr, len:int, is_pooled:bool) = {
        ptr = ptr
        len = len
        is_disposed = false
        is_pooled = is_pooled
    }

    member this.Dispose() =
        if not this.is_disposed then
            match this.is_pooled with
            | true  -> return' this.ptr
            | false -> free this.ptr 
        this.is_disposed <- true

    interface IDisposable with
        member this.Dispose() = this.Dispose()

    member this.Length = this.len

    member this.BufferSize = this.len * sizeof<'T>

    member this.Ptr = this.ptr

    member this.IsPooled = this.is_pooled

    member this.IsDisposed = this.is_disposed

    member this.ToInt() = nint this.ptr 

    member this.AsSpan() = Span<'T>(this.ptr, this.len)

    member this.Item
        with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get (i:int) =
            let p = NativePtr.ofVoidPtr<'T> this.ptr
            NativePtr.get p i
        and [<MethodImpl(MethodImplOptions.AggressiveInlining)>] set (i:int) value = 
            let p = NativePtr.ofVoidPtr<'T> this.ptr
            NativePtr.set p i value

module NativeArray =
    let empty<'T when 'T:unmanaged> () = new narray<'T>(nullptr, 0, false)

    let create<'T when 'T:unmanaged> (N:int) =
        new narray<'T>(alloc<'T> N, N, false)

    let delete (source:narray<'T>) =
        if source.IsPooled then Console.WriteLine("WARNING: pooled narray<'T> is deleted")
        NativeMemory.Free (source.Ptr)

    let rent<'T when 'T:unmanaged> (N:int) =
        new narray<'T>(rent<'T> N, N, true)
                
    let ofArray (source:array<'T>) = 
        let n = source.Length
        let size = n * sizeof<'T>
        use pta = fixed source
        let ptr = alloc<'T> n
        System.Buffer.MemoryCopy(~~pta, ptr, size, size)
        new narray<'T>(ptr, n, false)

    let ofSeq (source:seq<'T>) = 
        let n = Seq.length source
        let size = n * sizeof<'T>
        let ptr = alloc<'T> n
        let pta = cast<'T> ptr
        for i in 0..n-1 do
            pta[i] <- (Seq.item i source)
        new narray<'T>(ptr, n, false)

    let cast<'T,'U when 'T:unmanaged and 'U:unmanaged> (buf:narray<'U>) N =
        let ptr = buf.Ptr
        let len = buf.BufferSize / N
        if len >= buf.Length then failwith "narray.BufferSize is less in capacity to store N items of 'T"
        Span<'T>(ptr, len)


type [<Struct>] narray2d<'T when 'T:unmanaged> =
    val mutable private ptr: voidptr
    val mutable private i: int
    val mutable private j: int
    val mutable private is_disposed: bool
    val mutable private is_pooled: bool

    internal new(ptr:voidptr, I:int, J:int, is_pooled:bool) = {
        ptr = ptr
        i = I
        j = J
        is_disposed = false
        is_pooled = is_pooled
    }

    member this.Dispose() =
        if not this.is_disposed then
            match this.is_pooled with
            | true  -> return' this.ptr
            | false -> free this.ptr 
        this.is_disposed <- true

    interface IDisposable with
        member this.Dispose() = this.Dispose()

    member this.Length with get() = this.i * this.j
    
    member this.BufferSize with get() = this.Length * sizeof<'T>

    member this.I = this.i

    member this.J = this.j

    member this.Ptr = this.ptr

    member this.IsPooled = this.is_pooled

    member this.IsDisposed = this.is_disposed
    
    // member this.ToInt() = this.ptr |> NativePtr.ofVoidPtr<'T> |> NativePtr.toNativeInt 
    member this.ToInt() = nint this.ptr 

    member this.AsSpan() = Span<'T>(this.ptr, this.Length)

    member this.Item
        with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get (i:int, j:int) =
            let idx = i * this.j + j
            let p = NativePtr.ofVoidPtr<'T> this.ptr
            NativePtr.get p idx
        and [<MethodImpl(MethodImplOptions.AggressiveInlining)>] set (i:int, j:int) value = 
            let idx = i * this.j + j
            let p = NativePtr.ofVoidPtr<'T> this.ptr
            NativePtr.set p idx value

module NativeArray2D =
    let create<'T when 'T:unmanaged> I J =
        let N = I*J
        new narray2d<'T>(alloc<'T> N, I, J, false)

    let delete (source:narray2d<'T>) =
        if source.IsPooled then Console.WriteLine("WARNING: pooled narray<'T> is deleted")
        NativeMemory.Free (source.Ptr)

    let rent<'T when 'T:unmanaged> I J =
        let N = I*J
        new narray2d<'T>(rent<'T> N, I, J, true)


type [<Struct>] narray3d<'T when 'T:unmanaged> =
    val mutable private ptr: voidptr
    val mutable private i: int
    val mutable private j: int
    val mutable private k: int
    val mutable private is_disposed: bool
    val mutable private is_pooled: bool

    internal new(ptr:voidptr, I:int, J:int, K:int, is_pooled:bool) = {
        ptr = ptr
        i = I
        j = J
        k = K
        is_disposed = false
        is_pooled = is_pooled
    }

    member this.Dispose() =
        if not this.is_disposed then
            match this.is_pooled with
            | true  -> return' this.ptr
            | false -> free this.ptr 
        this.is_disposed <- true

    interface IDisposable with
        member this.Dispose() = this.Dispose()

    member this.Length with get() = this.I * this.J * this.K
    
    member this.BufferSize with get() = this.Length * sizeof<'T>
    
    member this.I = this.i

    member this.J = this.j

    member this.K = this.k

    member this.Ptr = this.ptr
    
    member this.IsPooled = this.is_pooled

    member this.IsDisposed = this.is_disposed

    member this.ToInt() = nint this.ptr 

    member this.AsSpan() = Span<'T>(this.ptr, this.Length)

    member this.Item
        with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get (i:int, j:int, k:int) =
                let idx = i * this.j * this.k + j * this.k + k
                let p = NativePtr.ofVoidPtr<'T> this.Ptr
                NativePtr.get p idx
            and [<MethodImpl(MethodImplOptions.AggressiveInlining)>] set (i:int, j:int, k:int) value = 
                let idx = i * this.j * this.k + j * this.k + k
                let p = NativePtr.ofVoidPtr<'T> this.Ptr
                NativePtr.set p idx value

module NativeArray3D =
    let create<'T when 'T:unmanaged> I J K =
        let N = I*J*K
        new narray3d<'T>(alloc<'T> N, I, J, K, false)

    let delete (source:narray3d<'T>) =
        if source.IsPooled then Console.WriteLine("WARNING: pooled narray<'T> is deleted")
        NativeMemory.Free (source.Ptr)

    let rent<'T when 'T:unmanaged> I J K =
        let N = I*J*K
        new narray3d<'T>(rent<'T> N, I, J, K, true)


module Bits =
    /// use this to use bits instead of bools for 0 - 1 and store information for discretization
    let get_bit (item:byref<byte>) n =
    // let get_bit (bytes:Span<byte>) n =
        // let item = bytes[n / 8]
        match n % 8 with
        | 0 -> (item &&& 0b00000001uy) > 0uy
        | 1 -> (item &&& 0b00000010uy) > 0uy
        | 2 -> (item &&& 0b00000100uy) > 0uy
        | 3 -> (item &&& 0b00001000uy) > 0uy
        | 4 -> (item &&& 0b00010000uy) > 0uy
        | 5 -> (item &&& 0b00100000uy) > 0uy
        | 6 -> (item &&& 0b01000000uy) > 0uy
        | 7 -> (item &&& 0b10000000uy) > 0uy
        | _ -> false

    let get_bit1d (bytes:Span<byte>) n = get_bit (&bytes[n / 8]) n    
    let get_bit2d (bytes:Span<byte>) n = get_bit (&bytes[n / 8 / 8]) n    
    let get_bit3d (bytes:Span<byte>) n = get_bit (&bytes[n / 8 / 8 / 8]) n

    let get_byte1d (bytes:Span<byte>) n = bytes[n / 8]
    let get_byte2d (bytes:Span<byte>) n = bytes[n / 8 / 8]
    let get_byte3d (bytes:Span<byte>) n = bytes[n / 8 / 8 / 8]

    let set_bit (item:byref<byte>) n value =
    // let set_bit (bytes:Span<byte>) n value =
        // let item = &bytes[n / 8]
        match value with
        | true -> 
            item <- match n % 8 with
                    | 0 -> (item ||| 0b00000001uy)
                    | 1 -> (item ||| 0b00000010uy)
                    | 2 -> (item ||| 0b00000100uy)
                    | 3 -> (item ||| 0b00001000uy)
                    | 4 -> (item ||| 0b00010000uy)
                    | 5 -> (item ||| 0b00100000uy)
                    | 6 -> (item ||| 0b01000000uy)
                    | 7 -> (item ||| 0b10000000uy)
                    | _ -> item
        | false ->
            item <- match n % 8 with
                    | 0 -> (item &&& 0b11111110uy)
                    | 1 -> (item &&& 0b11111101uy)
                    | 2 -> (item &&& 0b11111011uy)
                    | 3 -> (item &&& 0b11110111uy)
                    | 4 -> (item &&& 0b11101111uy)
                    | 5 -> (item &&& 0b11011111uy)
                    | 6 -> (item &&& 0b10111111uy)
                    | 7 -> (item &&& 0b01111111uy)
                    | _ -> item

    let set_bit1d (bytes:Span<byte>) n value = set_bit (&bytes[n / 8]) n value
            
    let set_bit2d (bytes:Span<byte>) n value = set_bit (&bytes[n / 8 / 8]) n value
            
    let set_bit3d (bytes:Span<byte>) n value = set_bit (&bytes[n / 8 / 8 / 8]) n value
            

    let bits_count (b:byte) =
        let b0 = if b &&& 0b00000001uy > 0uy then 1 else 0 
        let b1 = if b &&& 0b00000010uy > 0uy then 1 else 0 
        let b2 = if b &&& 0b00000100uy > 0uy then 1 else 0 
        let b3 = if b &&& 0b00001000uy > 0uy then 1 else 0 
        let b4 = if b &&& 0b00010000uy > 0uy then 1 else 0 
        let b5 = if b &&& 0b00100000uy > 0uy then 1 else 0 
        let b6 = if b &&& 0b01000000uy > 0uy then 1 else 0 
        let b7 = if b &&& 0b10000000uy > 0uy then 1 else 0 
        b0 + b1 + b2 + b3 + b4 + b5 + b6 + b7


    
[<Extension>]
type NArrayExtensions() =

    [<Extension>] 
    static member BitCount(bits:byte) = Bits.bits_count bits


    [<Extension>] 
    static member GetBit(bits:narray<byte>, i:int) =
        let ni = i / 8
        Bits.get_bit (&bits.AsSpan()[ni]) i 
    
    [<Extension>] 
    static member SetBit(bits:narray<byte>, i:int, value:bool) =
        let ni = i / 8
        Bits.set_bit (&bits.AsSpan()[ni]) i value
    
    [<Extension>] 
    static member GetByte(bits:narray<byte>, i:int) = bits.AsSpan()[i/8]

    [<Extension>] 
    static member GetBit(bits:narray2d<byte>, i:int, j:int) =
        let ni = i / 8
        let nj = j / 8
        Bits.get_bit (&bits.AsSpan()[ni*bits.J + nj]) j
    
    [<Extension>] 
    static member SetBit(bits:narray2d<byte>, i:int, j:int, value:bool) =
        let ni = i / 8
        let nj = j / 8
        Bits.set_bit (&bits.AsSpan()[ni*bits.J + nj]) j value
    
    [<Extension>] 
    static member GetByte(bits:narray2d<byte>, i:int, j:int) = 
        let ni = i / 8
        let nj = j / 8
        bits.AsSpan()[ni*bits.J + nj]

    [<Extension>] 
    static member GetBit(bits:narray3d<byte>, i:int, j:int, k:int) =
        let ni = i / 8
        let nj = j / 8
        let nk = k / 8
        Bits.get_bit (&bits.AsSpan()[ni*bits.J*bits.K + nj*bits.K + nk]) j
    
    [<Extension>] 
    static member SetBit(bits:narray3d<byte>, i:int, j:int, k:int, value:bool) =
        let ni = i / 8
        let nj = j / 8
        let nk = k / 8
        Bits.set_bit (&bits.AsSpan()[ni*bits.J*bits.K + nj*bits.K + nk]) j value
    
    [<Extension>] 
    static member GetByte(bits:narray3d<byte>, i:int, j:int, k:int) =
        let ni = i / 8
        let nj = j / 8
        let nk = k / 8
        bits.AsSpan()[ni*bits.J*bits.K + nj*bits.K + nk]


 // module NativeBuffer =
//     let append<'T when 'T:unmanaged> (idx:byref<int>) (data:'T) (buffer:narray<byte>) =
//         let size = sizeof<'T>
//         if size + idx >= buffer.Length then raise (new ArgumentOutOfRangeException())
//         let mutable d = data
//         let a = ~~(&&d)
//         let b = ~~(cast<byte> buffer.Ptr ++ idx)
//         Buffer.MemoryCopy(a, b, size, size)
//         idx <- idx + size
//         buffer


// type [<Struct>] nsparse<'T when 'T:unmanaged> =
//     val mutable values: narray<'T>
//     val mutable offs_a: narray<int>
//     val mutable offs_b: narray<int>
//     val mutable count: int
//     val mutable len:   int
//     val mutable is_disposed: bool
    
//     new(N:int) = {
//         values = NativeArray.rent<'T> N
//         offs_a = NativeArray.rent<int> N
//         offs_b = NativeArray.rent<int> N
//         len = 0
//         count = 0
//         is_disposed = false
//     }
        
//     interface IDisposable with
//         member this.Dispose() =
//             if not this.is_disposed then
//                 this.offs_a.Dispose()
//                 this.offs_b.Dispose()
//                 this.values.Dispose()
//             this.is_disposed <- true

//     member this.Length with get() = this.count
    
//     // member this.Offsets with get() = cast<int> this.offset

//     // member this.Values with get() = cast<'T> this.values
    
//     // member this.Ptr with get() = this.values
    
//     // member this.ToInt() = this.values |> NativePtr.ofVoidPtr<'T> |> NativePtr.toNativeInt 

//     member this.Dispose() =
//         if not this.is_disposed then
//             this.offs_a.Dispose()
//             this.offs_b.Dispose()
//             this.values.Dispose()
//         this.is_disposed <- true

    // member this.AsSpan() = Span<'T>(this.values, this.Length)

    // member this.Item
    //     with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get (i:int, j:int, k:int) =
    //             let p_offset = cast<int> this.offset
    //             let p_values = cast<'T> this.values
    //             let idx = NativePtr.get p_offset (i * this.J + j)
    //             NativePtr.get p_values (idx + k)
    //         and [<MethodImpl(MethodImplOptions.AggressiveInlining)>] set (i:int, j:int, k:int) value = 
    //             let p_offset = cast<int> this.offset
    //             let p_values = cast<'T> this.values
    //             let idx = NativePtr.get p_offset (i * this.J + j)
    //             NativePtr.set p_values (idx + k) value


