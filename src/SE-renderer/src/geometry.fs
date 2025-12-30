namespace SE.Renderer
// open ImGuiNET
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


type Model(vertices: array<float32>, indices: array<uint32>, attribs:list<int>) =
    let sum n =
        let mutable s = 0
        for i in 1..n do s <- s + attribs[i-1]
        s
    let f32 = sizeof<float32>
    let mutable transform = Matrix4.Identity

    new(vertices:array<float32>, indices:array<uint32>) = Model(vertices, indices, [3;3;4])

    interface IEnumerable<Triangle> with
        member this.GetEnumerator() = new MeshIterator(this)

    interface System.Collections.IEnumerable with
        member this.GetEnumerator() = null

    member this.Vertices with get() = vertices
    member this.Indices with get() = indices
    member this.L with get() = List.sum attribs
    member this.Stride with get() = (List.sum attribs) * f32
    member this.Attrib0 with get() = (sum 0) * f32 
    member this.Attrib1 with get() = (sum 1) * f32 
    member this.Attrib2 with get() = (sum 2) * f32
    member this.Attrib3 with get() = (sum 3) * f32
    member this.Attrib4 with get() = (sum 4) * f32
    member this.VerticesBufferSize with get() = vertices.Length * sizeof<float32>
    member this.IndicesBufferSize with get() = indices.Length * sizeof<uint32>

    member this.Transform with get() = transform and set(value) = transform <- value

