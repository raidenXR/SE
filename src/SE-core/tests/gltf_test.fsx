#load "../src/gltfLoader.fs"

open System.Text.Json
open SE.Gltf

let obj = JsonSerializer.Deserialize<GltfRoot>("BIMExample_edited.gltf")
obj
