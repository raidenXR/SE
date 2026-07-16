namespace SE.Core

open SE
open System
open System.Numerics
open System.Collections
open System.Runtime.InteropServices
open System.Runtime.CompilerServices
open FSharp.NativeInterop

module GridGeneration2D =
    
    let read_from_file path =
        let lines = System.IO.File.ReadAllLines(path)
        let vertices = ResizeArray<Vector2>(1000)

        for i in 0..lines.Length-1 do
            let values = lines[i].Split(',')
            if values.Length >= 2 then
                let x = Single.Parse(values[1])
                let y = Single.Parse(values[2])
                vertices.Add(Vector2(x,y))
        vertices.ToArray()

    let read_from_file_multiple path =
        let lines = System.IO.File.ReadAllLines(path)
        let mutable vertices = ResizeArray<Vector2>(1000)
        let blocks = ResizeArray<Vector2[]>(100)

        let mutable i = 0
        while i < lines.Length do
            if lines[i].Length > 10 then
                let values = lines[i].Split(',')
                if values.Length >= 2 then
                    let x = Single.Parse(values[1])
                    let y = Single.Parse(values[2])
                    vertices.Add(Vector2(x,y))
            else
                blocks.Add(vertices.ToArray())
                vertices.Clear()
                // vertices <- ResizeArray<Vector2>(1000)
                while lines[i].Length < 10 && i < lines.Length-1 do i <- i + 1

            i <- i + 1

        if vertices.Count > 0 then blocks.Add(vertices.ToArray())
        blocks.ToArray()
        
        // vertices <- ResizeArray<Vector2>(1000)
        // for block in blocks do
        //     for i in 0..block.Length-1 do vertices.Add(block[i])
        //     vertices.Add(block[0])
            
        // vertices.ToArray()        

    let center (a:Vector2) (b:Vector2) =
        let x = a.X + (b.X - a.X) / 2.f
        let y = a.Y + (b.Y - a.Y) / 2.f
        Vector2(x,y)

    let bounds (vertices:Vector2[]) =
        let mutable x_min = vertices[0].X  
        let mutable y_min = vertices[0].Y  
        let mutable x_max = vertices[0].X  
        let mutable y_max = vertices[0].Y  

        for i in 1..vertices.Length-1 do
            x_min <- min vertices[i].X x_min
            y_min <- min vertices[i].Y y_min
            x_max <- max vertices[i].X x_max
            y_max <- max vertices[i].Y y_max

        (Vector2(x_min,y_min), Vector2(x_max,y_max))

    let bounds_union (v1_min:Vector2) (v1_max:Vector2) (v2_min:Vector2) (v2_max:Vector2) =
        let x_min = min v1_min.X v2_min.X
        let y_min = min v1_min.Y v2_min.Y
        let x_max = max v1_max.X v2_max.X
        let y_max = max v1_max.Y v2_max.Y
        (Vector2(x_min,y_min), Vector2(x_max,y_max))

    let total_bounds (domains:array<Vector2[]>) =
        let (v0_min,v0_max) = bounds domains[0]
        let mutable v_min = v0_min
        let mutable v_max = v0_max
        
        for domain in domains do
            let (v1,v2) = bounds domain
            let (v_min',v_max') = bounds_union v1 v2 v_min v_max
            v_min <- v_min'
            v_max <- v_max'

        (v_min,v_max)
        

    let to_cartesian_system i j N (v_min:Vector2) (v_max:Vector2) =
        let dx = (v_max.X - v_min.X) / float32 N
        let dy = (v_max.Y - v_min.Y) / float32 N
        Vector2(float32 j * dx + v_min.X, float32 i * dy + v_min.Y)

    let to_stencil_system (N:int) (p:Vector2) (v_min:Vector2) (v_max:Vector2) =
        let i = int (Math.Round(float32(N-1)*(p.Y - v_min.Y) / (v_max.Y - v_min.Y) |> double, 0))
        let j = int (Math.Round(float32(N-1)*(p.X - v_min.X) / (v_max.X - v_min.X) |> double, 0))
        (i,j)

    // let rec assign_stencil_element (stencil:narray2d<byte>) N (v_min:Vector2) (v_max:Vector2) (a:Vector2) (b:Vector2) =
    //     let (ai, aj) = to_stencil_system N a v_min v_max
    //     let (bi, bj) = to_stencil_system N b v_min v_max

    //     if (abs(ai - bi) > 1 && abs(aj - bj) > 1) then
    //         assign_stencil_element stencil N v_min v_max a (center a b)
    //         assign_stencil_element stencil N v_min v_max (center a b) b
    //     else
    //         stencil.SetBit(ai,aj, true)
    //         stencil.SetBit(bi,bj, true)

    let rec assign_stencil_element (stencil:BitArray) N (v_min:Vector2) (v_max:Vector2) (a:Vector2) (b:Vector2) =
        let dx = (v_max.X - v_min.X) / float32 N
        let dy = (v_max.Y - v_min.Y) / float32 N
        
        if abs(a.Y - b.Y) > dy || abs(a.X - b.X) > dx then
            assign_stencil_element stencil N v_min v_max a (center a b)
            assign_stencil_element stencil N v_min v_max (center a b) b       
        else
            let c = center a b
            let (ci,cj) = to_stencil_system N c v_min v_max
            // stencil.SetBit(ci,cj, true)
            stencil[ci*N+cj] <- true

    let bitstencil (domain:Vector2[]) (N:int) =
        // if N % 8 <> 0 then failwith "N must be multiplicative of 8 / byte for bitstencil"
        let stencil = BitArray(N*N)
        let (v_min,v_max) = bounds domain

        for i in 0..domain.Length-2 do
            let a = domain[i+0]
            let b = domain[i+1]
            assign_stencil_element stencil N v_min v_max a b
        stencil
        
    let bitstencil_overwrite (domain:Vector2[]) (stencil:BitArray) N v_min v_max =
        for i in 0..domain.Length-2 do
            let a = domain[i+0]
            let b = domain[i+1]
            assign_stencil_element stencil N v_min v_max a b
        stencil

    let slice (N:int) v1 v2 (v_min:Vector2) (v_max:Vector2) (stencil:BitArray) =
        let sliced_stencil = BitArray(N*N)
        for i in 0..N-1 do
            for j in 0..N-1 do
                let v = to_cartesian_system i j N v1 v2
                if (v_min.X <= v.X && v.X <= v_max.X) &&
                    (v_min.Y <= v.Y && v.Y <= v_max.Y) then
                         sliced_stencil[i*N+j] <- stencil[i*N+j]
        sliced_stencil

    let reverse (N:int) (stencil:BitArray) =
        let reversed_stencil = BitArray(N*N)
        for i in 0..N-1 do
            for j in 0..N-1 do
                reversed_stencil[i*N+j] <- not stencil[i*N+j]
        reversed_stencil
        
    let measure_range (stencil:BitArray) N I = 
        let mutable lhs = 0
        let mutable rhs = N-1

        while not (stencil[I*N + lhs]) && lhs < N-1 do lhs <- lhs + 1  // advance
        while not (stencil[I*N + rhs]) && rhs > 1 do rhs <- rhs - 1  // advance
    
        while (stencil[I*N + lhs]) && lhs < N-1 do lhs <- lhs + 1  // advance
        while (stencil[I*N + rhs]) && rhs > 1 do rhs <- rhs - 1  // advance

        let a = min lhs rhs
        let b = max lhs rhs
        (a,b)

    let fill_line_check (stencil:BitArray) N I =
        let mutable b' = false
        let mutable j = 0
        while j < N && not b' do
            // if stencil[I*N+j] then b' <- true
            b' <- stencil[I*N+j]
            j <- j + 1
        b'

    let measure_marching_rows (stencil:BitArray) N I =
        let mutable n = 0
        let mutable j = 0
        while j < N do
            if stencil[I*N + j] then
                while stencil[I*N+j] do j <- j + 1  // advance
                n <- n + 1
                j <- j - 1
            j <- j + 1
        n

    let (|Even|Odd|Zero|) input =
        if input = 0 || input = 1 then Zero elif input % 2 = 0 then Even else Odd

    // let fill_bitstencil N (v_min:Vector2) (v_max:Vector2) (stencil:BitArray) =
    let fill_bitstencil N (stencil:BitArray) =
        let mutable i = 0
        while i < N do
            let (a,b) = measure_range stencil N i
            let mutable fill = fill_line_check stencil N i
            // let mutable fill = true

            let collisions = measure_marching_rows stencil N i
            match collisions with
            | Zero -> ()

            | Odd when i > 0 && i < N - 1 ->                
                for j in 0..N-1 do
                    let upper_row = stencil[(i-1)*N+j]
                    let lower_row = stencil[(i+1)*N+j]
                    stencil[i*N+j] <- stencil[i*N+j] || (upper_row || lower_row)
                    // stencil[i*N+j] <- stencil[(i-1)*N+j] // copy the upper row
                
            | Odd -> () // ignore first line, keep only the upper boundaries

            | Even when collisions = 2 && i > 0 && i < N-1 ->
                for j in 0..N-1 do
                    let upper_row = stencil[(i-1)*N+j]
                    let lower_row = stencil[(i+1)*N+j]
                    stencil[i*N+j] <- stencil[i*N+j] || (upper_row || lower_row)
                // for j in 0..N-1 do stencil[i*N+j] <- stencil[(i-1)*N+j] // copy the upper row
                
            | Even ->
                let mutable j = a
                while j <= b do
                    if stencil[i*N+j] then
                        while stencil[i*N+j] do j <- j + 1  // advance
                        j <- j - 1
                        fill <- not fill
                    
                    if fill then stencil[i*N+j] <- true
                    j <- j + 1
            i <- i + 1
        stencil
                

    let is_internal_node i j N (stencil:BitArray) =
        if (i < 1 || i > N - 1) || (j < 1 || j > N - 1) then
            false
        else
            let c = stencil[i*N+j]
            let u = stencil[(i-1)*N+j]
            let d = stencil[(i+1)*N+j]
            let l = stencil[i*N+(j-1)]
            let r = stencil[i*N+(j+1)]
            c && u && d && l && r
            
    let write_vertices (path:string) N (v_min:Vector2) (v_max:Vector2) (stencil:BitArray) =
        let sb = System.Text.StringBuilder(1024 * 1024 * 2)

        for i in 0..N-1 do
            for j in 1..N-1 do
                if stencil[i*N+j] && stencil[i*N+(j-1)] then
                    let p1 = to_cartesian_system i (j-1) N v_min v_max
                    let p2 = to_cartesian_system i (j-0) N v_min v_max
                    sb
                        .AppendLine($"{p1.X}  {p1.Y}")
                        .AppendLine($"{p2.X}  {p2.Y}")
                        .AppendLine("\n")
                        |> ignore
                    
        for i in 1..N-1 do
            for j in 0..N-1 do
                if stencil[i*N+j] && stencil[(i-1)*N+j] then
                    let p1 = to_cartesian_system (i-1) j N v_min v_max
                    let p2 = to_cartesian_system (i-0) j N v_min v_max
                    sb
                        .AppendLine($"{p1.X}  {p1.Y}")
                        .AppendLine($"{p2.X}  {p2.Y}")
                        .AppendLine("\n")
                        |> ignore

        use fs = System.IO.File.CreateText(path)
        fs.WriteLine(string sb)
        fs.Close()
    
    let write_stencil (path:string) (domain:Vector2[]) N (c1:char) (c2:char) =
        let stencil = bitstencil domain N
        use fs = System.IO.File.CreateText(path)

        for i=N-1 downto 0 do
            for j=0 to N-1 do
                if stencil[i*N+j] then fs.Write(c1)
                else fs.Write(c2)
            fs.Write('\n')



