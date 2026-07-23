namespace SE.Renderer
// open ImGuiNET
open OpenTK.Graphics
open OpenTK.Graphics.OpenGL4
// open OpenTK.Mathematics
open System.Numerics
open OpenTK.Windowing.Common
open OpenTK.Windowing.Desktop
open OpenTK.Windowing.GraphicsLibraryFramework
open System
open System.Collections.Generic
open System.ComponentModel
open System.Diagnostics
open System.Runtime.InteropServices
open System.Runtime.CompilerServices
open FSharp.NativeInterop

open SE
open SE.Spatial

module S =
    let sum n (l:list<int>) =
        let mutable s = 0
        for i in 1..(min n l.Length) do s <- s + l[i-1]
        s

type [<Struct>] GLMesh = {vao:int; vbo:int; ebo:int}

type [<Struct>] GLPrim = {vao:int; vbo:int}

type [<Struct>] ValueAnimation = {
    /// the index to the animation or GLTF.Root.animations array
    idx: int
    /// the curretn key-frame used on the animation
    mutable is_reversed: bool
    mutable is_active: bool
    mutable is_looped: bool
    mutable dt: float
}

type [<Struct>] Model =
    // val mutable vertices: narray<float32>
    // val mutable indices:  narray<uint32>
    val mutable mesh: MeshF
    val mutable private stride: int
    val mutable private l: int    
    val mutable private attrib0: int    
    val mutable private attrib1: int    
    val mutable private attrib2: int    
    val mutable private attrib3: int    
    val mutable private attrib4: int    
    val mutable private is_disposed: bool

    new(vertices:narray<float32>, indices:narray<uint32>, attribs:list<int>) =
        {
            // vertices = vertices
            // indices = indices
            mesh = {vertices = vertices; indices = indices; L = List.sum attribs}
            is_disposed = false
            l = List.sum attribs
            stride = (List.sum attribs) * (sizeof<float32>)
            attrib0 = (S.sum 0 attribs) * sizeof<float32>
            attrib1 = (S.sum 1 attribs) * sizeof<float32>
            attrib2 = (S.sum 2 attribs) * sizeof<float32>
            attrib3 = (S.sum 3 attribs) * sizeof<float32>
            attrib4 = (S.sum 4 attribs) * sizeof<float32>            
        }

    new(vertices:narray<float32>, attribs:list<int>) =
        {
            // vertices = vertices
            // indices = NativeArray.create 10
            mesh = {vertices = vertices; indices = NativeArray.empty(); L = List.sum attribs}
            is_disposed = false
            l = List.sum attribs
            stride = (List.sum attribs) * (sizeof<float32>)
            attrib0 = (S.sum 0 attribs) * sizeof<float32>
            attrib1 = (S.sum 1 attribs) * sizeof<float32>
            attrib2 = (S.sum 2 attribs) * sizeof<float32>
            attrib3 = (S.sum 3 attribs) * sizeof<float32>
            attrib4 = (S.sum 4 attribs) * sizeof<float32>            
        }

    member this.Dispose() =
        if not this.is_disposed then
            this.mesh.vertices.Dispose()
            this.mesh.indices.Dispose()
        this.is_disposed <- true

    interface IDisposable with
        member this.Dispose() = this.Dispose() 
        
    
    member this.Vertices with get() = this.mesh.vertices.AsSpan()
    member this.Indices with get()  = this.mesh.indices.AsSpan() 
    member this.L with get() = this.l
    member this.Stride with get() = this.stride
    member this.Attrib0 with get() = this.attrib0 
    member this.Attrib1 with get() = this.attrib1 
    member this.Attrib2 with get() = this.attrib2
    member this.Attrib3 with get() = this.attrib3
    member this.Attrib4 with get() = this.attrib4
    

