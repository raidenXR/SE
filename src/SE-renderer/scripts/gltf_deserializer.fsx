#r "nuget: OpenTK, 4.9.4"
#load "../src/gltfloader.fs"

open SE.Renderer.GLTF
open System.Numerics
open System.Text.Json


let _args = System.Environment.GetCommandLineArgs()
let gltf_path = _args[2]
let idx = gltf_path.LastIndexOf('.')
let gltf_output = gltf_path[0..idx - 1] + "_deserialized.txt"
let gltf_handle = new Deserializer(gltf_path)
let gltf = gltf_handle.Root

let fs = System.IO.File.CreateText(gltf_output)


for i,mesh in (Array.indexed gltf.meshes) do
    fs.WriteLine($"mesh: {i}")
    for j,primitive in (Array.indexed mesh.primitives) do
        fs.WriteLine($"primitive: {j}")

        let p_accessor = gltf.accessors[primitive.attributes.POSITION]
        let n_accessor = gltf.accessors[primitive.attributes.NORMAL]
        let i_accessor = gltf.accessors[primitive.indices]
        let material = gltf.materials[primitive.material]
        let mode = mode (primitive.mode)

        let p_bv = gltf.bufferViews[p_accessor.bufferView]
        let n_bv = gltf.bufferViews[n_accessor.bufferView]
        let i_bv = gltf.bufferViews[i_accessor.bufferView]

        fs.WriteLine($"pos.count: {p_accessor.count}, norm.count: {n_accessor.count}, indices.count: {i_accessor.count}")
        let p_span = gltf_handle.AsSpan<Vector3>(p_accessor.byteOffset + p_bv.byteOffset, p_accessor.count)
        let n_span = gltf_handle.AsSpan<Vector3>(n_accessor.byteOffset + n_bv.byteOffset, n_accessor.count)
        let i_span = gltf_handle.AsSpan<uint16>(i_accessor.byteOffset + i_bv.byteOffset, i_accessor.count)

        fs.WriteLine("positions,  normals")
        if p_span.Length <> n_span.Length then failwith "positions and normals do not have the same Length"
        for i in 0..p_span.Length - 1 do fs.WriteLine((sprintf "%A,  %A" p_span[i] n_span[i]))
      
        fs.WriteLine("indices")
        for i in 0..3..i_span.Length - 1 do fs.WriteLine((sprintf "%d,  %d,  %d" i_span[i + 0] i_span[i + 1] i_span[i + 2]))


fs.Close()
gltf_handle.Dispose()
