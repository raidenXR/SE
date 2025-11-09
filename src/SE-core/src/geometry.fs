namespace SE.Geometry

open System
open System.Numerics
open SE.Numerics

type Triangle() =
    [<DefaultValue>] val mutable vert: varray3<Vector3> 
    [<DefaultValue>] val mutable edge: varray3<Vector3> 
    [<DefaultValue>] val mutable vert_norm: varray3<Vector3> 
    [<DefaultValue>] val mutable edge_norm: varray3<Vector3> 
    [<DefaultValue>] val mutable face_norm: Vector3 
    [<DefaultValue>] val mutable tri_plane_edge_norm: varray3<Vector3> 
    [<DefaultValue>] val mutable edge_len: varray3<float32> 


module Triangle =
    // let intersect () = c
    //
    let v_min (a:Vector3) (b:Vector3) =
        if a.Length() < b.Length() then a else b
    
    let pmin (t:Triangle) =
        v_min (t.vert[0]) (v_min t.vert[1] t.vert[2])
