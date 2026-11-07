namespace SE.Spatial

open System
open System.Numerics
open System.Threading
open System.Collections
open FSharp.Core
open System.Runtime.InteropServices
open System.Runtime.CompilerServices

module OctreeFlatten =
    open SE.Core

    type Flag =
        | Empty = 1uy
        | Leaf = 2uy
        | Node = 4uy

    type Bounds =
        | Undefined = 0uy
        | External = 1uy
        | Internal = 2uy
        | Boundary = 4uy
        

    [<Struct>]
    type Node<'T> =
        val parent: int
        val children: int
        val mutable value: 'T
        val mutable flag: Flag
        val mutable bounds: Bounds
        val level: byte
        val index: byte
        val v_min: Vector3
        val v_max: Vector3

        new(f, p, c, v, i, l, vmin, vmax) = {
            parent = p
            children = c
            value = v
            v_min = vmin
            v_max = vmax
            level = l
            index = i
            flag = f
            bounds = Bounds.Undefined
        }

        static member inline (!!) (n:Node<'T>) = n.value

        static member inline Empty =
            Node<'T>(Flag.Empty, -1, -1, Unchecked.defaultof<'T>, 0uy, 0uy, Vector3.Zero, Vector3.Zero)

        static member inline Leaf(p, v, i, l, vmin, vmax) =
            Node<'T>(Flag.Leaf, p, -1, v, i, l, vmin, vmax)

        static member inline Node(p, c, i, l, vmin, vmax) =
            Node<'T>(Flag.Node, p, c, Unchecked.defaultof<'T>, i, l, vmin, vmax)

        member inline n.Center =
            n.v_min + (n.v_max - n.v_min) / 2.f


    let intersect (p: Vector3) (v_min: Vector3) (v_max: Vector3) =
        let x = v_min.X <= p.X && p.X <= v_max.X
        let y = v_min.Y <= p.Y && p.Y <= v_max.Y
        let z = v_min.Z <= p.Z && p.Z <= v_max.Z
        x && y && z

        
    let private task_new (fn:unit -> unit) = System.Threading.Tasks.Task.Factory.StartNew(fn)

    let rec subdivide R N v_min v_max (bits:BitArray) (a:Vector3) (b:Vector3) (c:Vector3) =        
        if (b-a).Length() >= R || (a-c).Length() >= R || (c-b).Length() >= R then 
            let ab = GridGeneration3D.center a b
            let ac = GridGeneration3D.center a c
            let bc = GridGeneration3D.center b c
            subdivide R N v_min v_max bits a ab ac 
            subdivide R N v_min v_max bits ab b bc       
            subdivide R N v_min v_max bits ab bc ac       
            subdivide R N v_min v_max bits ac bc c     
        else
            let d = GridGeneration3D.triangle_center a b c
            let (ai,aj,ak) = GridGeneration3D.to_stencil_system N a v_min v_max
            let (bi,bj,bk) = GridGeneration3D.to_stencil_system N b v_min v_max
            let (ci,cj,ck) = GridGeneration3D.to_stencil_system N c v_min v_max
            let (di,dj,dk) = GridGeneration3D.to_stencil_system N d v_min v_max
            bits[ai*N*N+aj*N+ak] <- true
            bits[bi*N*N+bj*N+bk] <- true
            bits[ci*N*N+cj*N+ck] <- true
            bits[di*N*N+dj*N+dk] <- true

    let fill_scanlines N L (v_min:Vector3) (v_max:Vector3) (vertices:Span<float32>) (indices:Span<uint>) (bits:BitArray) =
        let dx = (v_max.X - v_min.X) / float32 N
        let dy = (v_max.Y - v_min.Y) / float32 N
        let dz = (v_max.Z - v_min.Z) / float32 N
        let dr = Vector3(dx,dy,dz)
        let R = dr.Length()
        let center = GridGeneration3D.center

        let indices_count = indices.Length / 3
        let p = &MemoryMarshal.GetReference(vertices)
        for i in 0..indices_count-1 do
            let i0 = int32 (indices[3*i+0])
            let i1 = int32 (indices[3*i+1])
            let i2 = int32 (indices[3*i+2])
            let v0 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i0))
            let v1 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i1))
            let v2 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i2))            
            subdivide R N v_min v_max bits v0 v1 v2
        
        for i in 0..N-1 do
            let mutable j = 0
            while j < N do
                let (a,b) = GridGeneration3D.measure_range bits N i j
                let mutable fill = GridGeneration3D.fill_line_check bits N i j

                let collisions = GridGeneration3D.measure_marching_rows bits N i j
                match collisions with
                | GridGeneration3D.Zero -> ()

                | GridGeneration3D.Odd when i > 0 && i < N - 1 && j > 0 && j < N - 1 ->                
                    for k in 0..N-1 do
                        let upper_row = bits[(i-1)*N*N+(j-1)*N+k]
                        let lower_row = bits[(i+1)*N*N+(j+1)*N+k]
                        bits[i*N*N+j*N+k] <- bits[i*N*N+j*N+k] || (upper_row || lower_row)
            
                | GridGeneration3D.Odd -> () // ignore first line, keep only the upper boundaries
            
                | GridGeneration3D.Even ->
                    let mutable k = a
                    while k <= b do
                        if bits[i*N*N+j*N+k] then
                            while bits[i*N*N+j*N+k] do k <- k + 1  // advance
                            k <- k - 1
                            fill <- not fill
                
                        if fill then bits[i*N*N+j*N+k] <- true
                        k <- k + 1
                j <- j + 1
        bits

    let fill_scanlines_2 N L (v_min: Vector3) (v_max: Vector3)
        (vertices: Span<float32>) (indices: Span<uint>) (bits: BitArray) =

        if N <= 0 then
            invalidArg "N" "Resolution must be positive."

        let rows = int64 N * int64 N

        if rows > int64 Int32.MaxValue / int64 N then
            invalidArg "N" "The stencil exceeds BitArray's indexing limit."

        if isNull bits || bits.Length <> int (rows * int64 N) then
            invalidArg "bits" "The stencil length must equal N*N*N."

        if L < 3 || vertices.Length % L <> 0 then
            invalidArg "vertices" "Vertex records must have stride L >= 3."

        if indices.Length % 3 <> 0 then
            invalidArg "indices" "Indices must contain complete triangles."

        let lo = [| double v_min.X; double v_min.Y; double v_min.Z |]
        let hi = [| double v_max.X; double v_max.Y; double v_max.Z |]

        for axis in 0 .. 2 do
            if not (Double.IsFinite lo[axis] && Double.IsFinite hi[axis])
               || hi[axis] <= lo[axis] then
                invalidArg "v_max" "Bounds must be finite and have positive extent."

        // Transform each vertex once into grid coordinates.
        let vertexCount = vertices.Length / L
        let points =
            Array.zeroCreate<struct (double * double * double)> vertexCount

        let scale =
            Array.init 3 (fun axis -> double N / (hi[axis] - lo[axis]))

        for vertex in 0 .. vertexCount - 1 do
            let offset = vertex * L

            for axis in 0 .. 2 do
                let value = double vertices[offset + axis]
                if not (Double.IsFinite value)
                   || value < lo[axis] || value > hi[axis] then
                    invalidArg "vertices"
                        "Positions must be finite and inside the bounds."

            points[vertex] <-
                struct (
                    (double vertices[offset] - lo[0]) * scale[0],
                    (double vertices[offset + 1] - lo[1]) * scale[1],
                    (double vertices[offset + 2] - lo[2]) * scale[2]
                )

        for position in 0 .. indices.Length - 1 do
            if uint64 indices[position] >= uint64 vertexCount then
                invalidArg "indices" "A vertex index is out of range."

        // Allocate intersection lists only for populated scanlines.
        let hits = Array.zeroCreate<ResizeArray<double>> (int rows)

        let firstSample x =
            max 0 (min N (int (ceil (x - 0.5))))

        let inline intersectEdge y ax ay bx by left right =
            if (ay <= y && y < by) || (by <= y && y < ay) then
                // Canonical direction gives shared edges consistent interpolation.
                let x =
                    if ay < by then
                        ax + (y - ay) * (bx - ax) / (by - ay)
                    else
                        bx + (y - by) * (ax - bx) / (ay - by)

                struct (min left x, max right x)
            else
                struct (left, right)

        for triangle in 0 .. indices.Length / 3 - 1 do
            let struct (ax, ay, az) = points[int indices[3 * triangle]]
            let struct (bx, by, bz) = points[int indices[3 * triangle + 1]]
            let struct (cx, cy, cz) = points[int indices[3 * triangle + 2]]

            let ux, uy, uz = bx - ax, by - ay, bz - az
            let vx, vy, vz = cx - ax, cy - ay, cz - az
            let determinant = ux * vy - uy * vx

            // Edge-on triangles do not contribute transverse Z crossings.
            if determinant <> 0.0 then
                let dzdx = (uz * vy - uy * vz) / determinant
                let dzdy = (ux * vz - uz * vx) / determinant

                let firstRow = firstSample (min ay (min by cy))
                let endRow = firstSample (max ay (max by cy))

                for i in firstRow .. endRow - 1 do
                    let y = double i + 0.5

                    let struct (l0, r0) =
                        intersectEdge y ax ay bx by
                            Double.PositiveInfinity Double.NegativeInfinity

                    let struct (l1, r1) =
                        intersectEdge y bx by cx cy l0 r0

                    let struct (left, right) =
                        intersectEdge y cx cy ax ay l1 r1

                    if left < right then
                        let firstColumn = firstSample left
                        let endColumn = firstSample right

                        for j in firstColumn .. endColumn - 1 do
                            let z =
                                az
                                + dzdx * (double j + 0.5 - ax)
                                + dzdy * (y - ay)

                            if not (Double.IsFinite z) then
                                invalidOp "A triangle intersection is not finite."

                            let row = i * N + j

                            if isNull hits[row] then
                                hits[row] <- ResizeArray<double>()

                            hits[row].Add(z)

        // Validate before modifying the output. Preserve crossing multiplicity.
        for row in 0 .. hits.Length - 1 do
            let line = hits[row]

            if not (isNull line) then
                line.Sort()

                if line.Count % 2 <> 0 then
                    invalidOp (
                        $"Odd intersection count at scanline ({row / N}, {row % N}); "
                        + "check mesh closure or numerical degeneracies."
                    )

        bits.SetAll(false)

        for row in 0 .. hits.Length - 1 do
            let line = hits[row]

            if not (isNull line) then
                let offset = row * N

                for pair in 0 .. line.Count / 2 - 1 do
                    let first = firstSample line[2 * pair]
                    let finish = firstSample line[2 * pair + 1]

                    for k in first .. finish - 1 do
                        bits[offset + k] <- true

        bits




    type Octree<'T>(N: int, k: int, v_min: Vector3, v_max: Vector3) =
        let mutable nodes = Array.zeroCreate<Node<'T>>(N * N * 8)
        // let c_idx_pool = System.Collections.Generic.Queue<int>(N)
        let c_idx_pool = System.Collections.Generic.Stack<int>(N)
        let mutable capacity = N * N * 8
        let mutable count = 0
        // let mutable cached_node = 0
        let mutable cached_node = new ThreadLocal<int>()
        let mutable stencil: BitArray = null
        let n' = log10 (float N) / log10 2. |> ceil |> int |> byte

        let add_children () =
            let mutable c = -1
            // if c_idx_pool.TryDequeue(&c) then
            match c_idx_pool.TryPop(&c) with
            | true -> c
            | false ->
                c <- count

                if count + 8 >= capacity then
                    Array.Resize(&nodes, capacity + (N * N * 8))
                    capacity <- capacity + (N * N * 8)

                for i in count .. (count + 7) do
                    nodes[i] <- Node<'T>.Empty

                count <- count + 8
                c

        let children ni =
            nodes.AsSpan(nodes[ni].children, 8)

        do
            nodes[0] <- Node<'T>.Node(-1, 1, 0uy, 0uy, v_min, v_max)
            count <- 1
            add_children () |> ignore

        let count_t fn =
            let rec count_from ni =
                let n = nodes[ni]

                match n.flag with
                | Flag.Empty ->
                    0

                | Flag.Leaf ->
                    if fn n.flag then 1 else 0

                | Flag.Node ->
                    let mutable total = if fn n.flag then 1 else 0

                    for ci in n.children .. (n.children + 7) do
                        total <- total + count_from ci

                    total
                | flag -> invalidOp $"{flag} is not defined"

            count_from 0


        let Empty = Node<'T>.Empty
        let Leaf = Node<'T>.Leaf
        let Node = Node<'T>.Node

        
        /// Iterates all reachable leaf nodes.
        let rec iter (fn: Octree<'T> -> int -> unit) (t:Octree<'T>) ni =
            let n = nodes[ni]

            match n.flag with
            | Flag.Node ->
                for ci in n.children .. (n.children + 7) do
                    iter fn t ci

            | Flag.Leaf ->
                fn t ni

            | Flag.Empty ->
                ()
            | flag -> invalidOp $"{flag} is not defined"


        /// Inserts a node without trimming.
        let rec insert (p: Vector3) n' ni =
            let n = nodes[ni]

            match n.flag with
            | Flag.Empty ->
                failwith "traversed to empty node, make sure that root is not out of bounds"

            | Flag.Leaf when not (intersect p n.v_min n.v_max) ->
                insert p n' n.parent

            | Flag.Leaf ->
                ni

            | Flag.Node when not (intersect p n.v_min n.v_max) ->
                insert p n' n.parent

            | Flag.Node ->
                let mutable v_min = n.v_min
                let mutable v_max = n.v_max
                let mutable idx = 0

                let o = v_min + (v_max - v_min) / 2f
                idx <- idx + if p.X < o.X then 0 else 1
                idx <- idx + if p.Y > o.Y then 0 else 2
                idx <- idx + if p.Z < o.Z then 0 else 4

                match idx with
                | 0 ->
                    v_min <- Vector3(v_min.X, o.Y, v_min.Z)
                    v_max <- Vector3(o.X, v_max.Y, o.Z)
                | 1 ->
                    v_min <- Vector3(o.X, o.Y, v_min.Z)
                    v_max <- Vector3(v_max.X, v_max.Y, o.Z)
                | 2 ->
                    v_min <- Vector3(v_min.X, v_min.Y, v_min.Z)
                    v_max <- Vector3(o.X, o.Y, o.Z)
                | 3 ->
                    v_min <- Vector3(o.X, v_min.Y, v_min.Z)
                    v_max <- Vector3(v_max.X, o.Y, o.Z)
                | 4 ->
                    v_min <- Vector3(v_min.X, o.Y, o.Z)
                    v_max <- Vector3(o.X, v_max.Y, v_max.Z)
                | 5 ->
                    v_min <- Vector3(o.X, o.Y, o.Z)
                    v_max <- Vector3(v_max.X, v_max.Y, v_max.Z)
                | 6 ->
                    v_min <- Vector3(v_min.X, v_min.Y, o.Z)
                    v_max <- Vector3(o.X, o.Y, v_max.Z)
                | 7 ->
                    v_min <- Vector3(o.X, v_min.Y, o.Z)
                    v_max <- Vector3(v_max.X, o.Y, v_max.Z)
                | _ ->
                    failwith "improper idx value"

                let c_idx = n.children + idx

                match nodes[c_idx].flag with
                | Flag.Empty when n.level >= n' ->
                    nodes[c_idx] <- Leaf (ni, Unchecked.defaultof<'T>, byte idx, n', v_min, v_max)

                | Flag.Empty ->
                    let childStart = add_children ()
                    nodes[c_idx] <- Node (ni, childStart, byte idx, n.level + 1uy, v_min, v_max)

                | _ ->
                    ()

                insert p n' c_idx
            | flag -> invalidOp $"{flag} is not defined"


        let forall fn ni =
            let mutable result = true

            for child in children ni do
                result <- result && fn child.flag

            result

        let filledBranch (node: Node<'T>) =
            node.flag.HasFlag Flag.Leaf
            && forall (fun f -> f.HasFlag Flag.Leaf) node.parent

        let lowestBrach (node: Node<'T>) =
            node.flag.HasFlag Flag.Leaf
            && not (filledBranch node)
            && forall (fun f -> not (f.HasFlag Flag.Node)) node.parent

        /// Converts a leaf's parent to a leaf, while retaining the internal root.
        let rec trim n' k v ni =
            let n = nodes[ni]

            match n.flag with
            | Flag.Leaf when (lowestBrach n && n' = n.level) || (filledBranch n && int n' - int n.level < k) ->
                let p = n.parent

                if p = 0 then
                    ni
                else
                    let parent = nodes[p]
                    let grandparent = parent.parent
                    let i = int parent.index
                    let mutable value: 'T = Unchecked.defaultof<'T>

                    for child in children p do
                        if child.flag = Flag.Leaf then
                            value <- child.value

                    // Clear the children that the collapsed leaf will no longer reference.
                    for ci in parent.children .. (parent.children + 7) do
                        nodes[ci] <- Empty
                    // c_idx_pool.Enqueue(parent.children)
                    c_idx_pool.Push(parent.children)

                    let collapsed = nodes[grandparent].children + i
                    nodes[collapsed] <- Leaf (grandparent, value, byte i, parent.level, parent.v_min, parent.v_max)
                    collapsed

            | Flag.Empty ->
                failwith "tried to trim Empty Node"

            | _ -> ni
  

        /// convert a Leaf to Node
        let rec dense n' ni =
            let n = nodes[ni]
            
            match n.flag with
            | Flag.Leaf  when n.level < n' ->
                let _c = add_children()
                let _value = n.value
                let i = n.index
                let l = n.level
                let p = n.parent
                let v1 = n.v_min
                let v2 = n.v_max
                nodes[ni] <- Node (p, _c, i, l, v1, v2)
                let _children = children ni

                let o = n.Center
                do
                    let v_min = Vector3(v1.X, o.Y, v1.Z)            
                    let v_max = Vector3(o.X, v2.Y, o.Z) 
                    _children[0] <- Leaf (ni, _value, 0uy, (l+1uy), v_min, v_max)
                do
                    let v_min = Vector3(o.X, o.Y, v1.Z)            
                    let v_max = Vector3(v2.X, v2.Y, o.Z) 
                    _children[1] <- Leaf (ni, _value, 1uy, (l+1uy), v_min, v_max)
                do
                    let v_min = Vector3(v1.X, v1.Y, v1.Z)            
                    let v_max = Vector3(o.X, o.Y, o.Z) 
                    _children[2] <- Leaf (ni, _value, 2uy, (l+1uy), v_min, v_max)
                do
                    let v_min = Vector3(o.X, v1.Y, v1.Z)            
                    let v_max = Vector3(v2.X, o.Y, o.Z) 
                    _children[3] <- Leaf (ni, _value, 3uy, (l+1uy), v_min, v_max)

                do
                    let v_min = Vector3(v1.X, o.Y, o.Z)            
                    let v_max = Vector3(o.X, v2.Y, v2.Z) 
                    _children[4] <- Leaf (ni, _value, 4uy, (l+1uy), v_min, v_max)

                do
                    let v_min = Vector3(o.X, v1.Y, o.Z)            
                    let v_max = Vector3(v2.X, v2.Y, v2.Z) 
                    _children[5] <- Leaf (ni, _value, 5uy, (l+1uy), v_min, v_max)

                do
                    let v_min = Vector3(v1.X, v1.Y, o.Z)            
                    let v_max = Vector3(o.X, o.Y, v2.Z) 
                    _children[6] <- Leaf (ni, _value, 6uy, (l+1uy), v_min, v_max)

                do
                    let v_min = Vector3(o.X, v1.Y, o.Z)            
                    let v_max = Vector3(v2.X, o.Y, v2.Z) 
                    _children[7] <- Leaf (ni, _value, 7uy, (l+1uy), v_min, v_max)

                ni

            | Flag.Node ->
                for ci in n.children..(n.children+7) do dense n' ci |> ignore
                n.children

            | _ -> ni

        /// traverses the whole tree and trims / denses the quadants
        let rec update n' k ni (_trim:int -> bool) (_dense:int -> bool) (_set:int -> 'T) =
            let n = nodes[ni]
            match n.flag with
            | Flag.Leaf when filledBranch n ->
                if _trim ni then
                    trim n' k (_set ni) ni |> ignore

                elif _dense ni then
                    dense n' ni |> ignore

            | Flag.Node ->
                for ci in n.children..(n.children+7) do update n' k ci _trim _dense _set

            | _ -> ()


        let rec remove (p: Vector3) n' ni =
            let n = nodes[ni]

            match n.flag with
            | Flag.Empty ->
                false

            | Flag.Leaf when not (intersect p n.v_min n.v_max) ->
                remove p n' n.parent

            | Flag.Leaf ->
                let c = children n.parent
                let i = int n.index
                let mutable found = false
                let mutable j = 0

                while not found && j < c.Length do
                    found <- if i = j || c[j].flag = Flag.Empty then false else true
                    j <- j + 1

                match found with
                | true ->
                    c[i] <- Empty
                    true
                | false ->
                    c[i] <- Empty
                    remove p n' n.parent

            | Flag.Node when not (intersect p n.v_min n.v_max) ->
                remove p n' n.parent

            | Flag.Node ->
                let mutable v_min = n.v_min
                let mutable v_max = n.v_max
                let mutable idx = 0

                // let o = v_min + (v_max - v_min) / 2f
                let o = n.Center
                idx <- idx + if p.X < o.X then 0 else 1
                idx <- idx + if p.Y > o.Y then 0 else 2
                idx <- idx + if p.Z < o.Z then 0 else 4

                match idx with
                | 0 ->
                    v_min <- Vector3(v_min.X, o.Y, v_min.Z)
                    v_max <- Vector3(o.X, v_max.Y, o.Z)
                | 1 ->
                    v_min <- Vector3(o.X, o.Y, v_min.Z)
                    v_max <- Vector3(v_max.X, v_max.Y, o.Z)
                | 2 ->
                    v_min <- Vector3(v_min.X, v_min.Y, v_min.Z)
                    v_max <- Vector3(o.X, o.Y, o.Z)
                | 3 ->
                    v_min <- Vector3(o.X, v_min.Y, v_min.Z)
                    v_max <- Vector3(v_max.X, o.Y, o.Z)
                | 4 ->
                    v_min <- Vector3(v_min.X, o.Y, o.Z)
                    v_max <- Vector3(o.X, v_max.Y, v_max.Z)
                | 5 ->
                    v_min <- Vector3(o.X, o.Y, o.Z)
                    v_max <- Vector3(v_max.X, v_max.Y, v_max.Z)
                | 6 ->
                    v_min <- Vector3(v_min.X, v_min.Y, o.Z)
                    v_max <- Vector3(o.X, o.Y, v_max.Z)
                | 7 ->
                    v_min <- Vector3(o.X, v_min.Y, o.Z)
                    v_max <- Vector3(v_max.X, o.Y, v_max.Z)
                | _ ->
                    failwith "improper idx value"

                remove p n' (n.children + idx)

            | flag -> invalidOp $"{flag} is not defined"


        let traverse (p: Vector3) n' k ni =
            if not (intersect p nodes[0].v_min nodes[0].v_max) then
                invalidArg "p" "Point is outside the octree bounds."

            let rec walk ni =
                let n = nodes[ni]

                match n.flag with
                | Flag.Empty ->
                    failwith "traversed to empty node, make sure that root is not out of bounds"

                | Flag.Leaf when not (intersect p n.v_min n.v_max) ->
                    walk n.parent
                    |> trim n' k Unchecked.defaultof<'T>

                | Flag.Leaf ->
                    trim n' k Unchecked.defaultof<'T> ni

                | Flag.Node when not (intersect p n.v_min n.v_max) ->
                    walk n.parent
                    |> trim n' k Unchecked.defaultof<'T>

                | Flag.Node ->
                    let o = n.Center
                    let mutable idx = 0
                    idx <- idx + if p.X < o.X then 0 else 1
                    idx <- idx + if p.Y > o.Y then 0 else 2
                    idx <- idx + if p.Z < o.Z then 0 else 4

                    let mutable v_min = n.v_min
                    let mutable v_max = n.v_max

                    match idx with
                    | 0 ->
                        v_min <- Vector3(v_min.X, o.Y, v_min.Z)
                        v_max <- Vector3(o.X, v_max.Y, o.Z)
                    | 1 ->
                        v_min <- Vector3(o.X, o.Y, v_min.Z)
                        v_max <- Vector3(v_max.X, v_max.Y, o.Z)
                    | 2 ->
                        v_min <- Vector3(v_min.X, v_min.Y, v_min.Z)
                        v_max <- Vector3(o.X, o.Y, o.Z)
                    | 3 ->
                        v_min <- Vector3(o.X, v_min.Y, v_min.Z)
                        v_max <- Vector3(v_max.X, o.Y, o.Z)
                    | 4 ->
                        v_min <- Vector3(v_min.X, o.Y, o.Z)
                        v_max <- Vector3(o.X, v_max.Y, v_max.Z)
                    | 5 ->
                        v_min <- Vector3(o.X, o.Y, o.Z)
                        v_max <- Vector3(v_max.X, v_max.Y, v_max.Z)
                    | 6 ->
                        v_min <- Vector3(v_min.X, v_min.Y, o.Z)
                        v_max <- Vector3(o.X, o.Y, v_max.Z)
                    | 7 ->
                        v_min <- Vector3(o.X, v_min.Y, o.Z)
                        v_max <- Vector3(v_max.X, o.Y, v_max.Z)
                    | _ ->
                        failwith "improper idx value"

                    let c_idx = n.children + idx

                    match nodes[c_idx].flag with
                    | Flag.Empty when n.level >= n' ->
                        nodes[c_idx] <- Leaf (ni, Unchecked.defaultof<'T>, byte idx, n', v_min, v_max)

                    | Flag.Empty ->
                        let childStart = add_children ()
                        nodes[c_idx] <- Node (ni, childStart, byte idx, n.level + 1uy, v_min, v_max)

                    | _ -> ()

                    walk c_idx
                    |> trim n' k Unchecked.defaultof<'T>

                | flag -> invalidOp $"{flag} is not defined"

            walk ni

        let rec traverse_retain (p: Vector3) i =
            if i < 0 || i > count-1 then
                -1
            else
                let n = nodes[i]

                match n.flag with
                | Flag.Empty -> i

                | Flag.Leaf when not (intersect p n.v_min n.v_max) ->
                    traverse_retain p n.parent

                | Flag.Leaf -> i

                | Flag.Node when not (intersect p n.v_min n.v_max) ->
                    traverse_retain p n.parent

                | Flag.Node ->
                    let mutable idx = 0
                    let o = n.Center
                    idx <- idx + if p.X < o.X then 0 else 1
                    idx <- idx + if p.Y > o.Y then 0 else 2
                    idx <- idx + if p.Z < o.Z then 0 else 4

                    if idx < 0 || idx > 7 then
                        failwith "improper idx value"

                    let c_idx = n.children + idx

                    match nodes[c_idx].flag with
                    | Flag.Empty -> c_idx
                    | _ -> traverse_retain p c_idx

                | flag -> invalidOp $"{flag} is not defined"

        /// Supports one-cell displacement.
        let iterate_node i j k ni =
            if ni < 0 || ni > count-1 then
                -1
            else
                let n = nodes[ni]
                match n.flag with
                | _ when i = 0 && j = 0 && k = 0 && n.flag <> Flag.Empty ->
                    ni

                | Flag.Leaf ->
                    let c = n.Center
                    let dx = (n.v_max - n.v_min).X / 2.f
                    let dy = (n.v_max - n.v_min).Y / 2.f
                    let dz = (n.v_max - n.v_min).Z / 2.f
                    let dx' = float32 (sign i) * (dx * 1.125f)
                    let dy' = float32 (sign j) * (dy * 1.125f)
                    let dz' = float32 (sign k) * (dz * 1.125f)
                    let dv = Vector3(dx', dy', dz')

                    let mutable v = c + dv
                    let mutable I = abs i
                    let mutable J = abs j
                    let mutable K = abs k

                    while intersect v n.v_min n.v_max && (I > 0 || J > 0 || K > 0) do
                        v <- v + dv

                        if not (intersect v n.v_min n.v_max) then
                            I <- I - 1
                            J <- J - 1
                            K <- K - 1

                    traverse_retain v ni

                | Flag.Empty ->
                    failwith "run iterate on EMPTY node, failed"

                | flag -> invalidOp $"{flag} is not defined"


        let rec traverse_closest (p: Vector3) i =
            let n = nodes[i]

            match n.flag with
            | Flag.Empty ->
                failwith "MUST not traverse to EMPTY"

            | Flag.Leaf -> i

            | Flag.Node when not (intersect p n.v_min n.v_max) ->
                let c = children i
                let mutable j' = -1
                let mutable jj = 0
                let mutable d' = (p - n.Center).Length()

                for j in 0 .. 7 do
                    match c[j].flag with
                    | Flag.Node ->
                        let x' = (p - c[j].Center).Length()

                        if d' > x' then
                            d' <- x'
                            j' <- j

                    | Flag.Leaf ->
                        let x' = (p - c[j].Center).Length()

                        if d' > x' then
                            d' <- x'
                            j' <- j
                            jj <- j

                    | _ -> ()

                if j' > -1 then
                    traverse_closest p (n.children + j')
                else
                    n.children + jj

            | Flag.Node ->
                let c = children i
                let o = n.Center
                let mutable idx = 0
                idx <- idx + if p.X < o.X then 0 else 1
                idx <- idx + if p.Y > o.Y then 0 else 2
                idx <- idx + if p.Z < o.Z then 0 else 4

                if idx < 0 || idx > 7 then
                    failwith "improper idx value"

                match c[idx].flag with
                | Flag.Empty ->
                    let mutable j' = -1
                    let mutable jj = 0
                    let mutable d' = (p - n.Center).Length()

                    for j in 0 .. 7 do
                        match c[j].flag with
                        | Flag.Node ->
                            let x' = (p - c[j].Center).Length()

                            if d' > x' then
                                d' <- x'
                                j' <- j

                        | Flag.Leaf ->
                            let x' = (p - c[j].Center).Length()

                            if d' > x' then
                                d' <- x'
                                j' <- j
                                jj <- j

                        | _ -> ()

                    if j' > -1 then
                        traverse_closest p (n.children + j')
                    else
                        n.children + jj

                | _ ->
                    traverse_closest p (n.children + idx)

            | flag -> invalidOp $"{flag} is not defined"


        member this.Insert(p) =
            if not (intersect p nodes[0].v_min nodes[0].v_max) then
                invalidArg "p" "Point is outside the octree bounds."

            cached_node.Value <- insert p n' cached_node.Value

        member this.Put(p: Vector3, value: 'T) =
            let leaf = traverse p n' k cached_node.Value

            if nodes[leaf].flag <> Flag.Leaf then
                invalidOp "Put traversal did not return a leaf."

            nodes[leaf].value <- value
            cached_node.Value <- leaf

        member this.Update(_trim, _dense, _set) =
            update n' k 0 _trim _dense _set
            this.AssignFlags()

        member this.GetCount() =
            count_t ((=) Flag.Leaf)

        member this.GetTotalCount() =
            count_t ((<>) Flag.Empty)

        member this.GetBoundaryCount() =
            let mutable c = 0
            // iter (fun i -> c <- if nodes[i].bounds = Bounds.Boundary then 1 else 0) 0
            for i in 0..count-1 do
                c <- c + if nodes[i].bounds = Bounds.Boundary then 1 else 0
            c

        member this.GetInternalCount() =
            let mutable c = 0
            // iter (fun i -> c <- if nodes[i].bounds = Bounds.Internal then 1 else 0) 0
            for i in 0..count-1 do
                c <- c + if nodes[i].bounds = Bounds.Internal then 1 else 0
            c

        // member this.Iter(fn: Octree<'T> -> int -> unit) =
        //     iter fn this 0

        member this.Iter (fn: Octree<'T> -> int -> unit) =
            for i in 0..count-1 do
                if nodes[i].flag = Flag.Leaf then fn this i
                

        member this.IterParallel (fn: Octree<'T> -> int -> unit) =
            Threading.Tasks.Parallel.For(1, count, (fun i -> if nodes[i].flag = Flag.Leaf then fn this i))
            |> ignore

        member this.Item
            with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get idx =
                &nodes[idx]

        /// Uses greedy child-center traversal, not a global nearest-leaf search.
        member this.MapTo(p: Vector3) =
            traverse_closest p 0

        member this.Remove(p: Vector3) =
            if remove p n' cached_node.Value then
                cached_node.Value <- 0

        member this.Remove(x: double, y: double, z: double) =
            this.Remove(Vector3(float32 x, float32 y, float32 z))

        member this.Nodes = nodes.AsSpan(0, count)
        member this.Capacity = count
        member this.BufferSize = capacity
        member this.MaxLevel = n'
        member this.Rank = N
        member this.TrimLevel = k
        member this.Vmin = v_min
        member this.Vmax = v_max
        member this.Root = nodes[0]

        member this.Item with get(u,i,j,k) =
            let idx = iterate_node i j k u
            &nodes[idx]

        member this.Item with get(p:Vector3) =
            let idx = traverse_retain p cached_node.Value
            &nodes[idx]
            
            // if u < 0 || u > count - 1 then
            //     failwith "node cannot be Empty"
            // else
            //     let idx = iterate_node i j k u
            //     if idx < 0 || idx > count-1 then Empty else nodes[idx]

        member this.AssignFlags() =
            this.Iter (fun t ni ->
                if ni > -1 && ni < count then
                    let n = &nodes[ni]
                    let u = n.flag
                    let bi = iterate_node -1 0 0 ni
                    let di = iterate_node 0 -1 0 ni
                    let ki = iterate_node 0 0 -1 ni
                    let fi = iterate_node 0 +1 0 ni
                    let hi = iterate_node +1 0 0 ni
                    let ji = iterate_node 0 0 +1 ni

                    let b = if bi < 0 || bi > count-1 then Flag.Empty else nodes[bi].flag
                    let d = if di < 0 || di > count-1 then Flag.Empty else nodes[di].flag
                    let k = if ki < 0 || ki > count-1 then Flag.Empty else nodes[ki].flag
                    let f = if fi < 0 || fi > count-1 then Flag.Empty else nodes[fi].flag
                    let h = if hi < 0 || hi > count-1 then Flag.Empty else nodes[hi].flag
                    let j = if ji < 0 || ji > count-1 then Flag.Empty else nodes[ji].flag

                    if u = Flag.Empty then n.bounds <- Bounds.External
                    elif
                        u = Flag.Leaf && 
                        b = Flag.Leaf && 
                        d = Flag.Leaf && 
                        k = Flag.Leaf && 
                        f = Flag.Leaf && 
                        h = Flag.Leaf && 
                        j = Flag.Leaf then
                            n.bounds <- Bounds.Internal
                    else
                        n.bounds <- Bounds.Boundary                     
            )
        

        /// returns the Elements (their centers) of the quadtree as a list of points
        member this.AsPoints () =
            let points = ResizeArray<Vector3>(1000)
            for i in 0..count-1 do
                if nodes[i].flag = Flag.Leaf then 
                    points.Add(nodes[i].Center)
            points.ToArray()
        
        /// returns an array of all the elements values
        member this.GetValues () =
            let values = ResizeArray<'T>(1000)
            for i in 0..count-1 do
                if nodes[i].flag = Flag.Leaf then 
                    values.Add(nodes[i].value)
            values.ToArray()

        member this.Stencil with get() = stencil and set(value) = stencil <- value

            
    /// Builds a Quadtree out of a filled stencil
    /// The values of the Leafs are undefined
    let ofStencil<'T> N _k (v_min:Vector3) (v_max:Vector3) (stencil:BitArray) =
        let octree = Octree<'T>(N,_k,v_min,v_max)
        octree.Stencil <- stencil        
        for i in 0..N-1 do
            for j in 0..N-1 do
                for k in 0..N-1 do
                    if stencil[i*N*N+j*N+k] then
                        let v = GridGeneration3D.to_cartesian_system i j k N v_min v_max
                        octree.Put(v, Unchecked.defaultof<'T>)
        octree.AssignFlags()
        octree       

        
    let ofSurface<'T> (N:int) L k (vertices:Span<float32>) (indices:Span<uint>) =
        let (v_min,v_max) = GridGeneration3D.bounds_SIMD vertices L
        let bits = fill_scanlines N L v_min v_max vertices indices (BitArray(N*N*N))
        ofStencil<'T> N k v_min v_max bits        

    /// WARNING: It Disposes the unmanged resources of mesh
    // if you want to retain the resources in memory, use ofSurface<'T> instead
    let ofMesh<'T> N k (mesh:Mesh) =
        let vertices = mesh.vertices.AsSpan()
        let indices  = mesh.indices.AsSpan()
        let L = mesh.L 
        let (v_min,v_max) = GridGeneration3D.bounds_SIMD vertices L
        let bits = fill_scanlines N L v_min v_max vertices indices (BitArray(N*N*N))
        // mesh.Dispose()
        ofStencil<'T> N k v_min v_max bits        
        
    /// WARNING: It Disposes the unmanged resources of mesh
    // create octrees out of a collection of meshes with varying N,k in parallel!!
    let ofMeshes<'T> v_min v_max (meshes:array<int*int*Mesh>) =
        meshes
        |> Array.Parallel.map (fun (N,k,mesh) ->
            let vertices = mesh.vertices.AsSpan()
            let indices  = mesh.indices.AsSpan()
            let bits = fill_scanlines N mesh.L v_min v_max vertices indices (BitArray(N*N*N))
            // mesh.Dispose()
            ofStencil<'T> N k v_min v_max bits        
        )
                    


    /// Builds a Quadtree out of a filled stencil
    /// The values of the Leafs are undefined
    let ofStencil_2<'T> N _k (v_min:Vector3) (v_max:Vector3) (stencil:BitArray) =
        let octree = Octree<'T>(N,_k,v_min,v_max)
        octree.Stencil <- stencil        
        for i in 0..N-1 do
            for j in 0..N-1 do
                for k in 0..N-1 do
                    if stencil[i*N*N+j*N+k] then
                        let v =
                            Vector3(
                                v_min.X + (float32 j + 0.5f) * (v_max.X - v_min.X) / float32 N,
                                v_min.Y + (float32 i + 0.5f) * (v_max.Y - v_min.Y) / float32 N,
                                v_min.Z + (float32 k + 0.5f) * (v_max.Z - v_min.Z) / float32 N
                            )                    
                        octree.Put(v, Unchecked.defaultof<'T>)
        octree.AssignFlags()
        octree                       

    /// WARNING: It Disposes the unmanged resources of mesh
    // if you want to retain the resources in memory, use ofSurface<'T> instead
    let ofMesh_2<'T> N k (mesh:Mesh) =
        let vertices = mesh.vertices.AsSpan()
        let indices  = mesh.indices.AsSpan()
        let L = mesh.L 
        let (v_min,v_max) = GridGeneration3D.bounds_SIMD vertices L
        let bits = fill_scanlines_2 N L v_min v_max vertices indices (BitArray(N*N*N))
        // mesh.Dispose()
        ofStencil_2<'T> N k v_min v_max bits        
        
        
