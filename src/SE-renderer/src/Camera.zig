const std = @import("std");
const numerics = @import("dotnet").numerics;
const Vector3 = numerics.Vector3;
const Matrix4x4 = numerics.Matrix4x4;
const vec3 = numerics.vec3;
const mat4x4 = numerics.mat4x4;

pos:   [3]f32,
right: [3]f32,
up:    [3]f32,
look:  [3]f32,

near_z: f32,
far_z:  f32,
aspect: f32,
fov_y:  f32,
near_window_h: f32,
far_window_h:  f32,

view_dirty: bool = true,

view: Matrix4x4,
proj: Matrix4x4,

const Self = @This();

pub const default = Self{
    .pos = .{0,0,0},
    .right = .{1,0,0},
    .up = .{0,1,0},
    .look = .{0,0,1},
    .near_z = 0,
    .far_z = 0,
    .aspect = 0,
    .fov_y = 0,
    .near_window_h = 0,
    .far_window_h = 0,
    .view_dirty = true,
    .view = mat4x4.identity,
    .proj = mat4x4.identity,
};


// Get/Set world camera position.
pub fn getPosition (c:Self) Vector3
{
    return c.pos;
}

// pub fn getPosition3f (c:Self) [3]f32
// {
//     return c.pos;
// }

pub fn setPosition (c:*Self, v:Vector3) void
{
    c.pos = v;
    c.view_dirty = true;
}

pub fn setPosition3f (c:*Self, x:f32, y:f32, z:f32) void
{
    c.pos = .{x, y, z};
    c.view_dirty = true;
}

// Get camera basis vectors.
pub fn getRight (c:Self) Vector3
{
    return c.right;    
}

// pub fn getRight3f (c:Self) [3]f32
// {
//     return c.right;    
// }

pub fn getUp (c:Self) Vector3
{
    return c.up;
}

// pub fn getUp3f (c:Self) [3]f32
// {
//     return c.up;
// }

pub fn getLook (c:Self) Vector3
{
    return c.look;
}

// pub fn getLook3f (c:Self) [3]f32
// {
//     return c.look;
// }

// Get frustum properties.
pub fn getNearZ (c:Self) f32
{
    return c.near_z;
}

pub fn getFarZ (c:Self) f32
{
    return c.far_z;
}

pub fn getAspect (c:Self) f32
{
    return c.aspect;
}

pub fn getFovY (c:Self) f32
{
    return c.fov_y;
}

pub fn getFovX (c:Self) f32
{
    const half_width = 0.5 * getNearWindowWidth(c);

    return 0.2 * std.math.atan(half_width / c.near_z);
}

// Get near and far plane dimensions in view space coordinates.
pub fn getNearWindowWidth (c:Self) f32
{
    return c.aspect * c.near_window_h;
}

pub fn getNearWindowHeight (c:Self) f32
{
    return c.near_window_h;
}

pub fn getFarWindowWidth (c:Self) f32
{
    return c.aspect * c.far_window_h;
}

pub fn getFarWindowHeight (c:Self) f32
{
    return c.far_window_h;
}

// Set frustrum
pub fn setLens (c:*Self, fov_y:f32, aspect:f32, zn:f32, zf:f32) void
{
    // cache properties
    c.fov_y = fov_y;
    c.aspect = aspect;
    c.near_z = zn;
    c.far_z = zf;

    c.near_window_h = 2 * c.near_z * @tan(0.5 * c.fov_y);
    c.far_window_h  = 2 * c.far_z * @tan(0.5 * c.fov_y);

    c.proj = mat4x4.createPerspectiveFieldOfView(c.fov_y, c.aspect, c.near_z, c.far_z);
}

// define camera space via LookAt parameters
pub fn lookAt (c:*Self, pos:Vector3, target:Vector3, world_up:Vector3) void
{
    const L = vec3.normalize(target - pos);
    const R = vec3.normalize(vec3.cross(world_up, L));
    const U = vec3.cross(L, R);

    c.pos = pos;
    c.look = L;
    c.right = R;
    c.up = U;

    c.view_dirty = true;
}

// get view/proj matrices
pub fn getView (c:Self) Vector3
{
    std.debug.assert(!c.view_dirty);

    return c.view;
}

pub fn getProj (c:Self) Vector3
{
    return c.proj;
}

// strafe/walk the camera a distance d.
pub fn strafe (c:*Self, d:f32) void
{
    const s: Vector3 = .{d, d, d};
    const r: Vector3 = c.right;
    const p: Vector3 = c.pos;
    
    c.pos = (s * r) + p;
    c.view_dirty = true;
}

pub fn walk (c:*Self, d:f32) void
{
    const s: Vector3 = .{d, d, d};
    const l: Vector3 = c.look;
    const p: Vector3 = c.pos;

    c.pos = (s * l) + p;
    c.view_dirty = true;
}

// rotate the camera
pub fn pitch (c:*Self, angle:f32) void
{    
	// Rotate up and look vector about the right vector.

	const R = mat4x4.createRotationXFromCenterPoint(angle, c.right);
	
	c.up   = vec3.transformNormal(c.up, R);
	c.look = vec3.transformNormal(c.look, R);

	c.view_dirty = true; 
}

pub fn rotateY (c:*Self, angle:f32) void
{
    // Rotate the basis vectors about the right vector.
    
    const R = mat4x4.createRotationY(angle);

    c.up   = vec3.transformNormal(c.up, R);
    c.look = vec3.transformNormal(c.look, R);

    c.view_dirty = true;    
}

// After modifying camera position/orientation, call to rebuild the view matrix.
pub fn updateViewMatrix (c:*Self) void
{
    if (c.view_dirty)
    {
        var R: Vector3 = c.right;
        var U: Vector3 = c.up;
        var L: Vector3 = c.look;
        const P: Vector3 = c.pos;

		// Keep camera's axes orthogonal to each other and of unit length.
        L = vec3.normalize(L);
        U = vec3.normalize(vec3.cross(L, R));
		
        // U, L already ortho-normal, so no need to normalize cross product.
        R = vec3.cross(U, L);

        // Fill in the view matrix entries.
        const x = -(P * R)[0];
        const y = -(P * U)[0];
        const z = -(P * L)[0];

        c.right = R;
        c.up = U;
        c.look = L;

        c.view = Matrix4x4{
            c.right[0], c.right[1], c.right[2], x,
            c.up[0], c.up[1], c.up[2], y,
            c.look[0], c.look[1], c.look[2], z,
            0, 0, 0, 1,            
        };

        c.view_dirty = false;
    }
}
