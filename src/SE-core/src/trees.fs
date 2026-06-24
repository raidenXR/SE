namespace SE.Core

open SE
open System
open System.Numerics
open System.Collections

module Quadtree =

    type Node<'T> =
        | Node of parent:Node<'T> * children:Node<'T>[] * idx:int * level:int * v_min:Vector2 * v_max:Vector2
        | Leaf of parent:Node<'T> * value:ref<ValueOption<'T>> * idx:int * level:int * v_min:Vector2 * v_max:Vector2 
        | Empty

    let rec write_vertices (node:Node<'T>) (fs:System.IO.StreamWriter) =
        match node with
        | Node (_,c,_,_,_,_) ->
            for ci in c do write_vertices ci fs

        | Leaf (_,v,_,_,v_min,v_max) ->
            let p = v_min + (v_max - v_min) / 2f
            fs.WriteLine($"{p.X}  {p.Y}")
            
        | Empty -> ()
        
    let rec vertices_to_points (node:Node<'T>) (points:ResizeArray<Vector2>) =
        match node with
        | Node (_,c,_,_,_,_) ->
            for ci in c do vertices_to_points ci points

        | Leaf (_,v,_,_,v_min,v_max) ->
            let p = v_min + (v_max - v_min) / 2f
            points.Add(p)
            
        | Empty -> ()
        
    let rec vertices_to_polygons (node:Node<'T>) (fill: 'T -> float32) (points:ResizeArray<Vector3>) =
        match node with
        | Node (_,c,_,_,_,_) ->
            for ci in c do vertices_to_polygons ci fill points

        | Leaf (_,v,_,_,v_min,v_max) ->
            let d = fill v.Value.Value
            let v0 = Vector3(v_min.X, v_min.X, d)
            let v1 = Vector3(v_min.X, v_max.Y, d)
            let v2 = Vector3(v_max.X, v_max.Y, d)
            let v3 = Vector3(v_max.X, v_min.Y, d)
            // points.Add(v0)
            // points.Add(v1)
            // points.Add(v2)
            // points.Add(v3)
            // points.Add(v0)

            points.Add(v0)
            points.Add(v1)
            points.Add(v3)

            points.Add(v1)
            points.Add(v2)
            points.Add(v3)
            
        | Empty -> ()
        
    let rec values_from_vertices (node:Node<'T>) (points:ResizeArray<'T>) =
        match node with
        | Node (_,c,_,_,_,_) ->
            for ci in c do values_from_vertices ci points

        | Leaf (_,v,_,_,v_min,v_max) ->
            match v.Value with
            | ValueSome V -> points.Add(V)
            | ValueNone -> ()
            
        | Empty -> ()
        
    let rec write_rects (node:Node<'T>) (fs:System.IO.StreamWriter) =
        match node with
        | Node (_,c,_,_,_,_) ->
            for ci in c do write_rects ci fs

        | Leaf (_,v,_,_,v_min,v_max) ->
            let v1 = v_min
            let v2 = Vector2(v_min.X, v_max.Y)
            let v3 = v_max
            let v4 = Vector2(v_max.X, v_min.Y)
            fs.WriteLine($"{v1.X}  {v1.Y}")
            fs.WriteLine($"{v2.X}  {v2.Y}")
            // fs.WriteLine("\n")
            
            fs.WriteLine($"{v2.X}  {v2.Y}")
            fs.WriteLine($"{v3.X}  {v3.Y}")
            // fs.WriteLine("\n")
            
            fs.WriteLine($"{v3.X}  {v3.Y}")
            fs.WriteLine($"{v4.X}  {v4.Y}")
            // fs.WriteLine("\n")

            fs.WriteLine($"{v4.X}  {v4.Y}")
            fs.WriteLine($"{v1.X}  {v1.Y}")
            fs.WriteLine("\n")
            
        | Empty -> ()
    
    let create<'T> N (v_min:Vector2) (v_max:Vector2) =
        let n = log10 (double N) / log10 2. |> ceil |> int
        let j = 0
        let node = Node (Empty, Array.create<Node<'T>> 4 Empty, 0, 0, v_min, v_max)
        node
        
    let parent = function | Node (p,_,_,_,_,_) | Leaf (p,_,_,_,_,_) -> p | Empty -> failwith "attempted to get parent on Empty Node"
    
    let children = function | Node (_,c,_,_,_,_) -> c | _ -> failwith "node has no children"

    let center = function | Leaf (_,_,_,_,v_min,v_max) | Node (_,_,_,_,v_min,v_max) -> v_min + (v_max - v_min) / 2.f | Empty -> failwith "attempty to get center of Empty node"

    let (==) a b = FSharp.Core.LanguagePrimitives.PhysicalEquality a b

    let intersect (p:Vector2) (v_min:Vector2) (v_max:Vector2) =
        let x = v_min.X <= p.X && p.X <= v_max.X
        let y = v_min.Y <= p.Y && p.Y <= v_max.Y
        x && y

    let rec count_rec (j:byref<int>) = function
        | Node (_,children,_,_,_,_) ->
             for c in children do count_rec &j c
        | Leaf _ -> j <- j + 1
        | Empty -> ()

    let rec count_total_rec (j:byref<int>) (node:Node<'T>) =
        j <- j + 1
        match node with 
        | Node (_,children,_,_,_,_) ->
            for c in children do count_total_rec &j c
        | _ -> ()

    let is_quadant = function
        | Node (p,c,_,_,_,_) -> Array.forall (function Leaf _ -> true | _ -> false) c 
        | _ -> false

    /// convert a Node to Leaf
    let rec trim n k v (node:Node<'T>) =
        match node with 
        | Leaf (p,_,_,l,_,_) when n = l ->
            if (Array.forall (function Node _ -> false | _ -> true) (children p)) then 
                match p with
                | Node (P,C,I,L,V1,V2) ->
                    let mutable value: ValueOption<'T> = v
                    for ci in C do
                        match ci with
                        | Leaf (_,V,_,_,_,_) when V.Value.IsSome -> value <- ValueSome V.Value.Value
                        | _ -> ()
                        
                    (children P)[I] <- Leaf (P,ref value,I,L,V1,V2)
                    (children P)[I]
                    // trim n k v p
                | _ -> node
            else
                node

        | Leaf (p,_,_,l,_,_) when n - l < k ->
            if (Array.forall (function Leaf _ -> true | _ -> false) (children p)) then 
                match p with
                | Node (P,C,I,L,V1,V2) ->
                    let mutable value: ValueOption<'T> = v
                    for ci in C do
                        match ci with
                        | Leaf (_,V,_,_,_,_) when V.Value.IsSome -> value <- ValueSome V.Value.Value
                        | _ -> ()
                        
                    (children P)[I] <- Leaf (P,ref value,I,L,V1,V2)
                    (children P)[I]
                    // trim n k v p
                | _ -> node
            else
                node
        | Empty ->
            failwith "tried to trim Empty Node"
        | _ -> node

    /// convert a Leaf to Node
    let rec dense n (node:Node<'T>) =
        match node with
        | Leaf (p,v,i,l,v1,v2) when l < n ->
            let _children = Array.create<Node<'T>> 4 Empty
            let _this = Node (p, _children, i, l, v1, v2)
            let _value = v.Value
            (children p)[i] <- _this            

            let o = v1 + (v2 - v1) / 2f
            do
                let v_min = Vector2(v1.X, o.Y)            
                let v_max = Vector2(o.X, v2.Y) 
                _children[0] <- Leaf (_this, ref _value, 0, (l+1), v_min, v_max)
            do
                let v_min = Vector2(o.X, o.Y)            
                let v_max = Vector2(v2.X, v2.Y) 
                _children[1] <- Leaf (_this, ref _value, 1, (l+1), v_min, v_max)
            do
                let v_min = Vector2(v1.X, v1.Y)            
                let v_max = Vector2(o.X, o.Y) 
                _children[2] <- Leaf (_this, ref _value, 2, (l+1), v_min, v_max)
            do
                let v_min = Vector2(o.X, v1.Y)            
                let v_max = Vector2(v2.X, o.Y) 
                _children[3] <- Leaf (_this, ref _value, 3, (l+1), v_min, v_max)

            _this

        | Node (_,c,_,_,_,_) ->
            for ci in c do dense n ci |> ignore
            c[0]

        | _ -> node


    let rec traverse (p:Vector2) n k (node:Node<'T>) =
        match node with
        | Empty -> failwith "traversed to empty node, make sure that root is not out of bounds"

        | Leaf (parent,_,_,l,v_min,v_max) when not (intersect p v_min v_max) ->
            traverse p n k parent
            |> trim n k ValueNone
            
        | Leaf _ ->
            node
            |> trim n k ValueNone

        | Node (parent,_,_,l,v_min,v_max) when not (intersect p v_min v_max) ->
            traverse p n k parent
            |> trim n k ValueNone

        | Node (_,c,_,l,v1,v2) ->   // traverse forward 
            let mutable v_min = v1
            let mutable v_max = v2
            let mutable idx = 0

            let o = v_min + (v_max - v_min) / 2f
            idx <- idx + if p.X < o.X then 0 else 1
            idx <- idx + if p.Y > o.Y then 0 else 2

            match idx with
            | 0 ->
                v_min <- Vector2(v_min.X, o.Y)            
                v_max <- Vector2(o.X, v_max.Y) 
            | 1 -> 
                v_min <- Vector2(o.X, o.Y)            
                v_max <- Vector2(v_max.X, v_max.Y) 
            | 2 -> 
                v_min <- Vector2(v_min.X, v_min.Y)            
                v_max <- Vector2(o.X, o.Y) 
            | 3 -> 
                v_min <- Vector2(o.X, v_min.Y)            
                v_max <- Vector2(v_max.X, o.Y) 
            | _ ->
                failwith "improper idx value"

            match c[idx] with
            | Empty when l >= n ->
                c[idx] <- Leaf (node, ref ValueNone, idx, n, v_min, v_max)
                traverse p n k c[idx]
                |> trim n k ValueNone

            | Empty ->
                c[idx] <- Node (node, Array.create<Node<'T>> 4 Empty, idx, (l+1), v_min, v_max) 
                traverse p n k c[idx]
                |> trim n k ValueNone
                
            | _ ->
                traverse p n k c[idx]
                |> trim n k ValueNone

        
    let rec traverse_retain (p:Vector2) (node:Node<'T>) =
        match node with
        // | Empty -> failwith "traversed to empty node, make sure that root is not out of bounds"
        | Empty -> node

        | Leaf (parent,_,_,l,v_min,v_max) when not (intersect p v_min v_max) ->
            traverse_retain p parent
            
        | Leaf _ ->
            node

        | Node (parent,_,_,l,v_min,v_max) when not (intersect p v_min v_max) ->
            traverse_retain p parent

        | Node (_,c,_,l,v1,v2) ->   // traverse forward 
            let mutable v_min = v1
            let mutable v_max = v2
            let mutable idx = 0

            let o = v_min + (v_max - v_min) / 2f
            idx <- idx + if p.X < o.X then 0 else 1
            idx <- idx + if p.Y > o.Y then 0 else 2

            match idx with
            | 0 ->
                v_min <- Vector2(v_min.X, o.Y)            
                v_max <- Vector2(o.X, v_max.Y) 
            | 1 -> 
                v_min <- Vector2(o.X, o.Y)            
                v_max <- Vector2(v_max.X, v_max.Y) 
            | 2 -> 
                v_min <- Vector2(v_min.X, v_min.Y)            
                v_max <- Vector2(o.X, o.Y) 
            | 3 -> 
                v_min <- Vector2(o.X, v_min.Y)            
                v_max <- Vector2(v_max.X, o.Y) 
            | _ ->
                failwith "improper idx value"

            match c[idx] with
            | Empty ->
                // printfn "idx: %d, level: %d" idx l
                // failwith "traversed to Empty node"
                c[idx]
                
            | _ ->
                traverse_retain p c[idx]
                
    let contains i j idx =        
        0 <= i + j + idx && i + j + idx <= 3

    // let iterate i j dx dy N (v_min:Vector2) (v_max:Vector2) (stencil:BitArray) (node:Node<'T>) =
    //     match node with
    //     | _ when i = 0 && j = 0 -> node
        
    //     // | Empty -> failwith "cannot iterate on Empty node"
    //     | Empty -> node

    //     | Leaf (_,_,_,_,v_min,v_max) | Node (_,_,_,_,v_min,v_max) ->
    //         let c = v_min + (v_max - v_min) / 2.f
    //         let dv = Vector2(float32 i * dx, float32 j * dy)
            
    //         let mutable _v = c
    //         let mutable _n = node
    //         let mutable tmp = node

    //         let mutable I = abs i
    //         let mutable J = abs j

    //         while _n == tmp && (I > 0 || J > 0) do
    //             _v <- _v + dv
    //             let (ii,jj) = GridGeneration2D.to_stencil_system N _v v_min v_max 

    //             // if not stencil[ii*N+jj] then
    //                 // failwith "[i,j] is out of bounds of the stencil"
    //             // else
    //             _n <- traverse_retain _v _n              

                    
    //             if not (tmp == _n) then
    //                 tmp <- _n
    //                 I <- I - 1
    //                 J <- J - 1                    
    //         _n

    let try_iterate i j (node:Node<'T>) =
        match node with
        | _ when i = 0 && j = 0 -> node
        
        | Leaf (_,_,_,l,v_min,v_max) | Node (_,_,_,l,v_min,v_max) ->
            let dv = v_max - v_min
            let dx = dv.X / 2.f + 1e-5f
            let dy = dv.Y / 2.f + 1e-5f
            let c = v_min + (v_max - v_min) / 2.f
            let dr = Vector2(float32 i * dx, float32 j * dy)
            
            let mutable _v = c
            let mutable _n = node
            let mutable tmp = node

            let mutable I = abs i
            let mutable J = abs j
            while _n == tmp && (I > 0 || J > 0) do
                _v <- _v + dr
                _n <- traverse_retain _v _n           
                    
                if not (tmp == _n) then
                    tmp <- _n
                    I <- I - 1
                    J <- J - 1                    
            _n

        | Empty -> node
        


    type Node<'T> with
        member this.Item
            with get(i:int,j:int) =
                match (try_iterate i j this) with
                | Node _ -> failwith "should always traverse to a leaf node"
                | Leaf (_,v,_,_,_,_) -> v.Value
                | Empty -> ValueNone

            and set(i:int,j:int) value =
                match (try_iterate i j this) with
                | Node _ -> failwith "should always traverse to a leaf node"
                | Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome value
                | Empty -> ()



    let (|IsInternalNode|IsBoundaryNode|IsExternal|) u =
        match u with
        | Leaf _ -> 
            let b = u[-1,0]
            let d = u[0,-1]
            let e = u[0,0]
            let f = u[0,1]
            let h = u[1,0]

            if b.IsSome && d.IsSome && e.IsSome && f.IsSome && h.IsSome then
                IsInternalNode
            else
                IsBoundaryNode

        | Node _ ->
            failwith "ActivePattern of Internal/Boundary node has to be called on leaf-node"
        
        | Empty -> IsExternal 
   

    let get_value = function
        | Leaf (_,v,_,_,_,_) -> v.Value.Value
        | _ -> failwith "The tmp_node HAS to be a Leaf, with an ASSIGNED value!!"


    /// traverses the whole tree and trims / denses the quadants
    let rec update n k (node:Node<'T>) (pred_trim:Node<'T> -> bool) (pred_dense:Node<'T> -> bool) (set_value:Node<'T> -> 'T) =
        match node with
        | Node (p,c,i,_,_,_) when is_quadant node ->
            if pred_trim node then
                // printfn "trimmed"
                trim n k (ValueSome(set_value node)) node |> ignore

            elif pred_dense node then
                // printfn "densed"
                dense n node |> ignore

        | Node (_,c,_,_,_,_) ->
            for ci in c do update n k ci pred_trim pred_dense set_value

        | _ -> ()



    type Root<'T>(N:int, k:int, v_min:Vector2, v_max:Vector2) =
        let root = create<'T> N v_min v_max
        let n = log10 (float N) / log10 2. |> ceil |> int 
        let mutable cached_node = root
        let mutable tmp_node = root
        let mutable stencil: BitArray = null

        let rec dd v _n = if _n < n then dd (v/2.f) (_n + 1) else v

        member this.GetCount() =
            let mutable c = 0
            count_rec &c root
            c

        member this.GetTotalCount() =
            let mutable c = 0
            count_total_rec &c root
            c

        member this.MaxLevel = n

        member this.Rank = N

        member this.TrimLevel = k

        member this.Vmin = v_min
        member this.Vmax = v_max
        member this.Root = root

        member this.WritePoints(path:string) =
            use fs = System.IO.File.CreateText(path)
            write_vertices root fs
            fs.Close()

        member this.WriteRects(path:string) =
            use fs = System.IO.File.CreateText(path)
            write_rects root fs
            fs.Close()

        /// returns the Elements (their centers) of the quadtree as a list of points
        member this.AsPoints () =
            let points = ResizeArray<Vector2>(1000)
            vertices_to_points root points
            points.ToArray()

        member this.AsPolygons (fill: 'T -> float32) =
            let points = ResizeArray<Vector3>(1000)
            vertices_to_polygons root fill points
            points.ToArray()

        member this.GetValues () =
            let values = ResizeArray<'T>(1000)
            values_from_vertices root values
            values.ToArray()

        member this.dX = match root with | Node (_,_,_,_,v_min,v_max) -> dd (v_max.X - v_min.X) 0 | _ -> Single.NaN    
        member this.dY = match root with | Node (_,_,_,_,v_min,v_max) -> dd (v_max.Y - v_min.Y) 0 | _ -> Single.NaN    
        member this.Stencil with get() = stencil and set(value) = stencil <- value

        member internal this.Tmp_node with get() = tmp_node and set value = tmp_node <- value
        member internal this.Cached_node with get() = cached_node and set value = cached_node <- value

        member this.Put(x:double, y:double, value:voption<'T>) =
            let p = Vector2(float32 x, float32 y)
            cached_node <- traverse p n k cached_node
            match cached_node with
            | Leaf (_,v,_,_,_,_) -> v.Value <- value
            | _ -> failwith "Item.get failed"         

        member this.Update(pred_trim: Node<'T> -> bool, pred_dense: Node<'T> -> bool, set_value: Node<'T> -> 'T) =
            update n k root pred_trim pred_dense set_value

        // /// Caches the Tmp_node of the Quadtree for the designated location
        // member this.NodeAt(x:double, y:double) =
        //     let p = Vector2(float32 x, float32 y)
        //     tmp_node <- traverse_retain p tmp_node
        //     match tmp_node with
        //     | Leaf _ -> tmp_node
        //     | _ -> failwith "Item.get failed"

        member this.Item
            with get (x:double, y:double) =
                let p = Vector2(float32 x, float32 y)
                cached_node <- traverse_retain p cached_node
                match cached_node with
                | Leaf (_,v,_,_,_,_) -> v.Value
                | _ -> failwith "Item.get failed"

            and set (x:double, y:double) value =
                let p = Vector2(float32 x, float32 y)
                cached_node <- traverse_retain p cached_node
                match cached_node with
                | Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome value
                | _ -> failwith "Item.get failed"         
                    
    

    /// Builds a Quadtree out of a filled stencil
    /// The values of the Leafs are undefined
    let ofStencil<'T> N k (v_min:Vector2) (v_max:Vector2) (stencil:BitArray) =
        let quadtree = Root<'T>(N,k,v_min,v_max)
        quadtree.Stencil <- stencil        
        for i in 0..N-1 do
            for j in 0..N-1 do
                let v = GridGeneration2D.to_cartesian_system i j N v_min v_max
                if stencil[i*N+j] then
                    quadtree.Put(double v.X, double v.Y, ValueNone)
        quadtree       

    /// sets initial values at the Leaf s of a built Quadtree
    let init (value:'T) (quadtree:Root<'T>) =
        let N = quadtree.Rank
        let v_min = quadtree.Vmin
        let v_max = quadtree.Vmax
        let stencil = if quadtree.Stencil <> null then quadtree.Stencil else failwith "Quadtree must have initialized stencil"
        for i in 0..N-1 do
            for j in 0..N-1 do
                let v = GridGeneration2D.to_cartesian_system i j N v_min v_max
                if stencil[i*N+j] then
                    quadtree[double v.X, double v.Y] <- value
                //     quadtree.Put(double v.X, double v.Y, ValueSome value)
        quadtree
        

    /// iterate all the leaf nodes of the tree
    /// The equivalent of a for-loop for the quadtree
    let rec iter (fn:Node<'T> -> unit) (node:Node<'T>) =
        match node with
        | Node (_,c,_,_,_,_) -> for ci in c do iter fn ci            
        | Leaf _ -> fn node
        | Empty -> ()


    // /// walks towards the direction indicated by I and J and updates i j while return the current node tmp
    // /// It walks ONLY to next neighbour node
    // let walk I J (i:byref<int>) (j:byref<int>) (root:Root<'T>) =
    //     let N  = root.Rank
    //     let dx = root.dX
    //     let dy = root.dY
    //     let stencil = root.Stencil
    //     let v_min = root.Vmin
    //     let v_max = root.Vmax 

    //     let c = v_min + (v_max - v_min) / 2.f
    //     let dv = Vector2(float32 I * dx, float32 J * dy)
    //     let tmp = root.Tmp_node
        
    //     let mutable v = c
    //     let mutable n = tmp

    //     printfn "before walk: i: %d, j: %d" i j
    //     while n == tmp do
    //         v <- v + dv
    //         i <- i + I
    //         j <- j + J
    //         n <- traverse_retain v n
    //     printfn "after walk:  i: %d, j: %d" i j
    //     n
        


    // let update (tree:Root<'T>) (pred_trim:'T -> 'T -> bool) (pred_dense: 'T -> 'T -> bool) =
    //     let N = tree.Rank
    //     let stencil = tree.Stencil
    //     let v_min = tree.Vmin
    //     let v_max = tree.Vmax

    //     // let mutable i = 0
    //     // let mutable j = 0

    //     for i in 0..N-1 do
    //         for j in 0..N-1 do
    //             if stencil[i*N+j] then
    //                 printfn "walk in i,j: %d,%d" i j
    //                 let v = GridGeneration2D.to_cartesian_system i j N v_min v_max
    //                 let u = tree.NodeAt(double v.X, double v.Y)
    //                 ignore tree[1,0] 
    //                 let u_rhs = tree.Tmp_node
                    
    //                 ignore tree[0,1]
    //                 let u_dhs = tree.Tmp_node
    //                 // let mutable i0 = i
    //                 // let mutable j0 = j
    //                 // let mutable i1 = i
    //                 // let mutable j1 = j
    //                 // let u_rhs = walk 1 0 &j0 &i0 tree
    //                 // let u_dhs = walk 0 1 &j1 &i1 tree
    //                 // i <- ii
    //                 // j <- jj

    //                 // tree.Tmp_node <- dense tree.MaxLevel u   

    //                 if pred_trim (get_value u) (get_value u_rhs) then
    //                     let (Leaf (_,v,_,_,_,_)) = u
    //                     tree.Tmp_node <- trim tree.MaxLevel tree.TrimLevel v.Value u

    //                 elif pred_dense (get_value u) (get_value u_rhs) then
    //                     tree.Tmp_node <- dense tree.MaxLevel u   

    //                 if pred_trim (get_value u) (get_value u_dhs) then
    //                     let (Leaf (_,v,_,_,_,_)) = u
    //                     tree.Tmp_node <- trim tree.MaxLevel tree.TrimLevel v.Value u

    //                 elif pred_dense (get_value u) (get_value u_dhs) then
    //                     tree.Tmp_node <- dense tree.MaxLevel u   
                    
