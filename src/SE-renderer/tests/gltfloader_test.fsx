#r "../bin/Debug/net9.0/SE-renderer.dll"
#r "nuget: OpenTK, 4.9.4"

open SE.Renderer
open SE.Renderer.GLTF
open OpenTK.Mathematics

let args = System.Environment.GetCommandLineArgs()
let path = if args.Length > 2 then args[2] else "../models/animated_object.gltf"
let mutable gltf: option<GLTF.Deserializer> = None

let model =
    match path with
    // | GLTF.IsTxt ->
    //     let (vertices,indices) = Geometry.load_txt (args[2], 0.55f, 0.55f, 0.53f, 1.0f)
    //     let _model = Model(vertices,indices)
    //     _model.Transform <- Matrix4.CreateScale(0.2f)
    //     _model
    | GLTF.IsGltf ->
        gltf <- Some (new GLTF.Deserializer(path))
        let struct(vertices,indices) = gltf.Value.ReadMesh_unmanaged(0)
        new ValueModel(vertices,indices, [3;3;4])
    // | GLTF.IsPly ->
    //     let (vertices,indices) = Geometry.load_ply (args[2], 0.55f, 0.55f, 0.53f, 1.0f)
    //     let _model = Model(vertices,indices)
    //     _model.Transform <- Matrix4.CreateScale(10.0f)
    //     _model
    // | GLTF.IsEmpty ->          
    //     let (vertices,indices) = Geometry.cube ()
    //     Model(vertices,indices)
    | _ ->
        let struct(vertices,indices) = Geometry.cube_unmanged()
        new ValueModel(vertices,indices, [3;3;4])

let game = new GltfWithParticles(gltf,model)
game.Run()
