#load "../src/gltfLoader.fs"
#load "../src/binLoader.fs"

open System
open System.Text.Json
open SE.Gltf

let args = System.Environment.GetCommandLineArgs()
if args.Length < 3 then failwith "forgot to add path_to_gltf in args"
let path = args[2]
let file = System.IO.File.ReadAllText(path)

let root = JsonSerializer.Deserialize<GltfRoot>(file)
let accessors = root.accessors

printfn "%A" accessors[0]

// open GltfBin
// let bobct = new BinLoader(path, 0)
// bobct.Print<float32>(28)
// (bobct :> IDisposable).Dispose()


open GltfBin
loadBuffers (root.buffers)
for vec in ((bufferview_VEC3 0 root).Slice(0,10)) do
    printfn "%A" vec
freeBuffers ()


