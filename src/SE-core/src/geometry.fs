namespace SE.Spatial

open System
open System.Numerics
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open SE

type [<Struct>] Ray = {origin:Vector3; direction:Vector3; length:float32}

type [<Struct>] CVBounds = {v_min:Vector3; v_max:Vector3}

type [<Struct>] Mesh = {vertices:narray<float>; L:int; indices:narray<uint32>}

type [<Struct>] MeshF = {vertices:narray<float32>; L:int; indices:narray<uint32>}

type [<Struct>] Triangle = {n0:Vector3; n1:Vector3; n2:Vector3}

type [<Struct>] Rectangle = {
    x_min: float32
    y_min: float32
    z_min: float32
    x_max: float32
    y_max: float32
    z_max: float32
}

type [<Struct>] Sphere = {
    r: float32
    x: float32
    y: float32
    z: float32
}

type [<Struct>] ConvexHull = {points:narray<float32>}

// // TODO: add capsule, and other common shapes FOR COLLISIONS
// type [<Struct>] Shape =
//     | Cube of cube: Rectangle
//     | Sphere of sphere: Sphere
//     | ConvexHull of convexhull: ConvexHull
//     | Mesh of mesh: Mesh

type [<Struct>] Voxels =
    val mutable voxels: narray3d<bool>
    val mutable bounds: CVBounds
    val mutable filled: int
    // val N: int
    // val mutable transf: Matrix4x4

    new(voxels:narray3d<bool>, filled:int, bounds:CVBounds) = {
        // N = N
        voxels = voxels
        filled = filled
        bounds = bounds
        // transf = Matrix4x4.Identity
    }

    member this.Dispose() = this.voxels.Dispose()
        
    interface IDisposable with
        member this.Dispose() = this.Dispose()


// type [<Struct>] Shape =
//     val mutable positions: narray<Vector3>
//     val mutable normals:   narray<Vector3>
//     val mutable indices:   narray<uint32>

//     new(n1:int, n2:int) = {
//         positions = NativeArray.create<Vector3> n1
//         normals   = NativeArray.create<Vector3> n1
//         indices   = NativeArray.create<uint32> n2
//     }

//     interface IDisposable with
//         member this.Dispose() = 
//             this.positions.Dispose()
//             this.normals.Dispose()
//             this.indices.Dispose()
            
//     member this.Dispose() = 
//         this.positions.Dispose()
//         this.normals.Dispose()
//         this.indices.Dispose()
        

module Geometry =
    /// calculates the bounds of a ControlVolume (CV) with SIMD intrisics
    let bounds (vertices:ReadOnlySpan<float32>) L =
        let vertices_count = vertices.Length / L
        let p = &MemoryMarshal.GetReference(vertices)
        let mutable v_min = Unsafe.As<float32,Vector3>(&p)
        let mutable v_max = Unsafe.As<float32,Vector3>(&p)

        for i in 0..vertices_count-1 do
            let v = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, i*L))
            v_min <- Vector3.Min(v, v_min)
            v_max <- Vector3.Max(v, v_max)
            
        {v_min = v_min; v_max = v_max} 


    let inline barycentric (a:inref<Vector3>) (b:inref<Vector3>) (c:inref<Vector3>) =
        let v_min = Vector3.Min(a, Vector3.Min(b,c))
        let v_max = Vector3.Max(a, Vector3.Max(b,c))
        v_min + (v_max - v_min) / 2.f
        
    /// creates a volume as voxels bool, where n is the resolution
    let voxels (mesh:MeshF) N =
        let L = mesh.L
        let indices_count = mesh.indices.Length / 3
        let indices  = mesh.indices.AsSpan()
        let vertices = mesh.vertices.AsSpan()
        let p = &MemoryMarshal.GetReference(vertices)
        let cv = bounds vertices L 
        let v_min = cv.v_min
        let v_max = cv.v_max
        let dv = v_max - v_min
        let n = float32 (N - 1)
        let mutable t_filled = 0
        let mutable voxels = NativeArray3D.create<bool> N N N

        for i in 0..indices_count-1 do
            let i0 = int32 (indices[3*i+0])
            let i1 = int32 (indices[3*i+1])
            let i2 = int32 (indices[3*i+2])

            let v0 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i0))
            let v1 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i1))
            let v2 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i2))
            let vc = barycentric &v0 &v1 &v2
            
            let idx_c = Vector3.Round(n * (vc - v_min) / dv)
            let idx_0 = Vector3.Round(n * (v0 - v_min) / dv) 
            let idx_1 = Vector3.Round(n * (v1 - v_min) / dv) 
            let idx_2 = Vector3.Round(n * (v2 - v_min) / dv) 
            
            voxels[int32(idx_c.X), int32(idx_c.Y), int32(idx_c.Z)] <- true
            voxels[int32(idx_0.X), int32(idx_0.Y), int32(idx_0.Z)] <- true
            voxels[int32(idx_1.X), int32(idx_1.Y), int32(idx_1.Z)] <- true            
            voxels[int32(idx_2.X), int32(idx_2.Y), int32(idx_2.Z)] <- true

            t_filled <- t_filled + 4
                        
        // VECTORIZE THIS PART !!! OR NOT ... ??
        // let hn = if N % 2 <> 0 then N / 2 else N / 2 + 1
        let hn = N / 2
        for ix in 0..N-1 do
            for iy in 0..N-1 do       
                for iz in 1..hn-1 do
                    voxels[ix,iy,iz] <- voxels[ix,iy,iz-1] || voxels[ix,iy,iz]
                    t_filled <- t_filled + if voxels[ix,iy,iz] then 1 else 0

                for iz=N-2 downto hn do
                    voxels[ix,iy,iz] <- voxels[ix,iy,iz+1] || voxels[ix,iy,iz]
                    t_filled <- t_filled + if voxels[ix,iy,iz] then 1 else 0       
       
        new Voxels(voxels, t_filled, cv)


