#r "../bin/Release/net9.0/SE-renderer.dll"
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
        let (vertices,indices) = Geometry.load_txt (args[2], 0.55f, 0.55f, 0.53f, 1.0f)
        let _model = Model(vertices,indices)
        _model.Transform <- Matrix4.CreateScale(0.2f)
        _model
    | GLTF.IsGltf ->
        use gltf = new GLTF.Deserializer(path)
        let (vertices,indices) = gltf.ReadAllMeshes()
        let _model = Model(vertices,indices)
        _model.Transform <- Matrix4.CreateScale(10.0f)
        _model
    | GLTF.IsPly ->
        let (vertices,indices) = Geometry.load_ply (args[2], 0.55f, 0.55f, 0.53f, 1.0f)
        let _model = Model(vertices,indices)
        _model.Transform <- Matrix4.CreateScale(10.0f)
        let mutable i = 0
        for mesh in (_model :> System.Collections.Generic.IEnumerable<Triangle>) do
            if i % 100 = 0 then printfn "[%d] v0: %A, v1: %A, v2: %A" i mesh.n0 mesh.n1 mesh.n2
            i <- i + 1
        _model
    | GLTF.IsEmpty ->          
        let (vertices,indices) = Geometry.cube ()
        Model(vertices,indices)

let game = new TestGame(model)
game.Run()
