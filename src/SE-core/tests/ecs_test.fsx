#load "../src/Core.fs"

open System
open System.Numerics
open SE.Core

// components
type [<Struct>] Position = Position of Vector3
type [<Struct>] Velocity = Velocity of Vector3

type [<Struct>] View = View of Matrix4x4
type [<Struct>] Projection = Projection of Matrix4x4

type [<Struct>] Rotation = Rotation of float
type [<Struct>] Temperature = Temperature of float

type [<Struct>] Phase = {mutable active:bool; mutable norm:float}


// tags
type Move = struct end
type Active = struct end


// initialization systems
let create_views (e:Entities) = 
    ignore e
    for i in 1..4 do entity () |> Entity.set (View (Matrix4x4.CreateScale(Vector3(0.3f + float32 i, 0.4f, 2f / float32 i)))) |> ignore
    
let print_views_A (e:Entities) =
    printfn "print_view_A"
    let views = Components.get<View>()
    for id in e do printfn "%A" views[id]     
        
let print_views_B (e:Entities) =
    printfn "print_view_B"
    let views = Components.get<View>()
    for view in views.Entities do printfn "%A" view   

system OnLoad [] (fun _ ->
    for i in 0..10 do
        entity ()
        |> Entity.add<Active>
        |> Entity.set ({active = false; norm = 0.34})
        |> ignore
)

system PostLoad [typeof<Active>; typeof<Phase>] (fun q ->
    let c0 = Components.get<Phase>()
    let c1 = Components.get<Active>()
    let entries = c0.Slice(q)
    for i in 0..entries.Length - 1 do
        let v = &entries[i]
        v.active <- not v.active
        v.norm <- float (i * i) * v.norm / 2.0

    printfn "query.len: %d" q.Count
    printfn "phases.count: %d" c0.Count
    printfn "active.count: %d" c1.Count
    for e in entries do
        printfn "state: %b, norm: %g" e.active e.norm        
)
    

system OnLoad [] create_views
system PostLoad [typeof<View>] print_views_A
system PostLoad [typeof<View>] print_views_B

// create movables
system OnLoad [] (fun _ ->
    for i in 0..10000 do
        entity () 
        |> Entity.set (Position (Vector3(0f, 5f, 4f)))
        |> Entity.set (Velocity (Vector3(7f, 9f, 6f)))
        |> Entity.set (Temperature 90.)
        |> Entity.add<Move>
        |> ignore
)

// create some rotations
system OnLoad [] (fun _ ->
    for i in 0..10 do
        entity ()
        |> Entity.set (Rotation 45.)
        |> ignore        
)


// create camera
system OnLoad [] (fun _ ->
    let camera = entity ()
    camera
    |> Entity.set (Position (Vector3(0f, -1f, 0f)))
    |> Entity.set (View (Matrix4x4.Identity))
    |> Entity.set (Projection (Matrix4x4.Identity))
    |> Entity.add<Move>
    |> Entity.printComponents |> ignore
)


// run some post-load validation
system PostLoad [typeof<Position>] (fun q -> printfn "query_position count: %d, " (Seq.length q))
system PostLoad [typeof<Velocity>] (fun q -> printfn "query_velocity count: %d, " (Seq.length q))
system PostLoad [typeof<Temperature>] (fun q -> printfn "query_temperature count: %d, " (Seq.length q))
system PostLoad [typeof<Rotation>] (fun q -> printfn "query_rotation count: %d, " (Seq.length q))

// update systems

// move system
system OnUpdate [typeof<Move>; typeof<Velocity>; typeof<Position>] (fun q ->
    let velocities = Components.get<Velocity>()
    let positions  = Components.get<Position>()
    for id in q do
        let (Velocity v) = velocities[id]
        let (Position p) = positions[id]
        positions[id] <- Position (p + v)        
)

// check that query function is working
system PostLoad [] (fun _ ->
    let q0 = query [
        typeof<Move>
        typeof<Position>
    ]

    let q1 = query [
        typeof<Velocity>
        typeof<Position>
    ]

    let q2 = query [
        typeof<View>
        typeof<Temperature>
    ]

    let q3 = query [
        typeof<Position>
    ]

    printfn "q0 entities: %d" (q0.Count)
    printfn "q1 entities: %d" (q1.Count)
    printfn "q2 entities: %d" (q2.Count)
    printfn "q3 entities: %d" (q3.Count)
)

// update rotations
system OnUpdate [typeof<Rotation>] (fun q ->
    let rotations = Components.get<Rotation>()
    for id in q do
        let (Rotation r) = rotations[id]
        let dr = r + 5. - (10. * System.Random.Shared.NextDouble())
        rotations[id] <- Rotation (r + dr)

    for id in q do
        printfn "%A" rotations[id]

    let first_id = q[0]
    first_id
    |> Entity.set (Temperature 0.5)
    |> Entity.add<Move>
    |> Entity.remove<Rotation> |> ignore
)

// systems chain -- from bevy example
// let system_f0 (q:Entities) = ...
// let system_f1 (q:Entities) = ...
// let system_f2 = system_f0 >> system_f1  // must return some value...

system OnUpdate [typeof<Velocity>; typeof<Temperature>] (fun q -> 
    let temperatures = Components.get<Temperature>()
    let velocities = Components.get<Velocity>()
    printfn "running system on update: count: %d" q.Count
    for id in q do
        let (Temperature t) = temperatures[id]
        temperatures[id] <- Temperature <| Random.Shared.NextDouble() * 10. - 5. + t

    for id in q do
        let (Velocity v) = velocities[id]
        let (Temperature t) = temperatures[id]
        ignore (float32(t) * v)        
)

// some obervers to test they trigger
observer OnAdd [typeof<Move>] (fun q -> printfn "trigger on add fired! [<Move>]")
observer OnSet [typeof<Temperature>] (fun q -> printfn "trigger on set fired! [<Temperature>]")
observer OnRemove [typeof<Rotation>] (fun q -> printfn "trigger on remove fired! [<Rotation>]")

// run all assigned systems
#time
Systems.progress_N (None)
#time
