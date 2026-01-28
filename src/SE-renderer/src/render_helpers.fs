namespace SE.Renderer
// open ImGuiNET
open OpenTK.Graphics
open OpenTK.Graphics.OpenGL4
open OpenTK.Windowing.Common
open OpenTK.Windowing.Desktop
open OpenTK.Windowing.GraphicsLibraryFramework
open System
open System.Numerics
open System.Collections.Generic
open System.ComponentModel
open System.Diagnostics
open System.Runtime.InteropServices
open System.Runtime.CompilerServices
open FSharp.NativeInterop


module Helpers =
    let createMesh_managed (ob_model:Model) =
        let vbo = GL.GenBuffer()
        GL.BindBuffer (BufferTarget.ArrayBuffer, vbo)
        GL.BufferData (BufferTarget.ArrayBuffer, ob_model.VerticesBufferSize, ob_model.Vertices, BufferUsageHint.StaticDraw)
    
        let vao = GL.GenVertexArray()
        GL.BindVertexArray(vao)
        GL.EnableVertexAttribArray(0)
        GL.EnableVertexAttribArray(1)
        GL.EnableVertexAttribArray(2)            
        GL.VertexAttribPointer(0, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib0)
        GL.VertexAttribPointer(1, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib1)
        GL.VertexAttribPointer(2, (GLTF.size "VEC4"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib2)

        let ebo = GL.GenBuffer()
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo)
        GL.BufferData(BufferTarget.ElementArrayBuffer, ob_model.IndicesBufferSize, ob_model.Indices, BufferUsageHint.StaticDraw)
        {vao = vao; vbo = vbo; ebo = ebo}

    let updateMesh_managed (ob_model:Model) (mesh:GLMesh) =
        GL.BindBuffer (BufferTarget.ArrayBuffer, mesh.vbo)
        GL.BufferData (BufferTarget.ArrayBuffer, ob_model.VerticesBufferSize, ob_model.Vertices, BufferUsageHint.StaticDraw)
        
        GL.BindVertexArray(mesh.vao)
        GL.EnableVertexAttribArray(0)
        GL.EnableVertexAttribArray(1)
        GL.EnableVertexAttribArray(2)            
        GL.VertexAttribPointer(0, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib0)
        GL.VertexAttribPointer(1, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib1)
        GL.VertexAttribPointer(2, (GLTF.size "VEC4"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib2)

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.ebo)
        GL.BufferData(BufferTarget.ElementArrayBuffer, ob_model.IndicesBufferSize, ob_model.Indices, BufferUsageHint.DynamicDraw)
        
    let drawMesh_managed (ob_model:Model) (mesh:GLMesh) =
        GL.BindVertexArray(mesh.vao)
        GL.DrawElements(PrimitiveType.Triangles, ob_model.Indices.Length, DrawElementsType.UnsignedInt, 0)

    let createMesh (ob_model:ValueModel) =
        let vbo = GL.GenBuffer()
        GL.BindBuffer (BufferTarget.ArrayBuffer, vbo)
        GL.BufferData (BufferTarget.ArrayBuffer, ob_model.vertices.BufferSize, ob_model.vertices.ToInt(), BufferUsageHint.StaticDraw)
    
        let vao = GL.GenVertexArray()
        GL.BindVertexArray(vao)
        GL.EnableVertexAttribArray(0)
        GL.EnableVertexAttribArray(1)
        GL.EnableVertexAttribArray(2)            
        GL.VertexAttribPointer(0, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib0)
        GL.VertexAttribPointer(1, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib1)
        GL.VertexAttribPointer(2, (GLTF.size "VEC4"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib2)

        let ebo = GL.GenBuffer()
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, ebo)
        GL.BufferData(BufferTarget.ElementArrayBuffer, ob_model.indices.BufferSize, ob_model.indices.ToInt(), BufferUsageHint.StaticDraw)
        {vao = vao; vbo = vbo; ebo = ebo}

    let updateMesh (ob_model:ValueModel) (mesh:GLMesh) =
        GL.BindBuffer (BufferTarget.ArrayBuffer, mesh.vbo)
        GL.BufferData (BufferTarget.ArrayBuffer, ob_model.vertices.BufferSize, ob_model.vertices.ToInt(), BufferUsageHint.StaticDraw)
    
        GL.BindVertexArray(mesh.vao)
        GL.EnableVertexAttribArray(0)
        GL.EnableVertexAttribArray(1)
        GL.EnableVertexAttribArray(2)            
        GL.VertexAttribPointer(0, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib0)
        GL.VertexAttribPointer(1, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib1)
        GL.VertexAttribPointer(2, (GLTF.size "VEC4"), VertexAttribPointerType.Float, false, ob_model.Stride, ob_model.Attrib2)

        GL.BindBuffer(BufferTarget.ElementArrayBuffer, mesh.ebo)
        GL.BufferData(BufferTarget.ElementArrayBuffer, ob_model.indices.BufferSize, ob_model.indices.ToInt(), BufferUsageHint.DynamicDraw)
        
    let drawMesh (ob_model:ValueModel) (mesh:GLMesh) =
        GL.BindVertexArray(mesh.vao)
        GL.DrawElements(PrimitiveType.Triangles, ob_model.Indices.Length, DrawElementsType.UnsignedInt, 0)


    let createPrim_managed (ob_model:Model) =
        let vbo = GL.GenBuffer()
        GL.BindBuffer (BufferTarget.ArrayBuffer, vbo)
        GL.BufferData (BufferTarget.ArrayBuffer, ob_model.VerticesBufferSize, ob_model.Vertices, BufferUsageHint.StaticDraw)
        
        let vao = GL.GenVertexArray()
        GL.BindVertexArray(vao)
        GL.EnableVertexAttribArray(0)
        GL.EnableVertexAttribArray(1)            
        GL.VertexAttribPointer(0, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.L, ob_model.Attrib0)
        GL.VertexAttribPointer(1, (GLTF.size "VEC4"), VertexAttribPointerType.Float, false, ob_model.L, ob_model.Attrib1)
        {vao = vao; vbo = vbo}

    let updatePrim_managed (ob_model:Model) (prim:GLPrim) =
        GL.BindBuffer (BufferTarget.ArrayBuffer, prim.vbo)
        GL.BufferData (BufferTarget.ArrayBuffer, ob_model.VerticesBufferSize, ob_model.Vertices, BufferUsageHint.DynamicDraw)
        
        GL.BindVertexArray(prim.vao)
        GL.EnableVertexAttribArray(0)
        GL.EnableVertexAttribArray(1)            
        GL.VertexAttribPointer(0, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.L, ob_model.Attrib0)
        GL.VertexAttribPointer(1, (GLTF.size "VEC4"), VertexAttribPointerType.Float, false, ob_model.L, ob_model.Attrib1)

    let drawPrim_managed (ob_model:Model) (prim:GLPrim) =
        GL.BindVertexArray(prim.vao)
        GL.DrawArrays(PrimitiveType.Points, 0, ob_model.Vertices.Length)


    let createPrim (ob_model:ValueModel) =
        let vbo = GL.GenBuffer()
        GL.BindBuffer (BufferTarget.ArrayBuffer, vbo)
        GL.BufferData (BufferTarget.ArrayBuffer, ob_model.vertices.BufferSize, ob_model.vertices.ToInt(), BufferUsageHint.StaticDraw)
        
        let vao = GL.GenVertexArray()
        GL.BindVertexArray(vao)
        GL.EnableVertexAttribArray(0)
        GL.EnableVertexAttribArray(1)            
        GL.VertexAttribPointer(0, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.L, ob_model.Attrib0)
        GL.VertexAttribPointer(1, (GLTF.size "VEC4"), VertexAttribPointerType.Float, false, ob_model.L, ob_model.Attrib1)
        {vao = vao; vbo = vbo}

    let updatePrim (ob_model:ValueModel) (prim:GLPrim) =
        GL.BindBuffer (BufferTarget.ArrayBuffer, prim.vbo)
        GL.BufferData (BufferTarget.ArrayBuffer, ob_model.vertices.BufferSize, ob_model.vertices.ToInt(), BufferUsageHint.DynamicDraw)
        
        GL.BindVertexArray(prim.vao)
        GL.EnableVertexAttribArray(0)
        GL.EnableVertexAttribArray(1)            
        GL.VertexAttribPointer(0, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.L, ob_model.Attrib0)
        GL.VertexAttribPointer(1, (GLTF.size "VEC4"), VertexAttribPointerType.Float, false, ob_model.L, ob_model.Attrib1)

    let updatePrim_sliced (ob_model:ValueModel) (t_filled:int) (prim:GLPrim) =
        GL.BindBuffer (BufferTarget.ArrayBuffer, prim.vbo)
        GL.BufferData (BufferTarget.ArrayBuffer, ob_model.Stride * t_filled, ob_model.vertices.ToInt(), BufferUsageHint.DynamicDraw)
        
        GL.BindVertexArray(prim.vao)
        GL.EnableVertexAttribArray(0)
        GL.EnableVertexAttribArray(1)            
        GL.VertexAttribPointer(0, (GLTF.size "VEC3"), VertexAttribPointerType.Float, false, ob_model.L, ob_model.Attrib0)
        GL.VertexAttribPointer(1, (GLTF.size "VEC4"), VertexAttribPointerType.Float, false, ob_model.L, ob_model.Attrib1)

    let drawPrim (ob_model:ValueModel) (prim:GLPrim) =
        GL.BindVertexArray(prim.vao)
        GL.DrawArrays(PrimitiveType.Points, 0, ob_model.Vertices.Length)


    let drawPrim_sliced (t_filled:int) (prim:GLPrim) =
        GL.BindVertexArray(prim.vao)
        GL.DrawArrays(PrimitiveType.Points, 0, t_filled)


    let default_color = [|0.53f; 0.55f; 0.53f; 1.0f|]

    let update_animation (gltf:GLTF.Deserializer, model:ValueModel, v_animation:byref<ValueAnimation>, time:float) =
        let root = gltf.Root
        let vertices = model.Vertices
        let indices  = model.Indices
        let p = &MemoryMarshal.GetReference(vertices)
        let ptr = &&p
        let L = model.L

        let mutable _t = Matrix4x4.Identity
        let mutable _r = Matrix4x4.Identity
        let mutable _s = Matrix4x4.Identity
        
        let animation = root.animations[v_animation.idx]

        for channel in animation.channels do
            let i_accessor = root.accessors[animation.samplers[channel.sampler].input]
            let o_accessor = root.accessors[animation.samplers[channel.sampler].output]
            let i_bv = root.bufferViews[i_accessor.bufferView]
            let o_bv = root.bufferViews[o_accessor.bufferView]
            let i_span = gltf.AsSpan<float32>(i_bv.byteOffset + i_accessor.byteOffset, i_accessor.count)
            let t_first = float(i_span[0])
            let t_last  = float(i_span[i_span.Length - 1])

            v_animation.dt <- v_animation.dt + if v_animation.is_reversed then -time else time

            if v_animation.is_looped then
                if v_animation.dt > t_last then
                    v_animation.dt <- t_last                
                    v_animation.is_reversed <- not v_animation.is_reversed
                
                elif v_animation.dt < t_first then
                    v_animation.dt <- t_first
                    v_animation.is_reversed <- not v_animation.is_reversed           
            
            let dt = float32(v_animation.dt)
            let mutable kf = 0  // key_frame  and i_span == time_span
            let interpolation_value =
                let mutable r = false
                while not r do
                    r <- (i_span[kf] <= dt && dt <= i_span[kf+1]) || (kf + 1 >= i_span.Length - 1)
                    kf <- if r then kf else kf + 1                
                (dt - i_span[kf]) / (i_span[kf+1] - i_span[kf])                 
            
            match channel.target.path with
            | "translation" ->
                let o_span = gltf.AsSpan<Vector3>(o_bv.byteOffset + o_accessor.byteOffset, o_accessor.count) 
                let t = o_span[kf] + interpolation_value * (o_span[kf+1] - o_span[kf])
                _t <- Matrix4x4.CreateTranslation(t)
            | "rotation" ->
                let o_span = gltf.AsSpan<Quaternion>(o_bv.byteOffset + o_accessor.byteOffset, o_accessor.count) 
                let r = o_span[kf] + Quaternion.Multiply(o_span[kf+1] - o_span[kf], interpolation_value)
                _r <- Matrix4x4.CreateFromQuaternion(r)
            | "scale" ->
                let o_span = gltf.AsSpan<Vector3>(o_bv.byteOffset + o_accessor.byteOffset, o_accessor.count) 
                let s = o_span[kf] + interpolation_value * (o_span[kf+1] - o_span[kf])
                _s <- Matrix4x4.CreateScale(s)
            | _ -> failwith $"{channel.target.path} is not implemented"

            let m_transform = _t * _r * _s
            let mesh = root.meshes[root.nodes[channel.target.node].mesh]

            let mutable pn = 0
            for primitive in mesh.primitives do
                let material_color = 
                    match root.materials with
                    | null -> default_color 
                    | _ -> root.materials[primitive.material].pbrMetallicRoughness.baseColorFactor
                let p_accessor = root.accessors[primitive.attributes.POSITION]
                let n_accessor = root.accessors[primitive.attributes.NORMAL]
                let i_accessor = root.accessors[primitive.indices]
                let p_bv = root.bufferViews[p_accessor.bufferView]
                let p_span = gltf.AsSpan<Vector3>(p_bv.byteOffset + p_accessor.byteOffset, p_accessor.count)
                let vertices_count = p_accessor.count

                let L = model.L
                for i in 0..vertices_count - 1 do
                    let v_transformed = Vector3.Transform(p_span[i], m_transform)
                    Unsafe.Write<Vector3>(~~(ptr ++ pn), v_transformed)
                    pn <- pn + L

        // recompute the normals
        let v_offset = sizeof<Vector3> / sizeof<float32>
        let indices_count = indices.Length / 3
        for i in 0..indices_count - 1 do
            let i0 = int32 (indices[3*i+0])
            let i1 = int32 (indices[3*i+1])
            let i2 = int32 (indices[3*i+2])
        
            let v0 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i0))
            let v1 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i1))
            let v2 = Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i2))

            let e0 = v1 - v0
            let e1 = v2 - v0
            let face_normal = Vector3.Cross(e0, e1)
        
            Unsafe.Write<Vector3>(~~(ptr ++ (L*i0 + v_offset)), face_normal + Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i0 + v_offset)))
            Unsafe.Write<Vector3>(~~(ptr ++ (L*i1 + v_offset)), face_normal + Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i1 + v_offset)))
            Unsafe.Write<Vector3>(~~(ptr ++ (L*i2 + v_offset)), face_normal + Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i2 + v_offset)))

        let vertices_count = vertices.Length / L
        for i in 0..vertices_count - 1 do
            let normal = Vector3.Normalize(Unsafe.As<float32,Vector3>(&Unsafe.Add(&p, L*i + v_offset)))
            Unsafe.Write<Vector3>(~~(ptr ++ (L*i + v_offset)), normal)

            

