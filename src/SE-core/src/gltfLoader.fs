namespace SE.Gltf

// ********************************
// Chapter 5, p. 66 (Properties reference)
// ********************************

open System
open System.Numerics
open System.Text.Json
open System.Text.Json.Serialization


module Maps =
    let componentTypes = Map [
        5120, 8
        5121, 8
        5122, 16
        5123, 16
        5125, 32
        5126, 32    
    ]

    let types = Map [
        "SCALAR", 1
        "VEC2", 2
        "VEC3", 3
        "VEC4", 4
        "MAT2", 4
        "MAT3", 9
        "MAT4", 16
    ]

    let paths = Map [
        "translation", 3
        "rotation", 4
        "scale", 3
        "weights", 1
    ]

type SCALAR = float32
// type VEC2 = array<float32>
// type VEC3 = array<float32>
// type VEC4 = array<float32>
// type MAT2 = array<float32>
// type MAT3 = array<float32>
// type MAT4 = array<float32>
type VEC2 = Vector2
type VEC3 = Vector3
type VEC4 = Vector4
// type MAT2 = Matrix3x2
// type MAT3 = Matrix3x2
type MAT4 = Matrix4x4


type Buffer = {
    /// The URI(or IRI) of the buffer.
    uri: string
    /// The length of the buffer in bytes.
    bufferLength: int
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

module Mesh =
    type Primitives = {
        attributes: obj
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

type GltfRoot = {
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



