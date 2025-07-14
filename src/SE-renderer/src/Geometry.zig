const std = @import("std");
const dotnet = @import("dotnet");
const numerics = dotnet.numerics;
const string = dotnet.String;
const vec3 = numerics.vec3;
const vec2 = numerics.vec2;
const c = @import("c.zig");
const rdr = @import("common.zig");

const Vector2 = @Vector(2, f32);
const Vector3 = @Vector(3, f32);
const Vector4 = @Vector(4, f32);
const Vertex = rdr.Vertex;
const MeshData = rdr.MeshData;
const SubmeshGeometry = rdr.SubmeshGeometry;

const print = std.debug.print;

const MeshList = struct
{
    vertices: std.ArrayListUnmanaged(Vertex) = .{},
    indices:  std.ArrayListUnmanaged(u32) = .{},
};

fn toString (v:Vertex, buffer:[]u8) []const u8
{
    const p = v.position;
    const n = v.normal;
    const u = v.tangent;
    const t = v.texcoord;
    const C = v.color;
    var offset: usize = 0;
    var str: []const u8 = undefined;
    
    str = std.fmt.bufPrint(buffer[offset..], "pos: [{d}, {d}, {d}], ", .{p[0], p[1], p[2]}) catch @panic("std.fmt.printBuff() failed");
    offset += str.len;
    str = std.fmt.bufPrint(buffer[offset..], "norm: [{d}, {d}, {d}], ", .{n[0], n[1], n[2]}) catch @panic("std.fmt.printBuff() failed");
    offset += str.len;
    str = std.fmt.bufPrint(buffer[offset..], "tang: [{d}, {d}, {d}], ", .{u[0], u[1], u[2]}) catch @panic("std.fmt.printBuff() failed");
    offset += str.len;
    str = std.fmt.bufPrint(buffer[offset..], "texc: [{d}, {d}], ", .{t[0], t[1]}) catch @panic("std.fmt.printBuff() failed");
    offset += str.len;
    str = std.fmt.bufPrint(buffer[offset..], "color: [{d}, {d}, {d}, {d}]", .{C[0], C[1], C[2], C[3]}) catch @panic("std.fmt.printBuff() failed");
    offset += str.len;

    return buffer[0..offset];
}

fn addNormals (vertices:[]Vertex, indices:[]u32) void
{
    const num_triangles = indices.len / 3;

    if (indices.len % 3 != 0) print ("indices len: {}, vertices len: {}\n", .{indices.len, vertices.len});
    
    for (0..num_triangles) |i|
    {
        const _i0 = indices[i * 3 + 0];
        const _i1 = indices[i * 3 + 1];
        const _i2 = indices[i * 3 + 2];

        const v0 = vertices[_i0];
        const v1 = vertices[_i1];
        const v2 = vertices[_i2];

        const e0 = v1.position - v0.position;
        const e1 = v2.position - v0.position;

        const face_normal = vec3.cross(e0, e1);

        vertices[_i0].normal += face_normal;
        vertices[_i1].normal += face_normal;
        vertices[_i2].normal += face_normal;
    }

    for (0..vertices.len) |i|
    {
        vertices[i].normal = vec3.normalize(vertices[i].normal);
    }
}

fn subdivide (allocator: std.mem.Allocator, mesh_data: *MeshList) !void
{
    var input_copy = MeshList{
        .vertices = try mesh_data.vertices.clone(allocator),
        .indices = try mesh_data.indices.clone(allocator),
    };
    defer input_copy.vertices.deinit(allocator);
    defer input_copy.indices.deinit(allocator);

    try mesh_data.vertices.resize(allocator, 0);
    try mesh_data.indices.resize(allocator, 0);

    const num_tris = input_copy.indices.items.len / 3;
    // std.debug.print("subdive: vertices_len: {}\n", .{input_copy.vertices.items.len});
    for (0..num_tris - 3) |I|
    {
        const i: u32 = @intCast(I);
        const v0 = input_copy.vertices.items[input_copy.indices.items[i * 3 + 0]];
        const v1 = input_copy.vertices.items[input_copy.indices.items[i * 3 + 1]];
        const v2 = input_copy.vertices.items[input_copy.indices.items[i * 3 + 2]];

        //
        // Generate the midpoints.
        //

        const m0 = midpoint(v0, v1);
        const m1 = midpoint(v1, v2);
        const m2 = midpoint(v0, v2);

        //
        // Add new geometry.
        //

        try mesh_data.vertices.append(allocator, v0);
        try mesh_data.vertices.append(allocator, v1);
        try mesh_data.vertices.append(allocator, v2);
        try mesh_data.vertices.append(allocator, m0);
        try mesh_data.vertices.append(allocator, m1);
        try mesh_data.vertices.append(allocator, m2);

        try mesh_data.indices.append(allocator, i * 6 + 0);
        try mesh_data.indices.append(allocator, i * 6 + 3);
        try mesh_data.indices.append(allocator, i * 6 + 5);

        try mesh_data.indices.append(allocator, i * 6 + 3);
        try mesh_data.indices.append(allocator, i * 6 + 4);
        try mesh_data.indices.append(allocator, i * 6 + 5);

        try mesh_data.indices.append(allocator, i * 6 + 6);
        try mesh_data.indices.append(allocator, i * 6 + 4);
        try mesh_data.indices.append(allocator, i * 6 + 2);

        try mesh_data.indices.append(allocator, i * 6 + 3);
        try mesh_data.indices.append(allocator, i * 6 + 1);
        try mesh_data.indices.append(allocator, i * 6 + 4);
    }
}

