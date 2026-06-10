namespace SE.Core

open System
open System.Numerics
open System.Runtime.CompilerServices


[<AllowNullLiteral>]
type Node<'T>(parent:Node<'T>, v_min:Vector3, v_max:Vector3) =
    let children = Array.zeroCreate<Node<'T>> 8
    let mutable value: ValueOption<'T> = ValueNone

    member this.V_min = v_min
    member this.V_max = v_max
    member this.Parent = parent
    member this.Children = children
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
            for c in node.Children do
                if c <> null then count_rec c

            if (check_children node.Children) then n <- n + 1
        count_rec node
        n
        
    let max_level (node:Node<'T>) = 
        let mutable n = 0
        let mutable m = 0
        let rec max_level_rec (node:Node<'T>) =
            n <- n + 1
            for c in node.Children do
                if c <> null then max_level_rec c
            n <- n - 1
        max_level_rec node
        n

    let intersect (p:Vector3) (v_min:Vector3) (v_max:Vector3) =
        let x = v_min.X <= p.X && p.X <= v_max.X
        let y = v_min.Y <= p.Y && p.Y <= v_max.Y
        let z = v_min.Z <= p.Z && p.Z <= v_max.Z
        x && y && z

    let rec traverse (p:Vector3) (node:Node<'T>) =
        // for i in 1..j do printf "  "
        // printfn "j: %d, for p: %A, node.V_min: %A, node.V_max: %A" j p node.V_min node.V_max
        if not (intersect p node.V_min node.V_max) then
            if j > 0 then
                j <- j - 1
                traverse p node.Parent 
            else
                // node
                // root
                // traverse_and_set p root v
                printfn "BoundinxBox: %A, %A" node.V_min node.V_max
                printfn "p: %A at j: %d" p j
                failwith "Vector3 p is does not intersect with the Octree bounding box"
        elif j >= n then // ensure that the level of the tree will not exceed min dx * dy * dz -- min octant
            j <- n
            // node.Value <- v
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
            | 1 -> 
                v_min <- Vector3(o.X, o.Y, node.V_min.Z)            
                v_max <- Vector3(node.V_max.X, node.V_max.Y, o.Z) 
            | 2 -> 
                v_min <- Vector3(node.V_min.X, node.V_min.Y, node.V_min.Z)            
                v_max <- Vector3(o.X, o.Y, o.Z) 
            | 3 -> 
                v_min <- Vector3(o.X, node.V_min.Y, node.V_min.Z)            
                v_max <- Vector3(node.V_max.X, o.Y, o.Z) 
            | 4 -> 
                v_min <- Vector3(node.V_min.X, o.Y, o.Z)            
                v_max <- Vector3(o.X, node.V_max.Y, node.V_max.Z) 
            | 5 -> 
                v_min <- Vector3(o.X, o.Y, o.Z)            
                v_max <- Vector3(node.V_max.X, node.V_max.Y, node.V_max.Z) 
            | 6 -> 
                v_min <- Vector3(node.V_min.X, node.V_min.Y, o.Z)            
                v_max <- Vector3(o.X, o.Y, node.V_max.Z) 
            | 7 -> 
                v_min <- Vector3(o.X, node.V_min.Y, o.Z)            
                v_max <- Vector3(node.V_max.X, o.Y, node.V_max.Z) 
            | _ ->
                failwith "improper idx value"

            let c = &node.Children[idx]
            c <- if c = null then Node(node, v_min, v_max) else c
            j <- j + 1
            traverse p c         
    
        
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

    // member this.Put(x:double, y:double, z:double, value:'T) =
    //     let p = Vector3(float32 x, float32 y, float32 z)
    //     current_node <- traverse p current_node value

    member this.Item
        with get (x:double, y:double, z:double) =
            let p = Vector3(float32 x, float32 y, float32 z)
            // get_value p current_node 
            current_node <- traverse p current_node 
            current_node.Value

        and set (x:double, y:double, z:double) value =
            let p = Vector3(float32 x, float32 y, float32 z)
            // set_value p value current_node pred
            current_node <- traverse p current_node 
            current_node.Value <- value


// type [<Struct>] Vec3 = {x:double; y:double; z:double}

// module Octree =
//     type Node<'T> =
//         | Node of parent:Node<'T> * children:Node<'T>[] * idx:int
//         | Leaf of parent:Node<'T> * value:ValueOption<'T> * idx:int  
//         | Empty


//     let create N (v_min:Vector3) (v_max:Vector3) =
//         let n = log10 (double N) / log10 2. |> ceil |> int
//         let j = 0
//         let node = Node (Empty, Array.create<Node<'T>> 8 Empty, 0)
//         node
        
//     // let rec traverse_forward (node:Node<'T>) (v_min:Vec3) (v_max:Vec3) =
//     //     match node with
//     //     | Node (p,c,i) -> traverse_forward c[i] v_min v_max
//     //     | Leaf (p,v,i) -> node

//     // let rec traverse_backward (node:Node<'T>) (v_min:Vec3) (v_max:Vec3) =
//     //     match node with
//     //     | Node (p,c,i) -> p
//     //     | Leaf (p,v,i) -> p

//     let intersect (p:Vector3) (v_min:Vector3) (v_max:Vector3) =
//         let x = v_min.X <= p.X && p.X <= v_max.X
//         let y = v_min.Y <= p.Y && p.Y <= v_max.Y
//         let z = v_min.Z <= p.Z && p.Z <= v_max.Z
//         x && y && z

//     let backward (v1:Vector3) (v2:Vector3) i =
//         match i with
//         | 0 ->
//             let x_min = v1.X
//             let y_min = v1.Y - (v2.Y - v1.Y)
//             let z_min = v1.Z
            
//             let x_max = v2.X + (v2.X - v1.X)
//             let y_max = v2.Y + (v2.Y - v1.Y)
//             let z_max = v2.Z + (v2.Z - v1.Z)
//             struct(Vector3(x_min, y_min, z_min), Vector3(x_max, y_max, z_max))
//         | 3 ->
//             let x_min = v1.X
//             let y_min = v1.Y
//             let z_min = v1.Z
            
//             let x_max = v2.X + (v2.X - v1.X)
//             let y_max = v2.Y + (v2.Y - v1.Y)
//             let z_max = v2.Z + (v2.Z - v1.Z)
//             struct(Vector3(x_min, y_min, z_min), Vector3(x_max, y_max, z_max))


//     let rec traverse (node:Node<'T>) (p:Vector3) (v_min:byref<Vector3>) (v_max:byref<Vector3>) (j:byref<int>) n =
//         match node with
//         | Node (parent,c,i) when not (intersect p v_min v_max) ->
//             let struct(v1, v2) = backward v_min v_max i
//             v_min <- v1
//             v_max <- v2
//             j <- j - 1
//             traverse parent p &v_min &v_max &j n 

//         | Node (_,c,i) ->   // traverse forward 
//             let o = v_min + (v_max - v_min) / 2f
//             let mutable idx = 0
//             idx <- idx + if p.X < o.X then 0 else 1
//             idx <- idx + if p.Y > o.Y then 0 else 2
//             idx <- idx + if p.Z < o.Z then 0 else 4
//             match idx with
//             | 0 ->
//                 v_min <- Vector3(v_min.X, o.Y, v_min.Z)            
//                 v_max <- Vector3(o.X, v_max.Y, o.Z) 
//             | 1 -> 
//                 v_min <- Vector3(o.X, o.Y, v_min.Z)            
//                 v_max <- Vector3(v_max.X, v_max.Y, o.Z) 
//             | 2 -> 
//                 v_min <- Vector3(v_min.X, v_min.Y, v_min.Z)            
//                 v_max <- Vector3(o.X, o.Y, o.Z) 
//             | 3 -> 
//                 v_min <- Vector3(o.X, v_min.Y, v_min.Z)            
//                 v_max <- Vector3(v_max.X, o.Y, o.Z) 
//             | 4 -> 
//                 v_min <- Vector3(v_min.X, o.Y, o.Z)            
//                 v_max <- Vector3(o.X, v_max.Y, v_max.Z) 
//             | 5 -> 
//                 v_min <- Vector3(o.X, o.Y, o.Z)            
//                 v_max <- Vector3(v_max.X, v_max.Y, v_max.Z) 
//             | 6 -> 
//                 v_min <- Vector3(v_min.X, v_min.Y, o.Z)            
//                 v_max <- Vector3(o.X, o.Y, v_max.Z) 
//             | 7 -> 
//                 v_min <- Vector3(o.X, v_min.Y, o.Z)            
//                 v_max <- Vector3(v_max.X, o.Y, v_max.Z) 
//             | _ ->
//                 failwith "improper idx value"

//             match c[idx] with
//             | Empty when j < n ->
//                 c[idx] <- Node (node, Array.create<Node<'T>> 8 Empty, idx) 
//                 j <- j + 1
//                 traverse c[idx] p &v_min &v_max &j n
//             | Empty ->
//                 c[idx] <- Leaf (node, ValueNone, idx)
//                 j <- j + 1
//                 traverse c[idx] p &v_min &v_max &j n
//             | _ ->
//                 j <- j + 1
//                 traverse c[idx] p &v_min &v_max &j n

//         | Leaf (p,v,i) -> node
//         | Empty -> failwith "traversed to empty node"
        


   
