#nowarn "9"
#nowarn "51"
open System
open System.Numerics
open System.Runtime.InteropServices
open System.Runtime.CompilerServices
open FSharp.NativeInterop

// add more ptr related operations
let inline stackalloc<'a when 'a: unmanaged> (length: int): Span<'a> =
  let p = NativePtr.stackalloc<'a> length |> NativePtr.toVoidPtr
  Span<'a>(p, length)

type [<Struct>] varray2<'T when 'T: unmanaged> =
    val mutable a: 'T
    val mutable b: 'T

    member x.Length with get() = 2
    member x.Item 
        with inline get(i:int) = NativePtr.get (&&x.a) i
        and inline set(i:int) value = NativePtr.set (&&x.a) i value
        
type [<Struct>] varray3<'T when 'T: unmanaged> =
    val mutable a: 'T
    val mutable b: 'T
    val mutable c: 'T

    member x.Length with get() = 3
    member x.Item 
        with inline get(i:int) = NativePtr.get (&&x.a) i
        and inline set(i:int) value = NativePtr.set (&&x.a) i value
            
type [<Struct>] varray4<'T when 'T: unmanaged> =
    val mutable a: 'T
    val mutable b: 'T
    val mutable c: 'T
    val mutable d: 'T

    member x.Length with get() = 4
    member x.Item 
        with inline get(i:int) = NativePtr.get (&&x.a) i
        and inline set(i:int) value = NativePtr.set (&&x.a) i value

// type [<Struct; StructLayout(LayoutKind.Explicit, Size = 6 * sizeof<'T>)>] varray6<'T when 'T: unmanaged> =
type [<Struct>] varray6<'T when 'T: unmanaged> =
    val mutable a: 'T
    val mutable b: 'T
    val mutable c: 'T
    val mutable d: 'T
    val mutable e: 'T
    val mutable f: 'T

    member x.Length with get() = 6
    member x.Item 
        with inline get(i:int) = NativePtr.get (&&x.a) i
        and inline set(i:int) value = NativePtr.set (&&x.a) i value

type Triangle() =
    [<DefaultValue>] val mutable vertices: varray3<Vector3>
    [<DefaultValue>] val mutable norm_vertices: varray3<Vector3>
    [<DefaultValue>] val mutable faces: varray3<Vector3>
    [<DefaultValue>] val mutable norm_faces: varray3<Vector3>
    [<DefaultValue>] val mutable indices: varray6<uint16>


do
    let mutable points = varray3<Vector3>()
    points[0] <- Vector3(4f,2f,5f)

    let mutable indices = varray6<uint16>()
    indices[3] <- 111us
    indices[5] <- 101us

    let triangle = Triangle()
    triangle.faces[0] <- Vector3.UnitX
    triangle.faces[1] <- Vector3.UnitY
    triangle.faces[2] <- Vector3.UnitZ

    printfn "sizeof varray2<Vector3>: %d" (sizeof<varray2<Vector3>>)
    printfn "sizeof varray3<Vector3>: %d" (sizeof<varray3<Vector3>>)
    printfn "sizeof varray4<Vector3>: %d" (sizeof<varray4<Vector3>>)
    printfn "sizeof varray6<uint16>:  %d" (sizeof<varray6<uint16>>)

    printfn "points: %A, %A, %A" points[0] points[1] points[2]
    printfn "triangle: %A, %A, %A" triangle.faces[0] triangle.faces[1] triangle.faces[2]

    let _array = stackalloc<Vector3> 3
    _array[0] <- Vector3(9f,9f,9f)
    for v in _array do printf "%A, " v  
    printfn ""
    for i in 0..indices.Length - 1 do printf "%A, " indices[i]  
    printfn ""

let inline mid a b =
    let _min = min a b
    let _max = max a b
    _min + (_max - _min) / 2.0f


module Quadtree = 
    type [<Struct>] Quad = {a:Vector2; b:Vector2}

    let inline between a b c = a <= c && c <= b

    let inline contains (c:Quad) (p:Vector2) =
        let x = between c.a.X c.b.X p.X
        let y = between c.a.Y c.b.Y p.Y
        x && y

    let rec quadtree (p:Vector2) n j (c:Quad) : Quad =
        let x_min = min (c.a.X) (c.b.X)
        let x_max = max (c.a.X) (c.b.X)
        let x_mid = mid x_min x_max
        let y_min = min (c.a.Y) (c.b.Y)
        let y_max = max (c.a.Y) (c.b.Y)
        let y_mid = mid y_min y_max
        let O = Vector2(x_mid, y_mid)
        let E = Vector2(x_max, y_mid)
        let W = Vector2(x_min, y_mid)
        let N = Vector2(x_mid, y_max)
        let S = Vector2(x_mid, y_min)
        let A = Vector2(x_min, y_max)
        let B = Vector2(x_max, y_max)
        let C = Vector2(x_min, y_min)
        let D = Vector2(x_max, y_min)
        let lu = {a = W; b = N}
        let ru = {a = O; b = B}
        let ll = {a = C; b = O}
        let lr = {a = S; b = E}

        let nc =
            if contains ru p then ru
            elif contains lu p then lu
            elif contains ll p then ll
            elif contains lr p then lr
            else c

        if n < j then quadtree p (n + 1) j nc else c


    let rec quadtree2 (p:Vector2) n j (c:Quad) =
        let o = Vector2(mid c.a.X c.b.X, mid c.a.Y c.b.Y)
        let x_min = if p.X < o.X then c.a.X else o.X
        let x_max = if p.X > o.X then c.b.X else o.X
        let y_min = if p.Y < o.Y then c.a.Y else o.Y
        let y_max = if p.Y > o.Y then c.b.Y else o.Y 
        let cn = {a = Vector2(x_min, y_min); b = Vector2(x_max, y_max)}
        if n < j then quadtree2 p (n + 1) j cn else c

module Octree =
    type [<Struct>] Octane = {a:Vector3; b:Vector3}
    
    let rec octree (p:Vector3) n j (c:Octane) =
        let o = Vector3(mid c.a.X c.b.X, mid c.a.Y c.b.Y, mid c.a.Z c.b.Z)
        let x_min = if p.X < o.X then c.a.X else o.X
        let x_max = if p.X > o.X then c.b.X else o.X
        let y_min = if p.Y < o.Y then c.a.Y else o.Y
        let y_max = if p.Y > o.Y then c.b.Y else o.Y 
        let z_min = if p.Z < o.Z then c.a.Z else o.Z
        let z_max = if p.Z > o.Z then c.b.Z else o.Z
        
        let cn = {a = Vector3(x_min, y_min, z_min); b = Vector3(x_max, y_max, z_max)}
        if n < j then octree p (n + 1) j cn else c


let pv2 = Vector2(0.3f, 0.98f)
let iq_q: Quadtree.Quad = {a = Vector2(-3.4f, 0.00980f); b = Vector2(13f, 9f)}
let pv3 = Vector3(7f, 6f, 5.1f)
let iq_o: Octree.Octane = {a = Vector3(-3.4f, 0.00980f, 5f); b = Vector3(13f, 9f, 14f)}

// printfn "initial: %A \n\npinpoint: %A" iq (quadtree pv 0 10 iq)
printfn "initial: %A \npinpoint: %A\n\n" iq_q (Quadtree.quadtree2 pv2 0 10 iq_q)
printfn "initial: %A \npinpoint: %A\n\n" iq_o (Octree.octree pv3 0 10 iq_o)