fn buildCylinderTopCap (allocator: std.mem.Allocator, bottom_radius: f32, top_radius: f32, h: f32, slice_count: u32, stack_count: u32, mesh_data: *MeshList) !void
{
    _ = bottom_radius; // autofix
    _ = stack_count; // autofix

    const base_index: u32 = @intCast(mesh_data.vertices.items.len);

    const y = 0.5 * h;
    const dtheta = 2 * std.math.pi / @as(f32, @floatFromInt(slice_count));

    // Duplicate cap ring vertices because the texture coordinates and normals differ.
    for (0..slice_count) |I|
    {
        const i: f32 = @floatFromInt(I);
        const x = top_radius * @cos(i * dtheta);
        const z = top_radius * @sin(i * dtheta);

        // scale down by the height to try and make top cap texture coord area
        // proportional to base.
        const u = x / h + 0.5;
        const v = z / h + 0.5;

        try mesh_data.vertices.append(allocator, Vertex{ .position = .{x, y, z}, .normal = .{0, 1, 0}, .texcoord = .{u, v}});
    }

    // Cap center vertex.
    try mesh_data.vertices.append(allocator, Vertex{ .position = .{0, y, 0}, .normal = .{0, 1, 0}, .texcoord = .{0.5, 0.5}});

    // Index of center vertex.
    const center_index: u32 = @intCast(mesh_data.vertices.items.len - 1);

    for (0..slice_count) |I|
    {
        const i: u32 = @intCast(I);
        try mesh_data.indices.append(allocator, center_index);
        try mesh_data.indices.append(allocator, base_index + i + 1);
        try mesh_data.indices.append(allocator, base_index + i);
    }
}

fn buildCylinderBottomCap (allocator: std.mem.Allocator, bottom_radius: f32, top_radius: f32, h: f32, slice_count: u32, stack_count: u32, mesh_data: *MeshList) !void
{
    _ = top_radius; // autofix
    _ = stack_count; // autofix
    //
    // Build bottom cap
    //

    const base_index: u32 = @intCast(mesh_data.vertices.items.len);
    const y = -0.5 * h;

    // vertices of ring
    const dtheta = 2 * std.math.pi / @as(f32, @floatFromInt(slice_count));
    for (0..slice_count) |I|
    {
        const i: f32 = @floatFromInt(I);
        const x = bottom_radius * @cos(i * dtheta);
        const z = bottom_radius * @sin(i * dtheta);

        // Scale down by the height to try and make top cap texture coord area.
        // proportional to base.
        const u = x / h + 0.5;
        const v = z / h + 0.5;

        try mesh_data.vertices.append(allocator, Vertex{ .position = .{x, y, z}, .normal = .{0, -1, 0}, .texcoord = .{u, v}});
    }

    // cap center vertex.
    try mesh_data.vertices.append(allocator, Vertex{ .position = .{0, y, 0}, .normal = .{0, -1, 0}, .texcoord = .{0.5, 0.5}});

    // cache the index of center vertex.
    const center_index: u32 = @intCast(mesh_data.vertices.items.len - 1);

    for (0..slice_count) |I|
    {
        const i: u32 = @intCast(I);
        try mesh_data.indices.append(allocator, center_index);
        try mesh_data.indices.append(allocator, base_index + i);
        try mesh_data.indices.append(allocator, base_index + i + 1);
    }
}

