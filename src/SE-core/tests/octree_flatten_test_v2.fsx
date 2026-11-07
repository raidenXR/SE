// #r "../bin/Debug/net10.0/SE-core.dll"
#r "../bin/Release/net10.0/SE-core.dll"

open System
open System.Numerics
open SE.Spatial
open SE.Core
open SE.ECS


printfn "size: %d" (sizeof<OctreeFlatten.Node<uint>>)

let rng = Random.Shared
let vs = Array.init 300_000 (fun _ ->
    let x = 100.f * float32(rng.NextDouble())
    let y = 100.f * float32(rng.NextDouble())
    let z = 100.f * float32(rng.NextDouble())
    Vector3(x,y,z)
)
let ns = Array.init 300_000 (fun _ ->
    let x = 5.0f - 10.f * float32(rng.NextDouble())
    let y = 5.0f - 10.f * float32(rng.NextDouble())
    let z = 5.0f - 10.f * float32(rng.NextDouble())
    Vector3(x,y,z)
)
let rs = Array.map2 (+) vs ns
let fs = rs |> Array.take 30_000

let N = 300
let k = 3

#time
let tree0 = new Octree.Root<Entity>(N, k, -10.f * Vector3.One, 110.f * Vector3.One)
for v in vs do
    tree0.Insert v
printfn "\ntree insert\ncount: %d, total_count: %d" (tree0.GetCount()) (tree0.GetTotalCount())
#time
GC.Collect()

#time
let tree_flatten0 = OctreeFlatten.Octree<Entity>(N, k, -10.f * Vector3.One, 110.f * Vector3.One)
for v in vs do
    tree_flatten0.Insert v
printfn "\ntree_flatten insert\ncount: %d, total_count: %d" (tree_flatten0.GetCount()) (tree_flatten0.GetTotalCount())
#time
GC.Collect()

#time
let tree = new Octree.Root<Entity>(N, k, -10.f * Vector3.One, 110.f * Vector3.One)
for v in vs do
    tree.Put(v, ValueNone)
printfn "\ntree put\ncount put: %d, total_count: %d, boundaries: %d, internal: %d" (tree.GetCount()) (tree.GetTotalCount()) (tree.GetBoundaryCount()) (tree.GetInternalCount())
#time
GC.Collect()

#time
let tree_flatten = OctreeFlatten.Octree<Entity>(N, k, -10.f * Vector3.One, 110.f * Vector3.One)
for v in vs do
    tree_flatten.Put(v, 0u)
tree_flatten.AssignFlags()
printfn "\ntree_flatten put\ncount: %d, total_count: %d, boundaries: %d, internal: %d" (tree_flatten.GetCount()) (tree_flatten.GetTotalCount()) (tree_flatten.GetBoundaryCount()) (tree_flatten.GetInternalCount())
#time
GC.Collect()


printfn "\ninit octree"
#time
tree.Iter (fun u ->
    match u with
    | Octree.Leaf (_,v,_,_,_,_) -> v.Value <- ValueSome (entity() |> set true |> set 10 |> set 0.1)
    | _ -> ()
)
#time
GC.Collect()

printfn "\ninit octree_flatten"
#time
tree_flatten.Iter (fun i ->
    let u = &tree_flatten[i]
    u.value <- entity() |> set true |> set 10 |> set 0.1
)
#time
GC.Collect()

// printfn "\nboundaries octree"
// #time
// for i in 0..100 do
//     tree.Iter (fun u ->
//         match u with
//         | Octree.Boundary & Octree.Leaf _ -> ()
//         | Octree.Internal & Octree.Leaf _ -> ()
//         | _ -> ()
//     )
// #time
// printfn "\nboundaries octree, internal: %d, boundaries: %d" (tree.GetInternalCount()) (tree.GetBoundaryCount())
// GC.Collect()

// printfn "\nboundaries octree_flatten"
// #time
// tree_flatten.AssignFlags()
// let u = tree_flatten
// for i in 0..100 do
//     tree_flatten.Iter (fun ni ->
//         match u[ni].bounds with
//         | OctreeFlatten.Bounds.Boundary -> ()
//         | OctreeFlatten.Bounds.Internal -> ()
//         | _ -> ()
//     )
// #time
// printfn "\nboundaries octree_flatten, iternal: %d, boundaries: %d" (tree_flatten.GetInternalCount()) (tree_flatten.GetBoundariesCount())
// GC.Collect()

