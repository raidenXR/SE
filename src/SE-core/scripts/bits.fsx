open System
open System.Numerics
open System.Runtime.CompilerServices
open System.Runtime.InteropServices

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

    let rec traverse (p:Vector3) (node:Node<'T>) (v:'T) (j:byref<int>) N (pred:'T -> 'T -> bool) =
        // printfn "j: %d, v_min: %A, v_max: %A" j node.V_min node.V_max
        if not node.HasValue then
            node.Value <- v
            node
        elif (pred v node.Value) then
            node
        elif not (intersect p node.V_min node.V_max) then
            j <- j - 1
            // printfn "traverse back"
            // if node.Parent = null then printfn "NULL parent at j: %d" j
            if j >= 0 then traverse p node.Parent v &j N pred else node
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
            traverse p c v &j N pred         
        // else node


    let rec count (node:Node<'T>) (j:byref<int>) N =
        j <- j + 1
        for c in node.Children do
            if c <> null then count c &j N
        

let bytes = Array.zeroCreate<byte> 100
Random.Shared.NextBytes(bytes)

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


printfn "0b%B, 0b%B" 78uy (78uy &&& 0b010uy)

for i in 0..(8*bytes.Length-1) do
    if i % 8 = 0 then
        printfn "%d, (0b%B)" bytes[i/8] bytes[i/8]
    let b = check bytes i
    printfn "i:%d: 0b%B %b" i bytes[i/8] b

#time
let buffer = Array.zeroCreate<byte> (100 * 100 * 100)
Random.Shared.NextBytes(buffer)
printfn "allocated memory: %gMB" (double buffer.Length / double 1024 / double 1024)

// for i in 0..10 do
//     System.Threading.Thread.Sleep(1000)
#time

let count = Vector<byte>.Count
printfn "count: %d" count
let bitmask = Vector<byte>([|for i in 0uy..byte(Vector<byte>.Count-1) -> i|])

bitwise_left bytes
bitwise_right bytes
bitwise_and bytes bitmask
bitwise_not bytes 
bitwise_or bytes bitmask
bitwise_xor bytes bitmask
bitwise_andnot bytes bitmask

let root = Octree.Node<double>(null, Vector3.Zero, Vector3(100f,100f,100f))
let mutable cached_node = root

let pred a b = abs (a - b) < 0.01

let mutable j = 0
#time
for ix in 1..100 do
    cached_node <- root
    for iy in 1..100 do
        for iz in 1..100 do
            let v = Vector3(float32 ix, float32 iy, float32 iz)
            let d = Random.Shared.NextDouble()
            cached_node <- Octree.traverse v cached_node d &j 100 pred
            // cached_node <- Octree.traverse v root d &j 100 pred
            // printfn "node ID: %d with j: %d" (leaf.GetHashCode()) j
#time

j <- 0
Octree.count root &j 100
printfn "count: %d, allocated memory for T: %gMB" j (11. * double j / 1024. / 1024.)

