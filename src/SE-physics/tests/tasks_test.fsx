#r "../bin/Debug/net10.0/SE-core.dll"
#r "../bin/Debug/net10.0/SE-physics.dll"
open SE.ECS
open SE.Physics
open System
open System.Threading
open System.Threading.Tasks
open System.Collections.Generic
open FSharp.Data.UnitSystems.SI.UnitSymbols

let sleep (n:int) = Thread.Sleep(n)

// let map ([<InlineIfLambda>] f: int -> int) xs  = ()

let inline parallel_for (ids:Entities) ([<InlineIfLambda>] fn: Entity -> unit) =
    Parallel.For(0, ids.Count, (fun i -> fn ids[i])) 

let _array = Array.init 10000 (fun _ -> entity())
let ents = ArraySegment(_array, 10, _array.Length-20)

printfn "arr: %d" _array[ents.Offset]
printfn "ent: %d" ents[0]
printfn "arr: %d" _array[ents.Offset + ents.Count-1]
printfn "ent: %d" ents[ents.Count-1]

printfn "serial"
#time 
for e in ents do
    sleep 5
#time 

printfn "\n\nparallel"
#time 
parallel_for ents (fun _ -> sleep 5)
#time 


printfn "\n\tasks"
#time 
wait_all [|
    task_new (fun _ -> sleep 3000)
    task_new (fun _ -> sleep 3000)
    task_new (fun _ -> sleep 3000)
    task_new (fun _ -> sleep 3000)
|]
#time 

#time
for i in 1..200 do
    for j in 1..10000 do
        let C0 = Dictionary<Entity,double>()
        let k1 = Dictionary<Entity,double>()
        let k2 = Dictionary<Entity,double>()
        let k3 = Dictionary<Entity,double>()
        let k4 = Dictionary<Entity,double>()

        let species = ResizeArray<Entity>()
        for k in 1..20 do
            species.Add(uint k)
        ignore (species.ToArray() |> Array.distinct)
        
        for k in 1..20 do
            C0.Add(uint k, System.Random.Shared.NextDouble())
            k1.Add(uint k, System.Random.Shared.NextDouble())
            k2.Add(uint k, System.Random.Shared.NextDouble())
            k3.Add(uint k, System.Random.Shared.NextDouble())
            k4.Add(uint k, System.Random.Shared.NextDouble())
#time

#time
for i in 1..200 do
    for j in 1..10000 do
        let C0 = Pools.concentrations_rent()
        let k1 = Pools.derivatives_rent()
        let k2 = Pools.derivatives_rent()
        let k3 = Pools.derivatives_rent()
        let k4 = Pools.derivatives_rent()

        let species = Pools.species_rent()
        for k in 1..20 do
            species.Add(uint k)
        ignore (species.ToArray() |> Array.distinct)
        Pools.species_return species
        
        for k in 1..20 do
            C0.Add(uint k, System.Random.Shared.NextDouble() * 1.<mol/m^3>)
            k1.Add(uint k, System.Random.Shared.NextDouble() * 1.<mol/(m^3 s)>)
            k2.Add(uint k, System.Random.Shared.NextDouble() * 1.<mol/(m^3 s)>)
            k3.Add(uint k, System.Random.Shared.NextDouble() * 1.<mol/(m^3 s)>)
            k4.Add(uint k, System.Random.Shared.NextDouble() * 1.<mol/(m^3 s)>)

        Pools.concentrations_return C0
        Pools.derivatives_return k1
        Pools.derivatives_return k2
        Pools.derivatives_return k3
        Pools.derivatives_return k4
#time

#time
for i in 1..200 do
    for j in 1..10000 do
        let C0 = Dictionary<Entity,double>()
        let k1 = Dictionary<Entity,double>()
        let k2 = Dictionary<Entity,double>()
        let k3 = Dictionary<Entity,double>()
        let k4 = Dictionary<Entity,double>()

        let species = ResizeArray<Entity>()
        for k in 1..20 do
            species.Add(uint k)
        ignore (species.ToArray() |> Array.distinct)
        
        for k in 1..20 do
            C0.Add(uint k, System.Random.Shared.NextDouble())
            k1.Add(uint k, System.Random.Shared.NextDouble())
            k2.Add(uint k, System.Random.Shared.NextDouble())
            k3.Add(uint k, System.Random.Shared.NextDouble())
            k4.Add(uint k, System.Random.Shared.NextDouble())
#time

