namespace SE.Spatial

open System
open System.Numerics
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open SE


// type [<Struct>] Mesh = {vertices:narray<float32>; L:int; indices:narray<uint32>} with
//     interface IDisposable with
//         member this.Dispose() =
//             this.vertices.Dispose()
//             this.indices.Dispose()

//     member this.Dispose() =
//         this.vertices.Dispose()
//         this.indices.Dispose()

//     member this.Stride = this.L * sizeof<float32>

//     static member points n = {vertices=NativeArray.create<float32> n; indices=NativeArray.empty<uint32>(); L=7}
        

[<Struct>]
type Mesh =
    val vertices: narray<float32>
    val indices: narray<uint32>
    val L: int

    new(vertices, indices, L) = {
        vertices = vertices
        indices = indices
        L = L
    }
    
    interface IDisposable with
        member this.Dispose() =
            this.vertices.Dispose()
            this.indices.Dispose()

    member this.Dispose() =
        this.vertices.Dispose()
        this.indices.Dispose()

    member this.Stride =
        this.L * sizeof<float32>

    static member points n = 
        new Mesh(NativeArray.create<float32> n, NativeArray.empty<uint32>(), 7)


type [<Struct>] Triangle = {n0:Vector3; n1:Vector3; n2:Vector3}