// exit 0


printfn "\niter octree"
#time
for i in 0..100 do
    tree.Iter (fun u ->
        let B = Components.get<bool>()
        let I = Components.get<int>()
        let F = Components.get<double>()
        match u with
        | Octree.Leaf (_,v,_,_,_,_) & Octree.Boundary ->
            let b = B[v.Value.Value]
            let i = I[v.Value.Value]
            let f = F[v.Value.Value]
            ()
        | _ -> ()
    )
#time    
GC.Collect()

printfn "\niter octree_flatten"
#time
for _ in 0..100 do
    tree_flatten.Iter (fun i ->
        let B = Components.get<bool>()
        let I = Components.get<int>()
        let F = Components.get<double>()
        let u = tree_flatten[i]
        match u.flag with
        | OctreeFlatten.Flag.Leaf -> 
            let b = B[u.value]
            let i = I[u.value]
            let f = F[u.value]
            ()
        | _ -> ()
    )
#time
GC.Collect()

printfn "\niter_2 octree_flatten"
#time
for _ in 0..100 do
    tree_flatten.Iter2 (fun i ->
        let B = Components.get<bool>()
        let I = Components.get<int>()
        let F = Components.get<double>()
        let u = tree_flatten[i]
        match u.bounds with
        | OctreeFlatten.Bounds.Boundary -> 
            let b = B[u.value]
            let i = I[u.value]
            let f = F[u.value]
            ()
        | _ -> ()
    )
#time
GC.Collect()

printfn "\niter Parallel octree"
#time
for i in 0..100 do
    tree.IterParallel 4 (fun u ->
        let B = Components.get<bool>()
        let I = Components.get<int>()
        let F = Components.get<double>()
        match u with
        | Octree.Leaf (_,v,_,_,_,_) & Octree.Boundary ->
            let b = B[v.Value.Value]
            let i = I[v.Value.Value]
            let f = F[v.Value.Value]
            ()
        | _ -> ()
    )
#time    
GC.Collect()

printfn "\niter Parallel octree_flatten"
#time
for _ in 0..100 do
    tree_flatten.IterParallel (fun t i ->
        let B = Components.get<bool>()
        let I = Components.get<int>()
        let F = Components.get<double>()
        let u = tree_flatten[i]
        match u.flag with
        | OctreeFlatten.Flag.Leaf -> 
            let b = B[u.value]
            let i = I[u.value]
            let f = F[u.value]
            ()
        | _ -> ()
    )
#time
GC.Collect()

// printfn "\noctree MapTo"
// #time
// for _ in 0..100 do
//     for v in rs do
//         match tree.MapTo(v) with
//         | Octree.Leaf _ & Octree.Boundary -> ()
//         | Octree.Leaf _ & Octree.Internal -> ()
//         | _ -> ()
// #time
// GC.Collect()

// printfn "\noctree_flatten MapTo"
// #time
// for _ in 0..100 do
//     for v in rs do
//         let idx = tree_flatten.MapTo(v)
//         match tree_flatten[idx].bounds with
//         | OctreeFlatten.Bounds.Boundary -> ()
//         | OctreeFlatten.Bounds.Internal -> ()
//         | _ -> ()
// #time
// GC.Collect()

printfn "\noctree Remove"
#time
for v in fs do
    tree.Remove(v)
#time
GC.Collect()


printfn "\noctree_flatten Remove"
#time
for v in fs do
    tree_flatten.Remove(v)
#time
GC.Collect()


printfn "\noctree methods"
#time
let points = tree.AsPoints()
let values = tree.GetValues()
eprintfn "points: %d, values: %d" points.Length values.Length
#time
GC.Collect()


printfn "\noctree_flatten methods"
#time
let points' = tree_flatten.AsPoints()
let values' = tree_flatten.GetValues()
eprintfn "points: %d, values: %d" points'.Length values'.Length
#time
GC.Collect()


