#r "../bin/Debug/net9.0/SE-renderer.dll"
#r "nuget: OpenTK, 4.9.4"

open System
open System.IO
open SE.Renderer
open OpenTK.Mathematics

let args = System.Environment.GetCommandLineArgs()
let path = if args.Length >= 3 then args[2] else String.Empty

let model =
    match path with
    | GLTF.IsTxt ->
        let (vertices,indices) = Geometry.load (args[2], 0.55f, 0.55f, 0.53f, 1.0f)
        let _model = Model(vertices,indices)
        _model.Scale <- Matrix4.CreateScale(0.2f)
        _model
    | GLTF.IsGltf ->
        use gltf = new GLTF.Deserializer(path)
        let (vertices,indices) = gltf.ReadAllMeshes()
        let _model = Model(vertices,indices)
        _model.Scale <- Matrix4.CreateScale(10.0f)
        _model
    | GLTF.IsEmpty ->          
        let (vertices,indices) = Geometry.cube ()
        Model(vertices,indices)

let game = new TestGame(model)
game.Run()