pub fn createBox (allocator: std.mem.Allocator, w: f32, h: f32, d: f32, numSubdivisions: u32) !MeshData
{
    _ = numSubdivisions; // autofix
    var mesh_data = MeshList{};

    const w2 = 0.5 * w;
    const h2 = 0.5 * h;
    const d2 = 0.5 * d;

    const positions = [24]Vector3{
        // Fill in the front face vertex data.
        .{-w2, -h2, -d2}, .{-w2, h2, -d2}, .{w2, h2, -d2}, .{w2, -h2, -d2},

        // Fill in the back face vertex data.
        .{-w2, -h2, d2}, .{w2, -h2, d2}, .{w2, h2, d2}, .{-w2, h2, d2},

        // Fill in the top face vertex data.
        .{-w2, h2, -d2}, .{-w2, h2, d2}, .{w2, h2, d2}, .{w2, h2, -d2},

        // Fill in the bottom face vertex data.
        .{-w2, -h2, -d2}, .{w2, -h2, -d2}, .{w2, -h2, d2}, .{-w2, -h2, d2},

        // Fill in the left face vertex data.
        .{-w2, -h2, d2}, .{-w2, h2, d2}, .{-w2, h2, -d2}, .{-w2, -h2, -d2},

        // Fill in the right face vertex data.
        .{w2, -h2, -d2}, .{w2, h2, -d2}, .{w2, h2, d2}, .{w2, -h2, d2},          
    };

    const normals = [24]Vector3{
        .{0, 0, -1}, .{0, 0, -1}, .{0, 0, -1}, .{0, 0, -1},
        .{0, 0, 1},  .{0, 0, 1},  .{0, 0, 1},  .{0, 0, 1},
        .{0, 1, 0},  .{0, 1, 0},  .{0, 1, 0},  .{0, 1, 0},
        .{0, -1, 0}, .{0, -1, 0}, .{0, -1, 0}, .{0, -1, 0},
        .{-1, 0, 0}, .{-1, 0, 0}, .{-1, 0, 0}, .{-1, 0, 0},        
        .{1, 0, 0},  .{1, 0, 0},  .{1, 0, 0},  .{1, 0, 0},
    };

    const texcoords = [24]Vector2{
        .{0, 1}, .{0, 0}, .{1, 0}, .{1, 1},        
        .{1, 1}, .{0, 1}, .{0, 0}, .{1, 0},        
        .{0, 1}, .{0, 0}, .{1, 0}, .{1, 1},        
        .{1, 1}, .{0, 1}, .{0, 0}, .{1, 0},        
        .{0, 1}, .{0, 0}, .{1, 0}, .{1, 1},        
        .{0, 1}, .{0, 0}, .{1, 0}, .{1, 1},        
    };

    var vertices: [24]Vertex = undefined;
    for (&vertices, 0..) |*v, i|
    {
        v.position = positions[i];
        v.normal   = normals[i];
        v.tangent  = .{0, 0, 0};
        v.texcoord = texcoords[i];
        v.color    = .{0, 0, 0, 0};
    }
     
    try mesh_data.vertices.appendSlice(allocator, &vertices);

	const i = [36]u32{
		 0,  1,  2,  0,  2,  3,
		 6,  5,  4,  7,  6,  4,
		 8,  9, 10,  8, 10, 11,
		14, 13, 12, 15, 14, 12,
		16, 17, 18, 16, 18, 19,
		22, 21, 20, 23, 22, 20
	};
    try mesh_data.indices.appendSlice(allocator, &i);

    // put a cap on the number of subdivisions.
    // const num_subdivisions = @min(numSubdivisions, 6);
    // for (0..num_subdivisions) |_|
    // {
    //     try subdivide(allocator, &mesh_data);
    // }
    
    return MeshData{
        .vertices = try mesh_data.vertices.toOwnedSlice(allocator),
        .indices = try mesh_data.indices.toOwnedSlice(allocator),
    };
}

