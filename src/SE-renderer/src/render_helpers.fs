namespace SE.Renderer
// open ImGuiNET
open OpenTK.Graphics
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics
open OpenTK.Windowing.Common
open OpenTK.Windowing.Desktop
open OpenTK.Windowing.GraphicsLibraryFramework
open System
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

    let drawPrim (ob_model:ValueModel) (prim:GLPrim) =
        GL.BindVertexArray(prim.vao)
        GL.DrawArrays(PrimitiveType.Points, 0, ob_model.Vertices.Length)


    let update_animation_managed (gltf:GLTF.Deserializer, model:Model, v:byref<ValueAnimation>, time:float) =
        let root = gltf.Root
        let vertices = model.Vertices
        let indices  = model.Indices
        if abs(v.dt - time) > 200 then v.is_reversed <- not v.is_reversed
        v.dt <- if abs(v.dt - time) > 200 then v.dt - 200. else v.dt + time
        v.kf <- if v.is_reversed then int v.dt else int (200. - v.dt)
        
        let animation = root.animations[v.idx]
        let mutable t = System.Numerics.Matrix4x4.Identity
        let mutable r = System.Numerics.Matrix4x4.Identity
        let mutable s = System.Numerics.Matrix4x4.Identity
        for channel in animation.channels do
            let i_accessor = root.accessors[animation.samplers[channel.sampler].input]
            let o_accessor = root.accessors[animation.samplers[channel.sampler].output]
            let i_bv = root.bufferViews[i_accessor.bufferView]
            let o_bv = root.bufferViews[o_accessor.bufferView]
            let i_span = gltf.AsSpan<float32>(i_bv.byteOffset + i_accessor.byteOffset, i_accessor.count)
            if v.kf > o_accessor.count - 1 then v.kf <- 0

            match channel.target.path with
            | "translation" ->
                let o_span = gltf.AsSpan<System.Numerics.Vector3>(o_bv.byteOffset + o_accessor.byteOffset, o_accessor.count) 
                t <- System.Numerics.Matrix4x4.CreateTranslation(o_span[v.kf])
            | "rotation" ->
                let o_span = gltf.AsSpan<System.Numerics.Quaternion>(o_bv.byteOffset + o_accessor.byteOffset, o_accessor.count) 
                r <- System.Numerics.Matrix4x4.CreateFromQuaternion(o_span[v.kf])
            | "scale" ->
                let o_span = gltf.AsSpan<System.Numerics.Vector3>(o_bv.byteOffset + o_accessor.byteOffset, o_accessor.count) 
                s <- System.Numerics.Matrix4x4.CreateScale(o_span[v.kf])
            | _ -> failwith $"{channel.target.path} is not implemented"

            let m_transform = t * r * s

            let mesh = root.meshes[root.nodes[channel.target.node].mesh]
            let mutable pn = 0
            for primitive in mesh.primitives do
                let material_color = 
                    if root.materials <> null then
                        let material = root.materials[primitive.material]
                        material.pbrMetallicRoughness.baseColorFactor
                    else
                        [|0.53f; 0.55f; 0.53f; 1.0f|]
                let p_accessor = root.accessors[primitive.attributes.POSITION]
                let n_accessor = root.accessors[primitive.attributes.NORMAL]
                let i_accessor = root.accessors[primitive.indices]
                let p_bv = root.bufferViews[p_accessor.bufferView]
                let n_bv = root.bufferViews[n_accessor.bufferView]
                let i_bv = root.bufferViews[i_accessor.bufferView]
                let p_span = gltf.AsSpan<System.Numerics.Vector3>(p_bv.byteOffset + p_accessor.byteOffset, p_accessor.count)
                let n_span = gltf.AsSpan<System.Numerics.Vector3>(n_bv.byteOffset + n_accessor.byteOffset, n_accessor.count)
                let i_span = gltf.AsSpan<uint16>(i_bv.byteOffset + i_accessor.byteOffset, i_accessor.count)
                let vertices_count = p_accessor.count

                let L = model.L
                for i in 0..vertices_count - 1 do
                    let p = System.Numerics.Vector3.Transform(p_span[i], m_transform)
                    vertices[pn+0] <- p.X
                    vertices[pn+1] <- p.Y
                    vertices[pn+2] <- p.Z                    
                    pn <- pn + L



    let update_animation (gltf:GLTF.Deserializer, model:ValueModel, v:byref<ValueAnimation>, time:float) =
        let root = gltf.Root
        let vertices = model.Vertices
        let indices  = model.Indices
        if abs(v.dt - time) > 200 then v.is_reversed <- not v.is_reversed
        v.dt <- if abs(v.dt - time) > 200 then v.dt - 200. else v.dt + time
        v.kf <- if v.is_reversed then int v.dt else int (200. - v.dt)
        
        let animation = root.animations[v.idx]
        let mutable t = System.Numerics.Matrix4x4.Identity
        let mutable r = System.Numerics.Matrix4x4.Identity
        let mutable s = System.Numerics.Matrix4x4.Identity
        for channel in animation.channels do
            let i_accessor = root.accessors[animation.samplers[channel.sampler].input]
            let o_accessor = root.accessors[animation.samplers[channel.sampler].output]
            let i_bv = root.bufferViews[i_accessor.bufferView]
            let o_bv = root.bufferViews[o_accessor.bufferView]
            let i_span = gltf.AsSpan<float32>(i_bv.byteOffset + i_accessor.byteOffset, i_accessor.count)
            v.kf <- if v.kf < 0 then 0 else v.kf
            v.kf <- if v.kf > o_accessor.count - 1 then 0 else v.kf

            match channel.target.path with
            | "translation" ->
                let o_span = gltf.AsSpan<System.Numerics.Vector3>(o_bv.byteOffset + o_accessor.byteOffset, o_accessor.count) 
                t <- System.Numerics.Matrix4x4.CreateTranslation(o_span[v.kf])
            | "rotation" ->
                let o_span = gltf.AsSpan<System.Numerics.Quaternion>(o_bv.byteOffset + o_accessor.byteOffset, o_accessor.count) 
                r <- System.Numerics.Matrix4x4.CreateFromQuaternion(o_span[v.kf])
            | "scale" ->
                let o_span = gltf.AsSpan<System.Numerics.Vector3>(o_bv.byteOffset + o_accessor.byteOffset, o_accessor.count) 
                s <- System.Numerics.Matrix4x4.CreateScale(o_span[v.kf])
            | _ -> failwith $"{channel.target.path} is not implemented"

            let m_transform = t * r * s

            let mesh = root.meshes[root.nodes[channel.target.node].mesh]
            let mutable pn = 0
            for primitive in mesh.primitives do
                let material_color = 
                    if root.materials <> null then
                        let material = root.materials[primitive.material]
                        material.pbrMetallicRoughness.baseColorFactor
                    else
                        [|0.53f; 0.55f; 0.53f; 1.0f|]
                let p_accessor = root.accessors[primitive.attributes.POSITION]
                let n_accessor = root.accessors[primitive.attributes.NORMAL]
                let i_accessor = root.accessors[primitive.indices]
                let p_bv = root.bufferViews[p_accessor.bufferView]
                let n_bv = root.bufferViews[n_accessor.bufferView]
                let i_bv = root.bufferViews[i_accessor.bufferView]
                let p_span = gltf.AsSpan<System.Numerics.Vector3>(p_bv.byteOffset + p_accessor.byteOffset, p_accessor.count)
                let n_span = gltf.AsSpan<System.Numerics.Vector3>(n_bv.byteOffset + n_accessor.byteOffset, n_accessor.count)
                let i_span = gltf.AsSpan<uint16>(i_bv.byteOffset + i_accessor.byteOffset, i_accessor.count)
                let vertices_count = p_accessor.count

                let L = model.L
                for i in 0..vertices_count - 1 do
                    let p = System.Numerics.Vector3.Transform(p_span[i], m_transform)
                    vertices[pn+0] <- p.X
                    vertices[pn+1] <- p.Y
                    vertices[pn+2] <- p.Z                    
                    pn <- pn + L



