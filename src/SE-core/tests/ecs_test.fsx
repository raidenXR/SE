#load "../src/Core.fs"

open System
open System.Numerics
open SE.Core

type [<Struct>] Position = Position of Vector3
type [<Struct>] Velocity = Velocity of Vector3
type [<Struct>] Rotation = Rotation of float
type [<Struct>] Temperature = Temperature of float


let assert_ecs (t:Type) (id:Entity) =
    let c = Components.components_table[t]
    if c.Contains id then
       printfn "passed"
    else 
        Console.ForegroundColor <- ConsoleColor.Red
        printfn $"{c} does not contain {Entity.sprintf id}"
        Console.ForegroundColor <- ConsoleColor.White 

let q_a = [typeof<Position>; typeof<Temperature>]
let q_b = [typeof<Position>; typeof<Temperature>]
if q_a = q_b then printfn "list equality checked"


// let ent = Entity.create()
// ent |> Entity.add<Temperature>
// assert_ecs (typeof<Temperature>) (ent)

// ent |> Entity.set (Rotation 90)
// assert_ecs (typeof<Rotation>) (ent)

// ent |> Entity.remove<Temperature>
// assert_ecs (typeof<Temperature>) (ent)


let entity1 = 
    Entity.create()
    |> Entity.set (Position (Vector3(5f,4f,3f)))
    |> Entity.set (Velocity (Vector3(5f,4f,3f)))
    |> Entity.set (Rotation 45.34356)
    |> Entity.set (Temperature 32.)

let entity2 = 
    Entity.create()
    |> Entity.set (Position (Vector3(5f,4f,3f)))
    |> Entity.set (Velocity (Vector3(5f,4f,3f)))
    |> Entity.set (Rotation 45.34356)
    |> Entity.set (Temperature 32.)

let entity3 = 
    Entity.create()
    |> Entity.set (Position (Vector3(5f,4f,3f)))
    |> Entity.set (Velocity (Vector3(5f,4f,3f)))
    |> Entity.set (Rotation 45.34356)
    |> Entity.set (Temperature 32.)

let entity4 = 
    Entity.create()
    |> Entity.set (Position (Vector3(5f,4f,3f)))
    |> Entity.set (Velocity (Vector3(5f,4f,3f)))
    |> Entity.set (Rotation 45.34356)
    |> Entity.set (Temperature 32.)

let entity5 = Entity.create()

System.create OnLoad [typeof<Position>] (fun q ->
    printf "query_position count: %d, " (Seq.length q)
    for id in q do Entity.printf id
    printfn ""
)
System.create OnLoad [typeof<Velocity>] (fun q ->
    printf "query_velocity count: %d, " (Seq.length q)
    for id in q do Entity.printf id
    printfn ""
)
System.create OnLoad [typeof<Temperature>] (fun q ->
    printf "query_temperature count: %d, " (Seq.length q)
    for id in q do Entity.printf id
    printfn ""
)
System.create OnLoad [typeof<Rotation>] (fun q ->
    printf "query_rotation count: %d, " (Seq.length q)
    for id in q do Entity.printf id
    printfn ""
)

System.create Phase.OnUpdate [typeof<Velocity>; typeof<Temperature>] (fun q -> 
    let temperatures = Components.get<Temperature>()
    let velocities = Components.get<Velocity>()
    for id in q do
        let (Temperature t) = temperatures[id]
        temperatures[id] <- Temperature <| Random.Shared.NextDouble() * 10. - 5. + t
    for id in q do
        printfn "%s %A, %A" (Entity.sprintf id) (temperatures[id]) (velocities[id])
)

Observers.create OnSet [typeof<Position>] (fun q ->
    printfn "trigger on set fired! [<Position>]"
    for id in q do Entity.printf id
    printfn ""        
)
Observers.create OnSet [typeof<Temperature>] (fun q ->
    printfn "trigger on set fired! [<Temperature>]"
    for id in q do Entity.printf id
    printfn ""        
)

Observers.create OnRemove [typeof<Temperature>] (fun q ->
    printfn "trigger on remove fired! [<Temperature>]"
    for id in q do Entity.printf id
    printfn ""        
)

Observers.create OnAdd [typeof<Temperature>] (fun q ->
    printfn "trigger on added! [<Temperature>]"
    for id in q do Entity.printf id
    printfn ""        
)

entity3
|> Entity.set (Temperature 15.4)
entity3
|> Entity.set (Temperature 45.4)

entity1
|> Entity.set (Temperature 23.4)

entity2
|> Entity.add<Temperature>

entity2
|> Entity.remove<Velocity>

entity3
|> Entity.remove<Temperature>

entity5
|> Entity.set (Velocity (Vector3(15f,-4f,-3f)))
|> Entity.set (Temperature 32.)
// |> Entity.add<Velocity>
// |> Entity.add<Temperature>

// run all assigned systems
System.progress ()
