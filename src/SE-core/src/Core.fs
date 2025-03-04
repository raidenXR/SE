namespace SE.Core

open System
open System.Collections.Generic

type Entity = uint32
        
/// Container for caching Entityt ids set and iterating over
/// Acts as a Filter, for running over those filters systems etc
type Types = list<Type>

/// Container for caching Entityt ids set and iterating over
/// Acts as a Filter, for running over those filters systems etc
type Query = list<Type>

type IComponents =
    abstract Count: int
    abstract Entities: ArraySegment<Entity>
    abstract Remove: Entity -> unit
    abstract Contains: Entity -> bool
    abstract PrintEntities: unit -> unit
    
type Phase =
    | OnLoad
    | PostLoad
    | PreUpdate
    | OnUpdate
    | OnValidate
    | PostUpdate
    | PreStore
    | OnStore
    | Free


type Trigger =
    | OnAdd
    | OnSet
    | OnUpdate
    | OnRemove
    | OnSort
    | OnIterate


/// A system is a query combined with a callback. 
/// Systems can be either ran manually or ran as part of an ECS-managed main loop (see Pipeline)
type System = Types * (seq<Entity> -> unit)

/// Observers are similar to systems, in that they are queries that are combined with a callback. 
/// The difference between systems and observers is that systems are executed periodically for all matching entities, 
/// whereas observers are executed whenever a matching event occurs.
type Observer = Types * (seq<Entity> -> unit)


