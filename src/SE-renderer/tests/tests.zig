const std = @import("std");
const print = std.debug.print;
const assert = std.debug.assert;
// const GeometryGenerator = @import("../src/GeometryGenerator.zig");

test "test loop" {
    const stack_count: u32 = 12;

    for (0..stack_count) |i|
    {
        const phi: f32 = std.math.pi / @as(f32, @floatFromInt(i));

        print("{d}, {}\n", .{phi, i});
    }
}

test "upcasting and indexing" {
    const a: u32 = 10;
    const b: usize = 30;

    print("\nupcasting: {d}\n", .{a + b});

    const c: u32 = a + b;
    print ("downcasting: {d}\n", .{c});

    const i: u32 = 2;
    const buf = [_]u32{0, 1, 2, 3, 4, 5, 6, 7, 8, 9};
    print ("indexing: {d}\n", .{buf[i]});


    const v = @Vector(3, f32){4, 5, 6};
    const f: [3]f32 = v;
    print ("array: {any}\n", .{f});
}

test "vector - arraylist" {
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    const allocator = gpa.allocator();
    var a = try std.ArrayListUnmanaged(u32).initCapacity(allocator, 10);
    defer a.deinit(allocator);

    print ("a.len: {}, a.capacity: {}\n", .{a.items.len, a.capacity});
    
    a.appendSliceAssumeCapacity(&.{0,0,0,0,0,0,0,0,0,0});
    
    const b = a;

    a.items.len = 0;

    print("arraylist value-reference copy: b: {d}, a: {d}\n", .{b.items.len, a.items.len});
}

fn insert (a:usize, b:usize) void
{
    // print ("a: {}, b: {}\n", .{a, b});
    const z: u32 = @intCast(a + b);
    _ = z;
}

test "args" {
    const a: u32 = 8;
    const b: u32 = 12;

    insert (a, b);
}

// test "box" {
//     var gpa = std.heap.GeneralPurposeAllocator(.{}){};
//     const allocator = gpa.allocator();

//     var model = try GeometryGenerator.createBox( allocator, 40, 40, 5, 4);
//     defer model.deinit(allocator);
    
// }

// test "quad" {
//     var gpa = std.heap.GeneralPurposeAllocator(.{}){};
//     const allocator = gpa.allocator();
    
//     var model = try GeometryGenerator.createQuad( allocator, 12, 43, 60, 100, 0);
//     defer model.deinit(allocator);

//     for (0..3) |i|
//     {
//         const v = model.vertices.items[i];
//         _ = v; // autofix
//         const ii = model.indices.items[i];
//         _ = ii; // autofix

//         // print ("v: {any}, i: {d}\n", .{v, ii});
//     }
// }

// test "grid" {
//     var gpa = std.heap.GeneralPurposeAllocator(.{}){};
//     const allocator = gpa.allocator();
    
//     var model = try GeometryGenerator.createGrid( allocator, 12, 43, 12, 12);
//     defer model.deinit(allocator);

//     for (0..3) |i|
//     {
//         const v = model.vertices.items[i];
//         _ = v; // autofix
//         const ii = model.indices.items[i];
//         _ = ii; // autofix

//         // print ("v: {any}, i: {d}\n", .{v, ii});
//     }
    
// }

// test "load model" {
//     var gpa = std.heap.GeneralPurposeAllocator(.{}){};
//     const allocator = gpa.allocator();

//     var model = try GeometryGenerator.loadModel(allocator, "car.txt");
//     defer model.deinit(allocator);

//     var i: usize = 0;
//     var j: usize = 0;
//     while (i < 20)
//     {
//         const v = model.vertices.items[i];
//         const c = [3]u32{model.indices.items[j + 0], model.indices.items[j + 1], model.indices.items[j + 2]};
//         i += 1;
//         j += 3;

//         print ("vertex: {any}, indices: {any}\n", .{v, c});
//     }
// }


