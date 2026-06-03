open System
open System.Numerics
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

module Quadtree =
    type [<AllowNullLiteral>] Node<'T>(parent:Node<'T>, v_min:Vector2, v_max:Vector2) =
        let children = Array.zeroCreate<Node<'T>> 4
        let mutable value: ValueOption<'T> = ValueNone

        member this.V_min = v_min
        member this.V_max = v_max
        member this.Parent = parent
        member this.Children = children
        member this.Value with get() = value.Value and set(v) = value <- ValueSome v
        member this.HasValue = value.IsSome
    
    let intersect (p:Vector2) (v_min:Vector2) (v_max:Vector2) =
        let x = v_min.X <= p.X && p.X <= v_max.X
        let y = v_min.Y <= p.Y && p.Y <= v_max.Y
        x && y

    let rec traverse (p:Vector2) (node:Node<'T>) (v:'T) (j:byref<int>) n (pred:'T -> 'T -> bool) =
        // printfn "j: %d, v_min: %A, v_max: %A" j node.V_min node.V_max
        if not node.HasValue then
            node.Value <- v
            node
        elif (pred v node.Value) then
            node
        elif j > n then
            j <- n
            node
        elif not (intersect p node.V_min node.V_max) then
            // printfn "traverse back"
            // if node.Parent = null then printfn "NULL parent at j: %d" j
            if j > 0 then
                j <- j - 1
                traverse p node.Parent v &j n pred
            else
                node
        else
        // elif (intersect p node.V_min node.V_max) then
            let o = node.V_min + (node.V_max - node.V_min) / 2f
            let mutable idx = 0
            let mutable v_min = node.V_min
            let mutable v_max = node.V_max

            idx <- idx + if p.X < o.X then 0 else 1
            idx <- idx + if p.Y > o.Y then 0 else 2

            match idx with
            | 0 ->
                v_min <- Vector2(node.V_min.X, o.Y )            
                v_max <- Vector2(o.X, node.V_max.Y) 
            | 1 -> 
                v_min <- Vector2(o.X, o.Y)            
                v_max <- Vector2(node.V_max.X, node.V_max.Y) 
            | 2 -> 
                v_min <- Vector2(node.V_min.X, node.V_min.Y)            
                v_max <- Vector2(o.X, o.Y) 
            | 3 -> 
                v_min <- Vector2(o.X, node.V_min.Y)            
                v_max <- Vector2(node.V_max.X, o.Y) 
            | _ -> failwith "improper idx value"

            let c = &node.Children[idx]
            c <- if c = null then Node(node, v_min, v_max) else c
            j <- j + 1
            traverse p c v &j n pred         
        // else node

    /// traverse up to a leaf node and return the Data-value
    let rec get_value (p:Vector2) (node:Node<'T>) j =
        if not (intersect p node.V_min node.V_max) then
            if j > 0 then get_value p node.Parent (j-1) else node.Value
        else
            let o = node.V_min + (node.V_max - node.V_min) / 2f
            let mutable idx = 0
            let mutable v_min = node.V_min
            let mutable v_max = node.V_max

            idx <- idx + if p.X < o.X then 0 else 1
            idx <- idx + if p.Y > o.Y then 0 else 2

            let c = node.Children[idx]
            if c <> null then get_value p c (j+1) else node.Value
        
    /// traverse up to a leaf node and set the Data-value
    let rec set_value (p:Vector2) (v:'T) (node:Node<'T>) j (pred: 'T -> 'T -> bool) =
        if not (intersect p node.V_min node.V_max) then
            if j > 0 then
                set_value p v node.Parent (j-1) pred
            else
                node.Value <- v
        else
            let o = node.V_min + (node.V_max - node.V_min) / 2f
            let mutable idx = 0
            let mutable v_min = node.V_min
            let mutable v_max = node.V_max

            idx <- idx + if p.X < o.X then 0 else 1
            idx <- idx + if p.Y > o.Y then 0 else 2

            let c = node.Children[idx]
            if c = null || (pred v node.Value) then
                node.Value <- v 
            else
                set_value p v c (j+1) pred

    let rec count (node:Node<'T>) (j:byref<int>) N =
        j <- j + 1
        for c in node.Children do
            if c <> null then count c &j N
        

module Octree =
    // type [<Struct>] NodeIndex = {ix:int; iy:int; iz:int}
    
    let inline mid a b =
        let _min = min a b
        let _max = max a b
        _min + (_max - _min) / 2.0f

    type [<AllowNullLiteral>] Node<'T>(parent:Node<'T>, v_min:Vector3, v_max:Vector3) =
        let children = Array.zeroCreate<Node<'T>> 8
        let mutable value: ValueOption<'T> = ValueNone

        member this.V_min = v_min
        member this.V_max = v_max
        member this.Parent = parent
        member this.Children = children
        member this.Value with get() = value.Value and set(v) = value <- ValueSome v
        member this.HasValue = value.IsSome

        // member this.HasChildren =
        //     let mutable b = false
        //     let mutable i = 0
        //     while i < 8 && not b do
        //         b <- children[i] <> null
        //         i <- i + 1



    let [<Literal>] e = 1e-2f

    let inline at_range a b =
        if abs(a - b) < e then true else false

    let inline lt (a:Vector3) (b:Vector3) =
        a.X < b.X || a.Y < b.Y || a.Z < b.Z

    let inline gt (a:Vector3) (b:Vector3) =
        a.X > b.X || a.Y > b.Y || a.Z > b.Z

    let intersect (p:Vector3) (v_min:Vector3) (v_max:Vector3) =
        let x = v_min.X <= p.X && p.X <= v_max.X
        let y = v_min.Y <= p.Y && p.Y <= v_max.Y
        let z = v_min.Z <= p.Z && p.Z <= v_max.Z
        x && y && z

    /// The value of N will be dependent on n, because n = max_level N
    let rec traverse (p:Vector3) (node:Node<'T>) (v:'T) (j:byref<int>) n (pred:'T -> 'T -> bool) =
        // printfn "j: %d, v_min: %A, v_max: %A" j node.V_min node.V_max
        if not node.HasValue then
            node.Value <- v
            node
        elif (pred v node.Value) then
            node
        elif j > n then // ensure that the level of the tree will not exceed min dx * dy * dz -- min octant
            j <- n
            node
        elif not (intersect p node.V_min node.V_max) then
            // printfn "traverse back"
            // if node.Parent = null then printfn "NULL parent at j: %d" j
            if j > 0 then
                j <- j - 1
                traverse p node.Parent v &j n pred 
            else
                node
        else
        // elif (intersect p node.V_min node.V_max) then
            let o = node.V_min + (node.V_max - node.V_min) / 2f
            let mutable idx = 0
            let mutable v_min = node.V_min
            let mutable v_max = node.V_max

            idx <- idx + if p.X < o.X then 0 else 1
            idx <- idx + if p.Y > o.Y then 0 else 2
            idx <- idx + if p.Z < o.Z then 0 else 4

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
            | _ -> failwith "improper idx value"

            let c = &node.Children[idx]
            c <- if c = null then Node(node, v_min, v_max) else c
            j <- j + 1
            traverse p c v &j n pred         
        // else node

    /// traverse up to a leaf node and return the Data-value
    let rec get_value (p:Vector3) (node:Node<'T>) j =
        if not (intersect p node.V_min node.V_max) then
            if j > 0 then get_value p node.Parent (j-1) else node.Value
        else
            let o = node.V_min + (node.V_max - node.V_min) / 2f
            let mutable idx = 0
            let mutable v_min = node.V_min
            let mutable v_max = node.V_max

            idx <- idx + if p.X < o.X then 0 else 1
            idx <- idx + if p.Y > o.Y then 0 else 2
            idx <- idx + if p.Z < o.Z then 0 else 4

            let c = node.Children[idx]
            if c <> null then get_value p c (j+1) else node.Value

    /// traverse up to a leaf node and set the Data-value
    let rec set_value (p:Vector3) (v:'T) (node:Node<'T>) j (pred: 'T -> 'T -> bool) =
        if not (intersect p node.V_min node.V_max) then
            if j > 0 then
                set_value p v node.Parent (j-1) pred
            else
                node.Value <- v
        else
            let o = node.V_min + (node.V_max - node.V_min) / 2f
            let mutable idx = 0
            let mutable v_min = node.V_min
            let mutable v_max = node.V_max

            idx <- idx + if p.X < o.X then 0 else 1
            idx <- idx + if p.Y > o.Y then 0 else 2
            idx <- idx + if p.Z < o.Z then 0 else 4

            let c = node.Children[idx]
            if c = null || (pred v node.Value) then
                node.Value <- v
            else
                set_value p v c (j+1) pred

    let count (node:Node<'T>) = 
        let mutable n = 0
        let rec count_rec (node:Node<'T>) =
            n <- n + 1
            for c in node.Children do
                if c <> null then count_rec c
        count_rec node
        n
        

module Bits = 
    // bitwise operations
    let bitwise_left (bytes:byte[]) =
        let count = Vector<byte>.Count

        for i in 0..count..bytes.Length-count do
            let v = Vector<byte>(bytes, i)
            Vector.ShiftLeft(v, 1).CopyTo(bytes, i)

        for i in bytes.Length-count+1..bytes.Length-1 do
            bytes[i] <- bytes[i] <<< 1

    let bitwise_right (bytes:byte[]) =
        let count = Vector<byte>.Count

        for i in 0..count..bytes.Length-count do
            let v = Vector<byte>(bytes, i)
            Vector.ShiftRightLogical(v, 1).CopyTo(bytes, i)

        for i in bytes.Length-count+1..bytes.Length-1 do
            bytes[i] <- bytes[i] >>> 1

    let bitwise_and (bytes:byte[]) V =
        let count = Vector<byte>.Count

        for i in 0..count..bytes.Length-count do
            let v = Vector<byte>(bytes, i)
            Vector.BitwiseAnd(v, V).CopyTo(bytes, i)

        for i in bytes.Length-count+1..bytes.Length-1 do
            bytes[i] <- bytes[i] &&& V[0]

    let bitwise_or (bytes:byte[]) V =
        let count = Vector<byte>.Count
        let len = bytes.Length

        for i in 0..count..len-count do
            let v = Vector<byte>(bytes, i)
            Vector.BitwiseOr(v, V).CopyTo(bytes, i)

        for i in len-count+1..bytes.Length-1 do
            bytes[i] <- bytes[i] ||| V[0]

    let bitwise_xor (bytes:byte[]) V =
        let count = Vector<byte>.Count
        let a = bytes.Length / count
        let b = bytes.Length % count

        for i in 0..count..bytes.Length-count do
            let v = Vector<byte>(bytes, i)
            Vector.Xor(v, V).CopyTo(bytes, i)

        for i in bytes.Length-count+1..bytes.Length-1 do
            bytes[i] <- bytes[i] ^^^ V[0]

    let bitwise_not (bytes:byte[]) =
        let count = Vector<byte>.Count

        for i in 0..count..bytes.Length-count do
            let v = Vector<byte>(bytes, i)
            Vector.AndNot(v, v).CopyTo(bytes, i)

        for i in bytes.Length-count+1..bytes.Length-1 do
            bytes[i] <- ~~~bytes[i]

    let bitwise_andnot (bytes:byte[]) V =
        let count = Vector<byte>.Count

        for i in 0..count..bytes.Length-count do
            let v = Vector<byte>(bytes, i)
            Vector.AndNot(v, V).CopyTo(bytes, i)

        for i in bytes.Length-count+1..bytes.Length-1 do
            bytes[i] <- ~~~(bytes[i] &&& V[0]) 

    /// use this to use bits instead of bools for 0 - 1 and store information for discretization
    let check (bytes:byte[]) n =
        let item = bytes[n / 8]
        match n % 8 with
        | 0 -> (item &&& 0b00000001uy) > 0uy
        | 1 -> (item &&& 0b00000010uy) > 0uy
        | 2 -> (item &&& 0b00000100uy) > 0uy
        | 3 -> (item &&& 0b00001000uy) > 0uy
        | 4 -> (item &&& 0b00010000uy) > 0uy
        | 5 -> (item &&& 0b00100000uy) > 0uy
        | 6 -> (item &&& 0b01000000uy) > 0uy
        | 7 -> (item &&& 0b10000000uy) > 0uy
        | _ -> false

    let bits_count (b:byte) =
        let b0 = if b &&& 0b00000001uy > 0uy then 1 else 0 
        let b1 = if b &&& 0b00000010uy > 0uy then 1 else 0 
        let b2 = if b &&& 0b00000100uy > 0uy then 1 else 0 
        let b3 = if b &&& 0b00001000uy > 0uy then 1 else 0 
        let b4 = if b &&& 0b00010000uy > 0uy then 1 else 0 
        let b5 = if b &&& 0b00100000uy > 0uy then 1 else 0 
        let b6 = if b &&& 0b01000000uy > 0uy then 1 else 0 
        let b7 = if b &&& 0b10000000uy > 0uy then 1 else 0 
        b0 + b1 + b2 + b3 + b4 + b5 + b6 + b7


/// this function is to compute the maximum number of levels for the tree
// it should not exceed the dx * dy * dz (the smallest volume) of the discretization
let log2 a = log10 a / log10 2.
let max_level = log2 >> ceil >> int

let N = 800
let n = max_level (double N)
printfn "n: %d" n

let bytes = Array.zeroCreate<byte> N
Random.Shared.NextBytes(bytes)
printfn "0b%B, 0b%B" 78uy (78uy &&& 0b010uy)
printfn "bits_count: %d" (Bits.bits_count 78uy)

// for i in 0..(8*bytes.Length-1) do
    // if i % 8 = 0 then
        // printfn "%d, (0b%B)" bytes[i/8] bytes[i/8]
    // let b = check bytes i
    // printfn "i:%d: 0b%B %b" i bytes[i/8] b

#time
let buffer = Array.zeroCreate<byte> (N * N * N)
Random.Shared.NextBytes(buffer)
printfn "allocated memory: %gMB" (double buffer.Length / double 1024 / double 1024)

// for i in 0..10 do
//     System.Threading.Thread.Sleep(1000)
#time

let count = Vector<byte>.Count
printfn "count: %d" count
let bitmask = Vector<byte>([|for i in 0uy..byte(Vector<byte>.Count-1) -> i|])

Bits.bitwise_left bytes
Bits.bitwise_right bytes
Bits.bitwise_and bytes bitmask
Bits.bitwise_not bytes 
Bits.bitwise_or bytes bitmask
Bits.bitwise_xor bytes bitmask
Bits.bitwise_andnot bytes bitmask

let root = Octree.Node<double>(null, Vector3.Zero, Vector3.One)
let mutable cached_node = root

let pred a b = abs (a - b) < 0.1

let mutable j = 0
let mutable d_value = 20.

#time
// WHEN TRAVERSING THE N (MAX LEVEL OF THE TREE - number of subdivision)
// should go up to 
for ix in 1..300 do
    for iy in 1..200 do
        for iz in 1..400 do
            let x = float32 ix / float32 N
            let y = float32 iy / float32 N
            let z = float32 iz / float32 N
            let v = Vector3(x,y,z)
            // let v = Vector3(float32 (10*ix), float32 (10*iy), float32 (10*iz))
            let d = Random.Shared.NextDouble() - 0.5
            d_value <- d_value + d
            // j <-0
            // if cached_node = null || root = null then failwith "cached_node is null"
            cached_node <- Octree.traverse v cached_node d_value &j n pred
            // cached_node <- Octree.traverse v root d &j n N pred
            // printfn "node ID: %d with j: %d" (cached_node.GetHashCode()) j
#time


#time 
j <- 0
let _count = Octree.count root
printfn "count: %d, allocated memory for T: %gMB" _count (11. * double _count / 1024. / 1024.)
#time

let x = Vector3(5.4f, 2.6f, 9.8f)
let p = Vector3(3.4f, 5.6f, 7.8f)
#time
Octree.set_value x 120.45 root 0 pred
#time

#time
let node_value = Octree.get_value x root 0
printfn "value at node: %g" node_value
#time

let mutable V = Vector3.One * 100f
// printfn "ni: %d, V: %A" 0 V
for ni in 1..n do
    V <- V / 2.f
    printfn "ni: %d, V: %A" ni V
    

