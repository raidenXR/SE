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


// initialization state - create some entities
let create_temperature_entities () =
    for i = 0 to 100 do
        let ent = Entity.create ()
        ent
        |> Entity.add<Temperature>
        |> ignore
    printfn "create_temperature_entities function run."

let create_position_entities () =
    for i = 0 to 1000 do
        let ent = Entity.create ()
        ent
        |> Entity.add<Position>
        |> Entity.add<Velocity>
        |> ignore
    printfn "create_position_entitities function run."

let create_velocity_entities () =
    for i = 0 to 10000 do
        let ent = Entity.create ()
        ent
        |> Entity.add<Velocity>
        |> ignore
    printfn "create_velocity_entitities function run."
    
System.create OnLoad [] (fun _ -> create_temperature_entities ())
System.create OnLoad [] (fun _ -> create_position_entities ())
System.create OnLoad [] (fun _ -> create_velocity_entities ())


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
    // for id in q do Entity.printf id
    printfn ""
)
System.create OnLoad [typeof<Velocity>] (fun q ->
    printf "query_velocity count: %d, " (Seq.length q)
    // for id in q do Entity.printf id
    printfn ""
)
System.create OnLoad [typeof<Temperature>] (fun q ->
    printf "query_temperature count: %d, " (Seq.length q)
    // for id in q do Entity.printf id
    printfn ""
)
System.create OnLoad [typeof<Rotation>] (fun q ->
    printf "query_rotation count: %d, " (Seq.length q)
    // for id in q do Entity.printf id
    printfn ""
)

System.create Phase.OnUpdate [typeof<Velocity>; typeof<Temperature>] (fun q -> 
    let temperatures = Components.get<Temperature>()
    let velocities = Components.get<Velocity>()
    printfn "running system on update: count: %d" q.Count
    for id in q do
        let (Temperature t) = temperatures[id]
        temperatures[id] <- Temperature <| Random.Shared.NextDouble() * 10. - 5. + t
    // Queries.build [typeof<Velocity>; typeof<Temperature>]
    // test intersect
    // let ents0 = temperatures.Entities
    // let ents1 = velocities.Entities
    // let buffer = Array.zeroCreate<Entity> 1000
    // let seg = Queries.intersect ents0 ents1 buffer
    // for id in seg do
    //     printfn "%s %A, %A" (Entity.sprintf id) (temperatures[id]) (velocities[id])
        
    for id in q do
        printfn "%s %A, %A" (Entity.sprintf id) (temperatures[id]) (velocities[id])

    let ent0 = temperatures.Entities[1]
    ent0
    |> Entity.set (Temperature 43)
    |> ignore
)

Observers.create OnSet [typeof<Position>] (fun q ->
    printfn "trigger on set fired! [<Position>]"
    // for id in q do Entity.printf id
    // printfn ""        
)
Observers.create OnSet [typeof<Temperature>] (fun q ->
    printfn "trigger on set fired! [<Temperature>]"
    // for id in q do Entity.printf id
    // printfn ""        
)

Observers.create OnRemove [typeof<Temperature>] (fun q ->
    printfn "trigger on remove fired! [<Temperature>]"
    // for id in q do Entity.printf id
    // printfn ""        
)

// Observers.create OnAdd [typeof<Temperature>] (fun q ->
//  printfn "trigger on added! [<Temperature>]"
//  // for id in q do Entity.printf id
 // printfn ""        
// )

entity3
|> Entity.set (Temperature 15.4)
entity3
|> Entity.set (Temperature 45.4)

entity1
|> Entity.set (Temperature 23.4)

entity2
|> Entity.add<Temperature>
|> Entity.printComponents 
|> Entity.remove<Velocity>
|> Entity.printComponents 

entity3
|> Entity.printComponents
|> Entity.remove<Temperature>
|> Entity.printComponents 

entity5
|> Entity.set (Velocity (Vector3(15f,-4f,-3f)))
|> Entity.set (Temperature 32.)
|> Entity.add<Velocity>
|> Entity.add<Temperature>

// run all assigned systems
#time
System.progress ()
// #time
