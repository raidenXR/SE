#r "bin/Release/net10.0/SE-core.dll"
#r "bin/Release/net10.0/SE-renderer.dll"

open SE.Core
open SE.ECS
open SE.Renderer
open SE.Renderer.Imaging

let path_2d = "cool_image_00.png"
let path_3d = "bun_zipper.ply"
let gltf: option<GLTF.Deserializer> = None
let N = 500
let L = 10

let (stencil_2d,N',v_min_2d,v_max_2d) = get_pixels N path_2d
let mesh =
    match gltf with
    | Some gltf -> gltf.ReadMeshF(0)
    | None -> RGeometry.load_ply_unmanaged (path_3d, 0.55f, 0.55f, 0.53f, 1.0f)
// let (v_min_3d,v_max_3d) = GridGeneration3D.bounds_SIMD (mesh.vertices.AsSpan()) L

// let stencil_3d = 
//     // GridGeneration3D.bitstencil vertices indices v_min v_max N
//     System.Collections.BitArray(N*N*N)
//     |> GridGeneration3D.assign_voxels_SIMD (mesh.vertices.AsSpan()) (mesh.indices.AsSpan()) L N 
//     |> GridGeneration3D.fill_bitstencil N

let valueof = Quadtree.valueof
// let kindof  = Quadtree.kindof

let tree_2d =
    // Quadtree.ofBoundaries<double> N 4 points
    stencil_2d
    |> Quadtree.ofStencil<double> N 4 v_min_2d v_max_2d
    |> Quadtree.init 0.00

do
    if N <> N' then failwith "N cannot differ"
    tree_2d.Add <- (+)
    tree_2d.Div <- (/)

let tree_3d =
    Octree.ofSurface<double> N L 4 (mesh.vertices.AsSpan()) (mesh.indices.AsSpan())
    // stencil_3d
    // |> Octree.ofStencil<double> N 4 v_min_3d v_max_3d
    // |> Octree.init 0.00

do
    if N <> N' then failwith "N cannot differ"
    tree_3d.Add <- (+)
    tree_3d.Div <- (/)


#time
printfn "nodes_2d_leaf: %d" (tree_2d.GetCount())
printfn "nodes_2d_total: %d" (tree_2d.GetTotalCount())
printfn "nodes_3d_leaf: %d" (tree_3d.GetCount())
printfn "nodes_3d_total: %d" (tree_3d.GetTotalCount())
#time

