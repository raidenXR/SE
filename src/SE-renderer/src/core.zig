const std = @import("std");

const Entity = u32;

const Types = []const type;

const Query = []const type;


const Phase = enum
{
    OnLoad,
    PostLoad,
    PreUpdate,
    OnUpdate,
    OnValidate,
    PostValidate,
    PreStore,
    OnStore,
    Free,
};

const Trigger = enum
{
    OnAdd,
    OnSet,
    OnUpdate,
    OnRemove,
    OnSort,
    OnIterate,
};

const System = struct
{
    types: Types,
    function: fn([]Entity) void,
};

const Observer = struct
{
    types: Types,
    function: fn([]Entity) void,
};


fn Components (comptime T: type) type
{
    return struct
    {
        count: usize = 0,
        capacity: usize = 10,
        ids: []Entity,
        items: []T,    

        idx_current: usize = undefined,
        id_current: Entity = 0x00,

        idx_prev: usize = undefined,
        id_prev: Entity = 0x00,        

        const Self = @This();

        fn first (s:Self) Entity
        {
            return s.ids[0];
        }

        fn last (s:Self) Entity
        {
            return s.ids[s.count - 1];
        }

        fn linear_search (s:Self, a:usize, b:usize, id:Entity, idx:*usize) bool
        {
            var i = a;
            var r = false;
            while (i < b) : (i += 1) {
                if (s.ids[i] == id) {
                    idx.* = i;
                    r = true;
                    s.idx_prev = s.idx_current;
                    s.id_prev = s.id_current;
                    s.idx_current = id;
                    s.idx_current = i;
                    break;
                }
            }
            return r;
        }

        fn binary_search (s:Self, a:usize, b:usize, id:Entity, idx:*usize) bool
        {
            var r = false;
            const m = a + (b - a) / 2;
            if (id < s.ids[m] and (b - a) > 4) 
            {
                r = binary_search (s, a, m, id, &idx);
            }
            else if (id > s.ids[m] and (b - a > 4)) 
            {
                r = binary_search (s, m, b, id, &idx);    
            }
            else 
            {
                r = linear_search (s, a, b, id, &idx);
            }
            return r;
        }

        fn resize (s:*Self) void
        {
            s.capacity *= 2;
            
        }
    };    
}
