#nowarn "9"
namespace SE.Renderer
open System
open System.IO
open System.Numerics
open System.Text.Json
open System.Text.Json.Serialization
open System.Runtime.InteropServices
open System.Runtime.CompilerServices
open FSharp.NativeInterop
open OpenTK.Graphics
open OpenTK.Graphics.OpenGL4


module GLTF = 
    type Buffer = {
        /// The URI(or IRI) of the buffer.
        uri: string
        /// The length of the buffer in bytes.
        byteLength: int
        /// The user-defined name of this object.
        name: string
        extensions: obj
        extras: obj
    }

    type BufferView = {
        /// The index of the buffer,
        buffer: int
        /// The offset into the buffer in bytes.
        byteOffset: int
        /// The length of the bufferView in bytes.
        byteLength: int
        /// The stride, in bytes.
        byteStride: int
        /// The hint representing the intended GPU buffer type to use with this buffer view.
        target: int
        /// The user defined name of this object.
        name: string
        extensions: obj
        extras: obj
    }

    type Image = {
        uri: string
        mimeType: string
        bufferView: int
        name: string
        extensions: obj
        extras: obj
    }

    type Texture = {    // (Normal, Occlusive, Emissive)
        sampler: int
        source: int
        name: string
        extensions: obj
        extras: obj
    }

    type TextureInfo = {
        index: int
        texCoord: int
        extensions: obj
        extras: obj
    }

    type Sampler = {
        magFilter: int
        minFilter: int
        wrapS: int
        wrapT: int
        name: string
        extensions: obj
        extras: obj
    }

    type Attributes = {
        NORMAL: int
        POSITION: int
        TANGENT: int
        TEXCOORD_0: int
        TEXCOORD_1: int
        COLOR_0: int
        COLOR_1: int
    }

    module Mesh =
        type Primitives = {
            attributes: Attributes
            indices: int
            material: int
            mode: int
            targets: obj array
            extensions: obj
            extras: obj
        }

    type Mesh = {
        primitives: Mesh.Primitives array
        weights: float32 array
        name: string
        extensions: obj
        extras: obj
    }

    module Material =
        type PbrMetallicRoughness = {
            baseColorFactor: array<float32>  // float4
            baseColorTexture: TextureInfo
            metallicFactor: float32
            roughnessFactor: float32
            metallicRoughnessTexture: TextureInfo
            extensions: obj
            extras: obj
        }

        type NormalTextureInfo = {
            index: int
            texCoord: int
            scale: float32
            extensions: obj
            extras: obj        
        }

        type OcclusionTextureInfo = {
            index: int
            texCoord: int
            strength: float32
            extensions: obj
            extras: obj
        }


    type Material = {
        name: string
        extensions: obj
        extras: obj
        pbrMetallicRoughness: Material.PbrMetallicRoughness
        normalTexture: Material.NormalTextureInfo
        occlusionTexture: Material.OcclusionTextureInfo
        emissiveTexture: TextureInfo
        emissiveFactor: float32 array  // float 3
        alphaMode: string
        alphaCutoff: float32
        doubleSided: bool
    }

    module Camera = 
        type Orthographic = {
            xmag: float32
            ymag: float32
            zfar: float32
            znear: float32
            extensions: obj
            extras: obj
        }

        type Perspective = {
            aspectRation: float32
            yfov: float32
            zfar: float32
            znear: float32
            extensions: obj
            extras: obj        
        }


    type Camera = {
        orthographics: Camera.Orthographic
        perspective: Camera.Perspective    
        [<JsonPropertyName("type")>] type': string
        name: string
        extensions: obj
        extras: obj
    }

    module Animation =
        type Target = {
            /// The index of the node to animate. When undefined, the animated object MAY be defined by an extension.
            node: int
            /// The name of the node’s TRS property to animate, or the "weights" of the Morph Targets it instantiates.
            /// For the "translation" property, the values that are provided by the sampler are the translation along the X,
            /// Y, and Z axes. For the "rotation" property, the values are a quaternion in the order (x, y, z, w),
            /// where w is the scalar. For the "scale" property, the values are the scaling factors along the X, Y, and Z axes.
            path: string
            extensions: obj
            extras: obj
        }

        type Channel = {
            /// The index of a sampler in this animation used to compute the value for the target.
            sampler: int
            /// The descriptor of the animated property.
            target: Target
            extensions: obj
            extras: obj
        }

        type Interpolation = 
            | LINEAR = 0
            | STEP = 1
            | CUBICSPLINE = 2

        type Sampler = {
            /// The index of an accessor containing keyframe timestamps.
            input: int
            /// Interpolation algorithm.
            interpolation: string
            /// The index of an accessor, containing keyframe output values.
            output: int
            extensions: obj
            extras: obj
        }

    type Animation = {
        /// An array of animation channels. An animation channel combines an animation sampler
        /// with a target property being animated. Different channels of the same animation
        /// MUST NOT have the same targets.
        channels: array<Animation.Channel>
        /// An array of animation samplers. An animation sampler combines timestamps with a
        /// sequence of output values and defines an interpolation algorithm.
        samplers: array<Animation.Sampler>
        /// The user defined name of this object
        name: string
        extensions: obj
        extras: obj
    }


    module Accessor =
        type SparceIndices = {
            /// The index of the buffer view with sparse indices. The referenced buffer view MUST NOT
            /// have its target or byteStride properties defined. The buffer view and the optional
            /// byteOffset MUST be aligned to the componentType byte length.
            byfferView: int
            /// The offset relative to the start of the buffer view in bytes.
            byteOffset: int
            /// The indices data type.
            componentType: int
            extenstions: obj
            extras: obj
        }

        type SparseValues = {
            /// The index of the bufferView with sparse values. The referenced buffer view MUST NOT
            /// have its target or byteStride properties defined.
            byfferView: int
            /// The offset relative to the start of the bufferView in bytes.
            byteOffset: int
            extenstions: obj
            extras: obj
        }

        type Sparse = {
            /// Number of deviatingaccessor values stored in the sparse array.
            count: int
            /// An object pointing to a buffer view containing the indices of deviating
            /// accessor values. The number of indices is equal to count. Indices MUST strictly increase.
            indices: SparceIndices
            /// An object pointing to a buffer view containing the deviating accessor values
            values: SparseValues
            extensions: obj
            extras: obj
        }

    type Accessor = {
        /// the index of the bufferview
        bufferView: int
        /// the offset relative to the start of the byteview in bytes
        byteOffset: int
        /// the datatype of the accessor's components
        componentType: int
        normalized: bool
        /// the number of elements references by this accessor
        count: int
        /// specifies if the accessor's elements are scalars, vectors, or matrices
        [<JsonPropertyName("type")>] type': string
        /// maximum value of each component in this accessor
        max: array<float32>
        /// minimum value of each component in this accessor
        min: array<float32>
        /// sparse storage of elements that deviate from their initialization value
        spars: Accessor.Sparse
        /// the user defined name of this object
        name: string
        extenstions: obj
        extras: obj    
    }

    type Asset = {
        copyright: string
        generator: string 
        version: string
        minVersion: string
        extenstions: obj
        extras: obj    
    }

    type Node = {
        camera: int
        children: int array
        skin: int 
        matrix: float32 array // 16
        mesh: int
        rotation: float32 array // 4
        scale: float32 array
        translation: float32 array // 3
        weights: float32 array
        name: string 
        extensions: obj
        extras: obj
    }

    type Scene = {
        nodes: int array
        name: string
        extensions: obj
        extras: obj
    }

    type Skin = {
        inverseBitMatrices: int
        skeleton: int
        joints: int array
        name: string
        extensions: obj
        extras: obj
    }

    type Root = {
        extensionsUsed: array<string> 
        extensionsRequired: array<string> 
        /// An array of accessors.
        accessors: array<Accessor>
        /// An array of keyframe animations. 
        animations: array<Animation> 
        asset: Asset
        /// An array of buffers.
        buffers: array<Buffer> 
        /// An array of bufferViews.
        bufferViews: array<BufferView> 
        /// An array of cameras.
        cameras: array<Camera> 
        /// An array of images.
        images: array<Image> 
        /// An array of materials.
        materials: array<Material> 
        /// An array of meshes.
        meshes: array<Mesh> 
        nodes: array<Node> 
        sampler: array<Sampler> 
        scene: int
        scenes: array<Scene> 
        skins: array<Skin> 
        /// An array of textures.
        textures: array<Texture> 
        extensions: obj
        extras: obj
    }

    let size = function
        | "SCALAR" -> 1
        | "VEC2" -> 2
        | "VEC3" -> 3
        | "VEC4" -> 4
        | "MAT2" -> 4
        | "MAT3" -> 9
        | "MAT4" -> 16
        | _ -> failwith "this accessor.type' is not defined"
    
    let ptr_type = function
        | 5120 -> VertexAttribPointerType.Byte
        | 5121 -> VertexAttribPointerType.UnsignedByte
        | 5122 -> VertexAttribPointerType.Short
        | 5123 -> VertexAttribPointerType.UnsignedShort
        | 5125 -> VertexAttribPointerType.UnsignedInt
        | 5126 -> VertexAttribPointerType.Float
        | _ -> failwith "this accessor.componentType is not defined"

    let draw_type = function
        | 5121 -> DrawElementsType.UnsignedByte
        | 5123 -> DrawElementsType.UnsignedShort
        | 5125 -> DrawElementsType.UnsignedInt
        | _ -> failwith "this accessor.componentType is not defined"

    let mode = function
        | 0 -> PrimitiveType.Points
        | 1 -> PrimitiveType.Lines
        | 2 -> PrimitiveType.LineLoop
        | 3 -> PrimitiveType.LineStrip
        | 4 -> PrimitiveType.Triangles
        | 5 -> PrimitiveType.TriangleStrip
        | 6 -> PrimitiveType.TriangleFan
        | _ -> failwith "this primitives.mode is not defined"

    // let paths = Map [
    //     "translation", 3
    //     "rotation", 4
    //     "scale", 3
    //     "weights", 1
    // ]
     
    let inline private cast<'T when 'T: unmanaged> ptr = NativePtr.ofVoidPtr<'T> ptr

    // overload pointer operators
    let inline private (!) ptr = NativePtr.read<'T> ptr
    let inline private (~~) ptr = NativePtr.toVoidPtr ptr
    let inline private (++) ptr offset = NativePtr.add ptr offset
    let inline private (--) ptr offset = NativePtr.add ptr (-offset)

    let (|IsTxt|IsGltf|IsPly|IsEmpty|) (str:string) =
        if str.Contains(".gltf") then IsGltf
        elif str.Contains(".txt") then IsTxt
        elif str.Contains(".ply") then IsPly
        else IsEmpty


    type Deserializer(path:string) =
        let mutable handle = 0n
        let mutable is_disposed = false
        let gltf_str = path
        let pindx = gltf_str.LastIndexOf('.')
        let gltf_bin = gltf_str[0..pindx] + "bin"
        let gltf = File.ReadAllText(gltf_str)
        let root = JsonSerializer.Deserialize<Root>(gltf)
        let fs = System.IO.File.ReadAllBytes(gltf_bin)

        do
            let b = fixed fs
            handle <- Marshal.AllocHGlobal(fs.Length)
            System.Buffer.MemoryCopy(~~b, handle.ToPointer(), fs.Length, fs.Length)

        interface IDisposable with
            member this.Dispose() =
                if not is_disposed then
                    Marshal.FreeHGlobal(handle)
                is_disposed <- true
                    

        member this.AsSpan<'T>(byteOffset:int, count:int) =
            let ptr = cast<byte> (handle.ToPointer())
            let a = ptr ++ byteOffset
            Span<'T>(~~a, count)

        member this.Dispose() =
            if not is_disposed then
                Marshal.FreeHGlobal(handle)
            is_disposed <- true

        member this.Root with get() = root

        member this.Bin with get() = fs

        member this.ReadAllMeshes() =
            let vertices = ResizeArray<float32>(10000)
            let indices = ResizeArray<uint32>(20000)

            for mesh in root.meshes do
                let mutable base_vertex = 0u
                for primitive in mesh.primitives do
                    let material = root.materials[primitive.material]
                    let material_color = material.pbrMetallicRoughness.baseColorFactor
                    let p_accessor = root.accessors[primitive.attributes.POSITION]
                    let n_accessor = root.accessors[primitive.attributes.NORMAL]
                    let i_accessor = root.accessors[primitive.indices]
                    let p_bv = root.bufferViews[p_accessor.bufferView]
                    let n_bv = root.bufferViews[n_accessor.bufferView]
                    let i_bv = root.bufferViews[i_accessor.bufferView]
                    let p_span = this.AsSpan<Vector3>(p_bv.byteOffset + p_accessor.byteOffset, p_accessor.count)
                    let n_span = this.AsSpan<Vector3>(n_bv.byteOffset + n_accessor.byteOffset, n_accessor.count)
                    let i_span = this.AsSpan<uint16>(i_bv.byteOffset + i_accessor.byteOffset, i_accessor.count)
                    let vertices_count = p_accessor.count

                    for i in 0..vertices_count - 1 do
                        vertices.Add(p_span[i].X)
                        vertices.Add(p_span[i].Y)
                        vertices.Add(p_span[i].Z)
                        vertices.Add(n_span[i].X)
                        vertices.Add(n_span[i].Y)
                        vertices.Add(n_span[i].Z)
                        vertices.Add(material_color[0])
                        vertices.Add(material_color[1])
                        vertices.Add(material_color[2])
                        vertices.Add(material_color[3])
                    for i in i_span do
                        indices.Add(base_vertex + uint32 i)
                    base_vertex <- base_vertex + (uint32 vertices_count)

            (vertices.ToArray(),indices.ToArray())
            
    
    
