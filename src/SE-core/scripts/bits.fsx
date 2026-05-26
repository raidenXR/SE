open System

let bytes = Array.zeroCreate<byte> 10
Random.Shared.NextBytes(bytes)

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

for i in 0..10 do
    System.Threading.Thread.Sleep(1000)
#time
