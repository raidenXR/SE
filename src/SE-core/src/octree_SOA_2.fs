namespace SE.Core

open SE
open System
open System.Numerics
open System.Collections
open System.Threading
open FSharp.Core
open System.Runtime.InteropServices
open System.Runtime.CompilerServices
open System.Collections.Concurrent

module OctreeSOA_2 =
    
    [<Flags>]
    type Flag =
        | Empty = 0uy
        | Node = 1uy
        | Leaf = 2uy

    [<Struct>]
    type NodeId =
        struct
            val s: int
            val i: byte
            val l: byte
            val f: Flag

            new(s,i,l,f) = {s=s; i=i; l=l; f=f}

            static member Empty = NodeId(-1, 0uy, 0uy, Flag.Empty)
        end

    [<Struct>]
    type Data<'T> =
        | Value of v:'T
        | Children of c:array<NodeId>
        | Empty

    // [<Literal>]
    let _size = 1024*100

    let buffer<'T> () = ResizeArray<'T>(_size)

    let rec dd n v _n = if _n < n then dd n (v/2.f) (_n + 1) else v

    type Root<'T>(N:int, k:int, v_min:Vector3, v_max:Vector3) =
        let n_ids = ConcurrentStack<NodeId>()
        let mutable is_disposed = false

        let mutable cached_node = new ThreadLocal<NodeId>(fun _ -> NodeId.Empty)

        interface IDisposable with
            member this.Dispose() =
                if not is_disposed then
                    cached_node.Dispose()
                is_disposed <- true

        [<DefaultValue>] val mutable parents: ResizeArray<NodeId>
        [<DefaultValue>] val mutable targets: ResizeArray<Data<'T>>
        [<DefaultValue>] val mutable cbounds: ResizeArray<struct(Vector3*Vector3)>

        [<DefaultValue>] val mutable count: int
        [<DefaultValue>] val mutable root: NodeId
        [<DefaultValue>] val mutable n: int
        [<DefaultValue>] val mutable k: int

        member this.N_Ids = n_ids
        

        static member threadlock = new Object()

        static member Create(N, k, v_min, v_max) =
            let x = new Root<'T>(N, k, v_min, v_max)
            x.parents <- buffer()
            x.targets <- buffer()
            x.cbounds <- buffer()
            x.n <- log10 (float N) / log10 2. |> ceil |> int 
            x.k <- k        
            x.count <- 0
            x.root <- NodeId.Empty
            x

        member this.dX = dd this.n (v_max.X - v_min.X) 0   
        member this.dY = dd this.n (v_max.Y - v_min.Y) 0   
        member this.dZ = dd this.n (v_max.Z - v_min.Z) 0   
        member this.CachedNode with get() = cached_node and set value = cached_node <- value

        
    let create_node (tree:Root<'T>) =    
        let mutable i = NodeId.Empty
        match tree.N_Ids.TryPop(&i) with
        | true -> i.s
        | false -> 
            tree.parents.Add(NodeId.Empty)
            tree.targets.Add(Empty)
            tree.cbounds.Add(struct(Vector3.Zero,Vector3.Zero))
            let id = tree.count
            tree.count <- tree.count + 1
            id
                

    let inline center (tree:Root<'T>) (id:NodeId) =
        match id.f with
        | Flag.Node | Flag.Leaf ->
            let struct(v_min,v_max) = tree.cbounds[id.s]
            v_min + (v_max - v_min) / 2.f
        | _ ->
            failwith "MUST be NOT Empty"

    let inline valueof (tree:Root<'T>) (id:NodeId) =
        match id.f with
        | Flag.Leaf ->
            match tree.targets[id.s] with
            | Value t -> ValueSome t
            | Empty -> ValueNone
            | Children _ -> failwith "must be a leaf"
        | _ -> failwith "Must be leaf"
        
    let inline children (tree:Root<'T>) (id:NodeId) =
        match id.f with
        | Flag.Node ->
            match tree.targets[id.s] with
            | Children c -> c
            // | _ -> [||]
            | Value _ -> failwith "cannot take children from  Value"
            | Empty -> failwith "cannot take children from Empty"            
        | _ -> failwith "Must be leaf"

    let inline parent (tree:Root<'T>) (id:NodeId) =
        match id.f with
        | Flag.Leaf | Flag.Node -> tree.parents[id.s]
        | _ -> failwith "Must be leaf"


    let leaf (p:NodeId) v i l v_min v_max (tree:Root<'T>) =
        if p.f <> Flag.Node then failwith "leaf cannot have non-Node parent"
        let id = create_node tree
        tree.parents[id] <- p
        tree.targets[id] <- match v with | ValueSome t -> Value t | ValueNone -> Empty
        tree.cbounds[id] <- struct(v_min,v_max)
        NodeId(id, i, l, Flag.Leaf)
            
    let node p i l v_min v_max (tree:Root<'T>) =
        let id = create_node tree
        tree.parents[id] <- p
        tree.targets[id] <- Children (System.Buffers.ArrayPool<NodeId>.Shared.Rent(8))
        tree.cbounds[id] <- struct(v_min,v_max)            
        NodeId(id, i, l, Flag.Node)


    let rec destroy_node (tree:Root<'T>) (id:NodeId) =
        match id.f with
        | Flag.Node ->
            if id.l > 0uy then
                let p = tree.parents[id.s]
                let c = children tree id
                for i in 0..7 do
                    destroy_node tree c[i]
                (children tree p)[int id.i] <- NodeId.Empty
                System.Buffers.ArrayPool<NodeId>.Shared.Return(c)
                tree.N_Ids.Push(id)
                
        | Flag.Leaf ->
            if id.l > 0uy then
                let p = tree.parents[id.s]
                (children tree p)[int id.i] <- NodeId.Empty                
                tree.N_Ids.Push(id)

        | _ -> ()

             
        
    let rec vertices_to_points (points:ResizeArray<Vector3>) (id:NodeId) (tree:Root<'T>) =
        match tree.targets[id.s] with
        | Children c ->
            for i in 0..7 do vertices_to_points points c[i] tree
        | Value _ ->    
            points.Add(center tree id)            
        | Empty -> ()

    let intersect (p:Vector3) (cell:struct(Vector3*Vector3)) =
        let struct(v_min,v_max) = cell
        let x = v_min.X <= p.X && p.X <= v_max.X
        let y = v_min.Y <= p.Y && p.Y <= v_max.Y
        let z = v_min.Z <= p.Z && p.Z <= v_max.Z
        x && y && z


    let forall (fn: NodeId -> bool) (ids:NodeId[]) =
        let mutable b = ids.Length >= 8
        let mutable i = 0
        while i < 8 && b do
            b <- b && (fn ids[i])
            i <- i + 1
        b

    /// convert a Node to Leaf
    let rec trim n k v (tree:Root<'T>) (id:NodeId) : NodeId =
        match id.f with 
        | Flag.Leaf when n = id.l ->
            let p = tree.parents[id.s]
            if (forall (fun t -> match t.f with | Flag.Node -> false | _ -> true) (children tree p)) then 
                match p.f with
                | Flag.Node ->
                    let P = tree.parents[p.s]
                    let C = children tree p
                    let I  = p.i
                    let L  = p.l
                    let struct(V1,V2) = tree.cbounds[p.s]
                    let mutable value:voption<'T> = v
                    for i in 0..7 do
                        match C[i].f with
                        | Flag.Leaf ->
                            let _value = valueof tree C[i]
                            value <- if _value.IsSome then ValueSome _value.Value else value
                            destroy_node tree C[i]
                        | _ -> ()
                        
                    let _this = &(children tree P)[int I]
                    _this <- NodeId(_this.s, _this.i, _this.l, Flag.Leaf)
                    System.Buffers.ArrayPool<NodeId>.Shared.Return(children tree p)
                    tree.targets[p.s] <- if value.IsSome then Value (value.Value) else Empty
                    _this
                | _ -> id
            else
                id

        | Flag.Leaf when n - id.l < k ->
            let p = tree.parents[id.s]
            if (forall (fun t -> match t.f with | Flag.Leaf -> true | _ -> false) (children tree p)) then 
                match p.f with
                | Flag.Node ->
                    let P = tree.parents[p.s]
                    let C = children tree p
                    let I  = p.i
                    let L  = p.l
                    let struct(V1,V2) = tree.cbounds[p.s]
                    let mutable value: ValueOption<'T> = v
                    for i in 0..7 do
                        match C[i].f with
                        | Flag.Leaf ->
                            let _value = valueof tree C[i]
                            value <- if _value.IsSome then ValueSome _value.Value else value
                            destroy_node tree C[i]
                        | _ -> ()

                    let _this = &(children tree P)[int I]
                    _this <- NodeId(_this.s, _this.i, _this.l, Flag.Leaf)
                    System.Buffers.ArrayPool<NodeId>.Shared.Return(children tree p)
                    tree.targets[p.s] <- if value.IsSome then Value (value.Value) else Empty
                    _this
                | _ -> id
            else
                id
        | Flag.Empty ->
            failwith "tried to trim Empty Node"
        | _ -> id


    /// convert a Leaf to Node
    let rec dense n (id:NodeId) (tree:Root<'T>) : NodeId =
        match id.f with
        | Flag.Leaf when id.l < n ->
            let p = tree.parents[id.s]
            let v = valueof tree id
            let i  = id.i
            let l  = id.l
            let struct(v1,v2) = tree.cbounds[id.s]

            let _this = &(children tree p)[int id.i]
            _this <- NodeId(_this.s, _this.i, _this.l, Flag.Node)
            let c = System.Buffers.ArrayPool<NodeId>.Shared.Rent(8)
            tree.targets[_this.s] <- Children c

            let o = v1 + (v2 - v1) / 2f
            do
                let v_min = Vector3(v1.X, o.Y, v1.Z)            
                let v_max = Vector3(o.X, v2.Y, o.Z) 
                c[0] <- leaf _this v 0uy (l+1uy) v_min v_max tree
            do
                let v_min = Vector3(o.X, o.Y, v1.Z)            
                let v_max = Vector3(v2.X, v2.Y, o.Z) 
                c[1] <- leaf _this v 1uy (l+1uy) v_min v_max tree
            do
                let v_min = Vector3(v1.X, v1.Y, v1.Z)            
                let v_max = Vector3(o.X, o.Y, o.Z) 
                c[2] <- leaf _this v 2uy (l+1uy) v_min v_max tree
            do
                let v_min = Vector3(o.X, v1.Y, v1.Z)            
                let v_max = Vector3(v2.X, o.Y, o.Z) 
                c[3] <- leaf _this v 3uy (l+1uy) v_min v_max tree

            do
                let v_min = Vector3(v1.X, o.Y, o.Z)            
                let v_max = Vector3(o.X, v2.Y, v2.Z) 
                c[4] <- leaf _this v 4uy (l+1uy) v_min v_max tree

            do
                let v_min = Vector3(o.X, v1.Y, o.Z)            
                let v_max = Vector3(v2.X, v2.Y, v2.Z) 
                c[5] <- leaf _this v 5uy (l+1uy) v_min v_max tree

            do
                let v_min = Vector3(v1.X, v1.Y, o.Z)            
                let v_max = Vector3(o.X, o.Y, v2.Z) 
                c[6] <- leaf _this v 6uy (l+1uy) v_min v_max tree

            do
                let v_min = Vector3(o.X, v1.Y, o.Z)            
                let v_max = Vector3(v2.X, o.Y, v2.Z) 
                c[7] <- leaf _this v 7uy (l+1uy) v_min v_max tree

            _this

        | Flag.Node ->
            let c = children tree id
            for i in 0..7 do dense n c[i] tree |> ignore
            c[0]

        | _ -> id



    let rec traverse (p:Vector3) n k (tree:Root<'T>) (id:NodeId) : NodeId =
        match id.f with
        | Flag.Empty ->
            printfn "empty node: %d, %d, %d, %A" id.s id.i id.l id.f
            failwith "traversed to empty node, make sure that root is not out of bounds"

        | Flag.Leaf when not (intersect p tree.cbounds[id.s]) ->
            let parent = tree.parents[id.s]
            traverse p n k tree parent
            |> trim n k ValueNone tree
            
        | Flag.Leaf ->
            id
            |> trim n k ValueNone tree

        | Flag.Node when not (intersect p tree.cbounds[id.s]) ->
            let parent = tree.parents[id.s]
            traverse p n k tree parent
            |> trim n k ValueNone tree

        | Flag.Node ->   // traverse forward
            let l = id.l
            let struct(v1,v2) = tree.cbounds[id.s]
            let mutable v_min = v1
            let mutable v_max = v2
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

            let c = children tree id
            match c[idx].f with
            | Flag.Empty when l >= n ->
                c[idx] <- leaf id ValueNone (byte idx) n v_min v_max tree
                traverse p n k tree c[idx]
                |> trim n k ValueNone tree

            | Flag.Empty ->
                c[idx] <- node id (byte idx) (l+1uy) v_min v_max tree 
                traverse p n k tree c[idx]
                |> trim n k ValueNone tree
                
            | _ ->
                traverse p n k tree c[idx]
                |> trim n k ValueNone tree

        | _ -> failwith "invalid flag"


    let rec insert (p:Vector3) n (tree:Root<'T>) (id:NodeId) : NodeId =
        match id.f with
        | Flag.Empty ->
            printfn "empty node: %d, %d, %d, %A" id.s id.i id.l id.f
            failwith "traversed to empty node, make sure that root is not out of bounds"

        | Flag.Leaf when not (intersect p tree.cbounds[id.s]) ->
            let parent = tree.parents[id.s]
            insert p n tree parent
            
        | Flag.Leaf ->
            id

        | Flag.Node when not (intersect p tree.cbounds[id.s]) ->
            let parent = tree.parents[id.s]
            insert p n tree parent

        | Flag.Node ->   // traverse forward
            let l = id.l
            let struct(v1,v2) = tree.cbounds[id.s]
            let mutable v_min = v1
            let mutable v_max = v2
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

            let c = children tree id
            match c[idx].f with
            | Flag.Empty when l >= n ->
                c[idx] <- leaf id ValueNone (byte idx) n v_min v_max tree
                insert p n tree c[idx]

            | Flag.Empty ->
                c[idx] <- node id (byte idx) (l+1uy) v_min v_max tree 
                insert p n tree c[idx]
                
            | _ ->
                insert p n tree c[idx]

        | _ -> failwith "invalid flag"

    let rec traverse_retain (p:Vector3) (tree:Root<'T>) (id:NodeId) =
        match id.f with
        | Flag.Empty -> id

        | Flag.Leaf when not (intersect p tree.cbounds[id.s]) ->
            let parent = tree.parents[id.s]
            traverse_retain p tree parent
            
        | Flag.Leaf -> id

        | Flag.Node when not (intersect p tree.cbounds[id.s]) ->
            let parent = tree.parents[id.s]
            traverse_retain p tree parent

        | Flag.Node ->   // traverse forward
            let struct(v_min,v_max) = tree.cbounds[id.s]
            let mutable idx = 0
            let o = v_min + (v_max - v_min) / 2f
            idx <- idx + if p.X < o.X then 0 else 1
            idx <- idx + if p.Y > o.Y then 0 else 2
            idx <- idx + if p.Z < o.Z then 0 else 4

            if idx < 0 || idx > 7 then failwith "improper idx value"

            let c = children tree id 
            match c[idx].f with
            | Flag.Empty -> c[idx]                
            | _ -> traverse_retain p tree c[idx]
        | _ -> failwith "invalid flag"            
               

    let iterate_node i j k (tree:Root<'T>) (id:NodeId) =
        match id.f with
        | _ when i = 0 && j = 0 && k = 0 -> id

        | Flag.Leaf | Flag.Node ->
            let struct(v_min,v_max) = tree.cbounds[id.s]
            let c = v_min + (v_max - v_min) / 2.f
            let dx = (v_max - v_min).X / 2.f
            let dy = (v_max - v_min).Y / 2.f
            let dz = (v_max - v_min).Z / 2.f
            let dx' = float32(sign i) * (dx * 1.125f)
            let dy' = float32(sign j) * (dy * 1.125f)
            let dz' = float32(sign k) * (dz * 1.125f)
            let dv = Vector3(dx', dy', dz')
            
            let mutable v = c + dv

            let mutable I = abs i
            let mutable J = abs j
            let mutable K = abs k

            // this will work only for on cell displacement
            while intersect v tree.cbounds[id.s]  && (I > 0 || J > 0 || K > 0) do
                v <- v + dv   // displace the point until it does not intersect the cell

                if not (intersect v tree.cbounds[id.s]) then
                    I <- I - 1
                    J <- J - 1
                    K <- K - 1

            traverse_retain v tree id 
            
        | Flag.Empty -> failwith "run iterate on EMPTY node, failed"
        | _ -> failwith "invalid flag"


    // let (|Internal|External|Boundary|) (tree:Root<'T>) (id:NodeId) =
    let (|Internal|External|Boundary|) (pair:struct(NodeId*Root<'T>)) =
        let struct(id,tree) = pair
        let b = (iterate_node -1 0 0 tree id).f
        let d = (iterate_node 0 -1 0 tree id).f
        let k = (iterate_node 0 0 -1 tree id).f
        let f = (iterate_node 0 1 0 tree id).f
        let h = (iterate_node 1 0 0 tree id).f
        let j = (iterate_node 0 0 1 tree id).f
        let u = id.f

        match (b,d,u,f,h,k,j) with
        | _,_,Flag.Empty,_,_,_,_ -> External
        | Flag.Leaf, Flag.Leaf, Flag.Leaf, Flag.Leaf, Flag.Leaf, Flag.Leaf, Flag.Leaf -> Internal
        | _,_,_,_,_,_,_ -> Boundary

    
    let contains (p:Vector3) (tree:Root<'T>) id =
        match (traverse_retain p tree id).f with
        | Flag.Leaf  -> true
        | Flag.Empty -> false
        | Flag.Node  -> failwith "contains SHOULD traverse to deepest level"
        | _ -> failwith "invalid flag"


    /// iterate all the leaf nodes of the tree
    /// The equivalent of a for-loop for the quadtree
    let rec iter (fn:Root<'T> -> NodeId -> unit) tree (id:NodeId) =
        match id.f with
        | Flag.Node ->
            let c = children tree id
            for i in 0..7 do iter fn tree c[i]            
        | Flag.Leaf -> fn tree id
        | Flag.Empty -> ()
        | _ -> failwith "invalid flag"



    type Root<'T> with
        member this.Put(x:double, y:double, z:double, value:voption<'T>) =
            let p = Vector3(float32 x, float32 y, float32 z)
            let n = byte this.n
            let k = byte this.k
            this.CachedNode.Value <- traverse p n k this this.CachedNode.Value
            let id = this.CachedNode.Value
            match id.f with
            | Flag.Leaf ->
                this.targets[id.s] <- if value.IsSome then Value value.Value else Empty
            | Flag.Node -> failwith $"Item.get failed on Node: {id.s}"         
            | _ -> failwith "Item.get failed"         

        member this.PutF(p:Vector3, value:voption<'T>) =
            let n = byte this.n
            let k = byte this.k
            let id = traverse p n k this this.CachedNode.Value 
            this.CachedNode.Value <- id
            match id.f with
            | Flag.Leaf ->
                this.targets[id.s] <- if value.IsSome then Value value.Value else Empty
            | Flag.Node -> failwith $"Item.get failed on Node: {id.s}"         
            | _ -> failwith "Item.get failed on Empty"         

        member this.InsertF(p:Vector3, value:voption<'T>) =
            let n = byte this.n
            let id = insert p n this this.CachedNode.Value
            this.CachedNode.Value <- id
            match id.f with
            | Flag.Leaf ->
                this.targets[id.s] <- if value.IsSome then Value value.Value else Empty
            | Flag.Node -> failwith $"Item.get failed on Node: {id.s}"         
            | _ -> failwith "Item.get failed on Empty"         

    
        member this.Item
            with get (x:double, y:double, z:double) =
                let p = Vector3(float32 x, float32 y, float32 z)
                this.CachedNode.Value <- traverse_retain p this this.CachedNode.Value
                let id = this.CachedNode.Value
                match id.f with
                | Flag.Leaf ->
                    match this.targets[id.s] with
                    | Value t -> ValueSome t
                    | Empty -> ValueNone
                    | Children _ -> failwith "node must be leaf"
                | _ -> failwith "Item.get failed"

            and set (x:double, y:double, z:double) (value:voption<'T>) =
                let p = Vector3(float32 x, float32 y, float32 z)
                this.CachedNode.Value <- traverse_retain p this this.CachedNode.Value
                let id = this.CachedNode.Value
                match id.f with
                | Flag.Leaf -> this.targets[id.s] <- if value.IsSome then Value (value.Value) else Empty
                | _ -> failwith "Item.get failed"         

        member this.GetCount() =
            let mutable _c = 0
            for t in this.targets do
                match t with
                | Children c -> ()
                | _ -> _c <- _c + 1
            _c

        member this.GetTotalCount() =
            let mutable _c = 0
            for t in this.targets do
                _c <- _c + 1
            _c

        member this.GetInternalCount() =
            let mutable _c = 0
            for t in this.targets do
                match t with
                | Children c ->
                    for i in 0..7 do
                        match c[i].f with
                        | Flag.Leaf ->
                            match struct(c[i],this) with
                            | Internal -> _c <- _c + 1
                            | _ -> ()
                        | _ -> ()
                | _ -> ()                        
            _c

        member this.GetBoundaryCount() =
            let mutable _c = 0
            for t in this.targets do
                match t with
                | Children c ->
                    for i in 0..7 do
                        match c[i].f with
                        | Flag.Leaf ->
                            match struct(c[i],this) with
                            | Boundary -> _c <- _c + 1
                            | _ -> ()
                        | _ -> ()
                | _ -> ()                        
            _c

        member this.Iter (fn:Root<'T> -> NodeId -> unit) = iter fn this this.root

        member this.IterParallel (num_threads:int) (fn:Root<'T> -> NodeId -> unit) =
            let root = this.root
            match num_threads with
            | 1 ->
                iter fn this root
            | 2 ->
                let c = children this root
                let ts = [|
                    Tasks.Task.Run (fun _ -> iter fn this c[0]; iter fn this c[1]; iter fn this c[2]; iter fn this c[3])
                    Tasks.Task.Run (fun _ -> iter fn this c[4]; iter fn this c[5]; iter fn this c[6]; iter fn this c[7])
                |]
                Tasks.Task.WaitAll(ts)
            | 3 ->
                let c = children this root
                let ts = [|
                    Tasks.Task.Run (fun _ -> iter fn this c[0]; iter fn this c[1]; iter fn this c[2])
                    Tasks.Task.Run (fun _ -> iter fn this c[3]; iter fn this c[4]; iter fn this c[5])
                    Tasks.Task.Run (fun _ -> iter fn this c[6]; iter fn this c[7])                
                |]
                Tasks.Task.WaitAll(ts)            
            | 4 ->
                let c = children this root
                let ts = [|
                    Tasks.Task.Run (fun _ -> iter fn this c[0]; iter fn this c[1])
                    Tasks.Task.Run (fun _ -> iter fn this c[2]; iter fn this c[3])
                    Tasks.Task.Run (fun _ -> iter fn this c[4]; iter fn this c[5])
                    Tasks.Task.Run (fun _ -> iter fn this c[6]; iter fn this c[7])
                |]
                Tasks.Task.WaitAll(ts)
            | 5 ->
                let c = children this root
                let ts = [|
                    Tasks.Task.Run (fun _ -> iter fn this c[0]; iter fn this c[1])
                    Tasks.Task.Run (fun _ -> iter fn this c[2]; iter fn this c[3])
                    Tasks.Task.Run (fun _ -> iter fn this c[4]; iter fn this c[5])
                    Tasks.Task.Run (fun _ -> iter fn this c[6])
                    Tasks.Task.Run (fun _ -> iter fn this c[7])
                |]
                Tasks.Task.WaitAll(ts)
            | 6 ->
                let c = children this root
                let ts = [|
                    Tasks.Task.Run (fun _ -> iter fn this c[0]; iter fn this c[1])
                    Tasks.Task.Run (fun _ -> iter fn this c[2]; iter fn this c[3])
                    Tasks.Task.Run (fun _ -> iter fn this c[4])
                    Tasks.Task.Run (fun _ -> iter fn this c[5])
                    Tasks.Task.Run (fun _ -> iter fn this c[6])
                    Tasks.Task.Run (fun _ -> iter fn this c[7])
                |]
                Tasks.Task.WaitAll(ts)
            | 7 ->
                let c = children this root
                let ts = [|
                    Tasks.Task.Run (fun _ -> iter fn this c[0]; iter fn this c[1])
                    Tasks.Task.Run (fun _ -> iter fn this c[2])
                    Tasks.Task.Run (fun _ -> iter fn this c[3])
                    Tasks.Task.Run (fun _ -> iter fn this c[4])
                    Tasks.Task.Run (fun _ -> iter fn this c[5])
                    Tasks.Task.Run (fun _ -> iter fn this c[6])
                    Tasks.Task.Run (fun _ -> iter fn this c[7])
                |]
                Tasks.Task.WaitAll(ts)
            | _ ->
                let c = children this root
                let ts = [|
                    Tasks.Task.Run (fun _ -> iter fn this c[0])
                    Tasks.Task.Run (fun _ -> iter fn this c[1])
                    Tasks.Task.Run (fun _ -> iter fn this c[2])
                    Tasks.Task.Run (fun _ -> iter fn this c[3])
                    Tasks.Task.Run (fun _ -> iter fn this c[4])
                    Tasks.Task.Run (fun _ -> iter fn this c[5])
                    Tasks.Task.Run (fun _ -> iter fn this c[6])
                    Tasks.Task.Run (fun _ -> iter fn this c[7])
                |]
                Tasks.Task.WaitAll(ts)


    let fill_scanlines N L (v_min:Vector3) (v_max:Vector3) (vertices:Span<float32>) (indices:Span<uint>) (bits:BitArray) =
        let dx = (v_max.X - v_min.X) / float32 N
        let dy = (v_max.Y - v_min.Y) / float32 N
        let dz = (v_max.Z - v_min.Z) / float32 N
        let dr = Vector3(dx,dy,dz)
        let center = GridGeneration3D.center

        let rec subdivide (a:Vector3) (b:Vector3) (c:Vector3) =        
            let R = dr.Length()
            if (b-a).Length() >= R || (a-c).Length() >= R || (c-b).Length() >= R then 
                let ab = center a b
                let ac = center a c
                let bc = center b c
                subdivide a ab ac 
                subdivide ab b bc       
                subdivide ab bc ac       
                subdivide ac bc c     
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

        let indices_count = indices.Length / 3
        let p = &MemoryMarshal.GetReference(vertices)
        for i in 0..indices_count-1 do
            let i0 = int32 (indices[3*i+0])
            let i1 = int32 (indices[3*i+1])
            let i2 = int32 (indices[3*i+2])
            let v0 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i0))
            let v1 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i1))
            let v2 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i2))            
            subdivide v0 v1 v2
        
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

    /// Builds a Quadtree out of a filled stencil
    /// The values of the Leafs are undefined
    let ofStencil<'T> N _k (v_min:Vector3) (v_max:Vector3) (stencil:BitArray) =
        let octree = Root<'T>.Create(N,_k,v_min,v_max)
        octree.root <- node NodeId.Empty 0uy 0uy v_min v_max octree
        octree.CachedNode.Value <- octree.root
        // octree.Stencil <- stencil        
        for i in 0..N-1 do
            for j in 0..N-1 do
                for k in 0..N-1 do
                    if stencil[i*N*N+j*N+k] then
                        let v = GridGeneration3D.to_cartesian_system i j k N v_min v_max
                        octree.PutF(v, ValueNone)
                        // octree.InsertF(v, ValueNone)
        octree       

    let ofSurface<'T> (N:int) L k (vertices:Span<float32>) (indices:Span<uint>) =
        let (v_min,v_max) = GridGeneration3D.bounds_SIMD vertices L
        let bits = fill_scanlines N L v_min v_max vertices indices (BitArray(N*N*N))
        ofStencil<'T> N k v_min v_max bits        


        

