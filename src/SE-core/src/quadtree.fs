namespace SE.Core

open SE
open System
open System.Numerics
open System.Collections
open FSharp.Core

module Quadtree =
    
    type [<Struct>] NodeKind = | Internal | Boundary | External

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
            let v0 = Vector3(v_min.X, v_min.Y, d)
            let v1 = Vector3(v_min.X, v_max.Y, d)
            let v2 = Vector3(v_max.X, v_max.Y, d)
            let v3 = Vector3(v_max.X, v_min.Y, d)
            points.Add(v0)
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
            
            fs.WriteLine($"{v2.X}  {v2.Y}")
            fs.WriteLine($"{v3.X}  {v3.Y}")
            
            fs.WriteLine($"{v3.X}  {v3.Y}")
            fs.WriteLine($"{v4.X}  {v4.Y}")

            fs.WriteLine($"{v4.X}  {v4.Y}")
            fs.WriteLine($"{v1.X}  {v1.Y}")
            fs.WriteLine("\n")
            
        | Empty -> ()
    
    let rec write_rects_to_sb (node:Node<'T>) (sb:System.Text.StringBuilder) =
        match node with
        | Node (_,c,_,_,_,_) ->
            for ci in c do write_rects_to_sb ci sb

        | Leaf (_,v,_,_,v_min,v_max) ->
            let v1 = v_min
            let v2 = Vector2(v_min.X, v_max.Y)
            let v3 = v_max
            let v4 = Vector2(v_max.X, v_min.Y)
            sb.AppendLine($"{v1.X}  {v1.Y}") |> ignore
            sb.AppendLine($"{v2.X}  {v2.Y}") |> ignore
            
            sb.AppendLine($"{v2.X}  {v2.Y}") |> ignore
            sb.AppendLine($"{v3.X}  {v3.Y}") |> ignore
            
            sb.AppendLine($"{v3.X}  {v3.Y}") |> ignore
            sb.AppendLine($"{v4.X}  {v4.Y}") |> ignore

            sb.AppendLine($"{v4.X}  {v4.Y}") |> ignore
            sb.AppendLine($"{v1.X}  {v1.Y}") |> ignore
            sb.AppendLine("\n") |> ignore 
            
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

    let rec copy (source:Node<'T>) (dest:Node<'T>) =
        match (source,dest) with
        | Node (_,c,i,l,v_min,v_max), Node (_,_,_,l',_,_) when l = l' ->
            for i in 0..c.Length-1 do 
                copy c[i] dest

        | Node (_,c,i,l,v_min,v_max), Node (_,c',_,l',_,_) when l' < l ->
            let dest' = Node (dest,(Array.create<Node<'T>> 4 Empty),i,l,v_min,v_max)
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

            if idx < 0 || idx > 3 then failwith "improper idx value"

            match c[idx] with
            | Empty -> c[idx]                
            | _ -> traverse_retain p c[idx]              
               
        
    /// return the node closest to the point
    /// NOT nessesary a leaf node, just the furthest lavel that intersects x,y from the root
    let rec traverse_map (p:Vector2) (node:Node<'T>) =
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

            if idx < 0 || idx > 3 then failwith "improper idx value"

            match c[idx] with
            | Empty -> node                
            | _ -> traverse_map p c[idx]              

    // let contains i j idx =        
    //     0 <= i + j + idx && i + j + idx <= 3

    /// dx and dy are the minumum dv elements of the tree
    let iterate i j dx dy (node:Node<'T>) =
        match node with
        | _ when i = 0 && j = 0 -> node
        
        // | Empty -> failwith "cannot iterate on Empty node"
        // | Empty -> node

        | Leaf (_,_,_,_,v_min,v_max) | Node (_,_,_,_,v_min,v_max) ->
            let c = v_min + (v_max - v_min) / 2.f
            let dx' = float32(sign i) * dx
            let dy' = float32(sign j) * dy
            let dv = Vector2(dx', dy')
            
            let mutable v = c
            // let mutable n = node
            // let mutable tmp = node

            let mutable I = abs i
            let mutable J = abs j

            // this will work only for on cell displacement
            while intersect v v_min v_max  && (I > 0 || J > 0) do
                v <- v + dv   // displace the point until it does not intersect the cell

                if not (intersect v v_min v_max) then
                    I <- I - 1
                    J <- J - 1

            traverse_retain v node 
            
        | Empty -> failwith "run iterate on EMPTY node, failed"

            // while n == tmp && (I > 0 || J > 0) do
            //     v <- v + dv
            //     n <- traverse_retain v n          
                
            //     if not (tmp == n) then
            //         tmp <- n
            //         I <- I - 1
            //         J <- J - 1                    
            // n
        
    // let try_iterate i j (node:Node<'T>) =
    //     match node with
    //     | _ when i = 0 && j = 0 -> node
        
    //     | Leaf (_,_,_,l,v_min,v_max) | Node (_,_,_,l,v_min,v_max) ->
    //         let dv = v_max - v_min
    //         let dx = dv.X / 2.f + 1e-5f
    //         let dy = dv.Y / 2.f + 1e-5f
    //         let c = v_min + (v_max - v_min) / 2.f
    //         let dr = Vector2(float32 i * dx, float32 j * dy)
            
    //         let mutable _v = c
    //         let mutable _n = node
    //         let mutable tmp = node

    //         let mutable I = abs i
    //         let mutable J = abs j
    //         while _n == tmp && (I > 0 || J > 0) do
    //             _v <- _v + dr
    //             _n <- traverse_retain _v _n           
                    
    //             if not (tmp == _n) then
    //                 tmp <- _n
    //                 I <- I - 1
    //                 J <- J - 1                    
    //         _n

    //     | Empty -> node
        

    let rec iterate_sum (add:'T -> 'T -> 'T) (_n:byref<int>) (acc:byref<'T>) (node:Node<'T>) = 
    // let rec iterate_sum (_n:byref<int>) (acc:byref<'T>) (node:Node<'T>) = 
        match node with
        | Leaf (_,v,_,_,_,_) ->
            _n <- _n + 1
            acc <- add acc v.Value.Value
            // acc <- Operators.(+) acc (v.Value.Value)
            
        | Node (_,c,_,_,_,_) ->
            for ci in c do iterate_sum add &_n &acc ci

        | Empty -> ()


    // let kindof dx dy (u:Node<'T>) =
    // // let (|Internal|Boundary|External|) dx dy u =
    //     let b = iterate -1 0 dx dy u
    //     let d = iterate 0 -1 dx dy u
    //     let e = iterate 0 0 dx dy u
    //     let f = iterate 0 1 dx dy u
    //     let h = iterate 1 0 dx dy u

    //     match (b,d,e,f,h) with
    //     | _,_,Empty,_,_ -> External
    //     | Leaf _, Leaf _, Leaf _, Leaf _, Leaf _ -> Internal
    //     | _,_,_,_,_ -> Boundary
   

    let valueof = function
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
        // let mutable tmp_node = root
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

        member this.TrimLevel = k

        member this.Vmin = v_min
        member this.Vmax = v_max
        member this.Root = root

        member this.Copy() =
            let copy' = Root<'T>(N,k,v_min,v_max)
            copy root (copy'.Root) |> ignore
            
            if this.Stencil <> null then
                let stencil' = BitArray(N*N)
                for i in 0..N-1 do
                    for j in 0..N-1 do
                        stencil'[i*N+j] <- stencil[i*N+j]                    
                        
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
            let points = ResizeArray<Vector2>(1000)
            vertices_to_points root points
            points.ToArray()

        member this.AsPolygons (fill: 'T -> float32) =
            let points = ResizeArray<Vector3>(1000)
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
        member this.Stencil with get() = stencil and set(value) = stencil <- value

        member this.Add with set value = add <- value
        member this.Div with set value = div <- value

        // member internal this.Tmp_node with get() = tmp_node and set value = tmp_node <- value
        member internal this.Cached_node with get() = cached_node and set value = cached_node <- value

        member this.Put(x:double, y:double, value:voption<'T>) =
            let p = Vector2(float32 x, float32 y)
            cached_node <- traverse p n k cached_node
            match cached_node with
            | Leaf (_,v,_,_,_,_) -> v.Value <- value
            | _ -> failwith "Item.get failed"         

        member this.Update(pred_trim: Node<'T> -> bool, pred_dense: Node<'T> -> bool, set_value: Node<'T> -> 'T) =
            update n k root pred_trim pred_dense set_value


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

                    
        member this.Item
            with get(i:int,j:int) =
                match (iterate i j this.dX this.dY cached_node) with
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

            and set(i:int,j:int) value =
                match (iterate i j this.dX this.dY cached_node) with
                | Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome value
                | _ -> failwith "should always traverse to a leaf node"

        member this.MapTo(x:double, y:double) =
            cached_node <- traverse_map (Vector2(float32 x, float32 y)) root
    

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
        
    let kindof dx dy (u:Node<'T>) =
        // let u = quadtree.Cached_node
        // let dx = quadtree.dX
        // let dy = quadtree.dY
        let b = iterate -1 0 dx dy u
        let d = iterate 0 -1 dx dy u
        let e = iterate 0 0 dx dy u
        let f = iterate 0 1 dx dy u
        let h = iterate 1 0 dx dy u

        match (b,d,e,f,h) with
        | _,_,Empty,_,_ -> External
        | Leaf _, Leaf _, Leaf _, Leaf _, Leaf _ -> Internal
        | _,_,_,_,_ -> Boundary

    // let kindof (quadtree:Root<'T>) =
    //     let u = quadtree.Cached_node
    //     let dx = quadtree.dX
    //     let dy = quadtree.dY
    //     let b = iterate -1 0 dx dy u
    //     let d = iterate 0 -1 dx dy u
    //     let e = iterate 0 0 dx dy u
    //     let f = iterate 0 1 dx dy u
    //     let h = iterate 1 0 dx dy u

    //     match (b,d,e,f,h) with
    //     | _,_,Empty,_,_ -> External
    //     | Leaf _, Leaf _, Leaf _, Leaf _, Leaf _ -> Internal
    //     | _,_,_,_,_ -> Boundary
        
    /// iterate all the leaf nodes of the tree
    /// The equivalent of a for-loop for the quadtree
    let rec iter (fn:Node<'T> -> unit) (node:Node<'T>) =
        match node with
        | Node (_,c,_,_,_,_) -> for ci in c do iter fn ci            
        | Leaf _ -> fn node
        | Empty -> ()


    // let rec iter (fn:Node<'T> -> unit) (quadtree:Root<'T>) =
    //     match quadtree.Cached_node with
    //     | Node (_,c,_,_,_,_) ->
    //         for ci in c do
    //             quadtree.Cached_node <- ci
    //             iter fn quadtree            
    //     | Leaf _ ->
    //          // quadtree.Cached_node <- ci
    //          fn quadtree.Cached_node
    //     | Empty -> ()
        


    let rec morph (source:Node<'T>) (dest:Node<'T>) add div =
    // let rec morph (source:Node<'T>) (dest:Node<'T>) (add:'T -> 'T -> 'T) (div: 'T -> double -> 'T) =
        match (source,dest) with
        | Node (_,c,i,l,_,_), Node (_,c',i',l',_,_) when i = i' && l = l' ->
            for j in 0..c.Length-1 do morph c[j] c'[j] add div

        | Node (_,c,i,l,_,_), Node (_,c',i',l',_,_) ->
            // for j in 0..c'.Length-1 do morph c[j] c'[j] add div   //  keep the source constant and traverse-forward the dest
            for j in 0..c'.Length-1 do morph source c'[j] add div   //  keep the source constant and traverse-forward the dest
            // failwith "The case Node, Node of different i,l should not happen (?)"
        
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

    // /// map the x,y coordinates to a tree, when they are not included in leafs
    // let rec map (node:Node<'T>) (x':double) (y':double) add div =
    //     match node with
    //     | Node (p,_,_,_,v_min,v_max) when not (intersect (Vector2(float32 x', float32 y')) v_min v_max) ->
    //         let mutable _n = 0
    //         let mutable _t = Operators.Unchecked.defaultof<'T>

    //         iterate_sum add &_n &_t p
    //         printfn "_t: %A" _t
    //         div _t (double _n)
            
    //     | Node (_,c,_,l,v_min,v_max) ->   // traverse forward 
    //         let mutable idx = 0
    //         let p = Vector2(float32 x', float32 y')
    //         let o = v_min + (v_max - v_min) / 2f
    //         idx <- idx + if p.X < o.X then 0 else 1
    //         idx <- idx + if p.Y > o.Y then 0 else 2

    //         if idx < 0 || idx > 3 then failwith "improper idx value"

    //         match c[idx] with
    //         | Empty ->
    //             let mutable _n = 0
    //             let mutable _t = Operators.Unchecked.defaultof<'T>

    //             iterate_sum add &_n &_t node
    //             div _t (double _n)
                
    //         | _ ->
    //             map c[idx] x' y' add div              

    //     | Leaf (p,v,_,_,v_min,v_max) when not (intersect (Vector2(float32 x', float32 y')) v_min v_max) ->
    //         let mutable _n = 0
    //         let mutable _t = Operators.Unchecked.defaultof<'T>

    //         iterate_sum add &_n &_t p
    //         printfn "_t: %A" _t
    //         div _t (double _n)
            
        
    //     | Leaf (_,v,_,_,v_min,v_max) -> v.Value.Value

    //     | Empty -> failwith "map traversed to empty node"


       
                   
