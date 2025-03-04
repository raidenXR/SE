namespace SE.Gltf

// ********************************
// Chapter 5, p. 66 (Properties reference)
// ********************************

open System
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
type VEC2 = array<float32>
type VEC3 = array<float32>
type VEC4 = array<float32>
type MAT2 = array<float32>
type MAT3 = array<float32>
type MAT4 = array<float32>


type Buffer = {
    uri: string
    bufferLenght: int
    name: string
    extensions: obj
    extras: obj
}

type BufferView = {
    buffer: int
    byteOffset: int
    byteLength: int
    byteStride: int
    target: int
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
        node: int
        path: string
        extensions: obj
        extras: obj
    }

    type Channel = {
        sampler: int
        target: Target
        extensions: obj
        extras: obj
    }

    type Interpolation = 
        | LINEAR = 0
        | STEP = 1
        | CUBICSPLINE = 2

    type Sampler = {
        input: int
        interpolation: string
        output: int
        extensions: obj
        extras: obj
    }

type Animation = {
    channels: array<Animation.Channel>
    samplers: array<Animation.Sampler>
    name: string
    extensions: obj
    extras: obj
}


module Accessor =
    type SparceIndices = {
        byfferView: int
        byteOffset: int
        componentType: int
        extenstions: obj
        extras: obj
    }

    type SparseValues = {
        byfferView: int
        byteOffset: int
        extenstions: obj
        extras: obj
    }

    type Sparse = {
        count: int
        indices: SparceIndices
        values: SparseValues
        extensions: obj
        extras: obj
    }

type Accessor = {
    bufferView: int
    byteOffset: int
    componentType: int
    normalized: bool
    count: int
    [<JsonPropertyName("type")>] type': string
    max: array<float32>
    min: array<float32>
    spars: Accessor.Sparse
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
    extensionsUsed: string array
    extensionsRequired: string array
    accessors: Accessor array
    animations: Animation array
    asset: Asset
    buffers: Buffer array
    bufferViews: BufferView array
    cameras: Camera array
    images: Image array
    materials: Material array
    meshes: Mesh array
    nodes: Node array
    sampler: Sampler array
    scene: int
    scenes: Scene array
    skins: Skin array
    textures: Texture array
    extensions: obj
    extras: obj
}



