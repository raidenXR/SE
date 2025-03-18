#nowarn "9"
namespace SE.Gltf

open System
open System.Numerics
open System.Runtime
open System.Runtime.InteropServices
open System.Runtime.CompilerServices
open FSharp.NativeInterop

module GltfBin =
    let inline private cast<'T when 'T: unmanaged> ptr = NativePtr.ofVoidPtr<'T> ptr

    let inline private cast_ptr<'T when 'T: unmanaged> (ptr:nativeint) = NativePtr.ofVoidPtr<'T> (ptr.ToPointer())

    /// overloaded operator for dereferencing pointers
    let inline private (!) ptr = NativePtr.read<'T> ptr

    let inline private (~~) ptr = NativePtr.toVoidPtr ptr

    /// overloaded operator for adding/ offset pointers
    let inline private (++) ptr offset = NativePtr.add ptr offset


    let private buffers_map = new System.Collections.Generic.Dictionary<int,nativeint>() 


    let loadBuffers (buffers:seq<SE.Gltf.Buffer>) =        
        for i,buffer in (Seq.indexed buffers) do
            let len = buffer.bufferLength
            use uri = fixed IO.File.ReadAllBytes(buffer.uri)
            let ptr = Marshal.AllocHGlobal(len)
            System.Buffer.MemoryCopy(~~uri, ptr.ToPointer(), len, len)
            buffers_map.Add(i,ptr)

    let freeBuffers () =
        for kvp in buffers_map do
            Marshal.FreeHGlobal(kvp.Value)


    let bufferview_SCALAR (accessor_k:int) (root:GltfRoot) =
        let accessor = root.accessors[accessor_k]
        let bufferview = root.bufferViews[accessor.bufferView]
        let buffer = buffers_map[bufferview.buffer]
        let ptr = (cast_ptr<byte> buffer) ++ accessor.byteOffset
        Span<float32>(~~ptr, accessor.count)

        
    let bufferview_VEC3 (accessor_k:int) (root:GltfRoot) =
        let accessor = root.accessors[accessor_k]
        let bufferview = root.bufferViews[accessor.bufferView]
        let buffer = buffers_map[bufferview.buffer]
        let ptr = (cast_ptr<byte> buffer) ++ accessor.byteOffset
        Span<Vector3>(~~ptr, accessor.count)

        
    let bufferview_VEC4 (accessor_k:int) (root:GltfRoot) =
        let accessor = root.accessors[accessor_k]
        let bufferview = root.bufferViews[accessor.bufferView]
        let buffer = buffers_map[bufferview.buffer]
        let ptr = (cast_ptr<byte> buffer) ++ accessor.byteOffset
        Span<Vector4>(~~ptr, accessor.count)
        
    
    
