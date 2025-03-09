#load "../src/gltfLoader.fs"
#load "../src/binLoader.fs"

open System
open System.Text.Json
open SE.Gltf

let args = System.Environment.GetCommandLineArgs()
if args.Length < 3 then failwith "forgot to add path_to_gltf in args"
let path = args[2]
let file = System.IO.File.ReadAllText(path)

let obj = JsonSerializer.Deserialize<GltfRoot>(file)
let accessors = obj.accessors

printfn "%A" accessors[0]

open GltfBin
let bobct = new BinLoader(path, 0)
bobct.Print<float32>(228)
(bobct :> IDisposable).Dispose()


