namespace SE.Renderer
open ImGuiNET
open OpenTK.Graphics
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics
open OpenTK.Windowing.Common
open OpenTK.Windowing.Desktop
open OpenTK.Windowing.GraphicsLibraryFramework
open System
open System.ComponentModel
open System.Diagnostics

// open Dear_ImGui_Sample.Backends

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

    /// deserializes the vertices and indices from a .txt file
    let load (path:string, r:float32, g:float32, b:float32, a:float32) =
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