and MeshIterator(model:Model) =
    let vertices = model.Vertices
    let indices  = model.Indices
    let L = model.L
    let indices_count = indices.Length / 3
    let mutable i = -1
    
    interface IEnumerator<Triangle> with
        member this.Current with get() = 
            let i0 = int32 (indices[3 * i + 0])
            let i1 = int32 (indices[3 * i + 1])
            let i2 = int32 (indices[3 * i + 2])

            let v0 = Vector3(vertices[L*i0+0], vertices[L*i0+1], vertices[L*i0+2])
            let v1 = Vector3(vertices[L*i1+0], vertices[L*i1+1], vertices[L*i1+2])
            let v2 = Vector3(vertices[L*i2+0], vertices[L*i2+1], vertices[L*i2+2])
            {n0 = v0; n1 = v1; n2 = v2}

        member this.Dispose() = ()

    interface Collections.IEnumerator with
        member this.Current with get() = null

        member this.MoveNext() =
            i <- i + 1
            i < indices_count
        
        member this.Reset() =
            i <- -1


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

    let inline is_clamped v1 v v2 =
        v > v1 && v < v2

    /// gets the control volume that includes the includes the vertices
    let bounds (vertices:array<float32>) stride =
        let L = stride
        let mutable x_min = vertices[0]
        let mutable y_min = vertices[1]
        let mutable z_min = vertices[2]

        let mutable x_max = x_min
        let mutable y_max = y_min
        let mutable z_max = z_min

        let vertices_count = vertices.Length / L
        for i in 0..vertices_count - 1 do
            x_min <- min x_min vertices[L * i + 0]
            y_min <- min y_min vertices[L * i + 1]
            z_min <- min z_min vertices[L * i + 2]
            x_max <- max x_max vertices[L * i + 0]
            y_max <- max y_max vertices[L * i + 1]
            z_max <- max z_max vertices[L * i + 2]

        (Vector3(x_min,y_min,z_min), Vector3(x_max,y_max,z_max))        

        
    let inline triangle_center (t:Triangle) =
        let tx_min = min (min t.n0.X t.n1.X) t.n2.X
        let ty_min = min (min t.n0.Y t.n1.Y) t.n2.Y
        let tz_min = min (min t.n0.Z t.n1.Z) t.n2.Z
        let tx_max = max (max t.n0.X t.n1.X) t.n2.X
        let ty_max = max (max t.n0.Y t.n1.Y) t.n2.Y
        let tz_max = max (max t.n0.Z t.n1.Z) t.n2.Z

        Vector3(tx_min + (tx_max - tx_min) / 2.f, ty_min + (ty_max - ty_min) / 2.f, tz_min + (tz_max - tz_min) / 2.f)
        

    /// creates a volume as voxels bool, where n is the resolution
    let assign_voxels (model:Model) (n:int) (voxels:bool array3d) =
        let vertices = model.Vertices
        let indices  = model.Indices
        let stride   = model.L
        let v_min,v_max = bounds vertices stride 
        let x_min = v_min.X
        let y_min = v_min.Y
        let z_min = v_min.Z
        let x_max = v_max.X
        let y_max = v_max.Y
        let z_max = v_max.Z
        let dx = x_max - x_min
        let dy = y_max - y_min
        let dz = z_max - z_min
        let n_size = (min (min dx dy) dz) / float32(n)
        let mutable total_filled_voxels = 0

        for mesh in (model :> IEnumerable<Triangle>) do
            let v_center = triangle_center mesh
            let ix = (float32(n) * (v_center.X - x_min)) / dx |> int32  // normalize to [0..1] and convert to index [0..n]
            let iy = (float32(n) * (v_center.Y - y_min)) / dy |> int32
            let iz = (float32(n) * (v_center.Z - z_min)) / dz |> int32
            voxels[Math.Clamp(ix,0,n-1), Math.Clamp(iy,0,n-1), Math.Clamp(iz,0,n-1)] <- true

        let hn = if n % 2 <> 0 then n / 2 else n / 2 + 1

        for ix in 0..n-1 do
            for iy in 0..n-1 do       
                let mutable fill = false
                for iz in 0..hn-1 do
                    if voxels[ix,iy,iz] && not fill then
                        fill <- true
                        total_filled_voxels <- total_filled_voxels + 1
                    elif voxels[ix,iy,iz] then
                        fill <- false
                        total_filled_voxels <- total_filled_voxels + 1
                    elif fill then
                        voxels[ix,iy,iz] <- true
                        total_filled_voxels <- total_filled_voxels + 1

                fill <- false
                for iz=n-1 downto hn do
                    if voxels[ix,iy,iz] && not fill then
                        fill <- true
                        total_filled_voxels <- total_filled_voxels + 1
                    elif voxels[ix,iy,iz] then
                        fill <- false
                        total_filled_voxels <- total_filled_voxels + 1
                    elif fill then
                        voxels[ix,iy,iz] <- true
                        total_filled_voxels <- total_filled_voxels + 1
        total_filled_voxels                 


    let as_voxels (model:Model) (n:int) =
        let voxels = Array3D.zeroCreate<bool> n n n
        let t_filled = assign_voxels model n voxels
        (voxels,t_filled)

    let assign_particles (v_min:Vector3) (v_max:Vector3) (voxels:bool array3d) (particles:array<float32>) t n stride =
        let x_min = v_min.X
        let y_min = v_min.Y
        let z_min = v_min.Z
        let x_max = v_max.X
        let y_max = v_max.Y
        let z_max = v_max.Z
        let dx = (x_max - x_min) / float32(n)
        let dy = (y_max - y_min) / float32(n)
        let dz = (z_max - z_min) / float32(n)
            
        let mutable i = 0
        for ix in 0..n-1 do
            for iy in 0..n-1 do
                for iz in 0..n-1 do
                    let x = x_min + dx * float32(ix)
                    let y = y_min + dy * float32(iy)
                    let z = z_min + dz * float32(iz)
                    if voxels[ix,iy,iz] then
                        particles[i+0] <- x
                        particles[i+1] <- y
                        particles[i+2] <- z
                        particles[i+3] <- (x - x_min) / (dx * float32(n))
                        particles[i+4] <- (y - y_min) / (dy * float32(n))
                        particles[i+5] <- (z - z_min) / (dz * float32(n))
                        particles[i+6] <- 1.f
                        i <- i + stride

    /// gets a float32 array ready to be rendered from the shader 7-stride
    let get_particles (v_min:Vector3) (v_max:Vector3) (voxels:bool array3d) t n stride =
        let particles = Array.zeroCreate<float32> (t * stride)
        assign_particles v_min v_max voxels particles t n stride
        particles           


