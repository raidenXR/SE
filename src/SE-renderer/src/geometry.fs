namespace SE.Renderer
open ImGuiNET
open OpenTK.Graphics
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics
open OpenTK.Windowing.Common
open OpenTK.Windowing.Desktop
open OpenTK.Windowing.GraphicsLibraryFramework
open System
open System.Collections.Generic
open System.ComponentModel
open System.Diagnostics

// open Dear_ImGui_Sample.Backends

type [<Struct>] Triangle = {n0:Vector3; n1:Vector3; n2:Vector3}

type [<Struct>] Voxel = {c:Vector3; t:bool; data:float}

type MeshIterator(vertices:array<float32>, indices:array<uint32>) =
    let indices_count = indices.Length / 3
    let mutable i = -1
    
    interface IEnumerator<Triangle> with
        member this.Current with get() = 
            let i0 = int32 (indices[3 * i + 0])
            let i1 = int32 (indices[3 * i + 1])
            let i2 = int32 (indices[3 * i + 2])

            let v0 = Vector3(vertices[10 * i0 + 0], vertices[10 * i0 + 1], vertices[10 * i0 + 2])
            let v1 = Vector3(vertices[10 * i1 + 0], vertices[10 * i1 + 1], vertices[10 * i1 + 2])
            let v2 = Vector3(vertices[10 * i2 + 0], vertices[10 * i2 + 1], vertices[10 * i2 + 2])
            {n0 = v0; n1 = v1; n2 = v2}

        member this.Dispose() = ()

    interface Collections.IEnumerator with
        member this.Current with get() = null

        member this.MoveNext() =
            i <- i + 1
            i < indices_count
        
        member this.Reset() =
            i <- 0

type Model(vertices: array<float32>, indices: array<uint32>) =
    let mutable transform = Matrix4.Identity

    interface IEnumerable<Triangle> with
        member this.GetEnumerator() = new MeshIterator(vertices, indices)

    interface System.Collections.IEnumerable with
        member this.GetEnumerator() = null

    member this.Vertices with get() = vertices
    member this.Indices with get() = indices
    member this.Stride with get() = 10 * sizeof<float32>
    member this.Attrib0 with get() = 0 * sizeof<float32>
    member this.Attrib1 with get() = 3 * sizeof<float32>
    member this.Attrib2 with get() = 6 * sizeof<float32>
    member this.VerticesBufferSize with get() = vertices.Length * sizeof<float32>
    member this.IndicesBufferSize with get() = indices.Length * sizeof<uint32>

    member this.Transform with get() = transform and set(value) = transform <- value

    // member this.Print() =
    //     let v = vertices
    //     let i = indices
    //     let mutable j = 0
    //     while j < 100 do
    //         printfn "[%g, %g, %g], [%g, %g, %g]" v[j + 0] v[j + 1] v[j + 2] v[j + 3] v[j + 4] v[j + 5]
    //         j <- j + 10

    //     j <- 0
    //     while j < 30 do
    //         printfn "[%d, %d, %d]" i[j + 0] i[j + 1] i[j + 2]
    //         j <- j + 3

    // member this.SaveAsTxt(path:string) =
    //     use fs = System.IO.File.CreateText(path)
    //     fs.WriteLine("Vertices")
    //     let mutable n = 0
    //     for v in vertices do
    //         if n = 10 then
    //             fs.Write("\n")
    //             n <- 0
    //         fs.Write($"{v}, ")
    //         n <- n + 1
    //     fs.WriteLine("\nIndices")
    //     n <- 0
    //     for v in indices do
    //         if n = 3 then
    //             fs.Write("\n")
    //             n <- 0
    //         fs.Write($"{v}, ")
    //         n <- n + 1


module Geometry =
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
                let i0 = int32 (indices[3 * i + 0])
                let i1 = int32 (indices[3 * i + 1])
                let i2 = int32 (indices[3 * i + 2])
                
                let v0 = Vector3(positions[3 * i0 + 0], positions[3 * i0 + 1], positions[3 * i0 + 2])
                let v1 = Vector3(positions[3 * i1 + 0], positions[3 * i1 + 1], positions[3 * i1 + 2])
                let v2 = Vector3(positions[3 * i2 + 0], positions[3 * i2 + 1], positions[3 * i2 + 2])

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
                let normal = Vector3.Normalize(Vector3(n[3 * i + 0], n[3 * i + 1], n[3 * i + 2]))
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



    let cube_intersects (n:float32) (c:Vector3) (t:Triangle) =
        let ct (p:Vector3) =
            let d = c - p
            (d.X < n) && (d.Y < n) && (d.Z < n)
        (ct t.n0) || (ct t.n1) || (ct t.n2)

    let voxelize (resolution:int) (vertices:array<float32>) (indices:array<uint32>) =
        let mutable x_min = vertices[0]
        let mutable y_min = vertices[1]
        let mutable z_min = vertices[2]

        let mutable x_max = x_min
        let mutable y_max = y_min
        let mutable z_max = z_min

        for i in 0..10..vertices.Length - 10 do
            x_min <- min x_min vertices[i + 0]
            y_min <- min y_min vertices[i + 1]
            z_min <- min z_min vertices[i + 2]
            x_max <- max x_max vertices[i + 0]
            y_max <- max y_max vertices[i + 1]
            z_max <- max z_max vertices[i + 2]

        let dx = x_max - x_min
        let dy = y_max - y_min
        let dz = z_max - z_min
        let n = (min (min dx dy) dz) / float32(resolution)
        let indices_count = indices.Length / 3
        let voxels = Array.zeroCreate<Voxel> (resolution * resolution * resolution)        
        let model = Model(vertices, indices)

        for i in 0..resolution - 1 do
            for j in 0..resolution - 1 do
            let mutable voxel_state = false
            for mesh in (model :> System.Collections.Generic.IEnumerable<Triangle>) do
                let v0 = mesh.n0
                let v1 = mesh.n1
                let v2 = mesh.n2
            // for i in 0..indices_count - 1 do
            //     let i0 = int32 (indices[3 * i + 0])
            //     let i1 = int32 (indices[3 * i + 1])
            //     let i2 = int32 (indices[3 * i + 2])

            //     let v0 = Vector3(vertices[10 * i0 + 0], vertices[10 * i0 + 1], vertices[10 * i0 + 2])
            //     let v1 = Vector3(vertices[10 * i1 + 0], vertices[10 * i1 + 1], vertices[10 * i1 + 2])
            //     let v2 = Vector3(vertices[10 * i2 + 0], vertices[10 * i2 + 1], vertices[10 * i2 + 2])
            
                // if cube_intersects n c {n0 = v0; n1 = v1; n2 = v2} then voxel_state <- true

                for k in 0..resolution - 1 do
                    let x = float32(i) * n
                    let y = float32(j) * n
                    let z = float32(k) * n
                    let c = Vector3(x,y,z)


                    voxels[i * resolution * resolution + j * resolution + k] <- {c = c; t = voxel_state; data = 0.0}
        voxels
            
            
            

