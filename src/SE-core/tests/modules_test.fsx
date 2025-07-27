#load "../src/Core.fs"

open System
open System.Numerics
open SE.Core

// components
type [<Struct>] View = View of Matrix4x4
type [<Struct>] Rotation = Rotation of float
type [<Struct>] TimerDt = {dt:float32}


// tags
type Move = struct end
type StressSolver = struct end


// test Entity module
printfn "#################\ntest entity module\n#################"
let e0 =
    entity ()
    |> Entity.add<Move>
    |> Entity.add<Rotation>
    |> Entity.set (View (Matrix4x4.Identity))
    |> Entity.remove<Rotation>

printfn "Entity.exists: %b" (Entity.exist e0)
printfn "has<Move>: %b, has<View>: %b, has<Rotation>: %b" (Entity.has<Move> e0) (Entity.has<View> e0) (Entity.has<Rotation> e0)
printfn "view: %A" (Entity.get<View> e0)
Entity.printComponents e0
printfn ""

ignore (entity ()) // ignore 0x00u

// test query fn -- build/get 
let e1 =
    entity ()
    |> Entity.add<Rotation>
    |> Entity.add<Move>


printfn "#################\ntest query function\n#################"
let qt0 = query [typeof<Rotation>; typeof<Move>]
let qt1 = query [typeof<Move>]
printfn "qt0: %d" (qt0.Count)
printfn "qt1: %d, component.len: %d" (qt1.Count) (Components.get<Move>().Entities.Count)
printfn ""

printfn "#################\ntest relations module\n#################"
// test Relation module
relate e0 e1 (StressSolver())
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
printfn "get ids: -out: %s, -in: %s" (Entity.sprintf (Relation.get<TimerDt> In e0)) (Entity.sprintf (Relation.get<TimerDt> Out e2))


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
let stress_relations = Relation.from_storage<StressSolver>()
let stress_actors = Relation.relations<StressSolver> Out
stress_relations.PrintContent()
printfn "stess_actors.len: %d" stress_actors.Count
for e in stress_actors do
    Entity.printf e
    let stress_source = Relation.get<StressSolver> In e
    let stress_value  = Relation.value<StressSolver> e stress_source
    ()    

// test equality
let b = [typeof<Move>; typeof<TimerDt>] = [typeof<TimerDt>; typeof<Move>]
printfn "test equality: %b" b

