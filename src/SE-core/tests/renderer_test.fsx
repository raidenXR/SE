#nowarn "9"

#load "../src/rendererFS.fs"
#load "../src/Core.fs"

open SE.Core
open LibSErenderer
open FSharp.Core

printfn "lib is found: %A" (System.IO.File.Exists libname)

let triangle_positions = [|
    {X = -1f; Y = -1f; Z = 0f}    
    {X = 1f; Y = -1f; Z = 0f}    
    {X = 0f; Y = 1f; Z = 0f}    
|]

let color r g b a =
    let mutable c = 0u;
    c <- c ||| (r <<< 24)
    c <- c ||| (g <<< 16)
    c <- c ||| (b <<< 8)
    c <- c ||| (a)
    uint c

let triangle_colors = [|
    {V = (color 255u 0u 0u 255u)}
    {V = (color 0u 255u 0u 255u)}
    {V = (color 0u 0u 255u 255u)}
|]

let mutable context = init ("F# SDL3 Window: first example!!")
let _s = System.Runtime.CompilerServices.Unsafe.AsPointer(&context);
let s: nativeptr<Context> = NativeInterop.NativePtr.ofVoidPtr _s

createVertexBuffer (s, triangle_positions, triangle_colors, 16u, uint (triangle_positions.Length))
update(s, uint (triangle_positions.Length))
// quit(&s)
