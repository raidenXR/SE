namespace SE.Core

open SE
open System
open System.Numerics
open System.Collections
open FSharp.Core
open System.Runtime.InteropServices
open System.Runtime.CompilerServices

module Octree =

    type [<Struct>] NodeKind = | Internal | Boundary | External

    type Node<'T> =
        | Node of parent:Node<'T> * children:Node<'T>[] * idx:int * level:int * v_min:Vector3 * v_max:Vector3
        | Leaf of parent:Node<'T> * value:ref<ValueOption<'T>> * idx:int * level:int * v_min:Vector3 * v_max:Vector3 
        | Empty

    let rec write_vertices (node:Node<'T>) (fs:System.IO.StreamWriter) =
        match node with
        | Node (_,c,_,_,_,_) ->
            for ci in c do write_vertices ci fs

        | Leaf (_,v,_,_,v_min,v_max) ->
            let p = v_min + (v_max - v_min) / 2f
            fs.WriteLine($"{p.X}  {p.Y}  {p.Z}")
            
        | Empty -> ()
        
    let rec vertices_to_points (node:Node<'T>) (points:ResizeArray<Vector3>) =
        match node with
        | Node (_,c,_,_,_,_) ->
            for ci in c do vertices_to_points ci points

        | Leaf (_,v,_,_,v_min,v_max) ->
            let p = v_min + (v_max - v_min) / 2f
            points.Add(p)
            
        | Empty -> ()
        
    let rec write_points_to_sb (node:Node<'T>) (sb:System.Text.StringBuilder) =
        match node with
        | Node (_,c,_,_,_,_) ->
            for ci in c do write_points_to_sb ci sb

        | Leaf (_,v,_,_,v_min,v_max) ->
            let p = v_min + (v_max - v_min) / 2f
            sb.AppendLine($"{p.X}  {p.Y}  {p.Z}") |> ignore
            
        | Empty -> ()
        
    let rec vertices_to_polygons (node:Node<'T>) (fill: 'T -> float32) (points:ResizeArray<Vector4>) =
        match node with
        | Node (_,c,_,_,_,_) ->
            for ci in c do vertices_to_polygons ci fill points

        | Leaf (_,v,_,_,v_min,v_max) ->
            let d = fill v.Value.Value
            let v0 = Vector4(v_min.X, v_min.Y, v_min.Z, d)
            let v1 = Vector4(v_min.X, v_max.Y, v_min.Z, d)
            let v2 = Vector4(v_max.X, v_max.Y, v_min.Z, d)
            let v3 = Vector4(v_max.X, v_min.Y, v_min.Z, d)

            let v4 = Vector4(v_min.X, v_min.Y, v_max.Z, d)
            let v5 = Vector4(v_min.X, v_max.Y, v_max.Z, d)
            let v6 = Vector4(v_max.X, v_max.Y, v_max.Z, d)
            let v7 = Vector4(v_max.X, v_min.Y, v_max.Z, d)

            points.Add(v0)
            points.Add(v1)
            points.Add(v2)            
            points.Add(v0)
            points.Add(v3)
            points.Add(v2)
            
            points.Add(v3)
            points.Add(v2)
            points.Add(v7)
            points.Add(v2)
            points.Add(v6)
            points.Add(v7)
            
            points.Add(v0)
            points.Add(v6)
            points.Add(v7)
            points.Add(v0)
            points.Add(v7)
            points.Add(v3)
            
            points.Add(v1)
            points.Add(v4)
            points.Add(v5)
            points.Add(v1)
            points.Add(v2)
            points.Add(v5)
            
            points.Add(v0)
            points.Add(v1)
            points.Add(v4)
            points.Add(v0)
            points.Add(v6)
            points.Add(v4)
            
            points.Add(v0)
            points.Add(v6)
            points.Add(v7)
            points.Add(v0)
            points.Add(v3)
            points.Add(v7)
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
            let v2 = Vector3(v_min.X, v_max.Y, v_max.Z)
            let v3 = v_max
            let v4 = Vector3(v_max.X, v_min.Y, v_min.Z)
            fs.WriteLine($"{v1.X}  {v1.Y}  {v1.Z}")
            fs.WriteLine($"{v2.X}  {v2.Y}  {v2.Z}")
            
            fs.WriteLine($"{v2.X}  {v2.Y}  {v2.Z}")
            fs.WriteLine($"{v3.X}  {v3.Y}  {v3.Z}")
            
            fs.WriteLine($"{v3.X}  {v3.Y}  {v3.Z}")
            fs.WriteLine($"{v4.X}  {v4.Y}  {v4.Z}")

            fs.WriteLine($"{v4.X}  {v4.Y}  {v4.Z}")
            fs.WriteLine($"{v1.X}  {v1.Y}  {v1.Z}")
            fs.WriteLine("\n")
            
        | Empty -> ()
        
    let rec write_rects_to_sb (node:Node<'T>) (sb:System.Text.StringBuilder) =
        match node with
        | Node (_,c,_,_,_,_) ->
            for ci in c do write_rects_to_sb ci sb

        | Leaf (_,v,_,_,v_min,v_max) ->
            let v1 = v_min
            let v2 = Vector3(v_min.X, v_max.Y, v_max.Z)
            let v3 = v_max
            let v4 = Vector3(v_max.X, v_min.Y, v_min.Z)
            sb.AppendLine($"{v1.X}  {v1.Y}  {v1.Z}") |> ignore
            sb.AppendLine($"{v2.X}  {v2.Y}  {v2.Z}") |> ignore
            
            sb.AppendLine($"{v2.X}  {v2.Y}  {v2.Z}") |> ignore
            sb.AppendLine($"{v3.X}  {v3.Y}  {v3.Z}") |> ignore
            
            sb.AppendLine($"{v3.X}  {v3.Y}  {v3.Z}") |> ignore
            sb.AppendLine($"{v4.X}  {v4.Y}  {v4.Z}") |> ignore

            sb.AppendLine($"{v4.X}  {v4.Y}  {v4.Z}") |> ignore
            sb.AppendLine($"{v1.X}  {v1.Y}  {v1.Z}") |> ignore
            sb.AppendLine("\n") |> ignore 
            
        | Empty -> ()
    

    let create<'T> N (v_min:Vector3) (v_max:Vector3) =
        let n = log10 (double N) / log10 2. |> ceil |> int
        let j = 0
        let node = Node (Empty, Array.create<Node<'T>> 8 Empty, 0, 0, v_min, v_max)
        node
        
    let parent = function | Node (p,_,_,_,_,_) | Leaf (p,_,_,_,_,_) -> p | Empty -> failwith "attempted to get parent on Empty Node"
    
    let children = function | Node (_,c,_,_,_,_) -> c | _ -> failwith "node has no children"

    let center = function | Leaf (_,_,_,_,v_min,v_max) | Node (_,_,_,_,v_min,v_max) -> v_min + (v_max - v_min) / 2.f | Empty -> failwith "attempty to get center of Empty node"

    let (==) a b = FSharp.Core.LanguagePrimitives.PhysicalEquality a b

    let intersect (p:Vector3) (v_min:Vector3) (v_max:Vector3) =
        let x = v_min.X <= p.X && p.X <= v_max.X
        let y = v_min.Y <= p.Y && p.Y <= v_max.Y
        let z = v_min.Z <= p.Z && p.Z <= v_max.Z
        x && y && z

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

    let rec copy (source:Node<'T>) (dest:Node<'T>) =
        match (source,dest) with
        | Node (_,c,i,l,v_min,v_max), Node (_,_,_,l',_,_) when l = l' ->
            for i in 0..c.Length-1 do 
                copy c[i] dest

        | Node (_,c,i,l,v_min,v_max), Node (_,c',_,l',_,_) when l' < l ->
            let dest' = Node (dest,(Array.create<Node<'T>> 8 Empty),i,l,v_min,v_max)
            c'[i] <- dest'
            
            for i in 0..c.Length-1 do 
                copy c[i] dest'

        | Leaf (_,v,i,l,v_min,v_max), Node (_,c',_,_,_,_) ->
            c'[i] <- Leaf (dest,(ref v.Value),i,l,v_min,v_max)

        | Empty, _ -> ()

        | _, _ -> failwith "Not Implemented case"

    
    let rec copy_value (source:Node<'T>) (dest:Node<'T>) =
        match (source,dest) with
        | Node (_,c,i,l,_,_), Node (_,c',i',l',_,_) when l = l' && i = i' ->
            for j in 0..c.Length-1 do 
                copy c[j] c'[j]

        | Leaf (_,v,_,_,_,_), Leaf (_,v',_,_,_,_) ->
            v'.Value <- v.Value

        | Empty, Empty -> ()

        | _, _ -> failwith "Not Implemented case"


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
            let _children = Array.create<Node<'T>> 8 Empty
            let _this = Node (p, _children, i, l, v1, v2)
            let _value = v.Value
            (children p)[i] <- _this            

            let o = v1 + (v2 - v1) / 2f
            do
                let v_min = Vector3(v1.X, o.Y, v1.Z)            
                let v_max = Vector3(o.X, v2.Y, o.Z) 
                _children[0] <- Leaf (_this, ref _value, 0, (l+1), v_min, v_max)
            do
                let v_min = Vector3(o.X, o.Y, v1.Z)            
                let v_max = Vector3(v2.X, v2.Y, o.Z) 
                _children[1] <- Leaf (_this, ref _value, 1, (l+1), v_min, v_max)
            do
                let v_min = Vector3(v1.X, v1.Y, v1.Z)            
                let v_max = Vector3(o.X, o.Y, o.Z) 
                _children[2] <- Leaf (_this, ref _value, 2, (l+1), v_min, v_max)
            do
                let v_min = Vector3(o.X, v1.Y, v1.Z)            
                let v_max = Vector3(v2.X, o.Y, o.Z) 
                _children[3] <- Leaf (_this, ref _value, 3, (l+1), v_min, v_max)

            do
                let v_min = Vector3(v1.X, o.Y, o.Z)            
                let v_max = Vector3(o.X, v2.Y, v2.Z) 
                _children[4] <- Leaf (_this, ref _value, 4, (l+1), v_min, v_max)

            do
                let v_min = Vector3(o.X, v1.Y, o.Z)            
                let v_max = Vector3(v2.X, v2.Y, v2.Z) 
                _children[5] <- Leaf (_this, ref _value, 5, (l+1), v_min, v_max)

            do
                let v_min = Vector3(v1.X, v1.Y, o.Z)            
                let v_max = Vector3(o.X, o.Y, v2.Z) 
                _children[6] <- Leaf (_this, ref _value, 6, (l+1), v_min, v_max)

            do
                let v_min = Vector3(o.X, v1.Y, o.Z)            
                let v_max = Vector3(v2.X, o.Y, v2.Z) 
                _children[7] <- Leaf (_this, ref _value, 7, (l+1), v_min, v_max)

            _this

        | Node (_,c,_,_,_,_) ->
            for ci in c do dense n ci |> ignore
            c[0]

        | _ -> node


    let rec traverse (p:Vector3) n k (node:Node<'T>) =
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
            | Empty when l >= n ->
                c[idx] <- Leaf (node, ref ValueNone, idx, n, v_min, v_max)
                traverse p n k c[idx]
                |> trim n k ValueNone

            | Empty ->
                c[idx] <- Node (node, Array.create<Node<'T>> 8 Empty, idx, (l+1), v_min, v_max) 
                traverse p n k c[idx]
                |> trim n k ValueNone
                
            | _ ->
                traverse p n k c[idx]
                |> trim n k ValueNone

        
    let rec traverse_retain (p:Vector3) (node:Node<'T>) =
        match node with
        | Empty -> node

        | Leaf (parent,_,_,l,v_min,v_max) when not (intersect p v_min v_max) ->
            traverse_retain p parent
            
        | Leaf _ -> node

        | Node (parent,_,_,l,v_min,v_max) when not (intersect p v_min v_max) ->
            traverse_retain p parent

        | Node (_,c,_,l,v_min,v_max) ->   // traverse forward 
            let mutable idx = 0
            let o = v_min + (v_max - v_min) / 2f
            idx <- idx + if p.X < o.X then 0 else 1
            idx <- idx + if p.Y > o.Y then 0 else 2
            idx <- idx + if p.Z < o.Z then 0 else 4

            if idx < 0 || idx > 7 then failwith "improper idx value"

            match c[idx] with
            | Empty -> c[idx]                
            | _ -> traverse_retain p c[idx]              
               

    /// return the node closest to the point
    /// NOT nessesary a leaf node, just the furthest lavel that intersects x,y from the root
    let rec traverse_map (p:Vector3) (node:Node<'T>) =
        match node with
        | Empty -> failwith "MUST not traverse to EMPTY"

        | Leaf (parent,_,_,l,v_min,v_max) when not (intersect p v_min v_max) ->
            traverse_map p parent
            
        | Leaf _ -> node

        | Node (parent,_,_,l,v_min,v_max) when not (intersect p v_min v_max) ->
            traverse_map p parent

        | Node (_,c,_,l,v_min,v_max) ->   // traverse forward 
            let mutable idx = 0
            let o = v_min + (v_max - v_min) / 2f
            idx <- idx + if p.X < o.X then 0 else 1
            idx <- idx + if p.Y > o.Y then 0 else 2
            idx <- idx + if p.Z < o.Z then 0 else 4

            if idx < 0 || idx > 7 then failwith "improper idx value"

            match c[idx] with
            | Empty -> node                
            | _ -> traverse_map p c[idx]              

    // let contains i j idx =        
    //     0 <= i + j + idx && i + j + idx <= 7

    /// dx and dy are the minumum dv elements of the tree
    let iterate_node i j k (node:Node<'T>) =
        match node with
        | _ when i = 0 && j = 0 -> node

        | Leaf (_,_,_,_,v_min,v_max) | Node (_,_,_,_,v_min,v_max) ->
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
            while intersect v v_min v_max  && (I > 0 || J > 0 || K > 0) do
                v <- v + dv   // displace the point until it does not intersect the cell

                if not (intersect v v_min v_max) then
                    I <- I - 1
                    J <- J - 1
                    K <- K - 1

            traverse_retain v node 
            
        | Empty -> failwith "run iterate on EMPTY node, failed"


    /// dx and dy are the minumum dv elements of the tree
    let iterate i j k dx dy dz (node:Node<'T>) =
        match node with
        | _ when i = 0 && j = 0 -> node
        
        // | Empty -> failwith "cannot iterate on Empty node"
        // | Empty -> node

        | Leaf (_,_,_,_,v_min,v_max) | Node (_,_,_,_,v_min,v_max) ->
            let c = v_min + (v_max - v_min) / 2.f
            let dx' = float32(sign i) * dx
            let dy' = float32(sign j) * dy
            let dz' = float32(sign k) * dz
            let dv = Vector3(dx', dy', dz')
            
            let mutable v = c
            // let mutable n = node
            // let mutable tmp = node

            let mutable I = abs i
            let mutable J = abs j
            let mutable K = abs k

            // this will work only for on cell displacement
            while intersect v v_min v_max  && (I > 0 || J > 0 || K > 0) do
                v <- v + dv   // displace the point until it does not intersect the cell

                if not (intersect v v_min v_max) then
                    I <- I - 1
                    J <- J - 1
                    K <- K - 1

            traverse_retain v node 
            
        | Empty -> failwith "run iterate on EMPTY node, failed"


    let rec iterate_sum (add:'T -> 'T -> 'T) (_n:byref<int>) (acc:byref<'T>) (node:Node<'T>) = 
        match node with
        | Leaf (_,v,_,_,_,_) ->
            _n <- _n + 1
            acc <- add acc v.Value.Value
            
        | Node (_,c,_,_,_,_) ->
            for ci in c do iterate_sum add &_n &acc ci

        | Empty -> ()


    let valueof = function
        | Leaf (_,v,_,_,_,_) -> v.Value.Value
        | _ -> failwith "The tmp_node HAS to be a Leaf, with an ASSIGNED value!!"

    /// iterate all the leaf nodes of the tree
    /// The equivalent of a for-loop for the quadtree
    let rec iter (fn:Node<'T> -> unit) (node:Node<'T>) =
        match node with
        | Node (_,c,_,_,_,_) -> for ci in c do iter fn ci            
        | Leaf _ -> fn node
        | Empty -> ()


    /// traverses the whole tree and trims / denses the quadants
    let rec update n k (node:Node<'T>) (pred_trim:Node<'T> -> bool) (pred_dense:Node<'T> -> bool) (set_value:Node<'T> -> 'T) =
        match node with
        | Node (p,c,i,_,_,_) when is_quadant node ->
            if pred_trim node then
                trim n k (ValueSome(set_value node)) node |> ignore

            elif pred_dense node then
                dense n node |> ignore

        | Node (_,c,_,_,_,_) ->
            for ci in c do update n k ci pred_trim pred_dense set_value

        | _ -> ()


    let rec morph (source:Node<'T>) (dest:Node<'T>) add div =
        match (source,dest) with
        | Node (_,c,i,l,_,_), Node (_,c',i',l',_,_) when i = i' && l = l' ->
            for j in 0..c.Length-1 do morph c[j] c'[j] add div

        | Node (_,c,i,l,_,_), Node (_,c',i',l',_,_) ->
            // for j in 0..c'.Length-1 do morph c[j] c'[j] add div   //  keep the source constant and traverse-forward the dest
            for j in 0..c'.Length-1 do morph source c'[j] add div   //  keep the source constant and traverse-forward the dest
        
        // | Leaf (_,v,i,l,_,_), Leaf (_,v',i',l',_,_) when i = i' && l = l' ->
        | Leaf (_,v,i,l,_,_), Leaf (_,v',i',l',_,_) ->
            v'.Value <- ValueSome v.Value.Value

        | Node (_,c,i,l,_,_), Leaf (_,v,_,_,_,_) ->
            let mutable _n = 0
            let mutable _t = Operators.Unchecked.defaultof<'T> 

            iterate_sum add &_n &_t source
            v.Value <- ValueSome (div _t (double _n)) 

        | Leaf (_,v,_,_,_,_), Node (_,c,_,_,_,_) ->
            for ci in c do morph dest ci add div

        | Empty, _ -> ()

        | _, Empty -> ()


    let private task_new (fn:unit -> unit) = System.Threading.Tasks.Task.Factory.StartNew(fn)


    type Node<'T> with
        /// indexer on Node<'T> returns the local neighbours of the node
        member this.Item
            with get (i:int, j:int, k:int) = iterate_node i j k this

    type Root<'T>(N:int, _k:int, v_min:Vector3, v_max:Vector3) =
        let root = create<'T> N v_min v_max
        let n = log10 (float N) / log10 2. |> ceil |> int 
        let mutable cached_node = root
        let mutable stencil: BitArray = null
        let mutable add: ('T -> 'T -> 'T) = (fun a _ -> a)
        let mutable div: ('T -> double -> 'T) = (fun a _ -> a)

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

        member this.TrimLevel = _k

        member this.Vmin = v_min
        member this.Vmax = v_max
        member this.Root = root

        member this.Copy() =
            let copy' = Root<'T>(N,_k,v_min,v_max)
            copy root (copy'.Root) |> ignore
            
            if this.Stencil <> null then
                let stencil' = BitArray(N*N)
                for i in 0..N-1 do
                    for j in 0..N-1 do
                        for k in 0..N-1 do
                            stencil'[i*N*N+j*N+k] <- stencil[i*N*N+j*N+k]                    
                        
                copy'.Stencil <- stencil'
            copy'

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
            let points = ResizeArray<Vector3>(1000)
            vertices_to_points root points
            points.ToArray()

        member this.AsPolygons (fill: 'T -> float32) =
            let points = ResizeArray<Vector4>(1000)
            vertices_to_polygons root fill points
            points.ToArray()

        /// returns an array of all the elements values
        member this.GetValues () =
            let values = ResizeArray<'T>(1000)
            values_from_vertices root values
            values.ToArray()

        member this.CurrentNode with get() = cached_node and set value = cached_node <- value
        member this.dX = match root with | Node (_,_,_,_,v_min,v_max) -> dd (v_max.X - v_min.X) 0 | _ -> failwith "Root MUST be Node"   
        member this.dY = match root with | Node (_,_,_,_,v_min,v_max) -> dd (v_max.Y - v_min.Y) 0 | _ -> failwith "Root MUST be Node"   
        member this.dZ = match root with | Node (_,_,_,_,v_min,v_max) -> dd (v_max.Z - v_min.Z) 0 | _ -> failwith "Root MUST be Node"   
        member this.Stencil with get() = stencil and set(value) = stencil <- value

        member this.Add with set value = add <- value
        member this.Div with set value = div <- value

        // member internal this.Tmp_node with get() = tmp_node and set value = tmp_node <- value
        member internal this.Cached_node with get() = cached_node and set value = cached_node <- value

        member this.Put(x:double, y:double, z:double, value:voption<'T>) =
            let p = Vector3(float32 x, float32 y, float32 z)
            cached_node <- traverse p n _k cached_node
            match cached_node with
            | Leaf (_,v,_,_,_,_) -> v.Value <- value
            | _ -> failwith "Item.get failed"         

        member this.Update(pred_trim: Node<'T> -> bool, pred_dense: Node<'T> -> bool, set_value: Node<'T> -> 'T) =
            update n _k root pred_trim pred_dense set_value


        member this.Item
            with get (x:double, y:double, z:double) =
                let p = Vector3(float32 x, float32 y, float32 z)
                cached_node <- traverse_retain p cached_node
                match cached_node with
                | Leaf (_,v,_,_,_,_) -> v.Value
                | _ -> failwith "Item.get failed"

            and set (x:double, y:double, z:double) value =
                let p = Vector3(float32 x, float32 y, float32 z)
                cached_node <- traverse_retain p cached_node
                match cached_node with
                | Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome value
                | _ -> failwith "Item.get failed"         

                    
        member this.Item
            with get(i:int,j:int,k:int) =
                match (iterate i j k this.dX this.dY this.dZ cached_node) with
                | Leaf (_,v,_,_,_,_) -> v.Value.Value
                | Node (_,c,_,_,_,_) ->
                    let mutable _n = 0
                    let mutable _t = Operators.Unchecked.defaultof<'T>
                    iterate_sum add &_n &_t cached_node
                    div _t (double _n)                    
                // | Empty -> valueof cached_node
                | Empty ->                
                    let mutable _n = 0
                    let mutable _t = Operators.Unchecked.defaultof<'T>
                    iterate_sum add &_n &_t cached_node    // if current node is EMPTY use the values of rest Leafs of quadant(?)
                    div _t (double _n)                    
                // | Empty -> failwith "should always traverse to a leaf node"
                // | _ -> failwith "should always traverse to a leaf node"

            and set(i:int,j:int,k:int) value =
                match (iterate i j k this.dX this.dY this.dZ cached_node) with
                | Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome value
                | _ -> failwith "should always traverse to a leaf node"

        member this.MapTo(x:double, y:double, z:double) =
            cached_node <- traverse_map (Vector3(float32 x, float32 y, float32 z)) root
    
        /// Experimental method, DOT NOT take for granted that it works...
        member this.IterParallel (num_threads:int) (fn:Node<'T> -> unit) =
            match num_threads with
            | 1 ->
                iter fn root
            | 2 ->
                let c = children root
                let t0 = task_new (fun _ -> iter fn c[0]; iter fn c[1]; iter fn c[2]; iter fn c[3])
                let t1 = task_new (fun _ -> iter fn c[4]; iter fn c[5]; iter fn c[6]; iter fn c[7])
                t0.Wait()
                t1.Wait()
            | 3 ->
                let c = children root
                let t0 = task_new (fun _ -> iter fn c[0]; iter fn c[1]; iter fn c[2])
                let t1 = task_new (fun _ -> iter fn c[3]; iter fn c[4]; iter fn c[5])
                let t2 = task_new (fun _ -> iter fn c[6]; iter fn c[7])
                t0.Wait()
                t1.Wait()
                t2.Wait()
            | 4 ->
                let c = children root
                let t0 = task_new (fun _ -> iter fn c[0]; iter fn c[1])
                let t1 = task_new (fun _ -> iter fn c[2]; iter fn c[3])
                let t2 = task_new (fun _ -> iter fn c[4]; iter fn c[5])
                let t3 = task_new (fun _ -> iter fn c[6]; iter fn c[7])
                t0.Wait()
                t1.Wait()
                t2.Wait()
                t3.Wait()
            | 5 ->
                let c = children root
                let t0 = task_new (fun _ -> iter fn c[0]; iter fn c[1])
                let t1 = task_new (fun _ -> iter fn c[2]; iter fn c[3])
                let t2 = task_new (fun _ -> iter fn c[4]; iter fn c[5])
                let t3 = task_new (fun _ -> iter fn c[6])
                let t4 = task_new (fun _ -> iter fn c[7])
                t0.Wait()
                t1.Wait()
                t2.Wait()
                t3.Wait()
                t4.Wait()
            | 6 ->
                let c = children root
                let t0 = task_new (fun _ -> iter fn c[0]; iter fn c[1])
                let t1 = task_new (fun _ -> iter fn c[2]; iter fn c[3])
                let t2 = task_new (fun _ -> iter fn c[4])
                let t3 = task_new (fun _ -> iter fn c[5])
                let t4 = task_new (fun _ -> iter fn c[6])
                let t5 = task_new (fun _ -> iter fn c[7])
                t0.Wait()
                t1.Wait()
                t2.Wait()
                t3.Wait()
                t4.Wait()
                t5.Wait()
            | 7 ->
                let c = children root
                let t0 = task_new (fun _ -> iter fn c[0]; iter fn c[1])
                let t1 = task_new (fun _ -> iter fn c[2])
                let t2 = task_new (fun _ -> iter fn c[3])
                let t3 = task_new (fun _ -> iter fn c[4])
                let t4 = task_new (fun _ -> iter fn c[5])
                let t5 = task_new (fun _ -> iter fn c[6])
                let t6 = task_new (fun _ -> iter fn c[7])
                t0.Wait()
                t1.Wait()
                t2.Wait()
                t3.Wait()
                t4.Wait()
                t5.Wait()
                t6.Wait()
            | _ ->
                let c = children root
                let t0 = task_new (fun _ -> iter fn c[0])
                let t1 = task_new (fun _ -> iter fn c[1])
                let t2 = task_new (fun _ -> iter fn c[2])
                let t3 = task_new (fun _ -> iter fn c[3])
                let t4 = task_new (fun _ -> iter fn c[4])
                let t5 = task_new (fun _ -> iter fn c[5])
                let t6 = task_new (fun _ -> iter fn c[6])
                let t7 = task_new (fun _ -> iter fn c[7])
                t0.Wait()
                t1.Wait()
                t2.Wait()
                t3.Wait()
                t4.Wait()
                t5.Wait()
                t6.Wait()
                t7.Wait()



    /// Builds a Quadtree out of a filled stencil
    /// The values of the Leafs are undefined
    let ofStencil<'T> N _k (v_min:Vector3) (v_max:Vector3) (stencil:BitArray) =
        let quadtree = Root<'T>(N,_k,v_min,v_max)
        quadtree.Stencil <- stencil        
        for i in 0..N-1 do
            for j in 0..N-1 do
                for k in 0..N-1 do
                    let v = GridGeneration3D.to_cartesian_system i j k N v_min v_max
                    if stencil[i*N*N+j*N+k] then
                        quadtree.Put(double v.X, double v.Y, double v.Z, ValueNone)
        quadtree       

    /// sets initial values at the Leaf s of a built Quadtree
    let init (value:'T) (quadtree:Root<'T>) =
        let N = quadtree.Rank
        let v_min = quadtree.Vmin
        let v_max = quadtree.Vmax
        let stencil = if quadtree.Stencil <> null then quadtree.Stencil else failwith "Quadtree must have initialized stencil"
        for i in 0..N-1 do
            for j in 0..N-1 do
                for k in 0..N-1 do
                    let v = GridGeneration3D.to_cartesian_system i j k N v_min v_max
                    if stencil[i*N*N+j*N+k] then
                        // quadtree[double v.X, double v.Y, double v.Z] <- value
                        quadtree.Put(double v.X, double v.Y, double v.Z, ValueSome value)
        quadtree
        
    // let kindof dx dy dz (u:Node<'T>) =
    //     let b = iterate -1 0 0 dx dy dz u
    //     let d = iterate 0 -1 0 dx dy dz u
    //     let k = iterate 0 0 -1 dx dy dz u
    //     let e = iterate 0 0 0 dx dy dz u
    //     let f = iterate 0 1 0 dx dy dz u
    //     let h = iterate 1 0 0 dx dy dz u
    //     let j = iterate 0 0 1 dx dy dz u

    //     match (b,d,e,f,h,k,j) with
    //     | _,_,Empty,_,_,_,_ -> External
    //     | Leaf _, Leaf _, Leaf _, Leaf _, Leaf _, Leaf _, Leaf _ -> Internal
    //     | _,_,_,_,_,_,_ -> Boundary

    let kindof (u:Node<'T>) =
        let b = iterate_node -1 0 0 u
        let d = iterate_node 0 -1 0 u
        let k = iterate_node 0 0 -1 u
        // let e = iterate_node 0 0 0 u
        let f = iterate_node 0 1 0 u
        let h = iterate_node 1 0 0 u
        let j = iterate_node 0 0 1 u

        match (b,d,u,f,h,k,j) with
        | _,_,Empty,_,_,_,_ -> External
        | Leaf _, Leaf _, Leaf _, Leaf _, Leaf _, Leaf _, Leaf _ -> Internal
        | _,_,_,_,_,_,_ -> Boundary

    
    let contains (p:Vector3) (node:Node<'T>) =
        match (traverse_retain p node) with
        | Leaf _ -> true
        | Empty -> false
        | Node _ -> failwith "contains SHOULD traverse to deepest level"


    let fill_raycast N L (v_min:Vector3) (v_max:Vector3) (vertices:Span<float32>) (indices:Span<uint32>) (stencil:BitArray) =
        let dx = (v_max.X - v_min.X) / float32 N
        let dy = (v_max.Y - v_min.Y) / float32 N
        let dz = (v_max.Z - v_min.Z) / float32 N
        let vs = ResizeArray<Vector3>(1024)
        let tree = Root<byte>(N, 0, v_min, v_max)
        
        let center = GridGeneration3D.center
        let triangle_center = GridGeneration3D.triangle_center
            
        let rec subdivide (a:Vector3) (b:Vector3) (c:Vector3) =
            if abs(b.X - c.X) > dx || abs(b.Y - c.Y) > dy || abs(b.Z - c.Z) > dz then
                let ab = center a b
                let ac = center a c
                let bc = center b c
                subdivide a ab ac 
                subdivide ab b bc       
                subdivide ab bc ac       
                subdivide ac bc c     
            else
                vs.Add(triangle_center a b c)                
                
        printfn "start subdividing"
        let indices_count = indices.Length / 3
        let p = &MemoryMarshal.GetReference(vertices)
        for i in 1..indices_count-1 do
            let i0 = int32 (indices[3*i+0])
            let i1 = int32 (indices[3*i+1])
            let i2 = int32 (indices[3*i+2])

            let v0 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i0))
            let v1 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i1))
            let v2 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i2))

            subdivide v0 v1 v2

        printfn "constructing boundaries quadtree"
        for v in vs do
            tree.Put(double v.X, double v.Y, double v.Z, ValueNone)

        printfn "start raycasting, vs.len: %d" (vs.Count)
        for I in 2..vs.Count-1 do
            let a = vs[I-2]
            let b = vs[I-1]
            let c = vs[I-0]
            let d = triangle_center a b c
            // Nx = Ay * Bz - Az * By
            // Ny = Az * Bx - Ax * Bz
            // Nz = Ax * By - Ay * Bx
            let n = -Vector3(a.Y*b.Z - a.Z*b.Y, a.Z*b.X - a.X*b.Z, a.X*b.Y - a.Y*b.X)
            let (i,j,k) = GridGeneration3D.to_stencil_system N d v_min v_max 
            let i' = sign(n.Y)
            let j' = sign(n.X)
            let k' = sign(n.Z)

            let mutable ii = i + i'
            let mutable jj = j + j'
            let mutable kk = k + k'
            let mutable r = GridGeneration3D.to_cartesian_system ii jj kk N v_min v_max
            let mutable J = 1
            
            // printfn "i: %d" I
            while not (contains r tree.Root) && J < N do
                if (ii >= 0 && ii < N && jj >= 0 && jj < N && kk >= 0 && kk < N) then
                    stencil[ii*N*N+jj*N+kk] <- true
                ii <- ii + i'
                jj <- jj + j'
                kk <- kk + k'
                r <- GridGeneration3D.to_cartesian_system ii jj kk N v_min v_max
                J <- J + 1              
        (vs.ToArray(),stencil)

