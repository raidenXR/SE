namespace SE.Spatial
open System
open System.Numerics
open System.Runtime.CompilerServices
open System.Runtime.InteropServices
open SE

type [<Struct>] Ray = {origin:Vector3; direction:Vector3; length:float32}

type [<Struct>] CVBounds = {v_min:Vector3; v_max:Vector3}

type [<Struct>] Triangle = {n0:Vector3; n1:Vector3; n2:Vector3}

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


type [<Struct>] Shape =
    val mutable positions: narray<Vector3>
    val mutable normals:   narray<Vector3>
    val mutable indices:   narray<uint32>

    new(n1:int, n2:int) = {
        positions = NativeArray.create<Vector3> n1
        normals   = NativeArray.create<Vector3> n1
        indices   = NativeArray.create<uint32> n2
    }

    interface IDisposable with
        member this.Dispose() = 
            this.positions.Dispose()
            this.normals.Dispose()
            this.indices.Dispose()
            
    member this.Dispose() = 
        this.positions.Dispose()
        this.normals.Dispose()
        this.indices.Dispose()
        

module Geometry =
    /// calculates the bounds of a ControlVolume (CV) with SIMD intrisics
    let bounds (vertices:ReadOnlySpan<Vector3>)  =
        let mutable v_max = vertices[0]
        let mutable v_min = vertices[0]

        for v in vertices do
            v_min <- Vector3.Min(v, v_min)
            v_max <- Vector3.Max(v, v_max)
            
        {v_min = v_min; v_max = v_max} 


    let inline barycentric (a:inref<Vector3>) (b:inref<Vector3>) (c:inref<Vector3>) =
        let v_min = Vector3.Min(a, Vector3.Min(b,c))
        let v_max = Vector3.Max(a, Vector3.Max(b,c))
        v_min + (v_max - v_min) / 2.f
        
    /// creates a volume as voxels bool, where n is the resolution
    let assign_voxels (model:Shape) (v:byref<Voxels>) =
        let N = v.voxels.I
        let indices_count = model.indices.Length / 3
        let indices  = model.indices
        let p = model.positions.AsSpan()
        let n = model.normals.AsSpan()

        // let p = &MemoryMarshal.GetReference(positions)
        let cv = bounds p
        let v_min = cv.v_min
        let v_max = cv.v_max
        let dv = v_max - v_min
        let n = float32 (N - 1)
        let mutable t_filled = 0

        for i in 0..indices_count-1 do
            let i0 = indices[3*i+0] |> int32
            let i1 = indices[3*i+1] |> int32
            let i2 = indices[3*i+2] |> int32

            let v0 = p[i0]
            let v1 = p[i1]
            let v2 = p[i2]
            
            let vc = barycentric &v0 &v1 &v2
            let idx_c = Vector3.Clamp(n * (vc - v_min) / dv, Vector3.Zero, Vector3(n))
            v.voxels[int32(idx_c.X), int32(idx_c.Y), int32(idx_c.Z)] <- true
            
            let idx_0 = Vector3.Clamp(n * (v0 - v_min) / dv, Vector3.Zero, Vector3(n)) 
            v.voxels[int32(idx_0.X), int32(idx_0.Y), int32(idx_0.Z)] <- true

            let idx_1 = Vector3.Clamp(n * (v1 - v_min) / dv, Vector3.Zero, Vector3(n)) 
            v.voxels[int32(idx_1.X), int32(idx_1.Y), int32(idx_1.Z)] <- true
            
            let idx_2 = Vector3.Clamp(n * (v2 - v_min) / dv, Vector3.Zero, Vector3(n)) 
            v.voxels[int32(idx_2.X), int32(idx_2.Y), int32(idx_2.Z)] <- true

            t_filled <- t_filled + 4
                        
        // VECTORIZE THIS PART !!! OR NOT ... ??
        // let hn = if N % 2 <> 0 then N / 2 else N / 2 + 1
        let hn = N / 2
        for ix in 0..N-1 do
            for iy in 0..N-1 do       
                for iz in 1..hn-1 do
                    v.voxels[ix,iy,iz] <- v.voxels[ix,iy,iz-1] || v.voxels[ix,iy,iz]
                    t_filled <- t_filled + if v.voxels[ix,iy,iz] then 1 else 0

                for iz=N-2 downto hn do
                    v.voxels[ix,iy,iz] <- v.voxels[ix,iy,iz+1] || v.voxels[ix,iy,iz]
                    t_filled <- t_filled + if v.voxels[ix,iy,iz] then 1 else 0
        
        v.filled <- t_filled


