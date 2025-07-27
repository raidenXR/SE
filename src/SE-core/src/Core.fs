namespace SE.Core

open System
open System.Collections.Generic

type Entity = uint32

type Entities = ArraySegment<Entity>

type EntityStorage = {
    mutable ids: array<Entity>
    mutable count: int
    mutable rebuild: bool
}

[<Struct>]
type RelationKind =
    | In
    | Out

type Trigger =
    | OnAdd
    | OnSet
    | OnUpdate
    | OnRemove
    | OnSort
    | OnIterate

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

        
/// Container for caching Entity ids set and iterating over
/// Acts as a Filter, for running over those filters systems etc
type Types = list<Type>

/// A system is a query combined with a callback. 
/// Systems can be either ran manually or ran as part of an ECS-managed main loop (see Pipeline)
type System = Types * (Entities -> unit)

/// Observers are similar to systems, in that they are queries that are combined with a callback. 
/// The difference between systems and observers is that systems are executed periodically for all matching entities, 
/// whereas observers are executed whenever a matching event occurs.
type Observer = Types * (Entities -> unit)


type IComponents =
    abstract Count: int
    abstract Capacity: int
    abstract Entities: Entities
    abstract Remove: Entity -> unit
    abstract Clear: unit -> unit
    abstract Contains: Entity -> bool
    abstract Sort: unit -> unit
    abstract PrintEntities: unit -> unit
    