type VoxelizedVolume<'T>(model:Model, resolution:int, f:Vector3 -> 'T) as this =
    let (V_min,V_max) = Geometry.bounds model.Vertices model.L
    let n = resolution
    // let (voxels,t) = Geometry.as_voxels model n 
    let voxels = Array3D.zeroCreate<bool> n n n
    // let offsets = Dictionary<struct(uint16*uint16),int>(n*n)
    let offsets = Dictionary<int,int>(n*n)
    // let values = ResizeArray<'T>(t)
    let values = Array.zeroCreate<'T> (n*n*n)
    let mutable t_filled = 0
    let mutable v_min = V_min
    let mutable v_max = V_max

    let reset () =
        offsets.Clear()
        for ix in 0..n-1 do
            for iy in 0..n-1 do
                for iz in 0..n-1 do
                    voxels[ix,iy,iz] <- false
        let (V_min,V_max) = Geometry.bounds model.Vertices model.L
        v_min <- V_min
        v_max <- V_max

    let init () =
        reset ()
        ignore (Geometry.assign_voxels model n voxels)
        t_filled <- 0 
        let mutable offset = 0
        for ix in 0..n-1 do
            for iy in 0..n-1 do
                let mutable b = false
                for iz in 0..n-1 do
                    if not voxels[ix,iy,iz] then
                        offset <- offset + 1
                    if voxels[ix,iy,iz] then
                        // let dx = uint16 ix
                        // let dy = uint16 iy
                        // offsets.Add(struct(dx,dy), offset)o
                        if not b then 
                            let key = (iy <<< 16) ||| ix
                            offsets.Add(key, offset)
                            b <- true
                        // if t_filled >= values.Count then
                        //     values.Add((f (this.Point(ix,iy,iz))))
                        // else
                        //     values[t_filled] <- f (this.Point(ix,iy,iz))
                        let idx = n*n*ix + n*iy + iz
                        if idx > values.Length then printfn "idx: %d, values_len: %d, capacity: %d" idx values.Length t_filled 
                        values[idx] <- f (this.Point(ix,iy,iz))
                        t_filled <- t_filled + 1

    do
        init ()
                                          
    member this.VoxelArray with get() = voxels

    member this.T_filled with get() = t_filled

    member this.Voxel(ix:int, iy:int, iz:int) = voxels[ix,iy,iz]
    
    member this.Point(ix:int, iy:int, iz:int) =
        let x_min = v_min.X
        let y_min = v_min.Y
        let z_min = v_min.Z
        let x_max = v_max.X
        let y_max = v_max.Y
        let z_max = v_max.Z
        let dx = (x_max - x_min) / float32(n)
        let dy = (y_max - y_min) / float32(n)
        let dz = (z_max - z_min) / float32(n)
        let x = x_min + dx * float32(ix)
        let y = y_min + dy * float32(iy)
        let z = z_min + dz * float32(iz)
        Vector3(x,y,z)        

    member this.Value(ix:int, iy:int, iz:int) =
        if not voxels[ix,iy,iz] then failwith "this index is false in voxels"
        // let dx = uint16 ix
        // let dy = uint16 iy
        // let offset = offsets[struct(dx,dy)]
        let key = (iy <<< 16) ||| ix
        // let idx = n*n*ix + n*iy + iz - offsets[key]
        // let idx = offsets[key] + iz
        // if idx > t_filled then Console.WriteLine("idx > t_filled, {0}, {1}", idx, t_filled)
        // if idx < 0 then Console.WriteLine("idx < 0, {0}", idx)
        let idx = n*n*ix + n*iy + iz
        values[idx]

    member this.Recompute(f: Vector3 -> 'T) =
        init ()        

    // member this.ApplyFn(f: Vector3 -> 'T) =
    //     let mutable i = 0
    //     for ix in 0..n-1 do
    //         for iy in 0..n-1 do
    //             for iz in 0..n-1 do
    //                 if voxels[ix,iy,iz] then
    //                     if i >= values.Count then
    //                         values.Add((f (this.Point(ix,iy,iz))))
    //                     else
    //                         values[i] <- f (this.Point(ix,iy,iz))
    //                     i <- i + 1

