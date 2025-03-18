const std = @import("std");
const numerics = @import("numerics.zig");

const print = std.debug.print;
const assert = std.debug.assert;

const vec3   = numerics.vec3;
const vec4   = numerics.vec4;
const mat4x4 = numerics.mat4x4;
const mat3x2 = numerics.mat3x2;
const Vector2 = numerics.Vector2;
const Vector3 = numerics.Vector3;

test "vec3 tests" {

    const a = vec3.One;
    const r = mat4x4.createRotationZFromCenterPoint (45.0, a);

    print("{d}\n", .{r});
    print("{d}\n", .{vec3.dot(a, a)});
    print("{d}\n", .{vec3.normalize(a)});
    print("{d}\n", .{vec3.sqrt(a)});
    print("{}\n", .{vec3.equal(a, vec3.One)});
}

test "Matrix4x4 tests" {
    const t = mat3x2.createTranslation(Vector2{0.3, 0.4});
    const s = mat3x2.createScale(0.6, 0.6);
    const m = mat3x2.multiply(t, s);

    print("{any}\n", .{m});


    const t0 = mat4x4.createTranslation(Vector3{0.5, -0.5, -0.5});
    const s0 = mat4x4.createScale(0.5, 0.5, 0.5);
    const t1 = mat4x4.createTranslation(Vector3{0.5, 0.5, 0.5});
    const m0 = mat4x4.multiply(mat4x4.multiply(t0, s0), t1);

    print("{any}\n", .{m0});

    // assert (@reduce(.And, vec3.One > vec3.UnitY));
}

fn tupleReturn () struct{i32, f32}
{
    return .{34, 66.994};
}

test "tuple test" {
    const a, const b, const c = .{12, 45.90, "chars"};
    print ("{d}, {d}, {s}\n", .{a, b, c});
    
    const ia, const fa = tupleReturn();
    print ("{d}, {d}\n", .{ia, fa});
}