pub fn createSphere (allocator: std.mem.Allocator, r: f32, slice_count: u32, stack_count: u32) !MeshData
{
    var mesh_data = MeshList{};
    //
    // Compute the vertices stating at the top pole and moving down the stacks.
    //

    // Poles: note that there will be texture coordinate distortion as there is
    // not a unique point on the texture map to assign to the pole when mapping
    // a rectangular texture onto a sphere.
    const top_vertex = Vertex{.position = .{0, r, 0}, .normal = .{0, 1, 0}};
    const bot_vertex = Vertex{.position = .{0, -r, 0}, .normal = .{0, -1, 0}};

    try mesh_data.vertices.append(allocator, top_vertex);

    const phi_step = std.math.pi / @as(f32, @floatFromInt(stack_count));
    const theta_step = 2.0 * std.math.pi / @as(f32, @floatFromInt(slice_count));

    // Compute vertices for each stack ring (do not count the poles as rings).
    for (1..stack_count) |i|
    {
        const phi: f32 = @as(f32, @floatFromInt(i)) * phi_step;

        // vertices of ring
        for (0..slice_count + 1) |j|
        {
            const theta: f32 = @as(f32, @floatFromInt(j)) * theta_step;
            var v: Vertex = undefined;
            // spherical to cartesian
            v.position = .{
                r * @sin(phi) * @cos(theta),
                r * @cos(phi),
                r * @sin(phi) * @cos(theta),
            };
            // partial derivative of P with respect to theta
            v.tangent = .{
                -r * @sin(phi) * @sin(theta),
                0.0,
                r * @sin(phi) * @cos(theta),
            };
            v.normal = vec3.normalize(v.position);
            v.texcoord = .{ theta / std.math.pi, phi / std.math.pi };
            v.color = @splat(0);

            try mesh_data.vertices.append( allocator, v );
        }
    }

    try mesh_data.vertices.append( allocator, bot_vertex );

    //
    // Compute indices for top stack.  The top stack was written first to the vertex buffer
    // and connects the top pole to the first ring.
    //

    for (1..slice_count + 1) |I|
    {
        const i: u32 = @intCast(I);
        try mesh_data.indices.append( allocator, 0 );
        try mesh_data.indices.append( allocator, i + 1 );
        try mesh_data.indices.append( allocator, i );
    }

    //
    // Compute indices for inner stacks (not connected to poles).
    //

    // Offset the indices to the index of the first vertex in the first ring.
    // This is just skipping the top pole vertex.
    var base_index: u32 = 1;
    const ring_vertex_count = slice_count + 1;

    for (0..stack_count - 2) |I|
    {
        const i: u32 = @intCast(I);
        for (0..slice_count) |J|
        {
            const j: u32 = @intCast(J);
            try mesh_data.indices.append(allocator, base_index + i * ring_vertex_count + j);
            try mesh_data.indices.append(allocator, base_index + i * ring_vertex_count + j + 1);
            try mesh_data.indices.append(allocator, base_index + (i + 1) * ring_vertex_count + j);

            try mesh_data.indices.append(allocator, base_index + (i + 1) * ring_vertex_count + j);
            try mesh_data.indices.append(allocator, base_index + i * ring_vertex_count + j + 1);
            try mesh_data.indices.append(allocator, base_index + (i + 1) * ring_vertex_count + j + 1);
        }
    }

    //
    // Compute indices for bottom stack.  The bottom stack was written last to the vertex buffer
    // and connects the bottom pole to the bottom ring.
    //

    // South pole vertex was added last.
    const south_pole_index: u32 = @intCast(mesh_data.vertices.items.len - 1);

    // offset the indices to the index of the first vertex in the last ring.
    base_index = south_pole_index - ring_vertex_count;

    for (0..slice_count) |I|
    {
        const i: u32 = @intCast(I);
        try mesh_data.indices.append(allocator, south_pole_index);
        try mesh_data.indices.append(allocator, base_index + i);
        try mesh_data.indices.append(allocator, base_index + i + 1);
    }

    return MeshData{
        .vertices = try mesh_data.vertices.toOwnedSlice(allocator),
        .indices = try mesh_data.indices.toOwnedSlice(allocator),
    };
}



fn midpoint (v0: Vertex, v1: Vertex) Vertex
{
    const p0: Vector3 = v0.position;
    const p1: Vector3 = v1.position;

    const n0: Vector3 = v0.normal;
    const n1: Vector3 = v1.normal;

    const tan0: Vector3 = v0.tangent;
    const tan1: Vector3 = v1.tangent;

    const tex0: Vector2 = v0.texcoord;
    const tex1: Vector2 = v1.texcoord;

    // Compute the midpoints of all the attributes.  Vectors need to be normalized
    // since linear interpolating can make them not unit length.
    const pos = vec3.multiply(0.5, p0 + p1);
    const normal = vec3.normalize(vec3.multiply(0.5, n0 + n1));
    const tangent = vec3.normalize(vec3.multiply(0.5, tan0 + tan1));
    const tex = vec2.multiply(0.5, tex0 + tex1);

    return Vertex{
        .position = pos,
        .normal = normal,
        .tangent = tangent,
        .texcoord = tex,
    };
}

