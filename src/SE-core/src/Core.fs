namespace SE.Core

open System
open System.Collections.Generic

type Entity = uint32
        
/// Container for caching Entity ids set and iterating over
/// Acts as a Filter, for running over those filters systems etc
type Types = list<Type>

type EntityStorage = {
    mutable ids: array<Entity>
    mutable count: int
    mutable rebuild: bool
}

type IComponents =
    abstract Count: int
    abstract Capacity: int
    abstract Entities: ArraySegment<Entity>
    abstract Remove: Entity -> unit
    abstract Contains: Entity -> bool
    abstract Sort: unit -> unit
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
type System = Types * (ArraySegment<Entity> -> unit)

/// Observers are similar to systems, in that they are queries that are combined with a callback. 
/// The difference between systems and observers is that systems are executed periodically for all matching entities, 
/// whereas observers are executed whenever a matching event occurs.
type Observer = Types * (ArraySegment<Entity> -> unit)


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

    let sort () =
        for i = 0 to count - 2 do
            for j = i + 1 to count - 1 do                
                let a = ids[i]
                let b = ids[j]
                let c = items[i]
                let d = items[j]
                if a > b then
                    ids[i] <- b
                    ids[j] <- a
                    items[i] <- d
                    items[j] <- c 

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
        member this.Capacity with get() = capacity
        member this.Entities with get() = ArraySegment<Entity>(ids, 0, count)
        member this.Remove (id:Entity) = remove id
        member this.Contains (id:Entity) = 
            let mutable i = 0
            contains id &i
        member this.Sort () = sort ()
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

    member this.Sort () = sort ()
    
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
                    printfn $"Component<{typeof<'T>.Name}> Does not contain this Entity: {s}"
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
                    printfn $"Component<{typeof<'T>.Name}> Does not contain this Entity: {s}"
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

    /// check is storage for 'T is available and return instance.
    /// If storage is not available, create a new storage for type 'T
    let get<'T> () =
        if not (components_table.ContainsKey(typedefof<'T>)) then
            let pool = Components<'T>()
            components_table.Add(typedefof<'T>, pool :> IComponents)
            pool
        else
            components_table[typedefof<'T>] :?> Components<'T>

    /// returns the enties Slice for the type t' of Components 
    let entities (t':Type) =
        if components_table.ContainsKey t' then components_table[t'].Entities else failwith $"Components does not contain {t'.Name}"


module Queries =            
    let queries = new Dictionary<Types,EntityStorage>()
    let entities = System.Buffers.ArrayPool<Entity>.Create()    

    let private max_len (types:Types) =
        let mutable n = 0
        for t in types do 
            let c = Components.components_table[t]
            n <- max n c.Count
        n

    let private sort (ent_storage:EntityStorage) =
        let ids = ent_storage.ids
        let n = ent_storage.count
        for i = 0 to n - 2 do
            for j = i + 1 to n - 1 do
                let a = ids[i]
                let b = ids[j]
                if a > b then
                    ids[j] <- a
                    ids[i] <- b

    let private resize (ent_storage:EntityStorage) =
        Array.Resize(&ent_storage.ids, ent_storage.ids.Length * 2)

    let private copyTo (slice:ArraySegment<Entity>) (buffer:array<Entity>) =
        for i = 0 to slice.Count - 1 do
            buffer[i] <- slice[i]

    let intersect (a:ArraySegment<Entity>) (b:ArraySegment<Entity>) (buffer:array<Entity>) =
        if a[a.Count - 1] < b[0] || b[b.Count - 1] < a[0] then 
            ArraySegment<Entity>()
        else
            let mutable i = 0
            let mutable j = 0
            let mutable n = 0
            while i < a.Count && j < b.Count do
                if a[i] = b[j] then
                    buffer[n] <- a[i] 
                    i <- i + 1
                    j <- j + 1
                    n <- n + 1
                elif a[i] < b[j] then
                    i <- i + 1
                elif b[j] < a[i] then
                    j <- j + 1
            ArraySegment<Entity>(buffer, 0, n)


    let build (types:Types) = 
        if types.Length > 1 then
            // printfn $"Query.build is running for key: {types}"
            match queries.ContainsKey types with
            | true -> 
                let len = max_len types
                let q = queries[types]
                if q.ids.Length < len then resize q
                let buffer = q.ids
                let temp = entities.Rent len
                let mutable b = intersect (Components.entities types[0]) (Components.entities types[1]) buffer
                let mutable n = b.Count
                for i = 2 to types.Length - 1 do
                    let s = intersect (Components.entities types[i]) (b) temp
                    copyTo s buffer 
                    n <- s.Count
                    // printfn "[%d] segment.count: %d" i s.Count
                entities.Return(temp,true)
                queries[types].count <- n
                queries[types].rebuild <- false
            | false -> 
                let len = max_len types
                let buffer = entities.Rent len
                let temp = entities.Rent len
                let mutable b = intersect (Components.entities types[0]) (Components.entities types[1]) buffer
                let mutable n = b.Count
                for i = 2 to types.Length - 1 do
                    let s = intersect (Components.entities types[i]) (b) temp
                    copyTo s buffer         
                    n <- s.Count
                    // printfn "[%d] segment.count: %d" i s.Count
                entities.Return(temp,true)
                queries.Add (types,{ids = buffer; count = n; rebuild = false})            

    /// returns the entities Slice matching the types key
    let rec get (types:Types) =
        match types.Length with
        | 0 -> ArraySegment<Entity>()
        | 1 -> Components.entities types[0]
        | _ when queries.ContainsKey types ->
            let q = queries[types] 
            if q.rebuild then build types
            ArraySegment(q.ids, 0, q.count)         
        | _ ->
            build types
            get types

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
            if List.contains t' types then
                queries[types].rebuild <- true

    /// checks all mapped queries and removes the id from those queries that hava the type t'
    let remove (t':Type) (id:Entity) =
        let mutable r = true
        for kvp in queries do
            let types = kvp.Key
            let ids = kvp.Value
            for t in types do
                r <- r || t = t'
            if r then 
                queries[types].rebuild <- true
            
            

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

    let mutable private running = true

    let create (phase:Phase) (types:Types) (fn:ArraySegment<Entity> -> unit) =
        Queries.build types
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


    let quit () =
        running <- false


    let progress () =
        for (types,fn) in on_load do fn (Queries.get types)
        for (types,fn) in post_load do fn (Queries.get types)  
        while running do
            for (types,fn) in pre_update do fn (Queries.get types)  
            for (types,fn) in on_update do fn (Queries.get types)  
            for (types,fn) in on_validate do fn (Queries.get types)  
            for (types,fn) in post_update do fn (Queries.get types)  
            // for debugging and testing keep it like that to prevent eternal loop
            // keep it a single loop
            quit ()
        for (types,fn) in pre_store do fn (Queries.get types)  
        for (types,fn) in on_store do fn (Queries.get types)  


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
    
    /// The observers are created post-load
    let create (trigger:Trigger) (types:Types) (fn:ArraySegment<Entity> -> unit) =
        // System.create PostLoad types (fun _ -> 
        Queries.build types
        match trigger with
        | OnAdd -> on_add.Add (types,fn)
        | OnSet -> on_set.Add (types,fn)
        | OnUpdate -> on_update.Add (types,fn)
        | OnRemove -> on_remove.Add (types,fn)
        | OnSort -> on_sort.Add (types,fn)
        | OnIterate -> on_iterate.Add (types,fn)        
        // )

    let triggerOnAdd<'T> () =
        for (types,fn) in on_add do fn (Queries.get types)            

    let triggerOnSet<'T> () =
        for (types,fn) in on_set do fn (Queries.get types)            

    let triggerOnRemove<'T> () =
        for (types,fn) in on_remove do fn (Queries.get types)            
        


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

    let printComponents (id:Entity) =
        printf $"0x{id:X6} components: ["
        for kvp in Components.components_table do
            let t = kvp.Key
            let c = kvp.Value
            if c.Contains id then printf $"{t.Name}, "
        printfn "]"
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
