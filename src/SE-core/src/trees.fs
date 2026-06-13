namespace SE.Core

open SE
open System
open System.Numerics
open System.Runtime.CompilerServices


[<AllowNullLiteral>]
type Node<'T>(parent:Node<'T>, v_min:Vector3, v_max:Vector3) =
    // let children = Array.zeroCreate<Node<'T>> 8
    let mutable value: ValueOption<'T> = ValueNone
    [<DefaultValue>] val mutable c0: Node<'T>
    [<DefaultValue>] val mutable c1: Node<'T>
    [<DefaultValue>] val mutable c2: Node<'T>
    [<DefaultValue>] val mutable c3: Node<'T>
    [<DefaultValue>] val mutable c4: Node<'T>
    [<DefaultValue>] val mutable c5: Node<'T>
    [<DefaultValue>] val mutable c6: Node<'T>
    [<DefaultValue>] val mutable c7: Node<'T>

    member this.V_min = v_min
    member this.V_max = v_max
    member this.Parent = parent
    // member this.Children = children
    member this.Value with get() = value.Value and set(v) = value <- ValueSome v
    member this.HasValue = value.IsSome

    
type Octree<'T>(N:int, v_min:Vector3, v_max:Vector3, pred: 'T -> 'T -> bool) =
    let n = log10 (double N) / log10 2. |> ceil |> int
    let root = Node(null, v_min, v_max)
    let mutable j = 0
    let mutable current_node = root

    let rec dd v _n = if _n < n then dd (v/2.f) (_n + 1) else v

    let check_children (children:Node<'T>[]) =
        let mutable b = true
        for c in children do b <- b && c = null
        b
    
    /// count ONLY leaf nodes
    let count (node:Node<'T>) = 
        let mutable n = 0
        let rec count_rec (node:Node<'T>) =
            if node.c0 <> null then count_rec node.c0 
            if node.c1 <> null then count_rec node.c1 
            if node.c2 <> null then count_rec node.c2 
            if node.c3 <> null then count_rec node.c3 
            if node.c4 <> null then count_rec node.c4 
            if node.c5 <> null then count_rec node.c5 
            if node.c6 <> null then count_rec node.c6 
            if node.c7 <> null then count_rec node.c7 

            if node.c0 = null &&
                node.c1 = null &&  
                node.c2 = null &&  
                node.c3 = null &&  
                node.c4 = null &&  
                node.c5 = null &&  
                node.c6 = null &&  
                node.c7 = null then n <- n + 1

            // for c in node.Children do
            //     if c <> null then count_rec c

            // if (check_children node.Children) then n <- n + 1
        count_rec node
        n
        
    // let max_level (node:Node<'T>) = 
    //     let mutable n = 0
    //     let mutable m = 0
    //     let rec max_level_rec (node:Node<'T>) =
    //         n <- n + 1
    //         for c in node.Children do
    //             if c <> null then max_level_rec c
    //         n <- n - 1
    //     max_level_rec node
    //     n

    let intersect (p:Vector3) (v_min:Vector3) (v_max:Vector3) =
        let x = v_min.X <= p.X && p.X <= v_max.X
        let y = v_min.Y <= p.Y && p.Y <= v_max.Y
        let z = v_min.Z <= p.Z && p.Z <= v_max.Z
        x && y && z

    let rec traverse (p:Vector3) (node:Node<'T>) n =
        // for i in 1..j do printf "  "
        // printfn "j: %d, for p: %A, node.V_min: %A, node.V_max: %A" j p node.V_min node.V_max
        if not (intersect p node.V_min node.V_max) then
            if j > 0 then
                j <- j - 1
                traverse p node.Parent n
            else
                // node
                // root
                // traverse_and_set p root v
                printfn "BoundinxBox: %A, %A" node.V_min node.V_max
                printfn "p: %A at j: %d" p j
                failwith "Vector3 p is does not intersect with the Octree bounding box"
        elif j >= n then // ensure that the level of the tree will not exceed min dx * dy * dz -- min octant
            // node.Value <- v
            // struct(node,n)
            j <- n
            node
        else
            let o = node.V_min + (node.V_max - node.V_min) / 2f
            let mutable idx = 0
            idx <- idx + if p.X < o.X then 0 else 1
            idx <- idx + if p.Y > o.Y then 0 else 2
            idx <- idx + if p.Z < o.Z then 0 else 4

            let mutable v_min = node.V_min
            let mutable v_max = node.V_max
            match idx with
            | 0 ->
                v_min <- Vector3(node.V_min.X, o.Y, node.V_min.Z)            
                v_max <- Vector3(o.X, node.V_max.Y, o.Z) 
                let c = &node.c0
                c <- if c = null then Node(node, v_min, v_max) else c
                j <- j + 1
                traverse p c n         
            | 1 -> 
                v_min <- Vector3(o.X, o.Y, node.V_min.Z)            
                v_max <- Vector3(node.V_max.X, node.V_max.Y, o.Z) 
                let c = &node.c1
                c <- if c = null then Node(node, v_min, v_max) else c
                j <- j + 1
                traverse p c n         
            | 2 -> 
                v_min <- Vector3(node.V_min.X, node.V_min.Y, node.V_min.Z)            
                v_max <- Vector3(o.X, o.Y, o.Z) 
                let c = &node.c2
                c <- if c = null then Node(node, v_min, v_max) else c
                j <- j + 1
                traverse p c n         
            | 3 -> 
                v_min <- Vector3(o.X, node.V_min.Y, node.V_min.Z)            
                v_max <- Vector3(node.V_max.X, o.Y, o.Z) 
                let c = &node.c3
                c <- if c = null then Node(node, v_min, v_max) else c
                j <- j + 1
                traverse p c n         
            | 4 -> 
                v_min <- Vector3(node.V_min.X, o.Y, o.Z)            
                v_max <- Vector3(o.X, node.V_max.Y, node.V_max.Z) 
                let c = &node.c4
                c <- if c = null then Node(node, v_min, v_max) else c
                j <- j + 1
                traverse p c n         
            | 5 -> 
                v_min <- Vector3(o.X, o.Y, o.Z)            
                v_max <- Vector3(node.V_max.X, node.V_max.Y, node.V_max.Z) 
                let c = &node.c5
                c <- if c = null then Node(node, v_min, v_max) else c
                j <- j + 1
                traverse p c n         
            | 6 -> 
                v_min <- Vector3(node.V_min.X, node.V_min.Y, o.Z)            
                v_max <- Vector3(o.X, o.Y, node.V_max.Z) 
                let c = &node.c6
                c <- if c = null then Node(node, v_min, v_max) else c
                j <- j + 1
                traverse p c n         
            | 7 -> 
                v_min <- Vector3(o.X, node.V_min.Y, o.Z)            
                v_max <- Vector3(node.V_max.X, o.Y, node.V_max.Z) 
                let c = &node.c7
                c <- if c = null then Node(node, v_min, v_max) else c
                j <- j + 1
                traverse p c n         
            | _ ->
                failwith "improper idx value"

            // let c = &node.Children[idx]
            // c <- if c = null then Node(node, v_min, v_max) else c
            // j <- j + 1
            // traverse p c         
            
            // match idx with
            // | 0 ->
            //     let c = &node.c0
            //     c <- if c = null then Node(node, v_min, v_max) else c
            //     j <- j + 1
            //     traverse p c         
            // | 1 ->
            //     let c = &node.c1
            //     c <- if c = null then Node(node, v_min, v_max) else c
            //     j <- j + 1
            //     traverse p c         
            // | 2 ->
            //     let c = &node.c2
            //     c <- if c = null then Node(node, v_min, v_max) else c
            //     j <- j + 1
            //     traverse p c         
            // | 3 ->
            //     let c = &node.c3
            //     c <- if c = null then Node(node, v_min, v_max) else c
            //     j <- j + 1
            //     traverse p c         
            // | 4 ->
            //     let c = &node.c4
            //     c <- if c = null then Node(node, v_min, v_max) else c
            //     j <- j + 1
            //     traverse p c         
            // | 5 ->
            //     let c = &node.c5
            //     c <- if c = null then Node(node, v_min, v_max) else c
            //     j <- j + 1
            //     traverse p c         
            // | 6 ->
            //     let c = &node.c6
            //     c <- if c = null then Node(node, v_min, v_max) else c
            //     j <- j + 1
            //     traverse p c         
            // | 7 ->
            //     let c = &node.c7
            //     c <- if c = null then Node(node, v_min, v_max) else c
            //     j <- j + 1
            //     traverse p c         
            // | _ -> failwith "improper idx"


    
        
    // /// The value of N will be dependent on n, because n = max_level N
    // let rec traverse (p:Vector3) (node:Node<'T>) (v:'T) =
    //     if not node.HasValue then
    //         node.Value <- v
    //         node
    //     elif (pred v node.Value) then
    //         node
    //     elif j > n then // ensure that the level of the tree will not exceed min dx * dy * dz -- min octant
    //         j <- n
    //         node
    //     elif not (intersect p node.V_min node.V_max) then
    //         // printfn "traverse back"
    //         if j > 0 then
    //             j <- j - 1
    //             traverse p node.Parent v
    //         else
    //             node
    //     else
    //         let o = node.V_min + (node.V_max - node.V_min) / 2f
    //         let mutable idx = 0
    //         idx <- idx + if p.X < o.X then 0 else 1
    //         idx <- idx + if p.Y > o.Y then 0 else 2
    //         idx <- idx + if p.Z < o.Z then 0 else 4

    //         let mutable v_min = node.V_min
    //         let mutable v_max = node.V_max
    //         match idx with
    //         | 0 ->
    //             v_min <- Vector3(node.V_min.X, o.Y, node.V_min.Z)            
    //             v_max <- Vector3(o.X, node.V_max.Y, o.Z) 
    //         | 1 -> 
    //             v_min <- Vector3(o.X, o.Y, node.V_min.Z)            
    //             v_max <- Vector3(node.V_max.X, node.V_max.Y, o.Z) 
    //         | 2 -> 
    //             v_min <- Vector3(node.V_min.X, node.V_min.Y, node.V_min.Z)            
    //             v_max <- Vector3(o.X, o.Y, o.Z) 
    //         | 3 -> 
    //             v_min <- Vector3(o.X, node.V_min.Y, node.V_min.Z)            
    //             v_max <- Vector3(node.V_max.X, o.Y, o.Z) 
    //         | 4 -> 
    //             v_min <- Vector3(node.V_min.X, o.Y, o.Z)            
    //             v_max <- Vector3(o.X, node.V_max.Y, node.V_max.Z) 
    //         | 5 -> 
    //             v_min <- Vector3(o.X, o.Y, o.Z)            
    //             v_max <- Vector3(node.V_max.X, node.V_max.Y, node.V_max.Z) 
    //         | 6 -> 
    //             v_min <- Vector3(node.V_min.X, node.V_min.Y, o.Z)            
    //             v_max <- Vector3(o.X, o.Y, node.V_max.Z) 
    //         | 7 -> 
    //             v_min <- Vector3(o.X, node.V_min.Y, o.Z)            
    //             v_max <- Vector3(node.V_max.X, o.Y, node.V_max.Z) 
    //         | _ ->
    //             failwith "improper idx value"

    //         let c = &node.Children[idx]
    //         c <- if c = null then Node(node, v_min, v_max) else c
    //         j <- j + 1
    //         traverse p c v        
        
    // /// traverse up to a leaf node and return the Data-value
    // let rec get_value (p:Vector3) (node:Node<'T>) =
    //     if not (intersect p node.V_min node.V_max) then
    //         if j > 0 then
    //             j <- j - 1
    //             get_value p node.Parent
    //         else
    //             current_node <- node
    //             node.Value
    //     else
    //         let o = node.V_min + (node.V_max - node.V_min) / 2f
    //         let mutable idx = 0
    //         let mutable v_min = node.V_min
    //         let mutable v_max = node.V_max

    //         idx <- idx + if p.X < o.X then 0 else 1
    //         idx <- idx + if p.Y > o.Y then 0 else 2
    //         idx <- idx + if p.Z < o.Z then 0 else 4

    //         let c = node.Children[idx]
    //         if c <> null then
    //             j <- j + 1
    //             get_value p c
    //         else
    //             current_node <- node
    //             node.Value
        
    // /// traverse up to a leaf node and set the Data-value
    // let rec set_value (p:Vector3) (v:'T) (node:Node<'T>) (pred: 'T -> 'T -> bool) =
    //     if not (intersect p node.V_min node.V_max) then
    //         if j > 0 then
    //             j <- j - 1
    //             set_value p v node.Parent pred
    //         else
    //             current_node <- node
    //             node.Value <- v
    //     else
    //         let o = node.V_min + (node.V_max - node.V_min) / 2f
    //         let mutable idx = 0
    //         let mutable v_min = node.V_min
    //         let mutable v_max = node.V_max

    //         idx <- idx + if p.X < o.X then 0 else 1
    //         idx <- idx + if p.Y > o.Y then 0 else 2
    //         idx <- idx + if p.Z < o.Z then 0 else 4

    //         let c = node.Children[idx]
    //         if c = null || (pred v node.Value) then
    //             current_node <- node
    //             node.Value <- v
    //         else
    //             j <- j + 1
    //             set_value p v c pred

    new (N:int, v_min:Vector3, v_max:Vector3) = Octree(N, v_min, v_max, (fun _ _ -> false))

    member this.GetCount() = count root

    member this.MaxLevel = n

    member this.dX = dd (root.V_max.X - root.V_min.X) 0
    
    member this.dY = dd (root.V_max.Y - root.V_min.Y) 0
    
    member this.dZ = dd (root.V_max.Z - root.V_min.Z) 0

    member this.Item
        with get (x:double, y:double, z:double) =
            let p = Vector3(float32 x, float32 y, float32 z)
            current_node <- traverse p current_node n
            if not current_node.HasValue then printfn "No value at %A" p
            current_node.Value

        and set (x:double, y:double, z:double) value =
            let p = Vector3(float32 x, float32 y, float32 z)
            current_node <- traverse p current_node n
            current_node.Value <- value


// type [<Struct>] Vec3 = {x:double; y:double; z:double}

module Quadtree2 =
    type Node<'T> =
        | Node of parent:Node<'T> * children:Node<'T>[] * idx:int * v_min:Vector2 * v_max:Vector2
        | Leaf of parent:Node<'T> * value:ref<ValueOption<'T>> * idx:int * v_min:Vector2 * v_max:Vector2 
        | Empty
        
    let rec write_vertices (node:Node<'T>) (fs:System.IO.StreamWriter) =
        match node with
        | Node (_,c,_,_,_) ->
            for ci in c do write_vertices ci fs

        | Leaf (_,v,_,v_min,v_max) ->
            let p = v_min + (v_max - v_min) / 2f
            fs.WriteLine($"{p.X}  {p.Y}")
            
        | Empty -> ()
        
    let rec write_rects (node:Node<'T>) (fs:System.IO.StreamWriter) =
        match node with
        | Node (_,c,_,_,_) ->
            for ci in c do write_rects ci fs

        | Leaf (_,v,_,v_min,v_max) ->
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
        let node = Node (Empty, Array.create<Node<'T>> 4 Empty, 0, v_min, v_max)
        node
        
    let children = function | Node (_,c,_,_,_) -> c | _ -> failwith "node has no children"

    let intersect (p:Vector2) (v_min:Vector2) (v_max:Vector2) =
        let x = v_min.X <= p.X && p.X <= v_max.X
        let y = v_min.Y <= p.Y && p.Y <= v_max.Y
        x && y

    let rec count_rec (j:byref<int>) = function
        | Node (_,children,_,_,_) ->
             for c in children do count_rec &j c
        | Leaf _ -> j <- j + 1
        | Empty -> ()


    let rec trim (node:Node<'T>) j k n =
        match node with
        | Node (_,c,_,_,_) ->  // traverse forward
            for ci in c do
                trim ci (j+1) k n

        | Leaf (p,_,i,_,_) ->  // trim deepest branch
            match p with
            | Node (gp,c,p_i,v_min,v_max) ->  // trim - replace Node with Leaf
                // let is_all_c_leafs =
                //     let mutable is_all_c_leafs = true
                //     for ci in c do
                //         is_all_c_leafs <- is_all_c_leafs && (match ci with | Leaf _ -> true |  _ -> false)
                //     is_all_c_leafs
                // if is_all_c_leafs then
                if k > 1 then
                    if (c |> Array.fold (fun b ci -> match ci with | Node _ -> false | _ -> b) true) then
                        (children gp)[p_i] <- Leaf (gp, ref ValueNone, p_i, v_min, v_max)                
                else
                    if (c |> Array.fold (fun b ci -> match ci with | Leaf _ -> b | _ -> false) true) then
                        (children gp)[p_i] <- Leaf (gp, ref ValueNone, p_i, v_min, v_max)                
                    // printfn "trimmed branch"
            | _ -> ()
        | _ -> ()


    let rec traverse (node:Node<'T>) (p:Vector2) j n =
        match node with
        | Empty -> failwith "traversed to empty node, make sure that root is not out of bounds"

        | Leaf (parent,_,_,v_min,v_max) when not (intersect p v_min v_max) ->
            traverse parent p (j-1) n
            
        | Leaf _ -> struct(node,j)

        | Node (parent,_,_,v_min,v_max) when not (intersect p v_min v_max) ->
            traverse parent p (j-1) n

        | Node (_,c,_,v1,v2) ->   // traverse forward 
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
            | Empty when j >= n ->
                c[idx] <- Leaf (node, ref ValueNone, idx, v_min, v_max)
                traverse c[idx] p (j+1) n

            | Empty ->
                c[idx] <- Node (node, Array.create<Node<'T>> 4 Empty, idx, v_min, v_max) 
                traverse c[idx] p (j+1) n
            | _ ->
                traverse c[idx] p (j+1) n


    type Root<'T>(N:int, v_min:Vector2, v_max:Vector2) =
        let root = create<'T> N v_min v_max
        let n = log10 (float N) / log10 2. |> ceil |> int 
        let mutable cached_node = root
        let mutable cached_j = 0

        let rec dd v _n = if _n < n then dd (v/2.f) (_n + 1) else v

        member this.GetCount() =
            let mutable c = 0
            count_rec &c root
            c

        member this.MaxLevel = n

        member this.Trim(k:int) = trim root 0 k n

        member this.WritePoints(path:string) =
            use fs = System.IO.File.CreateText(path)
            write_vertices root fs
            fs.Close()

        member this.WriteRects(path:string) =
            use fs = System.IO.File.CreateText(path)
            write_rects root fs
            fs.Close()

        member this.dX = match root with | Node (_,_,_,v_min,v_max) -> dd (v_max.X - v_min.X) 0 | _ -> Single.NaN    
        member this.dY = match root with | Node (_,_,_,v_min,v_max) -> dd (v_max.Y - v_min.Y) 0 | _ -> Single.NaN    

        member this.Item
            with get (x:double, y:double) =
                let p = Vector2(float32 x, float32 y)
                let struct(node,j) = traverse cached_node p cached_j n
                cached_node <- node
                cached_j <- j
                match cached_node with
                | Leaf (_,v,_,_,_) -> v.Value
                | _ -> failwith "Item.get failed"

            and set (x:double, y:double) value =
                let p = Vector2(float32 x, float32 y)
                let struct(node,j) = traverse cached_node p cached_j n
                cached_node <- node
                cached_j <- j
                match cached_node with
                | Leaf (p,v,j,v_min,v_max) -> v.Value <- ValueSome value
                | _ -> failwith "Item.get failed"         
                    
 
module Octree2 =
    type Node<'T> =
        | Node of parent:Node<'T> * children:Node<'T>[] * level:int * v_min:Vector3 * v_max:Vector3
        | Leaf of parent:Node<'T> * value:ref<ValueOption<'T>> * level:int * v_min:Vector3 * v_max:Vector3 
        | Empty


    let create<'T> N (v_min:Vector3) (v_max:Vector3) =
        let n = log10 (double N) / log10 2. |> ceil |> int
        let j = 0
        let node = Node (Empty, Array.create<Node<'T>> 8 Empty, 0, v_min, v_max)
        node
        
    let intersect (p:Vector3) (v_min:Vector3) (v_max:Vector3) =
        let x = v_min.X <= p.X && p.X <= v_max.X
        let y = v_min.Y <= p.Y && p.Y <= v_max.Y
        let z = v_min.Z <= p.Z && p.Z <= v_max.Z
        x && y && z

    let (==) a b = FSharp.Core.LanguagePrimitives.PhysicalEquality a b

    let parent = function | Leaf (p,_,_,_,_) | Node (p,_,_,_,_) -> p | Empty -> failwith "Parent on empty node failed"

    let children = function | Node (_,c,_,_,_) -> c | _ -> failwith "node has no children"

    
    // /// convert Node to Leaf
    // let trim (node:Node<'T>) (v:'T) =
    //     match node with
    //     | Node (p,_,j,v_min,v_max) when j > 0 ->
    //         let (Node (_,c,_,_,_)) = p
    //         let idx =
    //             let mutable idx = -1
    //             for i in 0..c.Length-1 do idx <- if (c[i] == node) then i else idx
    //             idx
    //         c[idx] <- Leaf(p,ref (ValueSome v),j,v_min,v_max)
    //     | _ ->
    //         failwith "must be a Node in order to trim"
            


    let rec traverse (node:Node<'T>) (p:Vector3) j n =
        match node with
        | Empty -> failwith "traversed to empty node, make sure that root is not out of bounds"

        | Leaf (parent,_,_,v_min,v_max) when not (intersect p v_min v_max) ->
            traverse parent p (j-1) n
            
        | Leaf _ -> struct(node,j)

        | Node (parent,_,_,v_min,v_max) when not (intersect p v_min v_max) ->
            traverse parent p (j-1) n

        | Node (_,c,_,v1,v2) ->   // traverse forward 
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

            match c[idx] with
            | Empty when j >= n ->
                c[idx] <- Leaf (node, ref ValueNone, idx, v_min, v_max)
                traverse c[idx] p (j+1) n

            | Empty ->
                c[idx] <- Node (node, Array.create<Node<'T>> 8 Empty, idx, v_min, v_max) 
                traverse c[idx] p (j+1) n
            | _ ->
                traverse c[idx] p (j+1) n
        


    type Root<'T>(N:int, v_min:Vector3, v_max:Vector3) =
        let root = create<'T> N v_min v_max
        let n = log10 (float N) / log10 2. |> ceil |> int 
        let mutable cached_node = root
        let mutable cached_j = 0

        let rec dd v _n = if _n < n then dd (v/2.f) (_n + 1) else v

        let rec count_rec (j:byref<int>) = function
            | Node (_,children,_,_,_) ->
                 for c in children do count_rec &j c
            | Leaf _ -> j <- j + 1
            | Empty -> ()

        member this.GetCount() =
            let mutable c = 0
            count_rec &c root
            c

        member this.MaxLevel = n

        member this.dX = match root with | Node (_,_,_,v_min,v_max) -> dd (v_max.X - v_min.X) 0 | _ -> Single.NaN    
        member this.dY = match root with | Node (_,_,_,v_min,v_max) -> dd (v_max.Y - v_min.Y) 0 | _ -> Single.NaN    
        member this.dZ = match root with | Node (_,_,_,v_min,v_max) -> dd (v_max.Z - v_min.Z) 0 | _ -> Single.NaN

        member this.Item
            with get (x:double, y:double, z:double) =
                let p = Vector3(float32 x, float32 y, float32 z)
                let struct(node,j) = traverse cached_node p cached_j n
                cached_node <- node
                cached_j <- j
                match cached_node with
                | Leaf (_,v,_,_,_) -> v.Value
                | _ -> failwith "Item.get failed"

            and set (x:double, y:double, z:double) value =
                let p = Vector3(float32 x, float32 y, float32 z)
                let struct(node,j) = traverse cached_node p cached_j n
                cached_node <- node
                cached_j <- j
                match cached_node with
                | Leaf (p,v,j,v_min,v_max) -> v.Value <- ValueSome value
                | _ -> failwith "Item.get failed"
                    


module GridGeneration2D =

    type BitArray = System.Collections.BitArray
    
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
        
        if abs(a.Y - b.Y) > dy then
            assign_stencil_element stencil N v_min v_max a (center a b)
            assign_stencil_element stencil N v_min v_max (center a b) b       
        else
            let c = center a b
            let (ci,cj) = to_stencil_system N c v_min v_max
            // stencil.SetBit(ci,cj, true)
            stencil[ci*N+cj] <- true

    let bitstencil (domain:Vector2[]) N =
        if N % 8 <> 0 then failwith "N must be multiplicative of 8 / byte for bitstencil"
        // let stencil = NativeArray2D.create<byte> (N/8) (N/8)
        let stencil = BitArray(N*N)
        let (v_min,v_max) = bounds domain

        for i in 0..domain.Length-2 do
            let a = domain[i+0]
            let b = domain[i+1]
            assign_stencil_element stencil N v_min v_max a b

            // let c = center a b
            // let (ci,cj) = to_stencil_system N c v_min v_max
            // stencil.SetBit(ci,cj, true)
        // for v in domain do
        //     let (i,j) = to_stencil_system N v v_min v_max 
        //     // stencil.SetBit(i,j, true)
        //     stencil[i*N+j] <- true
        //     // printfn "(%d,%d) bit_count: %d" i j (Bits.bits_count (stencil.GetByte(i,j)))
            
        stencil
        
    let measure_range (stencil:BitArray) N I = 
        let mutable lhs = 0
        let mutable rhs = N-1

        while not (stencil[I*N + lhs]) do lhs <- lhs + 1  // advance
        while not (stencil[I*N + rhs]) do rhs <- rhs - 1  // advance
    
        while (stencil[I*N + lhs]) do lhs <- lhs + 1  // advance
        while (stencil[I*N + rhs]) do rhs <- rhs - 1  // advance

        let a = min lhs rhs
        let b = max lhs rhs
        (a,b)

    let measure_marching_rows (stencil:BitArray) N I =
        let mutable n = 0
        let mutable j = 0
        while j < N do
            if stencil[I*N + j] then
                while stencil[I*N+j] do j <- j + 1  // advance
                n <- n + 1
            j <- j + 1
        n

    let fill_bitstencil N (v_min:Vector2) (v_max:Vector2) (stencil:BitArray) =
        let mutable i = 0
        while i < N do
            let (a,b) = measure_range stencil N i
            let mutable fill = true

            let current_row = measure_marching_rows stencil N i
            match current_row with
            | 2 -> 
                for j = a to b do stencil[i*N+j] <- true
            | 3 ->
                if (i+1) < N then
                    let lower_row = measure_marching_rows stencil N (i+1)

                    if current_row = lower_row then
                        for j = a to b do stencil[i*N+j] <- true                    
                    else
                        let mutable j = a
                        while j <= b do
                            if stencil[i*N+j] then
                                while stencil[i*N+j] do j <- j + 1  // advance
                                j <- j - 1
                                fill <- not fill
                    
                            if fill then stencil[i*N+j] <- true
                            j <- j + 1
                else
                    for j = a to b do stencil[i*N+j] <- true                    
            | 4 ->
                let mutable j = a
                while j <= b do
                    if stencil[i*N+j] then
                        while stencil[i*N+j] do j <- j + 1  // advance
                        j <- j - 1
                        fill <- not fill
                    
                    if fill then stencil[i*N+j] <- true
                    j <- j + 1

            | _ -> ()

            i <- i + 1
        stencil
                
            
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


