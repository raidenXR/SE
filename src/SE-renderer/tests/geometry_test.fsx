#r "../bin/Debug/net9.0/SE-renderer.dll"
#r "nuget: OpenTK, 4.9.4"

open System
open SE.Renderer
open OpenTK.Mathematics
open System.IO

let args = System.Environment.GetCommandLineArgs()
let (vertices,indices) = Geometry.load_ply (args[2], 0.55f, 0.55f, 0.53f, 1.0f)
// let gltf = new GLTF.Deserializer(args[2])
// let (vertices,indices) = gltf.ReadAllMeshes()
let bunny_model = new Model(vertices, indices)
#time
let N = 30
// let mutable t = 0
let (voxels,t) = Geometry.as_voxels bunny_model N
// let t = Geometry.reverse_voxels voxels N
let (v_min,v_max) = Geometry.bounds vertices N bunny_model.L
printfn "total_voxels: %d, filled voxels: %d" (voxels.Length) t
let particles = Geometry.get_particles v_min v_max voxels t N 7
printfn "particles.len: %d, matches voxels: %A" (particles.Length / 7) ((particles.Length / 7) = t)
#time

let particles_model = new Model(particles, [||], [3;4])
particles_model.Transform <- Matrix4.CreateScale(10.f)
let L = particles_model.L
let particles_count = particles.Length / L

let game = new Particles(particles_model)
game.Run()

// gltf.Dispose()
