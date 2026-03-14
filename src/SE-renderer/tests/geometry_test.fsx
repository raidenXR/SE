#r "../bin/Debug/net10.0/SE-renderer.dll"
#r "nuget: OpenTK, 4.9.4"

open System
open SE.Renderer
open OpenTK.Mathematics
open System.IO

let args = System.Environment.GetCommandLineArgs()
let struct(vertices,indices) = Geometry.load_ply_unmanaged (args[2], 0.55f, 0.55f, 0.53f, 1.0f)
// let gltf = new GLTF.Deserializer(args[2])
// let (vertices,indices) = gltf.ReadAllMeshes()
let N = 200
let bunny_model = new ValueModel(vertices, indices, [3;3;4])
let particles = new NativeArray<float32>(N*N*N)
#time
let mutable voxels = new NativeArray3D<bool>(N,N,N)
let t_filled = Geometry.assign_voxels_SIMD bunny_model N &voxels
let cv_bounds = Geometry.bounds_SIMD (bunny_model.Vertices) (bunny_model.L)
printfn "total_voxels: %d, filled voxels: %d" (voxels.Length) t_filled
Geometry.assign_particles_SIMD (cv_bounds, voxels, particles.AsSpan(), bunny_model.L)
printfn "particles.len: %d, matches voxels: %A" (particles.Length / 7) ((particles.Length / 7) = t_filled)
#time

let particles_model = new ValueModel(particles, [3;4])
let L = particles_model.L
let particles_count = particles.Length / L

let game = new Particles(particles_model)
game.Run()

// gltf.Dispose()
