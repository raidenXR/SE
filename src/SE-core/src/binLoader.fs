#nowarn "9"
namespace SE.Gltf

open System
open System.Numerics
open System.Runtime
open System.Runtime.InteropServices
open System.Runtime.CompilerServices
open FSharp.NativeInterop

module GltfBin =
    let inline cast<'T when 'T: unmanaged> (ptr:voidptr) =
        let ptr: nativeptr<'T> = NativePtr.ofVoidPtr ptr
        ptr 

    let inline deref<'T when 'T: unmanaged> (ptr:nativeptr<'T>) =
        NativePtr.read ptr


    /// helper for reading .bin files from exported .gltf from FreeCAD
    type BinLoader(path:string, offset:int) =
        let path = path[0..path.Length - 6] + ".bin"
        let bin = System.IO.File.ReadAllBytes path
        let len = bin.Length - offset

        let copy_to_unmanaged (bin:array<byte>) (len:int) =
            let ptr = Marshal.AllocHGlobal len
            use ptr0 = fixed bin
            let ptr0 = NativePtr.add ptr0 len |> NativePtr.toVoidPtr
            Buffer.MemoryCopy(ptr0, ptr.ToPointer(), len, len)
            ptr
            

        let to_unmanaged = copy_to_unmanaged bin len
        let mutable is_disposed = false


        interface IDisposable with
            member this.Dispose() =
                if not is_disposed then
                    Marshal.FreeHGlobal(to_unmanaged)
                    is_disposed <- true
        
        /// use this method to print values in BinLoader slice of .bin file
        /// for debugging purposes
        member this.Print<'T when 'T: unmanaged>(count:int) =            
            let ptr = cast<'T> (to_unmanaged.ToPointer())
            for i = 0 to count - 1 do
                let vec = NativePtr.add ptr i
                printfn "%d: %A" i (deref vec)            
    
    
    