pub fn createGeosphere (allocator: std.mem.Allocator, radius: f32, numSubdivisions: u32) !MeshData
{
    var mesh_data = MeshList{};

    // put a cap on the number of subdivisions
    const num_subdivisions = @min(numSubdivisions, 6);

    // Approximate a sphere by tessellating an icosahedron.

    const X = 0.525731;
    const Z = 0.850651;

    const pos = [12]Vector3{
        .{ -X, 0, Z },  .{ X, 0, Z },
        .{ -X, 0, -Z }, .{ X, 0, -Z },
        .{ 0, Z, X },   .{ 0, Z, -X },
        .{ 0, -Z, X },  .{ 0, -Z, -X },
        .{ Z, X, 0 },   .{ -Z, X, 0 },
        .{ Z, -X, 0 },  .{ -Z, -X, 0 },
    };

    const k = [60]u32{
        1,4,0,  4,9,0,  4,5,9,  8,5,4,  1,8,4,
        1,10,8,  10,3,8, 8,3,5,  3,2,5,  3,7,2,
        3,10,7,  10,6,7,  6,11,7,   6,0,11,  6,1,0,
        10,1,6,  11,0,9,  2,11,9,  5,2,9,  11,2,7,
    };

    try mesh_data.vertices.resize( allocator, 12 );
    try mesh_data.indices.appendSlice( allocator, &k );

    for (0..12) |i|
        mesh_data.vertices.items[i].position = pos[i];

    for (0..num_subdivisions) |_|
        try subdivide(allocator, &mesh_data);

    const vertices = mesh_data.vertices.items;
    // Project vertices onto sphere and scale
    for (0..mesh_data.vertices.items.len) |i|
    {
        // Project onto unit sphere.
        const n = vec3.normalize(vertices[i].position);

        // Project onto sphere
        const p = vec3.multiply(radius, n);

        vertices[i].position = p;
        vertices[i].normal = n;
        vertices[i].color = @splat(0);

        // Derive texture coordinates from spherical coordinates.
        var theta: f32 = std.math.atan2(vertices[i].position[2], vertices[i].position[0]);

        const pi2: f32 = 2 * std.math.pi;
        // Put in [0, 2pi]
        if (theta < 0)
            theta += pi2;

        const phi = std.math.acos(vertices[i].position[1] / radius);

        vertices[i].texcoord[0] = theta / pi2;
        vertices[i].texcoord[1] = phi / pi2;

        // Partial derivative of P with respect to theta
        vertices[i].tangent[0] = -radius * @sin(phi) * @sin(theta);
        vertices[i].tangent[1] = 0;
        vertices[i].tangent[2] = radius * @sin(phi) * @cos(theta);

        const T: Vector3 = mesh_data.vertices.items[i].tangent;
        vertices[i].tangent = vec3.normalize(T);
    }
   
    return MeshData{
        .vertices = try mesh_data.vertices.toOwnedSlice(allocator),
        .indices = try mesh_data.indices.toOwnedSlice(allocator),
    };
}

pub fn createCylinder (allocator: std.mem.Allocator, bottom_radius: f32, top_radius: f32, h: f32, slice_count: u32, stack_count: u32) !MeshData
{
    var mesh_data = MeshList{};

    const stack_height: u32 = @as(u32, @intFromFloat(h)) / stack_count;

    // Amount to increment radius as we move up each stack level from bottom to top.
    const radius_step = (top_radius - bottom_radius) / @as(f32, @floatFromInt(stack_count));

    const ring_count = stack_count + 1;

    // Compute vertices for each stack ring starting at the bottom and moving up.
    for (0..ring_count) |I|
    {
        const i: u32 = @intCast(I);
        const y: f32 = -0.5 * h + @as(f32, @floatFromInt(i * stack_height));
        const r: f32 = bottom_radius + @as(f32, @floatFromInt(i)) * radius_step;

        // vertices of ring
        const dtheta: f32 = 2 * std.math.pi / @as(f32, @floatFromInt(slice_count));
        for (0..slice_count + 1) |J|
        {
            const j: u32 = @intCast(J);
            var _vertex: Vertex = undefined;

            const _c = @cos(@as(f32, @floatFromInt(j)) * dtheta);
            const s  = @sin(@as(f32, @floatFromInt(j)) * dtheta);

            _vertex.position = .{ r * _c, y, r * s };
            _vertex.texcoord = .{ @as(f32, @floatFromInt(j / slice_count)), @as(f32, @floatFromInt(i / stack_count)) };

            // this is unit length
            _vertex.tangent = .{ -s, 0, _c };

            const dr = bottom_radius - top_radius;
            const bitangent = [3]f32{ dr * _c, -h, dr * s };

            const T = _vertex.tangent;
            const B = bitangent;
            const N = vec3.normalize(vec3.cross(T, B));
            _vertex.normal = N;

            _vertex.color = @splat(0);

            try mesh_data.vertices.append(allocator, _vertex);
        }
    }

    // Add one because we duplicate the first and last vertex per ring
    // since the texture coordinates are different.
    const ring_vertex_count = slice_count + 1;

    // Compute indices for each stack.
    for (0..stack_count) |I|
    {
        const i: u32 = @intCast(I);
        for (0..slice_count) |J|
        {
            const j: u32 = @intCast(J);
            try mesh_data.indices.append(allocator, i * ring_vertex_count + j);
            try mesh_data.indices.append(allocator, (i + 1) * ring_vertex_count + j);
            try mesh_data.indices.append(allocator, (i + 1) * ring_vertex_count + j + 1);

            try mesh_data.indices.append(allocator, i * ring_vertex_count + j);
            try mesh_data.indices.append(allocator, (i + 1) * ring_vertex_count + j + 1);
            try mesh_data.indices.append(allocator, i * ring_vertex_count + j + 1);
        }
    }

    try buildCylinderTopCap(allocator, bottom_radius, top_radius, h, slice_count, stack_count, &mesh_data);
    try buildCylinderBottomCap(allocator, bottom_radius, top_radius, h, slice_count, stack_count, &mesh_data);

    return MeshData{
        .vertices = try mesh_data.vertices.toOwnedSlice(allocator),
        .indices = try mesh_data.indices.toOwnedSlice(allocator),
    };
}