/// Contairer class for storing components and entity ids
type Components<'T>() =
    let mutable count = 0
    let mutable capacity = 16
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
        for id in Entities(ids, 0, count) do
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
        Array.Resize(&items, capacity)

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
                    

    let warning condition (str:string) =
        if condition then
            Console.ForegroundColor <- ConsoleColor.Red
            Console.WriteLine str
            Console.ForegroundColor <- ConsoleColor.White

    let append (id:Entity) (value:'T) = 
        // if capacity <= count + 1 then resize()
        warning (count < 0 || count >= capacity || ids.Length <= count || items.Length <= count) $"append failed: count: {count}, capacity: {capacity}, ids: {ids.Length}, items: {items.Length}"
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

    let clear () = count <- 0        

    interface IComponents with
        member this.Count with get() = count
        member this.Capacity with get() = capacity
        member this.Entities with get() = Entities(ids, 0, count)
        member this.Remove (id:Entity) = remove id
        member this.Clear () = clear ()
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
                    // printEntities ()
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
                    

    member this.Entities with get() = Entities(ids, 0, count)

    member this.Clear() = clear ()



/// Components manager
module Components =
    let mutable components_table = new Dictionary<Type,IComponents>()

    // obsolete
    // let assign<'T> () =
    //     if not (components_table.ContainsKey(typedefof<'T>)) then
    //         let pool = Components<'T>()
    //         components_table.Add(typedefof<'T>, pool :> IComponents)        

    /// check is storage for 'T is available and return instance.
    /// If storage is not available, create a new storage for type 'T
    let get<'T> () =
        if not (components_table.ContainsKey(typeof<'T>)) then
            let pool = Components<'T>()
            components_table.Add(typeof<'T>, pool :> IComponents)
            pool
        else
            components_table[typeof<'T>] :?> Components<'T>

    /// returns the enties Slice for the type t' of Components 
    let entities (t':Type) =
        match components_table.ContainsKey t' with
        | true ->
            components_table[t'].Entities
        | false ->
            Console.ForegroundColor <- ConsoleColor.Red
            printfn "components_table.Key len: %d" (components_table.Count)
            for t in components_table.Keys do
                printfn "%s" (t.Name)
            Console.ForegroundColor <- ConsoleColor.White
            failwith $"Components does not contain {t'.Name}"

    let clearAll () =
        for components in components_table.Values do
            components.Clear()



type IRelations =
    abstract Count: int
    abstract Remove: struct(Entity * Entity) -> unit
    abstract Clear: unit -> unit
    abstract Contains: struct(Entity * Entity) -> bool
    abstract Has: RelationKind * Entity -> bool
    abstract Get: RelationKind * Entity -> Entity

/// Storage for relations, akin to Components
type Relations<'T>() =
    let mutable count = 0
    let mutable capacity = 16
    let mutable pairs = Array.zeroCreate<struct(Entity * Entity)> capacity
    let mutable items = Array.zeroCreate<'T> capacity
    let mutable lhs_ids = Array.zeroCreate<Entity> capacity
    let mutable rhs_ids = Array.zeroCreate<Entity> capacity

    // cache current
    let mutable idx_current = -1
    let mutable lhs_current = 0x00u
    let mutable rhs_current = 0x00u
    let mutable pair_current = struct(0x00u,0x00u)

    // cache previous
    let mutable idx_prev = -1
    let mutable lhs_prev = 0x00u
    let mutable rhs_prev = 0x00u
    let mutable pair_prev = struct(0x00u,0x00u)

    
    let cache (i:int) = 
        idx_prev <- idx_current
        pair_prev <- pair_current
        lhs_prev <- lhs_current
        rhs_prev <- rhs_current
        idx_current <- i
        pair_current <- pairs[i]
        lhs_current <- lhs_ids[i]
        rhs_current <- rhs_ids[i]

    let resize () =  
        capacity <- capacity * 2
        Array.Resize(&pairs, capacity)
        Array.Resize(&items, capacity)
        Array.Resize(&rhs_ids, capacity)
        Array.Resize(&lhs_ids, capacity)

    let append (a:Entity) (b:Entity) (value:'T) =        
        pairs[count] <- struct(a,b)
        items[count] <- value
        lhs_ids[count] <- a
        rhs_ids[count] <- b
        count <- count + 1

    let remove (pair:struct(Entity * Entity)) =
        let mutable i = 0
        let mutable n = 0
        let mutable r = false
        if idx_current >= 1 && idx_current - 1 < count && pairs[idx_current - 1] = pair then
            r <- true
            n <- idx_current - 1
            idx_current <- -1
        elif pair_current = pair && idx_current >= 0 then        
            r <- true
            n <- idx_current
            idx_current <- -1
        elif idx_current >= 0 && idx_current + 1 < count && pairs[idx_current + 1] = pair then
            r <- true
            n <- idx_current + 1
            idx_current <- -1
        while i < count && not r do
            if pairs[i] = pair then
                r <- true
                n <- i
            i <- i + 1
        if r then
            pairs[n] <- pairs[count - 1]
            items[n] <- items[count - 1]
            lhs_ids[n] <- lhs_ids[count - 1]
            rhs_ids[n] <- rhs_ids[count - 1]
            count <- count - 1

    let clear () = count <- 0
        
    let contains (a:Entity) (b:Entity) =
        let mutable i = 0
        let mutable r = false
        let pair = struct(a,b)
        if idx_current >= 1 && idx_current - 1 < count && pairs[idx_current - 1] = pair then
            r <- true
            cache (idx_current - 1)
        elif pair_current = pair && idx_current >= 0 then
            r <- true
        elif idx_current >= 0 && idx_current + 1 < count && pairs[idx_current + 1] = pair then
            r <- true
            cache (idx_current + 1)
        while i < count && not r do
            if pairs[i] = struct(a,b) then
                r <- true
                cache i
            i <- i + 1
        r

    let has (kind:RelationKind) (id:Entity) =
        let mutable i = 0
        let mutable r = false
        match kind with
        | Out ->
            if idx_current >= 1 && idx_current - 1 < count && lhs_ids[idx_current - 1] = id then
                r <- true            
                cache (idx_current - 1)
            elif lhs_current = id then r <- true
            elif idx_current >= 0 && idx_current + 1 < count && lhs_ids[idx_current + 1] = id then
                r <- true
                cache (idx_current + 1)
            while i < count && not r do
                let struct(lhs,rhs) = pairs[i]
                if lhs = id then
                    r <- true
                    cache i
                i <- i + 1
        | In -> 
            if idx_current >= 1 && idx_current - 1 < count && rhs_ids[idx_current - 1] = id then
                r <- true            
                cache (idx_current - 1)
            elif rhs_current = id then r <- true
            elif idx_current >= 0 && idx_current + 1 < count && rhs_ids[idx_current + 1] = id then
                r <- true
                cache (idx_current + 1)
            while i < count && not r do
                let struct(lhs,rhs) = pairs[i]
                if rhs = id then
                    r <- true
                    cache i
                i <- i + 1
        r       

    let get (kind:RelationKind) (id:Entity) =
        let mutable i = 0
        let mutable r = false
        match kind with
        | In ->
            let mutable e = rhs_ids[0]
            if idx_current >= 1 && idx_current - 1 < count && lhs_ids[idx_current - 1] = id then
                r <- true            
                e <- rhs_ids[idx_current - 1]
                cache (idx_current - 1)
            elif lhs_current = id then 
                r <- true
                e <- rhs_current
            elif idx_current >= 0 && idx_current + 1 < count && lhs_ids[idx_current + 1] = id then
                r <- true
                e <- rhs_ids[idx_current + 1]
                cache (idx_current + 1)
            while i < count && not r do
                let struct(lhs,rhs) = pairs[i]
                if lhs = id then
                    r <- true
                    e <- rhs                    
                    cache i
                i <- i + 1
            if r then e else failwith $"0x{0:X6}: no In relation exists"
        | Out ->
            let mutable e = lhs_ids[0]
            if idx_current >= 1 && idx_current - 1 < count && rhs_ids[idx_current - 1] = id then
                r <- true            
                e <- lhs_ids[idx_current - 1]
                cache (idx_current - 1)
            elif rhs_current = id then 
                r <- true
                e <- lhs_current
            elif idx_current >= 0 && idx_current + 1 < count && rhs_ids[idx_current + 1] = id then
                r <- true
                e <- lhs_ids[idx_current + 1]
                cache (idx_current + 1)
            while i < count && not r do
                let struct(lhs,rhs) = pairs[i]
                if rhs = id then
                    r <- true
                    e <- lhs
                    cache i
                i <- i + 1
            if r then e else failwith $"0x{0:X6}: no Out relation exists"
        

    let relations (kind:RelationKind) =
        match kind with
        | Out -> Entities(lhs_ids, 0, count)
        | In -> Entities(rhs_ids, 0, count)


    interface IRelations with
        member this.Count with get() = count
        member this.Remove (pair:struct(Entity * Entity)) = remove pair        
        member this.Clear () = clear ()
        member this.Contains (pair:struct(Entity * Entity)) =
            let struct(lhs,rhs) = pair
            contains lhs rhs        
        member this.Has (kind:RelationKind, e:Entity) = has kind e
        member this.Get (kind:RelationKind, e:Entity) = get kind e
        

    member this.Add (a:Entity, b:Entity, value:'T) =
        if count + 1 >= capacity then resize ()
        append a b value

    member this.Count () = count
    member this.Remove (relation:struct(Entity * Entity)) = remove relation
    member this.Clear () = clear ()
    member this.Has (e:Entity, kind:RelationKind) = has kind e
    member this.Get (e:Entity, kind:RelationKind) = get kind e
    member this.Relations (kind:RelationKind) = relations kind
    member this.Contains (pair:struct(Entity * Entity)) =
        let struct(a,b) = pair
        contains a b
        
    member this.Item with get(pair:struct(Entity * Entity)) =
        let mutable i = 0
        let mutable r = false
        let mutable v = items[0]        
        if idx_current >= 1 && idx_current - 1 < count && pairs[idx_current - 1] = pair then
            v <- items[idx_current - 1]
            r <- true
            cache (idx_current - 1)
        elif pair_current = pair && idx_current >= 0 then
            v <- items[idx_current]
            r <- true
        elif idx_current >= 0 && idx_current + 1 < count && pairs[idx_current + 1] = pair then
            v <- items[idx_current + 1]
            r <- true
            cache (idx_current + 1)
        while i < count && not r do
            if pairs[i] = pair then 
                r <- true
                v <- items[i]
            i <- i + 1
        if r then v else failwith "relations does not contain this pair"
        
    member this.PrintContent () =
        for i in 0..count - 1 do
            let struct(lhs,rhs) = pairs[i]
            let s_lhs = $"0x{lhs:X6}"
            let s_rhs = $"0x{rhs:X6}"
            printfn "(%s, %s): %A" s_lhs s_rhs items[i] 
    

module Relation =
    let mutable relations_table = new Dictionary<Type,IRelations>(16)

    let from_storage<'T> () =
        if not (relations_table.ContainsKey(typeof<'T>)) then
            let pool = Relations<'T>()
            relations_table.Add(typeof<'T>, pool :> IRelations)
            pool
        else
            relations_table[typeof<'T>] :?> Relations<'T>

    /// create a relation between pair (a,b)
    let create<'T> (a:Entity) (b:Entity) (relation:'T) =
        let relations = from_storage<'T>()
        relations.Add(a, b, relation)

    /// remove the relation of (a,b)
    let destroy<'T> (a:Entity) (b:Entity) = 
        let relations = from_storage<'T>()
        relations.Remove (struct(a, b))

    /// checks whether a relation between a and b exists.
    /// Whether it is (a,b) or (b,a)
    let exists<'T> (a:Entity) (b:Entity) =
        let relations = from_storage<'T>()
        relations.Contains(struct(a,b)) || relations.Contains(struct(b,a))

    /// returns the relation component for the pair (a,b) if it exists
    let value<'T> (a:Entity) (b:Entity) =
        let relations = from_storage<'T>()
        relations[struct(a,b)]        

    /// ckecks whether the entity is assigned in any relation of kind
    let has<'T> (kind:RelationKind) (e:Entity) =
        let relations = from_storage<'T>()
        relations.Has(e, kind)

    /// returns the entity that the target entity has a relation with
    let get<'T> (kind:RelationKind) (e:Entity) :Entity =
        let relations = from_storage<'T>()
        relations.Get(e, kind)

    /// gets relations of kind for the specified type 'T
    let relations<'T> (kind:RelationKind) =
        let relations = from_storage<'T>()
        relations.Relations(kind)


module Queries =            
    let queries = new Dictionary<Types,EntityStorage>(16)
    let entities = System.Buffers.ArrayPool<Entity>.Create()    

    let private warning (condition:bool) (str:string) =
        if condition then
            Console.ForegroundColor <- ConsoleColor.Yellow
            Console.WriteLine str
            Console.ForegroundColor <- ConsoleColor.White

    let private max_len (types:Types) =
        let mutable n = 1
        for t in types do 
            match Components.components_table.ContainsKey t with
            | true ->
                let c = Components.components_table[t]
                n <- max n c.Count
            | false ->
                warning true ($"{t} is not assigned in Components<'T>")
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

    let private copyTo (slice:Entities) (buffer:array<Entity>) =
        for i = 0 to slice.Count - 1 do
            buffer[i] <- slice[i]

    /// writes over a buffer the common ids between two Entities array segments
    let intersect (a:Entities) (b:Entities) (buffer:array<Entity>) =        
        if a.Count = 0 || b.Count = 0 then
            Entities()
        elif a[a.Count - 1] < b[0] || b[b.Count - 1] < a[0] then 
            warning (a.Count = 0 || b.Count = 0) (sprintf"intersect failed a.Count: %d, b.Count: %d" a.Count b.Count)
            Entities()
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
            Entities(buffer, 0, n)


    let private build (types:Types) = 
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
        | 0 -> Entities()
        | 1 -> if (Components.components_table.ContainsKey types[0]) then (Components.entities types[0]) else Entities()
        | _ when queries.ContainsKey types ->
            let q = queries[types] 
            if q.rebuild then build types
            Entities(q.ids, 0, q.count)         
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
        if not (queries.ContainsKey ([t'])) then
            let ids = Components.components_table[t'].Entities
            queries.Add([t'], {ids = ids.Array; count = ids.Count; rebuild = false})
            
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

    let create (phase:Phase) (types:Types) (fn:Entities -> unit) =
        // Queries.build types
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


    let progress (n_iterations:option<int>) =
        let mutable i = match n_iterations with | Some n -> n | None -> 0
        for (types,fn) in on_load do fn (Queries.get types)
        for (types,fn) in post_load do fn (Queries.get types)  
        while running do
            for (types,fn) in pre_update do fn (Queries.get types)  
            for (types,fn) in on_update do fn (Queries.get types)  
            for (types,fn) in on_validate do fn (Queries.get types)  
            for (types,fn) in post_update do fn (Queries.get types)  
            // for debugging and testing keep it like that to prevent eternal loop
            // keep it a single loop
            if i <= 0 then quit ()
            i <- i - 1
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
    let create (trigger:Trigger) (types:Types) (fn:Entities -> unit) =
        System.create PostLoad types (fun _ -> 
            // Queries.build types
            match trigger with
            | OnAdd -> on_add.Add (types,fn)
            | OnSet -> on_set.Add (types,fn)
            | Trigger.OnUpdate -> on_update.Add (types,fn)
            | OnRemove -> on_remove.Add (types,fn)
            | OnSort -> on_sort.Add (types,fn)
            | OnIterate -> on_iterate.Add (types,fn)        
        )

    let triggerOnAdd<'T> () =
        for (types,fn) in on_add do fn (Queries.get types)            

    let triggerOnSet<'T> () =
        for (types,fn) in on_set do fn (Queries.get types)            

    let triggerOnRemove<'T> () =
        for (types,fn) in on_remove do fn (Queries.get types)            
        


module Entity =
    let private entities = System.Collections.Generic.Stack<Entity>()
    let mutable private last: Entity = 0x00u

    /// creates a new entity - id
    let create () =
        match entities.Count with
        | 0 -> 
            last <- last + 1u
            last
        | _ ->
            entities.Pop()

    /// resets id to 0
    /// Use very carefully. i.e. When destroying world
    let reset () = last <- 0u

    /// destroys an entity, and removes its components
    let destroy (id:Entity) =
        entities.Push id
        for components in Components.components_table.Values do
            components.Remove id

    /// ckecks whether a id - uint value exists
    let exist (id:Entity) = 
        if id <= last && not (entities.Contains id) then true else false      

    /// adds some component over an entity 
    let add<'T> (id:Entity) =
        let components = Components.get<'T>()
        if not (components.Contains id) then
            components.Insert(id)
            Queries.add typeof<'T> id
            Observers.triggerOnAdd<'T>()
        id

    /// adds some component with a given value over an entity
    let set (value:'T) (id:Entity) =
        let components = Components.get<'T>()
        if not (components.Contains id) then 
            components.Add(id, value) 
            Queries.add typeof<'T> id
        else components[id] <- value
        Observers.triggerOnSet<'T>()
        id

    /// removes a component from an entity
    let remove<'T> (id:Entity) =
        let components = Components.get<'T>()
        if components.Contains id then
            components.Remove id
            Queries.remove typeof<'T> id
            Observers.triggerOnRemove<'T>()
        id

    /// returns the Value of the Component of type 'T for a specific entity id
    let get<'T> (id:Entity) =
        let components = Components.get<'T>()
        components[id]

    /// ckecks if an entity id has some type of Component
    let has<'T> (id:Entity) =
        let components = Components.get<'T>()
        components.Contains(id)

    /// prints the component-types that are assigned over an entity
    /// mostly for debugging purposes
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

        
// uses these as keywords for better scripting ergonomy of the API        
[<AutoOpen>]
module FnDecls =
    let entity = Entity.create
    let system = System.create
    let observer = Observers.create
    let query = Queries.get
    let relate = Relation.create

// /// a pipeline schedules and runs systems
// module Pipeline =
//     let systems = []
    
// /// Events manager
// /// an event determines when an observer is invoked
// module Events =
//     let mutable events_queue = new Queue<obj>()
