#load "../src/core.fs"

open System
open System.Numerics
open SE.Core

// components
type [<Struct>] View = View of Matrix4x4
type [<Struct>] Rotation = Rotation of float
type [<Struct>] TimerDt = {dt:float32}
type [<Struct>] StressSolver = {v:float}
type [<Struct>] Position = {mutable x:float; mutable y:float}

type [<Struct>] V1 = {v1:float}
type [<Struct>] V2 = {v2:float}
type [<Struct>] V3 = {v3:float}
type [<Struct>] V4 = {v4:float}

// tags
type Move = struct end
type BoundCondition = struct end


// test Entity module
printfn "\n#################\ntest entity module\n#################"
do
    let e0 =
        entity ()
        |> Entity.add<Move>
        |> Entity.add<Rotation>
        |> Entity.set (View (Matrix4x4.Identity))
        |> Entity.remove<Rotation>

    let components = Entity.components e0

    let e1 = entity_tagged "SomeEntry" |> Entity.set (Rotation 932) |> Entity.set (View Matrix4x4.Identity)

    printfn "Entity.exists: %b" (Entity.exist e0)
    printfn "has<Move>: %b, has<View>: %b, has<Rotation>: %b" (Entity.has<Move> e0) (Entity.has<View> e0) (Entity.has<Rotation> e0)
    printfn "view: %A" (Entity.get<View> e0)
    printfn "%A" (components |> List.map (fun x -> x.Name))
    printfn "%s" (Entity.sprintf e1)
    
// test Components module
printfn "\n#################\ntest components module\n#################"
do
    let N = 100
    let ids = Array.init N (fun _ -> entity())
    let v1 = Array.init N (fun i -> {v1 = float(i)}) 
    let v2 = Array.init N (fun i -> {v2 = 2.0 * float(i)}) 
    let v3 = Array.init N (fun i -> {v3 = 0.5 * float(i)}) 
    let v4 = Array.init N (fun i -> {v4 = float(i*i)}) 
    Components.init ids v1
    Components.init ids v2
    Components.init ids v3
    Components.init ids v4
    let v1_components = Components.get<V1>()
    let v2_components = Components.get<V2>()
    let v3_components = Components.get<V3>()
    let v4_components = Components.get<V4>()
    printfn "v1_components.Count: %d" v1_components.Count
    printfn "v2_components.Count: %d" v2_components.Count
    printfn "v3_components.Count: %d" v3_components.Count
    printfn "v4_components.Count: %d" v3_components.Count
    let q1 = query [typeof<V1>; typeof<V3>]
    let q2 = query [typeof<V1>; typeof<V2>; typeof<V3>; typeof<V4>]
    printfn "q1.len: %d,  q2.len: %d" q1.Count q2.Count

    let v1_span = v1_components.AsSpan(ids)
    let v2_span = v2_components.AsSpan(ids)
    printfn "v1: %g, v2: %g" (v1_span[10].v1) (v2_span[10].v2)

printfn "\n#################\ntest query function\n#################"
do
    let move_components = Components.get<Move>()
    let qt0 = query [typeof<Rotation>; typeof<Move>]
    let qt1 = query [typeof<Move>]
    let move_entries = move_components.AsSpan(qt1)
    printfn "qt0: %d" (qt0.Count)
    printfn "qt1: %d, component.len: %d" (qt1.Count) (Components.get<Move>().Entities.Count)
    printfn "move_components.len: %d" move_entries.Length
    printfn ""