pub fn createGrid (allocator: std.mem.Allocator, w: f32, d: f32, m: u32, n: u32) !MeshData
{
    const vertex_count = m * n;
    const face_count = (m - 1) * (n - 1) * 2;

    // const mesh_data = try MeshData.init(allocator, vertex_count, face_count * 3);
    var mesh_data = MeshList{};
    try mesh_data.vertices.resize(allocator, vertex_count);
    try mesh_data.indices.resize(allocator, face_count * 3);

    //
    // create the vertices
    //

    const half_width = 0.5 * w;
    const half_depth = 0.5 * d;

    const dx = w / @as(f32, @floatFromInt(n - 1));
    const dz = d / @as(f32, @floatFromInt(m - 1));

    const du = 1 / @as(f32, @floatFromInt(n - 1));
    const dv = 1 / @as(f32, @floatFromInt(m - 1));

    for (0..m - 1) |i|
    {
        const z = half_depth - @as(f32, @floatFromInt(i)) * dz;

        for (0..n - 1) |j|
        {
            const x = -half_width + @as(f32, @floatFromInt(j)) * dx;

            mesh_data.vertices.items[i * n + j].position = .{ x, 0, z };
            mesh_data.vertices.items[i * n + j].normal = .{ 0, 1, 0 };
            mesh_data.vertices.items[i * n + j].tangent = .{ 1, 0, 0 };

            // stretch texture over grid
            mesh_data.vertices.items[i * n + j].texcoord = .{ @as(f32, @floatFromInt(j)) * du, @as(f32, @floatFromInt(i)) * dv };
            mesh_data.vertices.items[i * n + j].color = @splat(0);
        }
    }

    //
    // create the indices
    //

    // iterate over each quad and compute indices.
    var k: usize = 0;
    for (0..m - 1) |I|
    {
        const i: u32 = @intCast(I);
        for (0..n - 1) |J|
        {
            const j: u32 = @intCast(J);
            mesh_data.indices.items[k + 0] = i * n + j;
            mesh_data.indices.items[k + 1] = i * n + j + 1;
            mesh_data.indices.items[k + 2] = (i + 1) * n + j;

            mesh_data.indices.items[k + 3] = (i + 1) * n + j;
            mesh_data.indices.items[k + 4] = i * n + j + 1;
            mesh_data.indices.items[k + 5] = (i + 1) * n + j + 1;

            k += 6; // next quad
        }
    }

    return MeshData{
        .vertices = try mesh_data.vertices.toOwnedSlice(allocator),
        .indices = try mesh_data.indices.toOwnedSlice(allocator),
    };
}