/// Contairer class for storing components and entity ids
type Components<'T>() =
    let mutable count = 0
    let mutable capacity = 10
    let mutable ids = Array.zeroCreate<Entity> capacity
    let mutable items = Array.zeroCreate<'T> capacity

    // cache current
    let mutable idx_current = -1
    let mutable id_current = 0x00u

    // cache prev
    let mutable idx_prev = -1
    let mutable id_prev = 0x00u

    let first () = ids[0]
    
    let last () = ids[count - 1]

    let printEntities () =
        for id in ArraySegment<Entity>(ids, 0, count) do
            printf $"0x{id:X6}, "
        printfn ""

    let linear_search a b id (idx:byref<int>) =
        let mutable i = a
        let mutable break' = false
        let mutable r = false
        while i < b && not break' do
            if ids[i] = id then
                break' <- true
                idx <- i
                r <- true
                idx_prev <- idx_current
                idx_current <- i
                id_prev <- id_current
                id_current <- id
            i <- i + 1
        r                

    let rec binary_search a b id (idx:byref<int>) =
        let m = a + (b - a) / 2
        if id < ids[m] && (b - a > 4) then binary_search a m id &idx
        elif id > ids[m] && (b - a > 4) then binary_search m b id &idx
        else linear_search a b id &idx
                
                
    let resize () = 
        capacity <- capacity * 2
        Array.Resize(&ids, capacity)
        Array.Resize(&ids, capacity)

    /// apply binary search for the lookup
    /// caches latest indices
    let contains (id:Entity) (idx:byref<int>) =
        if count = 0 || id < first() || id > last() then 
            idx <- -1
            false
        elif idx_current <> - 1 && ids[idx_current] = id then
            idx <- idx_current
            true
        elif idx_current <> - 1 && ids[idx_current + 1] = id then
            idx_prev <- idx_current
            idx_current <- idx_current + 1
            id_prev <- id_current
            id_current <- id
            idx <- idx_current
            true
        elif idx_current > 1 && ids[idx_current - 1] = id then
            idx_prev <- idx_current
            idx_current <- idx_current - 1
            id_prev <- id_current
            id_current <- id
            idx <- idx_current
            true
        elif idx_current > 0 && count > 2 then
            let l = idx_current - 1
            let r = if idx_current < count + 1 then idx_current + 1 else count - 1
            if ids[l] = id then
                idx <- l
                true
            elif ids[r] = id then
                idx <- r
                true
            elif count >= 8 then binary_search 0 count id &idx
            else linear_search 0 count id &idx            
        elif idx_prev <> - 1 && ids[idx_prev] = id then
            idx <- idx_prev
            true
        elif idx_prev > 0 && count > 2 then
            let l = idx_prev - 1
            let r = if idx_prev < count + 1 then idx_prev + 1 else count - 1
            if ids[l] = id then
                idx <- l 
                true
            elif ids[r] = id then
                idx <- r
                true
            elif count >= 8 then binary_search 0 count id &idx
            else linear_search 0 count id &idx            
        elif count >= 8 then binary_search 0 count id &idx
        else linear_search 0 count id &idx
                    

    let append (id:Entity) (value:'T) = 
        ids[count] <- id
        items[count] <- value
        count <- count + 1

    // let offset (dx:int) =
    //     if dx < 0 then
    //         for i = count - 1 to dx do 
    //             ids[i + 1] <- ids[i]
    //             items[i + 1] <- items[i]        

    let backward n = 
        for i = n to count - 2 do
            ids[i] <- ids[i + 1]
            items[i] <- items[i + 1]

    let forward n = 
        for i = count - 1 to n do
            ids[i + 1] <- ids[i]
            items[i + 1] <- items[i]

    let remove (id:Entity) =
        let mutable i = -1
        if contains id &i then
            backward i
            count <- count - 1
            idx_current <- -1
            idx_prev <- -1
            id_prev <- 0x00u
            id_current <- 0x00u

    interface IComponents with
        member this.Count with get() = count
        member this.Entities with get() = ArraySegment<Entity>(ids, 0, count)
        member this.Remove (id:Entity) = remove id
        member this.Contains (id:Entity) = 
            let mutable i = 0
            contains id &i
        member this.PrintEntities () = printEntities ()

    member this.Insert (id:Entity) =
        if count + 1 >= capacity then 
            resize ()
        if count = 0 || id > last() then 
            ids[count] <- id
            count <- count + 1 
        elif id < first() then
            forward 1
            ids[0] <- id
            count <- count + 1
        else
            let mutable i = -1
            if contains id &i then
                forward i
                ids[i] <- id
                count <- count + 1

    member this.Add (id:Entity, value:'T) =
        if count + 1 >= capacity then 
            resize ()
        if count = 0 || id > last() then 
            append id value 
        elif id < first() then
            forward 1
            ids[0] <- id
            items[0] <- value
            count <- count + 1
        else
            let mutable i = -1
            if contains id &i then
                forward i
                ids[i] <- id
                items[i] <- value
                count <- count + 1
            

    member this.Remove (id:Entity) =
        remove id

    member this.Contains (id:Entity) =
        let mutable i = -1
        contains id &i
    
    member this.Item
        with get(id:Entity) =
            if id_current = id && idx_current > -1 then
                items[idx_current]
            else
                let mutable i = -1
                match contains id &i with
                | false -> 
                    let s = $"0x{id:X6}"
                    Console.ForegroundColor <- ConsoleColor.Red
                    printfn $"Component<{typeof<'T>}> Does not contain this Entity: {s}"
                    printEntities ()
                    Console.ForegroundColor <- ConsoleColor.White
                    failwith ""
                | true -> 
                    // id_prev <- id_current
                    // idx_prev <- idx_current
                    // id_current <- id
                    // idx_current <- i
                    items[i]
        and set(id:Entity) value = 
            if id_current = id && idx_current > -1 then
                items[idx_current] <- value
            else
                let mutable i = -1
                match contains id &i with
                | false -> 
                    let s = $"0x{id:X6}"
                    Console.ForegroundColor <- ConsoleColor.Red
                    printfn $"Component<{typeof<'T>}> Does not contain this Entity: {s}"
                    printEntities ()
                    Console.ForegroundColor <- ConsoleColor.White
                    failwith ""
                | true -> 
                    // id_prev <- id_current
                    // idx_prev <- idx_current
                    // id_current <- id
                    // idx_current <- i
                    items[i] <- value
                    

    member this.Entities with get() = ArraySegment<Entity>(ids, 0, count)

    member this.Clear() = count <- 0



/// Components manager
module Components =
    let mutable components_table = new Dictionary<Type,IComponents>()

    let get<'T> () =
        if not (components_table.ContainsKey(typedefof<'T>)) then
            let pool = Components<'T>()
            components_table.Add(typedefof<'T>, pool :> IComponents)
            pool
        else
            components_table[typedefof<'T>] :?> Components<'T>
   

module Queries =
    let queries = new Dictionary<Types,Set<Entity>>()

    let create (types:Types) = 
        if not (queries.ContainsKey types) then
            let c0 = Components.components_table[types[0]]
            let mutable s = set (c0.Entities)
            for t in types do
                let c = Components.components_table[t]
                s <- Set.intersect s (set c.Entities)             
            queries.Add (types,s)            

    /// check whether all the components contain this id
    let contains (types:Types) (id:Entity) =
        let mutable r = true
        for t in types do
            let c = Components.components_table[t]
            r <- r && c.Contains id
        r

    /// checks all the mapped queries and adds the id to those queries that contain the id for 
    /// the rest of the types 
    let add (t':Type) (id:Entity) =
        // let mutable r = true
        for kvp in queries do
            let types = kvp.Key
            let ids = kvp.Value
            let c0 = Components.components_table[types[0]]
            let mutable s = set (c0.Entities)
            if List.contains t' types then
                for t in types do
                    let c = Components.components_table[t]
                    s <- Set.intersect s (set c.Entities)             
                queries[types] <- s

    /// checks all mapped queries and removes the id from those queries that hava the type t'
    let remove (t':Type) (id:Entity) =
        let mutable r = true
        for kvp in queries do
            let types = kvp.Key
            let ids = kvp.Value
            for t in types do
                r <- r || t = t'
            if r then queries[types] <- Set.remove id ids
            

/// Systems manager              
module System = 
    let on_load     = ResizeArray<System>()
    let post_load   = ResizeArray<System>()
    let pre_update  = ResizeArray<System>()
    let on_update   = ResizeArray<System>()
    let on_validate = ResizeArray<System>()
    let post_update = ResizeArray<System>()
    let pre_store   = ResizeArray<System>()
    let on_store    = ResizeArray<System>()


    let create (phase:Phase) (types:Types) (fn:seq<Entity> -> unit) =
        Queries.create types
        match phase with
        | OnLoad -> on_load.Add (types,fn)
        | PostLoad -> post_load.Add (types,fn)
        | PreUpdate -> pre_update.Add (types,fn)
        | Phase.OnUpdate -> on_update.Add (types,fn)
        | OnValidate -> on_validate.Add (types,fn)
        | PostUpdate -> post_update.Add (types,fn)
        | PreStore -> pre_update.Add (types,fn)
        | OnStore -> on_store.Add (types,fn)
        | Free -> ()


    let progress () =
        for (types,fn) in on_load do fn (Queries.queries[types])
        for (types,fn) in post_load do fn (Queries.queries[types])  
        for (types,fn) in pre_update do fn (Queries.queries[types])  
        for (types,fn) in on_update do fn (Queries.queries[types])  
        for (types,fn) in on_validate do fn (Queries.queries[types])  
        for (types,fn) in post_update do fn (Queries.queries[types])  
        for (types,fn) in pre_store do fn (Queries.queries[types])  
        for (types,fn) in on_store do fn (Queries.queries[types])  


/// Observers are similar to systems, in that they are queries that are combined with a callback. 
/// The difference between systems and observers is that systems are executed periodically for all matching entities, 
/// whereas observers are executed whenever a matching event occurs.
module Observers = 
    let on_add     = ResizeArray<Observer>()
    let on_set     = ResizeArray<Observer>()
    let on_update  = ResizeArray<Observer>()
    let on_remove  = ResizeArray<Observer>()
    let on_sort    = ResizeArray<Observer>()
    let on_iterate = ResizeArray<Observer>()
    
    let create (trigger:Trigger) (types:Types) (fn:seq<Entity> -> unit) =
        Queries.create types
        match trigger with
        | OnAdd -> on_add.Add (types,fn)
        | OnSet -> on_set.Add (types,fn)
        | OnUpdate -> on_update.Add (types,fn)
        | OnRemove -> on_remove.Add (types,fn)
        | OnSort -> on_sort.Add (types,fn)
        | OnIterate -> on_iterate.Add (types,fn)

    let triggerOnAdd<'T> () =
        for (types,fn) in on_add do fn (Queries.queries[types])            

    let triggerOnSet<'T> () =
        for (types,fn) in on_set do fn (Queries.queries[types])            

    let triggerOnRemove<'T> () =
        for (types,fn) in on_remove do fn (Queries.queries[types])            
        


module Entity =
    let private entities = System.Collections.Generic.Stack<Entity>()
    let mutable private last: Entity = 0x00u

    let create () =
        match entities.Count with
        | 0 -> 
            last <- last + 1u
            last
        | _ ->
            entities.Pop()

    let destroy (id:Entity) =
        entities.Push id
        for components in Components.components_table.Values do
            components.Remove id

    let exist (id:Entity) = 
        if id <= last && not (entities.Contains id) then true else false      

    let add<'T> (id:Entity) =
        let components = Components.get<'T>()
        if not (components.Contains id) then
            components.Insert(id)
            Queries.add typeof<'T> id
            Observers.triggerOnAdd<'T>()
        id
    
    let set (value:'T) (id:Entity) =
        let components = Components.get<'T>()
        if not (components.Contains id) then 
            components.Add(id, value) 
            Queries.add typeof<'T> id
        else components[id] <- value
        Observers.triggerOnSet<'T>()
        id

    let remove<'T> (id:Entity) =
        let components = Components.get<'T>()
        if components.Contains id then
            components.Remove id
            Queries.remove typeof<'T> id
            Observers.triggerOnRemove<'T>()
        id

    let sprintf (id:Entity) =
        $"0x{id:X6}, "

    let printfn (id:Entity) =
        Console.WriteLine("0x{0:X6}", id)
        
    let printf (id:Entity) =
        Console.Write("0x{0:X6}, ", id)
        
        

/// a pipeline schedules and runs systems
module Pipeline =
    let systems = []
    
/// Events manager
/// an event determines when an observer is invoked
module Events =
    let mutable events_queue = new Queue<obj>()
