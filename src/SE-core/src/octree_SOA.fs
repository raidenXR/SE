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

module OctreeSOA =

    type NodeId = int

    [<Flags>]
    type Flag =
        | Empty = 0uy
        | Node = 1uy
        | Leaf = 2uy

    [<Struct; IsByRefLike>]
    type Node<'T> =
        | Node of pn:NodeId * sn:NodeId * c:Span<NodeId>
        | Leaf of pl:NodeId * sl:NodeId * v:voption<'T>
        | Empty 

    [<Literal>]
    let _size = 1500000

    let buffer<'T> () = ResizeArray<'T>(_size)

    let rec dd n v _n = if _n < n then dd n (v/2.f) (_n + 1) else v

    // let create (c:byref<int>) (queue:ConcurrentQueue<NodeId>) =
    //     let mutable i = -1
    //     if queue.TryDequeue(&i) then
    //         struct(i,false)
    //     else
    //         // let p = c
    //         c <- c + 1
    //         struct(c,true)
            
    let create (c:byref<int>) (queue:ConcurrentStack<NodeId>) =
        let mutable i = -1
        if queue.TryPop(&i) then
            struct(i,false)
        else
            // let p = c
            c <- c + 1
            struct(c,true)

    // let destroy id (queue:ConcurrentQueue<NodeId>) =
        // queue.Enqueue(id)

    let destroy id (queue:ConcurrentStack<NodeId>) =
        queue.Push(id)


    type Root<'T>(N:int, k:int, v_min:Vector3, v_max:Vector3) =
        // let n_ids = ConcurrentQueue<NodeId>()
        // let v_ids = ConcurrentQueue<NodeId>()
        // let c_ids = ConcurrentQueue<NodeId>()
        let n_ids = ConcurrentStack<NodeId>()
        let v_ids = ConcurrentStack<NodeId>()
        let c_ids = ConcurrentStack<NodeId>()
        let mutable n_count = -1
        let mutable v_count = -1
        let mutable c_count = -1
        let mutable is_disposed = false

        let mutable cached_node = new ThreadLocal<NodeId>(fun _ -> 0)

        interface IDisposable with
            member this.Dispose() =
                if not is_disposed then
                    cached_node.Dispose()
                is_disposed <- true

        [<DefaultValue>] val mutable values:   ResizeArray<voption<'T>>
        [<DefaultValue>] val mutable children: ResizeArray<NodeId[]>
        [<DefaultValue>] val mutable parents:  ResizeArray<NodeId>
        [<DefaultValue>] val mutable targets:  ResizeArray<NodeId>
        [<DefaultValue>] val mutable cells:    ResizeArray<struct(Vector3*Vector3)>
        [<DefaultValue>] val mutable levels:   ResizeArray<byte>
        [<DefaultValue>] val mutable index:    ResizeArray<byte>
        [<DefaultValue>] val mutable flags:    ResizeArray<Flag>

        [<DefaultValue>] val mutable root: NodeId
        [<DefaultValue>] val mutable n: int
        [<DefaultValue>] val mutable k: int

        static member threadlock = new Object()

        static member Create(N, k, v_min, v_max) =
            let x = new Root<'T>(N, k, v_min, v_max)
            x.values <- buffer()
            x.children <- buffer()
            x.parents <- buffer()
            x.targets <- buffer()
            x.cells <- buffer()
            x.levels <- buffer()
            x.index <- buffer()
            x.flags <- buffer()
            x.n <- log10 (float N) / log10 2. |> ceil |> int 
            x.k <- k        
            x.root <- x.AddNode(-1, 0uy, 0uy, v_min, v_max)
            x

        member this.dX = dd this.n (v_max.X - v_min.X) 0   
        member this.dY = dd this.n (v_max.Y - v_min.Y) 0   
        member this.dZ = dd this.n (v_max.Z - v_min.Z) 0   
        member this.CachedNode = cached_node
        
        member private this.Add(flag, parent, index, level, v_min, v_max) =
            let struct(i,extend) = create &n_count n_ids
            let cell = struct(v_min,v_max)

            match flag with
            | Flag.Node when extend ->
                this.flags.Add(flag)
                this.parents.Add(parent)
                this.index.Add(index)
                this.levels.Add(level)
                this.cells.Add(cell)
                let children = System.Buffers.ArrayPool.Shared.Rent(8)
                for i in 0..7 do children[i] <- -1

                let struct(c_idx,c_extend) = create &c_count c_ids
                this.targets.Add(c_idx)
                
                if c_extend then
                    this.children.Add(children)
                else
                    this.children[c_idx] <- children
                i
                
            | Flag.Node ->
                this.flags[i] <- flag
                this.parents[i] <- parent
                this.index[i] <- index
                this.levels[i] <- level
                this.cells[i] <- cell
                let children = System.Buffers.ArrayPool.Shared.Rent(8)
                for i in 0..7 do children[i] <- -1

                let struct(c_idx,c_extend) = create &c_count c_ids
                this.targets[i] <- c_idx
                
                if c_extend then
                    this.children.Add(children)
                else
                    this.children[c_idx] <- children
                i
                                
            | Flag.Leaf when extend ->
                this.flags.Add(flag)
                this.parents.Add(parent)
                this.index.Add(index)
                this.levels.Add(level)
                this.cells.Add(cell)

                let struct(v_idx,v_extend) = create &v_count v_ids
                this.targets.Add(v_idx)

                if v_extend then
                    this.values.Add(ValueNone)
                else
                    this.values[v_idx] <- ValueNone
                    
                i
                
            | Flag.Leaf ->
                this.flags[i] <- flag
                this.parents[i] <- parent
                this.index[i] <- index
                this.levels[i] <- level
                this.cells[i] <- cell

                let struct(v_idx,v_extend) = create &v_count v_ids
                this.targets[i] <- v_idx

                if v_extend then
                    this.values.Add(ValueNone)
                else
                    this.values[v_idx] <- ValueNone
                i
                                
            | _ -> -1


        member this.AddParallel(flag, parent, index, level, v_min, v_max) =
            lock (Root<'T>.threadlock) (fun _ -> this.Add(flag, parent, index, level, v_min, v_max))


        member this.AddLeaf(parent, value, index, level, v_min, v_max) =
            let leaf = this.Add(Flag.Leaf, parent, index, level, v_min, v_max)
            this.values[this.targets[leaf]] <- value
            leaf

        member this.AddNode(parent, index, level, v_min, v_max) =
            let node = this.Add(Flag.Node, parent, index, level, v_min, v_max)
            node

        member this.Remove(id) =
            if id > 0 then 
                match this.flags[id] with
                | Flag.Node ->
                    let p = this.parents[id]
                    let c = this.targets[id]
                    let i = this.index[id]
                    let l = this.levels[id]

                    let children = this.children[c]
                    if l > 0uy then
                        for i in 0..7 do
                            this.Remove(children[i])

                    System.Buffers.ArrayPool.Shared.Return(children)
                    // this.flags[id] <- Flag.Empty
                    destroy id n_ids
                    destroy c c_ids
                
                | Flag.Leaf ->
                    let p = this.parents[id]
                    let v = this.targets[id]
                    let i = this.index[id]
                
                    // let c = this.children[this.targets[p]].AsSpan(0,8)
                    // c[int i] <- -1

                    // this.flags[id] <- Flag.Empty
                    destroy id n_ids
                    destroy v v_ids                

                | _ ->
                    ()
                    // this.flags[id] <- Flag.Empty

                if this.levels[id] > 0uy then
                    let p = this.parents[id]
                    let i = this.index[id]
                    let c = this.children[this.targets[p]].AsSpan(0,8)
                    c[int i] <- -1
                    this.flags[id] <- Flag.Empty



    let inline as_node (id:NodeId) (tree:Root<'T>) =
        if id = -1 then
            Empty
        else
            match tree.flags[id] with
            | Flag.Node  -> Node (tree.parents[id], id, Span(tree.children[tree.targets[id]], 0, 8))
            | Flag.Leaf  -> Leaf (tree.parents[id], id, tree.values[tree.targets[id]])
            | _ -> Empty

        
    let rec vertices_to_points (points:ResizeArray<Vector3>) id (tree:Root<'T>) =
        match (as_node id tree) with
        | Node (p,s,c) ->
            for ci in c do vertices_to_points points ci tree

        | Leaf (p,s,v) ->
            let struct(c,r) = tree.cells[s]
            points.Add(c)
            
        | Empty -> ()

    let parent id tree =
        match (as_node id tree) with
        | Node (p,_,_) | Leaf (p,_,_) -> p
        | Empty -> failwith "attempted to get parent of Empty node"
        
    let children id tree =
        match (as_node id tree) with
        | Node (_,s,_) -> tree.children[tree.targets[s]]
        | _ -> failwith "attempted to get parent of Empty node"
        
    let center id tree =
        match (as_node id tree) with
        | Node (_,s,_) | Leaf (_,s,_) ->
            let struct(v_min,v_max) = tree.cells[s]
            v_min + (v_max - v_min) / 2.f
        | Empty -> failwith "attempted to get parent of Empty node"

    let intersect (p:Vector3) (cell:struct(Vector3*Vector3)) =
        let struct(v_min,v_max) = cell
        let x = v_min.X <= p.X && p.X <= v_max.X
        let y = v_min.Y <= p.Y && p.Y <= v_max.Y
        let z = v_min.Z <= p.Z && p.Z <= v_max.Z
        x && y && z

    let rec count_rec (j:byref<int>) id tree =
        match (as_node id tree) with
        | Node (_,_,c) -> for ci in c do count_rec &j ci tree
        | Leaf _ -> j <- j + 1
        | Empty -> ()

    let rec count_total_rec (j:byref<int>) id tree =
        match (as_node id tree) with 
        | Node (_,_,c) ->
            j <- j + 1
            for ci in c do count_total_rec &j ci tree
        | Leaf _ ->
            j <- j + 1        
        | Empty -> ()


    let forall (fn: NodeId -> bool) (ids:NodeId[]) =
        let mutable b = true
        let mutable i = 0
        while i < 8 && b do
            b <- b && (fn ids[i])
            i <- i + 1
        b

    /// convert a Node to Leaf
    let trim n k v (tree:Root<'T>) id : NodeId =
        match (as_node id tree) with 
        | Leaf (p,s,_) when n = tree.levels[s] ->
            if (forall (fun t -> match (as_node t tree) with | Node _ -> false | _ -> true) (children p tree)) then 
                match (as_node p tree) with
                | Node (P,S,C) ->
                    let I  = tree.index[S]
                    let L  = tree.levels[S]
                    let struct(V1,V2) = tree.cells[S]
                    let mutable value: ValueOption<'T> = v
                    for ci in C do
                        match (as_node ci tree) with
                        | Leaf (_,_,V) when V.IsSome -> value <- ValueSome V.Value
                        | _ -> ()
                        
                    tree.Remove(s)
                    tree.Remove(S)
                    let leaf = tree.AddLeaf(P, value, I, L, V1, V2)
                    (children P tree)[int I] <- leaf
                    leaf
                | _ -> id
            else
                id

        | Leaf (p,s,_) when n - tree.levels[s] < k ->
            if (forall (fun t -> match (as_node t tree) with | Leaf _ -> true | _ -> false) (children p tree)) then 
                match (as_node p tree) with
                | Node (P,S,C) ->
                    let I  = tree.index[S]
                    let L  = tree.levels[S]
                    let struct(V1,V2) = tree.cells[S]
                    let mutable value: ValueOption<'T> = v
                    for ci in C do
                        match (as_node ci tree) with
                        | Leaf (_,_,V) when V.IsSome -> value <- ValueSome V.Value
                        | _ -> ()

                    tree.Remove(s)
                    tree.Remove(S)
                    let leaf = tree.AddLeaf(P, value, I, L, V1, V2)
                    (children P tree)[int I] <- leaf
                    leaf
                | _ -> id
            else
                id
        | Empty ->
            failwith "tried to trim Empty Node"
        | _ -> id


    /// convert a Leaf to Node
    let rec dense n id (tree:Root<'T>) : NodeId =
        match (as_node id tree) with
        | Leaf (p,s,v) when tree.levels[s] < n ->
            let i  = tree.index[s]
            let l  = tree.levels[s]
            let struct(v1,v2) = tree.cells[s]

            tree.Remove(s)
            let _this = tree.AddNode(p, i, l, v1, v2)
            let _children = (children _this tree).AsSpan(0,8)
            let _value = v
            (children p tree)[int i] <- _this            

            let struct(v1,v2) = tree.cells[s]
            let o = v1 + (v2 - v1) / 2f
            do
                let v_min = Vector3(v1.X, o.Y, v1.Z)            
                let v_max = Vector3(o.X, v2.Y, o.Z) 
                _children[0] <- tree.AddLeaf (_this, _value, 0uy, (l+1uy), v_min, v_max)
            do
                let v_min = Vector3(o.X, o.Y, v1.Z)            
                let v_max = Vector3(v2.X, v2.Y, o.Z) 
                _children[1] <- tree.AddLeaf (_this, _value, 1uy, (l+1uy), v_min, v_max)
            do
                let v_min = Vector3(v1.X, v1.Y, v1.Z)            
                let v_max = Vector3(o.X, o.Y, o.Z) 
                _children[2] <- tree.AddLeaf (_this, _value, 2uy, (l+1uy), v_min, v_max)
            do
                let v_min = Vector3(o.X, v1.Y, v1.Z)            
                let v_max = Vector3(v2.X, o.Y, o.Z) 
                _children[3] <- tree.AddLeaf (_this, _value, 3uy, (l+1uy), v_min, v_max)

            do
                let v_min = Vector3(v1.X, o.Y, o.Z)            
                let v_max = Vector3(o.X, v2.Y, v2.Z) 
                _children[4] <- tree.AddLeaf (_this, _value, 4uy, (l+1uy), v_min, v_max)

            do
                let v_min = Vector3(o.X, v1.Y, o.Z)            
                let v_max = Vector3(v2.X, v2.Y, v2.Z) 
                _children[5] <- tree.AddLeaf (_this, _value, 5uy, (l+1uy), v_min, v_max)

            do
                let v_min = Vector3(v1.X, v1.Y, o.Z)            
                let v_max = Vector3(o.X, o.Y, v2.Z) 
                _children[6] <- tree.AddLeaf (_this, _value, 6uy, (l+1uy), v_min, v_max)

            do
                let v_min = Vector3(o.X, v1.Y, o.Z)            
                let v_max = Vector3(v2.X, o.Y, v2.Z) 
                _children[7] <- tree.AddLeaf (_this, _value, 7uy, (l+1uy), v_min, v_max)

            _this

        | Node (_,s,c) ->
            for i in 0..7 do dense n c[i] tree |> ignore
            c[0]

        | _ -> id



    let rec traverse (p:Vector3) n k (tree:Root<'T>) id : NodeId =
        match (as_node id tree) with
        | Empty -> failwith "traversed to empty node, make sure that root is not out of bounds"

        | Leaf (parent,s,_) when not (intersect p tree.cells[s]) ->
            traverse p n k tree parent
            |> trim n k ValueNone tree
            
        | Leaf _ ->
            id
            |> trim n k ValueNone tree

        | Node (parent,s,_) when not (intersect p tree.cells[s]) ->
            traverse p n k tree parent
            |> trim n k ValueNone tree

        | Node (_,s,c) ->   // traverse forward
            let l = tree.levels[s]
            let struct(v1,v2) = tree.cells[s]
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

            match (as_node c[idx] tree) with
            | Empty when l >= n ->
                c[idx] <- tree.AddLeaf (id, ValueNone, byte idx, n, v_min, v_max)
                traverse p n k tree c[idx]
                |> trim n k ValueNone tree

            | Empty ->
                c[idx] <- tree.AddNode (id, byte idx, (l+1uy), v_min, v_max) 
                traverse p n k tree c[idx]
                |> trim n k ValueNone tree
                
            | _ ->
                traverse p n k tree c[idx]
                |> trim n k ValueNone tree


    let rec traverse_retain (p:Vector3) (tree:Root<'T>) id =
        match (as_node id tree) with
        | Empty -> id

        | Leaf (parent,s,_) when not (intersect p tree.cells[s]) ->
            traverse_retain p tree parent
            
        | Leaf _ -> id

        | Node (parent,s,_) when not (intersect p tree.cells[s]) ->
            traverse_retain p tree parent

        | Node (_,s,c) ->   // traverse forward
            let struct(v_min,v_max) = tree.cells[s]
            let mutable idx = 0
            let o = v_min + (v_max - v_min) / 2f
            idx <- idx + if p.X < o.X then 0 else 1
            idx <- idx + if p.Y > o.Y then 0 else 2
            idx <- idx + if p.Z < o.Z then 0 else 4

            if idx < 0 || idx > 7 then failwith "improper idx value"

            match (as_node c[idx] tree) with
            | Empty -> c[idx]                
            | _ -> traverse_retain p tree c[idx]              
               

    let iterate_node i j k (tree:Root<'T>) id =
        match (as_node id tree) with
        | _ when i = 0 && j = 0 && k = 0 -> id

        | Leaf (_,s,_) | Node (_,s,_) ->
            let struct(v_min,v_max) = tree.cells[s]
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
            while intersect v tree.cells[s]  && (I > 0 || J > 0 || K > 0) do
                v <- v + dv   // displace the point until it does not intersect the cell

                if not (intersect v tree.cells[s]) then
                    I <- I - 1
                    J <- J - 1
                    K <- K - 1

            traverse_retain v tree id 
            
        | Empty -> failwith "run iterate on EMPTY node, failed"


    let (|Internal|External|Boundary|) (pair:struct(NodeId*Root<'T>)) =
        let struct(id,tree) = pair
        let mutable bi = (iterate_node -1 0 0 tree id)
        let mutable di = (iterate_node 0 -1 0 tree id)
        let mutable ki = (iterate_node 0 0 -1 tree id)
        let mutable fi = (iterate_node 0 1 0 tree id)
        let mutable hi = (iterate_node 1 0 0 tree id)
        let mutable ji = (iterate_node 0 0 1 tree id)

        let b = as_node bi tree
        let d = as_node di tree
        let k = as_node ki tree
        let u = as_node id tree
        let f = as_node fi tree
        let h = as_node hi tree
        let j = as_node ji tree
        // let b = if bi >= 0 then tree.flags[bi] else Flag.Empty
        // let d = if di >= 0 then tree.flags[di] else Flag.Empty
        // let k = if ki >= 0 then tree.flags[ki] else Flag.Empty
        // let u = if id >= 0 then tree.flags[id] else Flag.Empty
        // let f = if fi >= 0 then tree.flags[fi] else Flag.Empty
        // let h = if hi >= 0 then tree.flags[hi] else Flag.Empty
        // let j = if ji >= 0 then tree.flags[ji] else Flag.Empty
        // try
        //     let b = tree.flags[bi]
        //     let d = tree.flags[di]
        //     let k = tree.flags[ki]
        //     let u = tree.flags[id]
        //     let f = tree.flags[fi]
        //     let h = tree.flags[hi]
        //     let j = tree.flags[ji]

        //     match (b,d,u,f,h,k,j) with
        //     | _,_,Flag.Empty,_,_,_,_ -> External
        //     | Flag.Leaf, Flag.Leaf, Flag.Leaf, Flag.Leaf, Flag.Leaf, Flag.Leaf, Flag.Leaf -> Internal
        //     | _,_,_,_,_,_,_ -> Boundary
        // with
        // | _ -> External

        let mutable ui = id
        ui <- match u with | Empty -> -1 | Leaf _ -> 0 | Node _ -> 1
        bi <- match b with | Empty -> -1 | Leaf _ -> 0 | Node _ -> 1
        di <- match d with | Empty -> -1 | Leaf _ -> 0 | Node _ -> 1
        ki <- match k with | Empty -> -1 | Leaf _ -> 0 | Node _ -> 1
        fi <- match f with | Empty -> -1 | Leaf _ -> 0 | Node _ -> 1
        hi <- match h with | Empty -> -1 | Leaf _ -> 0 | Node _ -> 1
        ji <- match j with | Empty -> -1 | Leaf _ -> 0 | Node _ -> 1
                 
        // // match (b,d,u,f,h,k,j) with
        // // | _,_,Empty,_,_,_,_ -> External
        // // | Leaf _, Leaf _, Leaf _, Leaf _, Leaf _, Leaf _, Leaf _-> Internal
        // // | _,_,_,_,_,_,_ -> Boundary

        if ui = -1 then
            External
        elif (ui ||| bi ||| di ||| ki ||| fi ||| fi ||| ji) = 0 then
        // elif (ui &&& di) = 0 then 
            Internal
        else
            Boundary

    
    let contains (p:Vector3) (tree:Root<'T>) id =
        match as_node (traverse_retain p tree id) tree with
        | Leaf _ -> true
        | Empty -> false
        | Node _ -> failwith "contains SHOULD traverse to deepest level"


    let valueof = function
        | Leaf (_,s,v) -> v.Value
        | _ -> failwith "The tmp_node HAS to be a Leaf, with an ASSIGNED value!!"

    /// iterate all the leaf nodes of the tree
    /// The equivalent of a for-loop for the quadtree
    let rec iter (fn:NodeId -> unit) tree id =
        match (as_node id tree) with
        | Node (_,s,c) -> for ci in c do iter fn tree ci            
        | Leaf _ -> fn id
        | Empty -> ()



    type Root<'T> with
        member this.Put(x:double, y:double, z:double, value:voption<'T>) =
            let p = Vector3(float32 x, float32 y, float32 z)
            let n = byte this.n
            let k = byte this.k
            this.CachedNode.Value <- traverse p n k this this.CachedNode.Value
            match (as_node this.CachedNode.Value this) with
            | Leaf (_,s,v) -> this.values[this.targets[s]] <- value
            | _ -> failwith "Item.get failed"         

        member this.PutF(p:Vector3, value:voption<'T>) =
            let n = byte this.n
            let k = byte this.k
            this.CachedNode.Value <- traverse p n k this this.CachedNode.Value
            match (as_node this.CachedNode.Value this) with
            | Leaf (_,s,v) -> this.values[this.targets[s]] <- value
            | Node (_,s,_) -> failwith $"Item.get failed on Node: {s}"         
            | Empty -> failwith "Item.get failed on Empty"         

    
        member this.Item
            with get (x:double, y:double, z:double) =
                let p = Vector3(float32 x, float32 y, float32 z)
                this.CachedNode.Value <- traverse_retain p this this.CachedNode.Value
                match (as_node this.CachedNode.Value this) with
                | Leaf (_,_,v) -> v.Value
                | _ -> failwith "Item.get failed"

            and set (x:double, y:double, z:double) value =
                let p = Vector3(float32 x, float32 y, float32 z)
                this.CachedNode.Value <- traverse_retain p this this.CachedNode.Value
                match (as_node this.CachedNode.Value this) with
                | Leaf (_,s,_) -> this.values[this.targets[s]] <- ValueSome value
                | _ -> failwith "Item.get failed"         

        member this.GetCount() =
            let mutable c = 0
            count_rec &c this.root this
            c

        member this.GetTotalCount() =
            let mutable c = 0
            count_total_rec &c this.root this
            c

        member this.GetInternalCount() =
            let mutable c = 0
            let is_internal id = match struct(id,this) with | Internal -> c <- c + 1 | _ -> ()
            iter is_internal this this.root
            c

        member this.GetBoundaryCount() =
            let mutable c = 0
            let is_boundary id = match struct(id,this) with | Boundary -> c <- c + 1 | _ -> ()
            iter is_boundary this this.root
            c

        member this.Iter (fn:NodeId -> unit) = iter fn this this.root


    let fill_scanlines N L (v_min:Vector3) (v_max:Vector3) (vertices:Span<float32>) (indices:Span<uint>) (bits:BitArray) =
        let dx = (v_max.X - v_min.X) / float32 N
        let dy = (v_max.Y - v_min.Y) / float32 N
        let dz = (v_max.Z - v_min.Z) / float32 N
        let dr = Vector3(dx,dy,dz)
        // let vs = ResizeArray<Vector2>(1024)
        // let tree = Root<byte>(N, 0, v_min, v_max)
        let center = GridGeneration3D.center

        let rec subdivide (a:Vector3) (b:Vector3) (c:Vector3) =        
            // let t = GridGeneration3D.triangle_center a b c
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
        
        // let mutable i = 0
        // while i < N do
        for i in 0..N-1 do
            let mutable j = 0
            while j < N do
                let (a,b) = GridGeneration3D.measure_range bits N i j
                let mutable fill = GridGeneration3D.fill_line_check bits N i j

                let collisions = GridGeneration3D.measure_marching_rows bits N i j
                match collisions with
                | GridGeneration3D.Zero -> ()

                | GridGeneration3D.Odd when i > 0 && i < N - 1 && j > 0 && j < N - 1 ->                
                    // printfn "Odd called"
                    for k in 0..N-1 do
                        let upper_row = bits[(i-1)*N*N+(j-1)*N+k]
                        let lower_row = bits[(i+1)*N*N+(j+1)*N+k]
                        bits[i*N*N+j*N+k] <- bits[i*N*N+j*N+k] || (upper_row || lower_row)
            
                | GridGeneration3D.Odd -> () // ignore first line, keep only the upper boundaries

                // | GridGeneration3D.Even when collisions = 2 && i > 0 && j > 0 && i < N-1 && j < N-1 ->
                //     // printfn "Even_when called"
                //     for k in 0..N-1 do
                //         let upper_row = stencil[(i-1)*N*N+(j-1)*N+k]
                //         let lower_row = stencil[(i+1)*N*N+(j+1)*N+k]
                //         stencil[i*N*N+j*N+k] <- stencil[i*N*N+j*N+k] || (upper_row || lower_row)
            
                | GridGeneration3D.Even ->
                    let mutable k = a
                    while k <= b do
                        if bits[i*N*N+j*N+k] then
                            while bits[i*N*N+j*N+k] do k <- k + 1  // advance
                            // printfn "Even called"
                            k <- k - 1
                            fill <- not fill
                
                        if fill then bits[i*N*N+j*N+k] <- true
                        k <- k + 1
                j <- j + 1
            // i <- i + 1
        bits

    /// Builds a Quadtree out of a filled stencil
    /// The values of the Leafs are undefined
    let ofStencil<'T> N _k (v_min:Vector3) (v_max:Vector3) (stencil:BitArray) =
        let octree = Root<'T>.Create(N,_k,v_min,v_max)
        // octree.Stencil <- stencil        
        for i in 0..N-1 do
            for j in 0..N-1 do
                for k in 0..N-1 do
                    if stencil[i*N*N+j*N+k] then
                        let v = GridGeneration3D.to_cartesian_system i j k N v_min v_max
                        octree.PutF(v, ValueNone)
                        // quadtree.Put(double v.X, double v.Y, double v.Z, ValueNone)
        octree       

    let ofSurface<'T> (N:int) L k (vertices:Span<float32>) (indices:Span<uint>) =
        let (v_min,v_max) = GridGeneration3D.bounds_SIMD vertices L
        let bits = fill_scanlines N L v_min v_max vertices indices (BitArray(N*N*N))
        ofStencil<'T> N k v_min v_max bits        


        