pub fn createQuad (allocator: std.mem.Allocator, x: f32, y: f32, w: f32, h: f32, d: f32) !MeshData
{
    var mesh_data = MeshList{};
    try mesh_data.vertices.resize(allocator, 4);
    try mesh_data.indices.resize(allocator, 6);

    mesh_data.vertices.items[0] = Vertex{.position = .{x, y - h, d}, .normal = .{0, 0, -1}, .texcoord = .{0, 1}};
    mesh_data.vertices.items[1] = Vertex{.position = .{x, y, d}, .normal = .{0, 0, -1}, .texcoord = .{1, 0}};
    mesh_data.vertices.items[2] = Vertex{.position = .{x + w, y, d}, .normal = .{0, 0, -1}, .texcoord = .{1, 1}};
    mesh_data.vertices.items[3] = Vertex{.position = .{x + w, y - h, d}, .normal = .{0, 0, -1}, .texcoord = .{1, 1}};

    mesh_data.indices.items[0] = 0;
    mesh_data.indices.items[1] = 1;
    mesh_data.indices.items[2] = 2;

    mesh_data.indices.items[3] = 0;
    mesh_data.indices.items[4] = 2;
    mesh_data.indices.items[5] = 3;

    addNormals(mesh_data.vertices.items, mesh_data.indices.items);

    return MeshData{
        .vertices = try mesh_data.vertices.toOwnedSlice(allocator),
        .indices = try mesh_data.indices.toOwnedSlice(allocator),
    };
}

pub fn loadModel (allocator: std.mem.Allocator, path: []const u8) !MeshData
{
    const model = try std.fs.cwd().readFileAlloc(allocator, path, 100 * 100 * 1024);
    defer allocator.free(model);

    var lines = std.mem.splitScalar(u8, model, '\n');
    var _vertices = std.ArrayListUnmanaged(Vertex){};
    var _indices  = std.ArrayListUnmanaged(u32){};
    var state: enum{ vertices, indices, none } = .none;

    while (lines.next()) |line|
    {
        if (string.contains(line, "VertexList"))
        {
            state = .vertices;
        }
        else if (string.contains(line, "Triangle"))
        {
            state = .indices;
        }
         else if (line.len > 0 and line[0] == '\t')
         {
            switch (state)
            {
                .vertices => {
                    if (line.len > 4)
                    {
                        
                        var items = std.mem.splitScalar(u8, line, ' ');
                        var vs: [6]f32 = @splat(0);
                        var i: usize = 0;
                        while (items.next()) |vi|
                        {
                            if (vi.len > 0)
                            {
                                const str = if (vi[0] != '-' and !std.ascii.isDigit(vi[0])) vi[1..] else vi;
                                vs[i] = std.fmt.parseFloat(f32, str) catch blk: {
                                    std.debug.print("error on parseVertex: -{s}-\n", .{str});
                                    break :blk 0.0;
                                };
                                i += 1;
                            }
                        }
    
                        const _vertex = Vertex{
                            .position = .{ vs[0], vs[1], vs[2] },
                            .normal = .{ vs[3], vs[4], vs[5] },
                            .tangent  = @splat(0),
                            .texcoord = @splat(0),
                            .color    = @splat(0),
                        };
    
                        try _vertices.append(allocator, _vertex);
                    }
                },
                .indices =>
                {
                    if (line.len > 4)
                    {
                        
                        var items = std.mem.splitScalar(u8, line, ' ');
                        var ids: [3]u32 = @splat(0);
                        var i: usize = 0;
                        while (items.next()) |ii|
                        {
                            if (ii.len > 0)
                            {
                                const str = if (!std.ascii.isDigit(ii[0])) ii[1..] else ii;
                                ids[i] = std.fmt.parseInt(u32, str, 10) catch blk: {
                                    std.debug.print("error on parseIndices: -{s}-\n", .{str});
                                    break :blk 0;
                                };
                                i += 1;
                            }
                        }
                        try _indices.append(allocator, ids[0]);
                        try _indices.append(allocator, ids[1]);
                        try _indices.append(allocator, ids[2]);
                    }
                },
                .none => {},
            }
        }
    }
    
    addNormals(_vertices.items, _indices.items);

    return MeshData{
        .vertices = try _vertices.toOwnedSlice(allocator),
        .indices = try _indices.toOwnedSlice(allocator),
    };
}


test "test loading-model" {
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    const allocator = gpa.allocator();

    var car_model = try loadModel(allocator, "content/models/car.txt");
    defer car_model.deinit(allocator);

    var skull_model = try loadModel(allocator, "content/models/skull.txt");
    defer skull_model.deinit(allocator);

    std.debug.print("testing loading model\n", .{});
    std.debug.print("car-model vertices: {}, indices: {}\n", .{car_model.vertices.len, car_model.indices.len});
    std.debug.print("skull-model vertices: {}, indices: {}\n", .{skull_model.vertices.len, skull_model.indices.len});

    const indices = car_model.indices;
    var i: usize = 0;
    var j: usize = 0;
    for (0..10) |_| {
        const v = car_model.vertices[i];
        _ = v; // autofix
        const _c = [3]u32{ indices[j + 0], indices[j + 1], indices[j + 2] };
        _ = _c;
        i += 1;
        j += 3;

        // std.debug.print ("vertices: {any}, indices: {any}\n", .{v, c});
    }
}