// test Relation module
printfn "\n#################\ntest relations module\n#################"
do
    let e0 = entity () |> Entity.set (Rotation 45) |> Entity.set {dt = 342f}
    let e1 = entity () |> Entity.set {dt = 392.23f} |> Entity.add<Move>
    relate e0 e1 {StressSolver.v = 5434.324}
    printfn "relation exists: %b, %b" (Relation.exists<StressSolver> e0 e1) (Relation.exists<StressSolver> e1 e0)
    printfn "relation expected: %b, %b" (Relation.has<StressSolver> Out e0) (Relation.has<StressSolver> In e1)
    printfn "relation reversed: %b, %b" (Relation.has<StressSolver> Out e1) (Relation.has<StressSolver> In e0)

    let e2 = entity ()
    let e3 = entity ()
    relate e0 e2 {TimerDt.dt = 7.f}
    relate e0 e3 {TimerDt.dt = 6.4f} 
    relate e0 e1 {TimerDt.dt = 2.4f} 
    let storage = Relation.relations_table[typeof<TimerDt>] :?> Relations<TimerDt>
    printfn "TimerDt relations -before Relation.destroy- exist: %b, count: %d" (Relation.exists<TimerDt> e0 e2) ((Relation.relations<TimerDt> Out).Count)
    storage.PrintContent()

    printfn "e0: %s" (Entity.sprintf e0)
    printfn "e2: %s" (Entity.sprintf e2)
    printfn "Relation value: %A" (Relation.value<TimerDt> e0 e2)
    printfn "Relation value: %A" (Relation.value<StressSolver> e0 e1)
    printfn "get ids: -out: %s, -in: %s" (Entity.sprintf (Relation.get<TimerDt> Out e0)) (Entity.sprintf (Relation.get<TimerDt> In e2))

    let relations_out = Relation.relations<StressSolver> Out
    let relations_in  = Relation.relations<StressSolver> In
    printfn "out_relations count: %d" (relations_out.Count)
    printfn "in_relations count: %d" (relations_in.Count)
    for e in relations_out do Entity.printfn e
    for e in relations_in do Entity.printfn e

    Relation.destroy<TimerDt> e0 e2
    printfn "TimerDt relations -after Relation.destroy- exist: %b, count: %d" (Relation.exists<TimerDt> e0 e2) ((Relation.relations<TimerDt> Out).Count)
    storage.PrintContent()
    printfn ""

// example of a real(?) use in some system??
do 
    let stress_relations = Relation.from_storage<StressSolver>()
    let stress_actors = Relation.relations<StressSolver> Out
    stress_relations.PrintContent()
    printfn "stess_actors.len: %d" stress_actors.Count
    for stress_actor in stress_actors do
        let stress_source = Relation.get<StressSolver> In stress_actor
        let stress_value  = Relation.value<StressSolver> stress_source stress_actor
        printfn "stress source:%s, stress actor:%s, stress value:%g" (Entity.sprintf stress_source) (Entity.sprintf stress_actor) stress_value.v

// test equality
do
    let s = struct(0x10u,0x09u) = struct(0x09u,0x10u)
    let b = [typeof<Move>; typeof<TimerDt>] = [typeof<TimerDt>; typeof<Move>]
    printfn "test equality: %b, %b" s b

// test Components.span<T>
printfn "\n#################\ntest components module\n#################"
do
    for i in 0..10 do
        entity () |> Entity.set {dt = 332f} |> Entity.set {v = 932.4} |> ignore
        entity () |> Entity.add<Move> |> ignore
        entity () |> Entity.set {x = 0; y = 0} |> ignore

    let q0 = query [typeof<Move>]
    let q1 = query [typeof<TimerDt>; typeof<StressSolver>]
    let q2 = query [typeof<BoundCondition>]
    let pos_q = query [typeof<Position>]
    let movables = Components.span<Move> q0
    let entries = Components.span<TimerDt> q1
    let positions = Components.span<Position> pos_q
    // let bcs = Components.span<BoundCondition> q2 

    printfn "movables.count %d, q0.count %d" movables.Length q0.Count
    printfn "entries.count %d, q1.count %d" entries.Length q1.Count
    printfn "bound_conditions.count %d" q2.Count

    for i in 0..positions.Length - 1 do
        let pos = &positions[i]
        pos.x <- 2.3 * (float i)
        pos.y <- pos.x * pos.x / 9.214

    for p in positions do printfn "pos: %A" p
    

    
