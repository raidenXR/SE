namespace SE.Data
open SE
open SE.Core
open SE.ECS
open SE.Spatial
open System
open System.Collections
open Microsoft.Data.Sqlite

module Blob =        
    let bits (x:BitArray) =
        let bits = Array.zeroCreate<byte> (x.Count / (sizeof<byte> * 8))
        printfn "bitset: %d" x.Count
        x.CopyTo(bits, 0)
        bits

    let vertices L (vertices:narray<float32>) =
        let size = vertices.BufferSize
        let bits = Array.zeroCreate<byte> size
        use ptr = fixed bits
        System.Buffer.MemoryCopy(vertices.Ptr, ~~ptr, size, size)
        bits
    
    let indices (indices:narray<uint32>) =
        let size = indices.BufferSize
        let bits = Array.zeroCreate<byte> size
        use ptr = fixed bits
        System.Buffer.MemoryCopy(indices.Ptr, ~~ptr, size, size)
        bits


module Serialize =
    let stencils str (bits:seq<string*int*BitArray>) =
        use connection = new SqliteConnection($"Data Source={str}")
        connection.Open()

        use create = connection.CreateCommand()
        create.CommandText <- """
            DROP TABLE IF EXISTS stencils
        """
        create.ExecuteNonQuery() |> ignore
        
        create.CommandText <- """
            CREATE TABLE IF NOT EXISTS stencils (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                N INTEGER,
                bits BLOB NOT NULL
            );
        """
        create.ExecuteNonQuery() |> ignore
        for (name,N,stencil) in bits do
            use insert = connection.CreateCommand()
            insert.CommandText <- """
                INSERT INTO stencils (name, N, bits)
                VALUES ($name, $N, $bits)
            """

            insert.Parameters.AddWithValue("$name", name) |> ignore
            insert.Parameters.AddWithValue("$N", N) |> ignore
            insert.Parameters.AddWithValue("$bits", (Blob.bits stencil)) |> ignore
            insert.ExecuteNonQuery() |> ignore


    let meshes str (meshes:array<string*Mesh>) =
        use connection = new SqliteConnection($"Data Source={str}")
        connection.Open()

        use create = connection.CreateCommand()
        create.CommandText <- """
            DROP TABLE IF EXISTS meshes
        """
        create.ExecuteNonQuery() |> ignore

        create.CommandText <- """
            CREATE TABLE IF NOT EXISTS meshes (
                id INTEGER PRIMARY KEY,
                name TEXT NOT NULL,
                vertices BLOB NOT NULL,
                vertices_count INTEGER,
                indices BLOB NOT NULL,
                indices_count INTEGER,
                L INTEGER 
            );
        """
        create.ExecuteNonQuery() |> ignore

        for (name,mesh) in meshes do
            use insert = connection.CreateCommand()
            insert.CommandText <- """
                INSERT INTO meshes (name, vertices, vertices_count, indices, indices_count, L)
                VALUES ($name, $vertices, $vertices_count, $indices, $indices_count, $L)
            """

            insert.Parameters.AddWithValue("$name", name) |> ignore
            insert.Parameters.AddWithValue("$vertices", (Blob.vertices mesh.L mesh.vertices)) |> ignore
            insert.Parameters.AddWithValue("$vertices_count", mesh.vertices.Length) |> ignore
            insert.Parameters.AddWithValue("$indices", (Blob.indices mesh.indices)) |> ignore
            insert.Parameters.AddWithValue("$indices_count", mesh.indices.Length) |> ignore
            insert.Parameters.AddWithValue("$L", mesh.L) |> ignore
            insert.ExecuteNonQuery() |> ignore
        
    

module Deserialize =
    let meshes str =
        use connection = new SqliteConnection($"Data Source={str}")
        connection.Open()
        
        use query = connection.CreateCommand()
        query.CommandText <- """
            SELECT id, name, vertices, vertices_count, indices, indices_count, L FROM meshes
        """
        
        use reader = query.ExecuteReader()
        // seq {            
        [|
            while reader.Read() do
                let name = reader.GetString(1)
                let buf1 = reader.GetFieldValue<byte[]>(2)
                let vcount = reader.GetInt32(3)
                let buf2 = reader.GetFieldValue<byte[]>(4)
                let icount = reader.GetInt32(5)
                let L = reader.GetInt32(6)

                printfn "buf1: %d, vcount: %d" (buf1.Length / 4) vcount
                printfn "buf2: %d, icount: %d" (buf2.Length / 4) icount

                let vertices = alloc<float32> vcount
                let indices  = alloc<uint32>  icount
                let vsize = vcount * sizeof<float32>
                let isize = icount * sizeof<uint32>

                let ptr1 = fixed buf1
                let ptr2 = fixed buf2
                System.Buffer.MemoryCopy(~~ptr1, vertices, vsize, vsize)
                System.Buffer.MemoryCopy(~~ptr2, indices, isize, isize)
                
                yield {
                    vertices = new narray<float32>(vertices, vcount, false)
                    indices  = new narray<uint32>(indices, icount, false)
                    L = L
                }
        |]
        // } |> Array.ofSeq
        


    let stencils str =
        use connection = new SqliteConnection($"Data Source={str}")
        connection.Open()
        
        use query = connection.CreateCommand()
        query.CommandText <- """
            SELECT id, name, N, bits FROM stencils
        """
        
        use reader = query.ExecuteReader()
        [|
            while reader.Read() do
                let name = reader.GetString(1)
                let N = reader.GetInt32(2)
                // let buf0 = Array.zeroCreate<byte> N
                // reader.GetBytes(3, 0, buf0, 0, N) |> ignore
                let buf0 = reader.GetFieldValue<byte[]>(3)

                (name,N,BitArray(buf0))
        |]
    