fn serializeModel (model:*MeshData, fs:std.fs.File) !void
{    
    var buffer: [512]u8 = undefined;
    
    for (model.vertices) |v|
    {
        const str = toString(v, &buffer);
        dotnet.File.writeLine(fs, str);
    }

    const indices = model.indices;
    for (0..indices.len / 3) |i|
    {
        const str = try std.fmt.bufPrint(&buffer, "{} {} {}", .{indices[i * 3 + 0], indices[i * 3 + 1], indices[i * 3 + 2]});
        dotnet.File.writeLine(fs, str);
    }
}

test "test create geometries" {
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    const allocator = gpa.allocator();
    const file: []const u8 = "tests/geometries.txt";
    if (dotnet.File.exists(file)) dotnet.File.delete(file);
    var fs = dotnet.File.create(file);
    defer fs.close();
    
    {
        var model = try createBox(allocator, 20, 20, 20, 3);
        std.debug.print("testing box\n", .{});
        std.debug.print("box vertices: {}, indices: {}\n", .{model.vertices.len, model.indices.len});

        dotnet.File.writeLine(fs, "");
        dotnet.File.writeLine(fs, "---------------------");
        dotnet.File.writeLine(fs, "box vertices");
        dotnet.File.writeLine(fs, "---------------------");
        try serializeModel(&model, fs);
        model.deinit(allocator);
    }

    {
        var model = try createSphere(allocator, 100, 20, 10);
        std.debug.print("testing Sphere\n", .{});
        std.debug.print("sphere vertices: {}, indices: {}\n", .{model.vertices.len, model.indices.len});

        dotnet.File.writeLine(fs, "");
        dotnet.File.writeLine(fs, "---------------------");
        dotnet.File.writeLine(fs, "sphere vertices");
        dotnet.File.writeLine(fs, "---------------------");
        try serializeModel(&model, fs);
        model.deinit(allocator);
    }
    
    {
        var model = try createGeosphere(allocator, 100, 2);
        std.debug.print("testing geosphere\n", .{});
        std.debug.print("geosphere vertices: {}, indices: {}\n", .{model.vertices.len, model.indices.len});

        dotnet.File.writeLine(fs, "");
        dotnet.File.writeLine(fs, "---------------------");
        dotnet.File.writeLine(fs, "geosphere vertices");
        dotnet.File.writeLine(fs, "---------------------");
        try serializeModel(&model, fs);
        model.deinit(allocator);
    }

    {
        var model = try createCylinder(allocator, 100, 100, 40, 10, 10);
        std.debug.print("testing cylinder\n", .{});
        std.debug.print("cylinder vertices: {}, indices: {}\n", .{model.vertices.len, model.indices.len});

        dotnet.File.writeLine(fs, "");
        dotnet.File.writeLine(fs, "---------------------");
        dotnet.File.writeLine(fs, "cylinder vertices");
        dotnet.File.writeLine(fs, "---------------------");
        try serializeModel(&model, fs);
        model.deinit(allocator);
    }

    {
        var model = try createGrid(allocator, 100, 100, 40, 3);
        std.debug.print("testing grid\n", .{});
        std.debug.print("grid vertices: {}, indices: {}\n", .{model.vertices.len, model.indices.len});

        dotnet.File.writeLine(fs, "");
        dotnet.File.writeLine(fs, "---------------------");
        dotnet.File.writeLine(fs, "grid vertices");
        dotnet.File.writeLine(fs, "---------------------");
        try serializeModel(&model, fs);
        model.deinit(allocator);
    }

    {
        var model = try createQuad(allocator, 100, 100, 40, 40, 3);
        std.debug.print("testing quad\n", .{});
        std.debug.print("quad vertices: {}, indices: {}\n", .{model.vertices.len, model.indices.len});
        dotnet.File.writeLine(fs, "");
        dotnet.File.writeLine(fs, "---------------------");
        dotnet.File.writeLine(fs, "quad vertices");
        dotnet.File.writeLine(fs, "---------------------");
        try serializeModel(&model, fs);
        model.deinit(allocator);
    }
}
