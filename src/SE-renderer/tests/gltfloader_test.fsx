#r "../bin/Debug/net9.0/SE-renderer.dll"
#r "nuget: OpenTK, 4.9.4"

open SE.Renderer
open SE.Renderer.GLTF
open OpenTK.Mathematics

let path = "../models/animated_object.gltf"
let gltf = new Deserializer(path)
let (vertices,indices) = gltf.ReadAllMeshes()
let model = Model(vertices,indices)
let mutable t = 0f
model.Transform <- Matrix4.CreateScale(10.0f)

let game = new GltfWithParticles(gltf,model)
game.Run()
