namespace SE.Core

open SE
open System
open System.Numerics
open System.Collections
open FSharp.Core
open System.Runtime.InteropServices
open System.Runtime.CompilerServices

module OctreeExperimental =

    let private task_new (fn:unit -> unit) = System.Threading.Tasks.Task.Factory.StartNew(fn)

    [<System.Flags>]
    type Flag =
        | Empty = 0uy
        | Node  = 1uy
        | Leaf  = 2uy

    [<AllowNullLiteral>]
    type Node<'T>() =
        [<DefaultValue>] val mutable flag:   Flag
        [<DefaultValue>] val mutable idx:    byte
        [<DefaultValue>] val mutable level:  byte
        [<DefaultValue>] val mutable v_min:  Vector3
        [<DefaultValue>] val mutable v_max:  Vector3

        [<DefaultValue>] val mutable value:  ValueOption<'T>

        [<DefaultValue>] val mutable parent: Node<'T> 
        let mutable children = InlineArray8<Node<'T>>()

        member this.Item
            with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get (index: int) =
                Unsafe.Add(&Unsafe.As<InlineArray8<Node<'T>>,Node<'T>>(&children), index)

            and  [<MethodImpl(MethodImplOptions.AggressiveInlining)>] set (index: int) (value: Node<'T>) = 
                Unsafe.Add(&Unsafe.As<InlineArray8<Node<'T>>,Node<'T>>(&children), index) <- value

        member this.Item
            with [<MethodImpl(MethodImplOptions.AggressiveInlining)>] get (index: byte) =
                Unsafe.Add(&Unsafe.As<InlineArray8<Node<'T>>,Node<'T>>(&children), int index)

            and  [<MethodImpl(MethodImplOptions.AggressiveInlining)>] set (index: byte) (value: Node<'T>) = 
                Unsafe.Add(&Unsafe.As<InlineArray8<Node<'T>>,Node<'T>>(&children), int index) <- value



    let (|Node|Leaf|Empty|) (node:Node<'T>) =
        if node = null then Empty
        elif node.flag = Flag.Node then Node
        elif node.flag = Flag.Leaf then Leaf
        else Empty    

    
    let Empty () :Node<'T> = null

    let Leaf (p,v,i,j,v_min,v_max) =
        let n = Node<'T>(parent = p, value = v, idx = i, level = j, flag = Flag.Leaf, v_min = v_min, v_max = v_max)
        if p <> null then p[i] <- n
        n

    let Node (p,i,j,v_min,v_max) =
        let n = Node<'T>(parent = p, idx = i, level = j, flag = Flag.Node, v_min = v_min, v_max = v_max)
        if p <> null then p[i] <- n
        for i in 0..7 do
            n[i]<- Empty()
        n


    let center node =
        match node with
        | Leaf | Node -> node.v_min + (node.v_max - node.v_min) / 2.f
        | Empty -> failwith "Node must be not Empty to get center"

    let intersect (p:Vector3) (v_min:Vector3) (v_max:Vector3) =
        let x = v_min.X <= p.X && p.X <= v_max.X
        let y = v_min.Y <= p.Y && p.Y <= v_max.Y
        let z = v_min.Z <= p.Z && p.Z <= v_max.Z
        x && y && z

    let rec count_rec (j:byref<int>) node =
        match node with
        | Node -> for i in 0..7 do count_rec &j node[i]
        | Leaf -> j <- j + 1
        | Empty -> ()

    let rec count_total_rec (j:byref<int>) (node:Node<'T>) =
        if node <> null && node.flag <> Flag.Empty then j <- j + 1
        match node with 
        | Node -> for i in 0..7 do count_total_rec &j node[i]
        | _ -> ()


    let forall (fn: Node<'T> -> bool) (node:Node<'T>) =
        let mutable b = true
        let mutable i = 0
        while i < 8 && b do
            b <- b && (fn node[i])
            i <- i + 1
        b

    /// convert a Node to Leaf
    let rec trim n k v (node:Node<'T>) =
        match node with 
        | Leaf when n = node.level ->
            let p = node.parent
            if (forall (function Node -> false | _ -> true) p) then 
                match p with
                | Node ->
                    let P = p.parent
                    let C = p.parent
                    let I = p.idx
                    let L = p.level
                    let V1 = p.v_min
                    let V2 = p.v_max                    
                    let mutable value: ValueOption<'T> = v
                    for i in 0..7 do
                        match C[i] with
                        | Leaf when C[i].value.IsSome -> value <- ValueSome C[i].value.Value
                        | _ -> ()
                        
                    P[I] <- Leaf (P,value,I,L,V1,V2)
                    P[I]
                | _ -> node
            else
                node

        | Leaf when n - node.level < k ->
            let p = node.parent
            if (forall (function Leaf -> true | _ -> false) p) then 
                match p with
                | Node ->
                    let P = p.parent
                    let C = p.parent
                    let I = p.idx
                    let L = p.level
                    let V1 = p.v_min
                    let V2 = p.v_max
                    let mutable value: ValueOption<'T> = v
                    for i in 0..7 do
                        match C[i] with
                        | Leaf when C[i].value.IsSome -> value <- ValueSome C[i].value.Value
                        | _ -> ()
                        
                    P[I] <- Leaf (P,value,I,L,V1,V2)
                    P[I]
                | _ -> node
            else
                node
        | Empty ->
            failwith "tried to trim Empty Node"
        | _ -> node


    let rec traverse (p:Vector3) n k (node:Node<'T>) =
        match node with
        | Empty -> failwith "traversed to empty node, make sure that root is not out of bounds"

        | Leaf when not (intersect p node.v_min node.v_max) ->
            traverse p n k node.parent
            |> trim n k ValueNone
            
        | Leaf _ ->
            node
            |> trim n k ValueNone

        | Node when not (intersect p node.v_min node.v_max) ->
            traverse p n k node.parent
            |> trim n k ValueNone

        | Node ->   // traverse forward 
            let c = node
            let l = node.level
            let mutable v_min = node.v_min
            let mutable v_max = node.v_max
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
                c[idx] <- Leaf (node, ValueNone, byte idx, n, v_min, v_max)
                traverse p n k c[idx]
                |> trim n k ValueNone

            | Empty ->
                c[idx] <- Node (node, byte idx, (l+1uy), v_min, v_max) 
                traverse p n k c[idx]
                |> trim n k ValueNone
                
            | _ ->
                traverse p n k c[idx]
                |> trim n k ValueNone


    let rec traverse_retain (p:Vector3) (node:Node<'T>) =
        match node with
        | Empty -> node

        | Leaf when not (intersect p node.v_min node.v_max) ->
            traverse_retain p node.parent
            
        | Leaf _ -> node

        | Node when not (intersect p node.v_min node.v_max) ->
            traverse_retain p node.parent

        | Node ->   // traverse forward 
            let c = node
            let v_min = node.v_min
            let v_max = node.v_max
            let mutable idx = 0
            let o = v_min + (v_max - v_min) / 2f
            idx <- idx + if p.X < o.X then 0 else 1
            idx <- idx + if p.Y > o.Y then 0 else 2
            idx <- idx + if p.Z < o.Z then 0 else 4

            if idx < 0 || idx > 7 then failwith "improper idx value"

            match c[idx] with
            | Empty -> c[idx]                
            | _ -> traverse_retain p c[idx]              

               
    /// dx and dy are the minumum dv elements of the tree
    let iterate_node i j k (node:Node<'T>) =
        match node with
        | _ when i = 0 && j = 0 && k = 0 -> node

        | Leaf | Node ->
            let v_min = node.v_min
            let v_max = node.v_max
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

    let (|Internal|External|Boundary|) (u:Node<'T>) =
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

        
    /// iterate all the leaf nodes of the tree
    /// The equivalent of a for-loop for the quadtree
    let rec iter (fn:Node<'T> -> unit) (node:Node<'T>) =
        match node with
        | Node  -> for i in 0..7 do iter fn node[i]            
        | Leaf  -> fn node
        | Empty -> ()


    type Root<'T>(N:int, _k:int, v_min:Vector3, v_max:Vector3) =
        let root = Node(Empty(), 0uy, 0uy, v_min, v_max)
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

        member this.GetInternalCount() =
            let mutable c = 0
            let is_internal = function | Internal -> c <- c + 1 | _ -> ()
            iter is_internal root
            c

        member this.GetBoundaryCount() =
            let mutable c = 0
            let is_boundary = function | Boundary -> c <- c + 1 | _ -> ()
            iter is_boundary root
            c

        member this.MaxLevel = n

        member this.Rank = N

        member this.TrimLevel = _k

        member this.Vmin = v_min
        member this.Vmax = v_max
        member this.Root = root

        member this.dX = match root with | Node -> dd (root.v_max.X - root.v_min.X) 0 | _ -> failwith "Root MUST be Node"   
        member this.dY = match root with | Node -> dd (root.v_max.Y - root.v_min.Y) 0 | _ -> failwith "Root MUST be Node"   
        member this.dZ = match root with | Node -> dd (root.v_max.Z - root.v_min.Z) 0 | _ -> failwith "Root MUST be Node"   
        member this.Stencil with get() = stencil and set(value) = stencil <- value

        member this.Add with set value = add <- value
        member this.Div with set value = div <- value

        member this.Put(x:double, y:double, z:double, value:voption<'T>) =
            let p = Vector3(float32 x, float32 y, float32 z)
            cached_node <- traverse p (byte n) (byte _k) cached_node
            match cached_node with
            | Leaf -> cached_node.value <- value
            | _ -> failwith "Item.get failed"         


        /// Experimental method, DOT NOT take for granted that it works...
        member this.IterParallel (num_threads:int) (fn:Node<'T> -> unit) =
            match num_threads with
            | 1 ->
                iter fn root
            | 2 ->
                let c = root
                let t0 = task_new (fun _ -> iter fn c[0]; iter fn c[1]; iter fn c[2]; iter fn c[3])
                let t1 = task_new (fun _ -> iter fn c[4]; iter fn c[5]; iter fn c[6]; iter fn c[7])
                t0.Wait()
                t1.Wait()
            | 3 ->
                let c = root
                let t0 = task_new (fun _ -> iter fn c[0]; iter fn c[1]; iter fn c[2])
                let t1 = task_new (fun _ -> iter fn c[3]; iter fn c[4]; iter fn c[5])
                let t2 = task_new (fun _ -> iter fn c[6]; iter fn c[7])
                t0.Wait()
                t1.Wait()
                t2.Wait()
            | 4 ->
                let c = root
                let t0 = task_new (fun _ -> iter fn c[0]; iter fn c[1])
                let t1 = task_new (fun _ -> iter fn c[2]; iter fn c[3])
                let t2 = task_new (fun _ -> iter fn c[4]; iter fn c[5])
                let t3 = task_new (fun _ -> iter fn c[6]; iter fn c[7])
                t0.Wait()
                t1.Wait()
                t2.Wait()
                t3.Wait()
            | 5 ->
                let c = root
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
                let c = root
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
                let c = root
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
                let c = root
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


    let ofSurface<'T> (N:int) L k (vertices:Span<float32>) (indices:Span<uint>) =
        let (v_min,v_max) = GridGeneration3D.bounds_SIMD vertices L
        let bits = Octree.fill_scanlines N L v_min v_max vertices indices (BitArray(N*N*N))
        ofStencil<'T> N k v_min v_max bits        


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


