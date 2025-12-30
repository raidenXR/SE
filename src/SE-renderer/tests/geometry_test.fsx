#r "../bin/Debug/net9.0/SE-renderer.dll"
#r "nuget: OpenTK, 4.9.4"

open System
open SE.Renderer
open OpenTK.Mathematics
open System.IO

let args = System.Environment.GetCommandLineArgs()
let (vertices,indices) = Geometry.load_ply (args[2], 0.55f, 0.55f, 0.53f, 1.0f)
let bunny_model = new Model(vertices, indices)
#time
let N = 20
let (voxels,t) = Geometry.as_voxels bunny_model N
let (v_min,v_max) = Geometry.bounds vertices bunny_model.L
printfn "total_voxels: %d, filled voxels: %d" (voxels.Length) t
let particles = Geometry.get_particles v_min v_max voxels t N 7
printfn "particles.len: %d, matches voxels: %A" (particles.Length / 7) ((particles.Length / 7) = t)
#time

// let fs = File.CreateText("descretized_volume.dat")
let particles_model = new Model(particles, [||], [3;4])
particles_model.Transform <- Matrix4.CreateScale(10.f)
let L = particles_model.L
let particles_count = particles.Length / L
// for i in 0..particles_count - 1 do
//     fs.WriteLine($"{particles[L*i+0]}  {particles[L*i+1]}  {particles[L*i+2]}")
// fs.Close()

let dv = DV<float>(N, v_min, v_max, t, voxels)
dv.ApplyFn(fun v -> float (v.X + v.Y / v.Z))
let mutable vs = 0
for ix in 0..N-1 do
    for iy in 0..N-1 do
        for iz in 0..N-1 do
            if dv.Voxels[ix,iy,iz] then
                vs <- vs + 1
                printfn "[%d, %d, %d]: %g at %A" ix iy iz (dv.Value(ix,iy,iz)) (dv.Point(ix,iy,iz))
printfn "vs: %d" vs
// let game = new Particles(particles_model)
// game.Run()
