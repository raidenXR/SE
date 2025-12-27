#r "../bin/Debug/net9.0/SE-renderer.dll"
#r "nuget: OpenTK, 4.9.4"

open System
open SE.Renderer
open OpenTK.Mathematics
open System.IO

let args = System.Environment.GetCommandLineArgs()
let (vertices,indices) = Geometry.load_ply (args[2], 0.55f, 0.55f, 0.53f, 1.0f)
#time
let N = 100
let (voxels,t) = Geometry.volume vertices indices N
let (v_min,v_max) = Geometry.get_CV vertices
printfn "total_voxels: %d, filled voxels: %d" (voxels.Length) t
let particles = Geometry.get_particles v_min v_max voxels t N
printfn "particles.len: %d, matches voxels: %A" (particles.Length / 7) ((particles.Length / 7) = t)
#time

let fs = File.CreateText("descretized_volume.dat")
let particles_count = particles.Length / 7
for i in 0..particles_count - 1 do
    fs.WriteLine($"{particles[7*i+0]}  {particles[7*i+1]}  {particles[7*i+2]}")
fs.Close()

let particles_model = new Model(particles, [|1u;1u;1u|])
particles_model.Transform <- Matrix4.CreateScale(10.f)
let game = new Particles(particles_model)
game.Run()