module RGeometry =
    let cube_vertices = [|
        // Fill in the front face vertex data.
        -0.5f; -0.5f; -0.5f; 0.0f; 0.0f; -1.0f;
        -0.5f; +0.5f; -0.5f; 0.0f; 0.0f; -1.0f;
        +0.5f; +0.5f; -0.5f; 0.0f; 0.0f; -1.0f;
        +0.5f; -0.5f; -0.5f; 0.0f; 0.0f; -1.0f;

        // Fill in the back face vertex data.
        -0.5f; -0.5f; +0.5f; 0.0f; 0.0f; 1.0f;
        +0.5f; -0.5f; +0.5f; 0.0f; 0.0f; 1.0f;
        +0.5f; +0.5f; +0.5f; 0.0f; 0.0f; 1.0f;
        -0.5f; +0.5f; +0.5f; 0.0f; 0.0f; 1.0f;

        // Fill in the top face vertex data.
        -0.5f; +0.5f; -0.5f; 0.0f; 1.0f; 0.0f;
        -0.5f; +0.5f; +0.5f; 0.0f; 1.0f; 0.0f;
        +0.5f; +0.5f; +0.5f; 0.0f; 1.0f; 0.0f;
        +0.5f; +0.5f; -0.5f; 0.0f; 1.0f; 0.0f;

        // Fill in the bottom face vertex data.
        -0.5f; -0.5f; -0.5f; 0.0f; -1.0f; 0.0f;
        +0.5f; -0.5f; -0.5f; 0.0f; -1.0f; 0.0f;
        +0.5f; -0.5f; +0.5f; 0.0f; -1.0f; 0.0f;
        -0.5f; -0.5f; +0.5f; 0.0f; -1.0f; 0.0f;

        // Fill in the left face vertex data.
        -0.5f; -0.5f; +0.5f; -1.0f; 0.0f; 0.0f;
        -0.5f; +0.5f; +0.5f; -1.0f; 0.0f; 0.0f;
        -0.5f; +0.5f; -0.5f; -1.0f; 0.0f; 0.0f;
        -0.5f; -0.5f; -0.5f; -1.0f; 0.0f; 0.0f;

        // Fill in the right face vertex data.
        +0.5f; -0.5f; -0.5f; 1.0f; 0.0f; 0.0f;
        +0.5f; +0.5f; -0.5f; 1.0f; 0.0f; 0.0f;
        +0.5f; +0.5f; +0.5f; 1.0f; 0.0f; 0.0f;
        +0.5f; -0.5f; +0.5f; 1.0f; 0.0f; 0.0f;
    |]

    let cube_positions = [|
        // Fill in the front face vertex data.
        -0.5f; -0.5f; -0.5f; 
        -0.5f; +0.5f; -0.5f; 
        +0.5f; +0.5f; -0.5f; 
        +0.5f; -0.5f; -0.5f; 

        // Fill in the back face vertex data.
        -0.5f; -0.5f; +0.5f;
        +0.5f; -0.5f; +0.5f;
        +0.5f; +0.5f; +0.5f;
        -0.5f; +0.5f; +0.5f;

        // Fill in the top face vertex data.
        -0.5f; +0.5f; -0.5f;
        -0.5f; +0.5f; +0.5f;
        +0.5f; +0.5f; +0.5f;
        +0.5f; +0.5f; -0.5f;

        // Fill in the bottom face vertex data.
        -0.5f; -0.5f; -0.5f;
        +0.5f; -0.5f; -0.5f;
        +0.5f; -0.5f; +0.5f;
        -0.5f; -0.5f; +0.5f;

        // Fill in the left face vertex data.
        -0.5f; -0.5f; +0.5f;
        -0.5f; +0.5f; +0.5f;
        -0.5f; +0.5f; -0.5f;
        -0.5f; -0.5f; -0.5f;

        // Fill in the right face vertex data.
        +0.5f; -0.5f; -0.5f;
        +0.5f; +0.5f; -0.5f;
        +0.5f; +0.5f; +0.5f;
        +0.5f; -0.5f; +0.5f;
    |]

    let cube_normals = [|
        // Fill in the front face vertex data.
        0.0f; 0.0f; -1.0f;
        0.0f; 0.0f; -1.0f;
        0.0f; 0.0f; -1.0f;
        0.0f; 0.0f; -1.0f;

        // Fill in the back face vertex data.
        0.0f; 0.0f; 1.0f;
        0.0f; 0.0f; 1.0f;
        0.0f; 0.0f; 1.0f;
        0.0f; 0.0f; 1.0f;

        // Fill in the top face vertex data.
        0.0f; 1.0f; 0.0f;
        0.0f; 1.0f; 0.0f;
        0.0f; 1.0f; 0.0f;
        0.0f; 1.0f; 0.0f;

        // Fill in the bottom face vertex data.
        0.0f; -1.0f; 0.0f;
        0.0f; -1.0f; 0.0f;
        0.0f; -1.0f; 0.0f;
        0.0f; -1.0f; 0.0f;

        // Fill in the left face vertex data.
        -1.0f; 0.0f; 0.0f;
        -1.0f; 0.0f; 0.0f;
        -1.0f; 0.0f; 0.0f;
        -1.0f; 0.0f; 0.0f;

        // Fill in the right face vertex data.
        1.0f; 0.0f; 0.0f;
        1.0f; 0.0f; 0.0f;
        1.0f; 0.0f; 0.0f;
        1.0f; 0.0f; 0.0f;
    |]

    let cube_colors = [|
        // Fill in the front face vertex data.
        0.3f; 0.3f; 0.3f; 1.0f;
        0.3f; 0.3f; 0.3f; 1.0f;
        0.3f; 0.3f; 0.3f; 1.0f;
        0.3f; 0.3f; 0.3f; 1.0f;

        // Fill in the back face vertex data.
        0.6f; 0.3f; 0.5f; 1.0f;
        0.6f; 0.3f; 0.5f; 1.0f;
        0.6f; 0.3f; 0.5f; 1.0f;
        0.6f; 0.3f; 0.5f; 1.0f;

        // Fill in the top face vertex data.
        0.3f; 0.4f; 0.6f; 1.0f;
        0.3f; 0.4f; 0.6f; 1.0f;
        0.3f; 0.4f; 0.6f; 1.0f;
        0.3f; 0.4f; 0.6f; 1.0f;

        // Fill in the bottom face vertex data.
        0.4f; 0.1f; 0.3f; 1.0f;
        0.4f; 0.1f; 0.3f; 1.0f;
        0.4f; 0.1f; 0.3f; 1.0f;
        0.4f; 0.1f; 0.3f; 1.0f;

        // Fill in the left face vertex data.
        0.1f; 0.1f; 0.3f; 1.0f;
        0.1f; 0.1f; 0.3f; 1.0f;
        0.1f; 0.1f; 0.3f; 1.0f;
        0.1f; 0.1f; 0.3f; 1.0f;

        // Fill in the right face vertex data.
        0.3f; 0.8f; 0.2f; 1.0f;
        0.3f; 0.8f; 0.2f; 1.0f;
        0.3f; 0.8f; 0.2f; 1.0f;
        0.3f; 0.8f; 0.2f; 1.0f;
    |]

    let cube_indices = [|
        0u; 1u; 2u; 0u; 2u; 3u;
        4u; 5u; 6u; 4u; 6u; 7u;
        8u; 9u; 10u; 8u; 10u; 11u;
        12u; 13u; 14u; 12u; 14u; 15u;
        16u; 17u; 18u; 16u; 18u; 19u;
        20u; 21u; 22u; 20u; 22u; 23u;
    |]


    let pointlights_positions = [|
        Vector3(0.7f, 0.2f, 2.0f);
        Vector3(2.3f, -3.3f, -4.0f);
        Vector3(-4.0f, 2.0f, -12.0f);
        Vector3(0.0f, 0.0f, -3.0f);
    |]

    /// combines two arrays into one where the entries are one after the other
    let compose (sb:int, b:array<_>) (sa, a:array<_>) =
        let l = a.Length + b.Length
        let v = Array.zeroCreate<_> l
        let s = sa + sb
        let n = if (a.Length / sa) = (b.Length / sb) then a.Length / sa else failwith "not matching entries count"
        for i in 0..n - 1 do
            for j in 0..sa - 1 do v[i * s + j] <- a[i * sa + j]
            for k in 0..sb - 1 do v[i * s + sa + k] <- b[i * sb + k]
        (s,v)


    let cube () =
        let (_,vertices) = (3,cube_positions) |> compose (3,cube_normals) |> compose (4,cube_colors)
        let indices = cube_indices
        (vertices,indices)

    let cube_unmanaged () =
        let vertices = NativeArray.create<float32> (cube_positions.Length + cube_normals.Length + cube_colors.Length)
        let v = vertices.AsSpan()
        let vertices_count = cube_positions.Length / 3
        for i in 0..vertices_count-1 do
            v[10*i+0] <- cube_positions[3*i+0]
            v[10*i+1] <- cube_positions[3*i+1]
            v[10*i+2] <- cube_positions[3*i+2]
            v[10*i+3] <- cube_normals[3*i+0]
            v[10*i+4] <- cube_normals[3*i+1]
            v[10*i+5] <- cube_normals[3*i+2]
            v[10*i+6] <- cube_colors[4*i+0]
            v[10*i+7] <- cube_colors[4*i+1]
            v[10*i+8] <- cube_colors[4*i+2]
            v[10*i+9] <- cube_colors[4*i+3]
        
        let indices = NativeArray.ofArray (cube_indices)        
        struct(vertices,indices)


    type State = | Vertices | Indices

    let compute_normal (p0:Vector3) (p1:Vector3) (p2:Vector3) =
        let u = p1 - p0
        let v = p2 - p0
        Vector3.Normalize(Vector3.Cross(u,v))

    /// deserializes the vertices and indices from a .ply file
    let load_ply (path:string, r:float32, g:float32, b:float32, a:float32) =
        let ply = System.IO.File.ReadAllLines(path)
        let vertices_count = ply[3].Split() |> Array.takeWhile (fun x -> x.Length > 0) |> Seq.item 2 |> Int32.Parse
        let indices_count  = ply[9].Split() |> Array.takeWhile (fun x -> x.Length > 0) |> Seq.item 2 |> Int32.Parse

        let I = 12 
        let J = 12 + vertices_count

        let positions =
            let v = Array.zeroCreate<float32> (vertices_count * 3)
            for i in 0..vertices_count - 1 do
                let values = ply[I + i].Split() |> Array.takeWhile (fun x -> x.Length > 0)
                v[3 * i + 0] <- Single.Parse(values[0])
                v[3 * i + 1] <- Single.Parse(values[1])
                v[3 * i + 2] <- Single.Parse(values[2])
            v
        printfn "vertices.count %d" vertices_count

        let indices =
            let idx = Array.zeroCreate<uint32> (indices_count * 3)
            for i in 0..indices_count - 1 do
                let values = ply[J + i].Split() |> Array.takeWhile (fun x -> x.Length > 0)
                idx[3 * i + 0] <- UInt32.Parse(values[1])
                idx[3 * i + 1] <- UInt32.Parse(values[2])
                idx[3 * i + 2] <- UInt32.Parse(values[3])
            idx
        printfn "indices.count: %d" indices_count

        let normals =
            let n = Array.zeroCreate<float32> (vertices_count * 3)
            for i in 0..indices_count - 1 do
                let i0 = int32 (indices[3*i+0])
                let i1 = int32 (indices[3*i+1])
                let i2 = int32 (indices[3*i+2])
                
                let v0 = Vector3(positions[3*i0+0], positions[3*i0+1], positions[3*i0+2])
                let v1 = Vector3(positions[3*i1+0], positions[3*i1+1], positions[3*i1+2])
                let v2 = Vector3(positions[3*i2+0], positions[3*i2+1], positions[3*i2+2])

                let e0 = v1 - v0
                let e1 = v2 - v0
                let face_normal = Vector3.Cross(e0, e1)
                
                n[3 * i0 + 0] <- n[3 * i0 + 0] + face_normal.X
                n[3 * i0 + 1] <- n[3 * i0 + 1] + face_normal.Y
                n[3 * i0 + 2] <- n[3 * i0 + 2] + face_normal.Z                
                n[3 * i1 + 0] <- n[3 * i1 + 0] + face_normal.X
                n[3 * i1 + 1] <- n[3 * i1 + 1] + face_normal.Y
                n[3 * i1 + 2] <- n[3 * i1 + 2] + face_normal.Z                
                n[3 * i2 + 0] <- n[3 * i2 + 0] + face_normal.X
                n[3 * i2 + 1] <- n[3 * i2 + 1] + face_normal.Y
                n[3 * i2 + 2] <- n[3 * i2 + 2] + face_normal.Z                

            for i in 0..vertices_count - 1 do
                let normal = Vector3.Normalize(Vector3(n[3*i+0], n[3*i+1], n[3*i+2]))
                n[3 * i + 0] <- normal.X                                                        
                n[3 * i + 1] <- normal.Y                                                        
                n[3 * i + 2] <- normal.Z                                                        
            n
            
        let colors =
            let c = Array.zeroCreate<float32> (vertices_count * 4)
            for i in 0..4..c.Length - 5 do
                c[i + 0] <- r
                c[i + 1] <- g
                c[i + 2] <- b
                c[i + 3] <- a
            c
            
        let (_,vertices) = (3,positions) |> compose (3,normals) |> compose (4,colors)
        (vertices,indices)

    /// deserializes the vertices and indices from a .ply file into a NativeArray tuple
    let load_ply_unmanaged (path:string, r:float32, g:float32, b:float32, a:float32) =
        let ply = System.IO.File.ReadAllLines(path)
        let vertices_count = ply[3].Split() |> Array.takeWhile (fun x -> x.Length > 0) |> Seq.item 2 |> Int32.Parse
        let indices_count  = ply[9].Split() |> Array.takeWhile (fun x -> x.Length > 0) |> Seq.item 2 |> Int32.Parse

        let I = 12 
        let J = 12 + vertices_count
        let vertices = NativeArray.create<float32>(vertices_count * 10)
        let indices  = NativeArray.create<uint32>(indices_count * 3)

        let v = vertices.AsSpan()
        for i in 0..vertices_count - 1 do
            let values = ply[I + i].Split() |> Array.takeWhile (fun x -> x.Length > 0)
            v[10 * i + 0] <- Single.Parse(values[0])
            v[10 * i + 1] <- Single.Parse(values[1])
            v[10 * i + 2] <- Single.Parse(values[2])

        let idx = indices.AsSpan()
        for i in 0..indices_count - 1 do
            let values = ply[J + i].Split() |> Array.takeWhile (fun x -> x.Length > 0)
            idx[3 * i + 0] <- UInt32.Parse(values[1])
            idx[3 * i + 1] <- UInt32.Parse(values[2])
            idx[3 * i + 2] <- UInt32.Parse(values[3])        
        
        for i in 0..indices_count - 1 do
            let i0 = int32 (idx[3*i+0])
            let i1 = int32 (idx[3*i+1])
            let i2 = int32 (idx[3*i+2])
            
            let v0 = Vector3(v[10*i0+0], v[10*i0+1], v[10*i0+2])
            let v1 = Vector3(v[10*i1+0], v[10*i1+1], v[10*i1+2])
            let v2 = Vector3(v[10*i2+0], v[10*i2+1], v[10*i2+2])

            let e0 = v1 - v0
            let e1 = v2 - v0
            let face_normal = Vector3.Cross(e0, e1)
            
            v[10 * i0 + 3] <- v[10 * i0 + 3] + face_normal.X
            v[10 * i0 + 4] <- v[10 * i0 + 4] + face_normal.Y
            v[10 * i0 + 5] <- v[10 * i0 + 5] + face_normal.Z                
            v[10 * i1 + 3] <- v[10 * i1 + 3] + face_normal.X
            v[10 * i1 + 4] <- v[10 * i1 + 4] + face_normal.Y
            v[10 * i1 + 5] <- v[10 * i1 + 5] + face_normal.Z                
            v[10 * i2 + 3] <- v[10 * i2 + 3] + face_normal.X
            v[10 * i2 + 4] <- v[10 * i2 + 4] + face_normal.Y
            v[10 * i2 + 5] <- v[10 * i2 + 5] + face_normal.Z                

        for i in 0..vertices_count - 1 do
            let normal = Vector3.Normalize(Vector3(v[10*i+3], v[10*i+4], v[10*i+5]))
            v[10 * i + 3] <- normal.X                                                        
            v[10 * i + 4] <- normal.Y                                                        
            v[10 * i + 5] <- normal.Z                                                        

        for i in 0..vertices_count - 1 do
            v[10*i + 6] <- r
            v[10*i + 7] <- b
            v[10*i + 8] <- g
            v[10*i + 9] <- a
        
        {
            vertices = vertices
            indices = indices
            L = 10
        }
        

    /// deserializes the vertices and indices from a .txt file
    let load_txt (path:string, r:float32, g:float32, b:float32, a:float32) =
        let src = System.IO.File.ReadAllText(path)
        let lines = src.Split('\n')
        let idx_0 = lines[0].IndexOf(':') + 2
        let idx_1 = lines[1].IndexOf(':') + 2
        let vertex_count = lines[0][idx_0..] |> Int32.Parse
        let indices_count = 3 * (lines[1][idx_1..] |> Int32.Parse)
        let mutable state = Vertices

        printfn "vertex.count: %d" vertex_count
        printfn "indices.count: %d" indices_count
        let positions = Array.zeroCreate<float32> (vertex_count * 3)
        let normals = Array.zeroCreate<float32> (vertex_count * 3)
        let indices = Array.zeroCreate<uint32> indices_count

        let contains_number (str:string) =
            let mutable b = false
            let mutable j = 0
            while j < str.Length && not b do
                b <- ('0' <= str[j] && str[j] <= '9')
                j <- j + 1
            b
        
        let mutable iv = 0
        let mutable ii = 0
        let mutable i = 0
        for line in lines[4..] do
            i <- i + 1
            if line.Contains('{') then state <- Indices                 
            if state = Vertices then
                let values = line.Split() |> Array.filter (contains_number) |> Array.map (fun x -> Single.Parse x)
                try
                    if values.Length = 6 then
                        positions[iv + 0] <- values[0]
                        positions[iv + 1] <- values[1]
                        positions[iv + 2] <- values[2]
                        normals[iv + 0] <- values[3]
                        normals[iv + 1] <- values[4]
                        normals[iv + 2] <- values[5]
                        iv <- iv + 3
                with
                | :? Exception as e ->
                    printfn "[%d]: %s" i line
                    raise e
            if state = Indices then
                let values = line.Split() |> Array.filter (contains_number) |> Array.map (fun x -> UInt32.Parse x)
                try
                    if values.Length = 3 then
                        indices[ii + 0] <- values[0]
                        indices[ii + 1] <- values[1]
                        indices[ii + 2] <- values[2]
                        ii <- ii + 3
                with
                | :? Exception as e ->
                    printfn "[%d]: %s" i line
                    raise e
        let colors =
            let c = Array.zeroCreate<float32> (vertex_count * 4)
            for i in 0..4..c.Length - 5 do
                c[i + 0] <- r
                c[i + 1] <- g
                c[i + 2] <- b
                c[i + 3] <- a
            c
        let (_,vertices) = (3,positions) |> compose (3,normals) |> compose (4,colors)
        (vertices,indices)


    let load_txt_unmanaged (path:string, r:float32, g:float32, b:float32, a:float32) =
        let (vertices,indices) = load_txt(path,r,g,b,a)
        {
            vertices = NativeArray.ofArray vertices
            indices  = NativeArray.ofArray indices
            L = 10
        }

    let tranform (transform:Matrix4x4) (mesh:MeshF) = 
        let vertices = cast<float32> mesh.vertices.Ptr
        let L = 10
        let len = mesh.vertices.Length / L
        for i in 0..len-1 do
            let p = cast<Vector3>(~~(vertices ++ (i*L)))
            let t = Vector3.Transform(!!p, transform)
            FSharp.NativeInterop.NativePtr.write p t
        mesh


    // let inline is_clamped v1 v v2 =
    //     v > v1 && v < v2

    // calculates the bounds of a ControlVolume (CV) with SIMD intrisics
    // let bounds_SIMD (vertices:ReadOnlySpan<float32>) L =
    //     let vertices_count = vertices.Length / L
    //     let p = &MemoryMarshal.GetReference(vertices)
    //     let mutable v_min = Unsafe.As<float32,Vector3>(&p)
    //     let mutable v_max = Unsafe.As<float32,Vector3>(&p)

    //     for i in 0..vertices_count-1 do
    //         let v = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, i*L))
    //         v_min <- Vector3.Min(v, v_min)
    //         v_max <- Vector3.Max(v, v_max)
            
    //     {v_min = v_min; v_max = v_max} 


    // let bounds_union (cv1:CVBounds) (cv2:CVBounds) =
    //     let v_min = Vector3.Min(cv1.v_min, cv2.v_min)
    //     let v_max = Vector3.Max(cv1.v_max, cv2.v_max)
    //     {v_min = v_min; v_max = v_max} 


    // let inline triangle_center_SIMD (a:inref<Vector3>) (b:inref<Vector3>) (c:inref<Vector3>) =
    //     let v_min = Vector3.Min(a, Vector3.Min(b,c))
    //     let v_max = Vector3.Max(a, Vector3.Max(b,c))
    //     v_min + (v_max - v_min) / 2.f


    /// Use this function to assign on a buffer
    let assign_particles_SIMD (cv:CVBounds, voxels:narray3d<bool>, particles:Span<float32>, L:int) =
        let I = voxels.I
        let J = voxels.J
        let K = voxels.K
        let N = voxels.I
        let n = float32 N
        let v_min = cv.v_min
        let v_max = cv.v_max
        let dv = (v_max - v_min) / n            
        let p = &&MemoryMarshal.GetReference(particles)
        let mutable i = 0
        for ix in 0..I-1 do
            for iy in 0..J-1 do
                for iz in 0..K-1 do
                    if voxels[ix,iy,iz] then
                        let v = v_min + dv * Vector3(float32(ix), float32(iy), float32(iz))
                        let c = (v - v_min) / (dv * n)
                        Unsafe.Write<Vector3>(~~(p ++ i),v)
                        Unsafe.Write<Vector4>(~~(p ++ (i+3)), Vector4(c,1.f))
                        i <- i + L


    let particles_SIMD L (v:Voxels) =
        let particles_array = NativeArray.create<float32>(v.filled * L)
        assign_particles_SIMD(v.bounds, v.voxels, particles_array.AsSpan(), L)
        particles_array