module GridGeneration3D =

    let center (a:Vector3) (b:Vector3) =
    // let center (a:Vector3) (b:Vector3) (c:Vector3) =
        let x = a.X + (b.X - a.X) / 2.f
        let y = a.Y + (b.Y - a.Y) / 2.f
        let z = a.Z + (b.Z - a.Z) / 2.f
        Vector3(x,y,z)

    let triangle_center a b c =
        let v_min = Vector3.Min(a, Vector3.Min(b,c))
        let v_max = Vector3.Max(a, Vector3.Max(b,c))
        v_min + (v_max - v_min) / 2.f

    let bounds (vertices:Vector3[]) =
        let mutable x_min = vertices[0].X  
        let mutable y_min = vertices[0].Y  
        let mutable z_min = vertices[0].Z  
        let mutable x_max = vertices[0].X  
        let mutable y_max = vertices[0].Y  
        let mutable z_max = vertices[0].Z  

        for i in 1..vertices.Length-1 do
            x_min <- min vertices[i].X x_min
            y_min <- min vertices[i].Y y_min
            z_min <- min vertices[i].Z z_min
            x_max <- max vertices[i].X x_max
            y_max <- max vertices[i].Y y_max
            z_max <- max vertices[i].Z z_max

        (Vector3(x_min,y_min,z_min), Vector3(x_max,y_max,z_max))

    /// calculates the bounds of a ControlVolume (CV) with SIMD intrisics
    let bounds_SIMD (vertices:ReadOnlySpan<float32>) L =
        let vertices_count = vertices.Length / L
        let p = &MemoryMarshal.GetReference(vertices)
        let mutable v_min = Unsafe.As<float32,Vector3>(&p)
        let mutable v_max = Unsafe.As<float32,Vector3>(&p)

        for i in 0..vertices_count-1 do
            let v = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, i*L))
            v_min <- Vector3.Min(v, v_min)
            v_max <- Vector3.Max(v, v_max)
            
        (v_min,v_max) 

        
    let bounds_union (v1_min:Vector3) (v1_max:Vector3) (v2_min:Vector3) (v2_max:Vector3) =
        let x_min = min v1_min.X v2_min.X
        let y_min = min v1_min.Y v2_min.Y
        let x_max = max v1_max.X v2_max.X
        let y_max = max v1_max.Y v2_max.Y
        let z_min = min v1_min.Z v2_min.Z
        let z_max = max v1_max.Z v2_max.Z
        (Vector3(x_min,y_min,z_min), Vector3(x_max,y_max,z_min))

    let total_bounds (domains:array<Vector3[]>) =
        let (v0_min,v0_max) = bounds domains[0]
        let mutable v_min = v0_min
        let mutable v_max = v0_max
        
        for domain in domains do
            let (v1,v2) = bounds domain
            let (v_min',v_max') = bounds_union v1 v2 v_min v_max
            v_min <- v_min'
            v_max <- v_max'

        (v_min,v_max)
        
    
    let to_cartesian_system i j k N (v_min:Vector3) (v_max:Vector3) =
        let dx = (v_max.X - v_min.X) / float32 N
        let dy = (v_max.Y - v_min.Y) / float32 N
        let dz = (v_max.Z - v_min.Z) / float32 N
        Vector3(float32 j * dx + v_min.X, float32 i * dy + v_min.Y, float32 k * dz + v_min.Z)

    let to_stencil_system (N:int) (p:Vector3) (v_min:Vector3) (v_max:Vector3) =
        let i = int (Math.Round(float32(N-1)*(p.Y - v_min.Y) / (v_max.Y - v_min.Y) |> double, 0))
        let j = int (Math.Round(float32(N-1)*(p.X - v_min.X) / (v_max.X - v_min.X) |> double, 0))
        let k = int (Math.Round(float32(N-1)*(p.Z - v_min.Z) / (v_max.Z - v_min.Z) |> double, 0))
        (i,j,k)


    let rec assign_stencil_element (stencil:BitArray) N (v_min:Vector3) (v_max:Vector3) (a:Vector3) (b:Vector3) (c:Vector3) =
        let dx = (v_max.X - v_min.X) / float32 N
        let dy = (v_max.Y - v_min.Y) / float32 N
        let dz = (v_max.Z - v_min.Z) / float32 N
        
        // if abs(a.X - b.X) > dx || abs(a.Y - b.Y) > dy || abs(a.Z - b.Z) > dz ||
            // abs(a.X - c.X) > dx || abs(a.Y - c.Y) > dy || abs(a.Z - c.Z) > dz || 
        if abs(b.X - c.X) > dx || abs(b.Y - c.Y) > dy || abs(b.Z - c.Z) > dz then
            let ab = center a b
            let ac = center a c
            let bc = center b c
            assign_stencil_element stencil N v_min v_max a ab ac 
            assign_stencil_element stencil N v_min v_max ab b bc       
            assign_stencil_element stencil N v_min v_max ab bc ac       
            assign_stencil_element stencil N v_min v_max ac bc c     
        else
            let t = triangle_center a b c
            let (ci,cj,ck) = to_stencil_system N t v_min v_max
            stencil[ci*N*N+cj*N+ck] <- true


    // let bitstencil (vertices:Vector3[]) (indices:int[]) v_min v_max (N:int) =
    //     let stencil = BitArray(N*N*N)
    //     // let (v_min,v_max) = bounds vertices
    //     let indices_count = indices.Length / 3

    //     for i in 0..indices_count-1 do
    //         let i0 = indices[i*3+0]
    //         let i1 = indices[i*3+1]
    //         let i2 = indices[i*3+2]

    //         let p0 = vertices[i0]
    //         let p1 = vertices[i1]
    //         let p2 = vertices[i2]
            
    //         assign_stencil_element stencil N v_min v_max p0 p1 p2
    //     stencil

    let measure_range (stencil:BitArray) N I J = 
        let mutable lhs = 0
        let mutable rhs = N-1

        while not (stencil[I*N*N + J*N + lhs]) && lhs < N-1 do lhs <- lhs + 1  // advance
        while not (stencil[I*N*N + J*N + rhs]) && rhs > 1 do rhs <- rhs - 1  // advance
    
        while (stencil[I*N*N + J*N + lhs]) && lhs < N-1 do lhs <- lhs + 1  // advance
        while (stencil[I*N*N + J*N + rhs]) && rhs > 1 do rhs <- rhs - 1  // advance

        let a = min lhs rhs
        let b = max lhs rhs
        (a,b)
        
    let fill_line_check (stencil:BitArray) N I J =
        let mutable b' = false
        let mutable k = 0
        while k < N && not b' do
            if stencil[I*N*N+J*N+k] then b' <- true
            k <- k + 1
        b'

    let measure_marching_rows (stencil:BitArray) N I J =
        let mutable n = 0
        let mutable k = 0
        while k < N do
            if stencil[I*N*N+J*N+k] then
                while stencil[I*N*N+J*N+k] do k <- k + 1  // advance
                n <- n + 1
                k <- k - 1
            k <- k + 1
        n

    let (|Even|Odd|Zero|) input =
        if input = 0 || input = 1 then Zero elif input % 2 = 0 then Even else Odd

        
    let fill_bitstencil N (stencil:BitArray) =
        let mutable i = 0
        while i < N do
            let mutable j = 0
            while j < N do
                let (a,b) = measure_range stencil N i j
                let mutable fill = fill_line_check stencil N i j

                let collisions = measure_marching_rows stencil N i j
                match collisions with
                | Zero -> ()

                | Odd when i > 0 && i < N - 1 && j > 0 && j < N - 1 ->                
                    // printfn "Odd called"
                    for k in 0..N-1 do
                        let upper_row = stencil[(i-1)*N*N+(j-1)*N+k]
                        let lower_row = stencil[(i+1)*N*N+(j+1)*N+k]
                        stencil[i*N*N+j*N+k] <- stencil[i*N*N+j*N+k] || (upper_row || lower_row)
                
                | Odd -> () // ignore first line, keep only the upper boundaries

                | Even when collisions = 2 && i > 0 && j > 0 && i < N-1 && j < N-1 ->
                    // printfn "Even_when called"
                    for k in 0..N-1 do
                        let upper_row = stencil[(i-1)*N*N+(j-1)*N+k]
                        let lower_row = stencil[(i+1)*N*N+(j+1)*N+k]
                        stencil[i*N*N+j*N+k] <- stencil[i*N*N+j*N+k] || (upper_row || lower_row)
                
                | Even ->
                    let mutable k = a
                    while k <= b do
                        if stencil[i*N*N+j*N+k] then
                            while stencil[i*N*N+j*N+k] do k <- k + 1  // advance
                            // printfn "Even called"
                            k <- k - 1
                            fill <- not fill
                    
                        if fill then stencil[i*N*N+j*N+k] <- true
                        k <- k + 1
                j <- j + 1
            i <- i + 1
        stencil
                
    // let is_internal_node i j N (stencil:BitArray) =
    //     if (i < 1 || i > N - 1) || (j < 1 || j > N - 1) then
    //         false
    //     else
    //         let c = stencil[i*N+j]
    //         let u = stencil[(i-1)*N+j]
    //         let d = stencil[(i+1)*N+j]
    //         let l = stencil[i*N+(j-1)]
    //         let r = stencil[i*N+(j+1)]
    //         c && u && d && l && r
            
            
    /// creates a volume as voxels bool, where n is the resolution
    let assign_voxels_SIMD (vertices:Span<float32>) (indices:Span<uint>) L N (stencil:BitArray) =
        let indices_count = indices.Length / 3
        let p = &MemoryMarshal.GetReference(vertices)
        let (v_min,v_max) = bounds_SIMD vertices L 

        for i in 0..indices_count-1 do
            let i0 = int32 (indices[3*i+0])
            let i1 = int32 (indices[3*i+1])
            let i2 = int32 (indices[3*i+2])

            let v0 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i0))
            let v1 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i1))
            let v2 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i2))
            
            assign_stencil_element stencil N v_min v_max v0 v1 v2
        stencil

