#r "../bin/Debug/net10.0/SE-core.dll"
open SE.ECS
open System
open System.Threading
open System.Threading.Tasks

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

