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
let stress_relation = relate e0 e1 (StressSolver())
printfn "relation exists: %b" (Relation.has<StressSolver> e0 e1)
printfn "relation expected: %b, %b" (Relation.hasOut<StressSolver> e0) (Relation.hasIn<StressSolver> e1)
printfn "relation reversed: %b, %b" (Relation.hasOut<StressSolver> e1) (Relation.hasIn<StressSolver> e0)
printfn "contains %b" (Relation.has<StressSolver> e0 e1)


let e2 = entity ()
relate e0 e2 ({dt = 7.f})

printfn "e0: %s" (Entity.sprintf e0)
printfn "e2: %s" (Entity.sprintf e2)
printfn "Relation value: %A" (Relation.value<TimerDt> e0 e2)
printfn "get ids: -out: %s, -int: %s" (Entity.sprintf (Relation.getOut<TimerDt> e0)) (Entity.sprintf (Relation.getIn<TimerDt> e2))


let out_entities = Relation.outRelations<StressSolver>()
let in_entities  = Relation.inRelations<StressSolver>()
printfn "out_relations count: %d" (out_entities.Count)
printfn "in_relations count: %d" (in_entities.Count)
for e in out_entities do Entity.printfn e
for e in in_entities do Entity.printfn e

Relation.destroy<TimerDt> e0 e2
printfn "TimerDt relations -after Relation.destroy- exist: %b, count: %d" (Relation.has<TimerDt> e0 e2) (Relation.outRelations<TimerDt>().Count)
printfn ""

// test equality
let b = [typeof<Move>; typeof<TimerDt>] = [typeof<TimerDt>; typeof<Move>]
printfn "test equality: %b" b

