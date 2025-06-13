const std = @import("std");
const numerics = @import("dotnet").numerics;
const string = @import("dotnet").String;
const Vector2 = numerics.Vector2;
const Vector3 = numerics.Vector3;
const vec3 = numerics.vec3;
const vec2 = numerics.vec2;
const c = @import("c.zig");


pub const Vertex = struct
{
    position:  [3]f32,
    normal:    [3]f32,
    tangent_u: [3]f32,
    tex_c:     [2]f32,
};

inline fn vertex (w: f32, h: f32, d: f32, n0: f32, n1: f32, n2: f32, t0: f32, t1: f32, t2: f32, tx0: f32, tx1: f32) Vertex
{
    return Vertex{
        .position  = .{ w, h, d },
        .normal    = .{ n0, n1, n2 },
        .tangent_u = .{ t0, t1, t2 },
        .tex_c     = .{ tx0, tx1 },
    };
}

pub const BoundingBox = struct
{
    x: f32,
    y: f32,
    z: f32,
    w: f32,
    h: f32,
    d: f32,
};

pub const SubmeshGeometry = struct
{
    indexCount: u32,
    startIndexLocation: u32,
    baseVertexLocation: i32,

    /// Bounding box of the geometry defined by this submesh.
    bounds: BoundingBox,
};

pub const MeshGeometry = struct
{
    name: []const u8,

    vertex_buffer_cpu: std.ArrayListUnmanaged(Vertex) = .{},
    index_buffer_cpu:  std.ArrayListUnmanaged(u32) = .{},

    vertex_buffer_gpu: ?*c.SDL_GPUBuffer = null,
    index_buffer_gpu:  ?*c.SDL_GPUBuffer = null,

    pipeline: ?*c.SDL_GPUGraphicsPipeline = null,

    vertex_buffer_uploader: ?*c.SDL_GPUTransferBuffer = null,
    index_buffer_uploader:  ?*c.SDL_GPUTransferBuffer = null,

    // data about the buffers
    total_indices_len:  u32 = 0,
    vertex_byte_stride: u32,
    vertex_buffer_byte_size: u32 = 0,
    index_buffer_byte_size:  u32 = 0,
    index_format: u32 = c.SDL_GPU_INDEXELEMENTSIZE_32BIT,

    // A MeshGeometry may store multiple geometries in one vertex/index buffer.
    // Use this container to define the Submesh geometries so we can draw
    // the Submeshes individually.

    pub fn deinit (m:*MeshGeometry, allocator:std.mem.Allocator, device:?*c.SDL_GPUDevice) void
    {
        m.vertex_buffer_cpu.deinit(allocator);
        m.index_buffer_cpu.deinit(allocator);

        c.SDL_ReleaseGPUGraphicsPipeline(device, m.pipeline);
        c.SDL_ReleaseGPUBuffer(device, m.vertex_buffer_gpu);
        c.SDL_ReleaseGPUBuffer(device, m.index_buffer_gpu);
    }
};

pub const Light = packed struct
{
    strength:    [3]f32,
    fallOfStart: f32,
    direction:   [3]f32,
    fallOfEnd:   f32,
    position:    [3]f32,
    spotPower:   f32,

    pub const default = Light{
        .strength = .{ 0.5, 0.5, 0.5 },
        .fallOfStart = 1,
        .direction = .{ 0, -1, 0 },
        .fallOfEnd = 10,
        .position = .{ 0, 0, 0 },
        .spotPower = 64,
    };
};

pub const MaterialConstants = struct
{
    diffuseAlbedo: [4]f32 = .{ 1, 1, 1, 1 },
    fresnelR0: [3]f32 = .{ 0.01, 0.01, 0.01 },
    roughness: f32 = 0.25,

    // used in texture mapping.
    matTransform: [16]f32 = numerics.mat4x4.identity,
};

pub const Material = struct
{
    /// unique material name for lookup.
    name: []const u8,

    /// index into constant buffer corresponding to this material.
    matCBIndex: i32,

    /// index into SRV heap for normal texture.
    normalSrvHeapIndex: i32,

    numFramesDirty: i32,

    // material constant buffer data used for shading.
    diffuseAlbedo: [4]f32 = .{ 1, 1, 1, 1 },
    fresnelR0: [3]f32 = .{ 0.01, 0.01, 0.01 },
    roughness: f32 = 0.25,
    matTransform: [16]f32 = numerics.mat4x4.identity,
};

pub const Texture = struct
{
    name:     []const u8,
    filename: []const u8,
    resource: ?*c.SDL_GPUSampler,
};

pub const MeshData = struct
{
    vertices: std.ArrayListUnmanaged(Vertex) = .{},
    indices:  std.ArrayListUnmanaged(u32) = .{},

    pub fn init (allocator: std.mem.Allocator, vertices_size: usize, indices_size: usize) !MeshData
    {
        return MeshData{
            .vertices = try std.ArrayListUnmanaged(Vertex).initCapacity(allocator, vertices_size),
            .indices  = try std.ArrayListUnmanaged(u32).initCapacity(allocator, indices_size),
        };
    }

    pub fn deinit (s: *MeshData, allocator: std.mem.Allocator) void
    {
        s.vertices.deinit(allocator);
        s.indices.deinit(allocator);
    }
};

fn parseVertex (line: []const u8, allocator: std.mem.Allocator, mesh_data: *MeshData) !void
{
    if (line.len < 4) return;

    var items = std.mem.splitScalar(u8, line, ' ');
    var vs: [6]f32 = @splat(0);
    var i: usize = 0;
    while (items.next()) |vi| {
        if (vi.len > 0) {
            const str = if (vi[0] != '-' and !std.ascii.isDigit(vi[0])) vi[1..] else vi;
            vs[i] = std.fmt.parseFloat(f32, str) catch {
                std.debug.print("error on parseVertex: -{s}-\n", .{str});
                // @panic ("parseVertex failed");
                return;
            };
            i += 1;
        }
    }
    const _vertex = Vertex{
        .position = .{ vs[0], vs[1], vs[2] },
        .normal = .{ vs[3], vs[4], vs[5] },
        .tangent_u = .{ 0, 0, 0 },
        .tex_c = .{ 0, 0 },
    };
    try mesh_data.vertices.append(allocator, _vertex);
}

fn parseIndices (line: []const u8, allocator: std.mem.Allocator, mesh_data: *MeshData) !void
{
    if (line.len < 4) return;

    var items = std.mem.splitScalar(u8, line, ' ');
    var ids: [3]u32 = @splat(0);
    var i: usize = 0;
    while (items.next()) |ii|
    {
        if (ii.len > 0)
        {
            const str = if (!std.ascii.isDigit(ii[0])) ii[1..] else ii;
            ids[i] = std.fmt.parseInt(u32, str, 10) catch {
                std.debug.print("error on parseIndices: -{s}-\n", .{str});
                // @panic ("parseIndices failed");
                return;
            };
            i += 1;
        }
    }
    try mesh_data.indices.append(allocator, ids[0]);
    try mesh_data.indices.append(allocator, ids[1]);
    try mesh_data.indices.append(allocator, ids[2]);
}

const State = enum
{
    vertices,
    indices,
    none,
};

pub fn loadModel (allocator: std.mem.Allocator, path: []const u8) !MeshData
{
    const model = try std.fs.cwd().readFileAlloc(allocator, path, 100 * 100 * 1024);
    defer allocator.free(model);

    var lines = std.mem.splitScalar(u8, model, '\n');

    const vertex_count_str = string.trimNoAlloc(lines.next().?[13..], ' ');
    const triangle_count_str = string.trimNoAlloc(lines.next().?[14..], ' ');

    const vertex_count = try std.fmt.parseInt(usize, vertex_count_str, 10);
    const triangle_count = try std.fmt.parseInt(usize, triangle_count_str, 10);

    // std.debug.print ("vertex_count: {d}, indices_count: {d}\n", .{vertex_count, triangle_count});

    var mesh_data = try MeshData.init(allocator, vertex_count, triangle_count);
    var state: State = .none;

    while (lines.next()) |line|
    {
        if (string.contains(line, "VertexList"))
        {
            state = .vertices;
            // std.debug.print("parsing VertexList...\n", .{});
        }
        else if (string.contains(line, "Triangle"))
        {
            state = .indices;
            // std.debug.print("parsing TriangleList...\n", .{});
        }
         else if (line.len > 0 and line[0] == '\t')
         {
            switch (state)
            {
                .vertices => try parseVertex(line, allocator, &mesh_data),
                .indices  => try parseIndices(line, allocator, &mesh_data),
                .none => {},
            }
        }
    }

    // std.debug.print("loaded model: vertices = {d}, indices = {d}\n", .{mesh_data.vertices.items.len, mesh_data.indices.items.len / 3});

    return mesh_data;
}

test "test loading-model" {
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    const allocator = gpa.allocator();

    var car_model = try loadModel(allocator, "car.txt");
    defer car_model.deinit(allocator);

    std.debug.print("testing loading model\n", .{});

    const indices = car_model.indices.items;
    var i: usize = 0;
    var j: usize = 0;
    for (0..10) |_| {
        const v = car_model.vertices.items[i];
        _ = v; // autofix
        const _c = [3]u32{ indices[j + 0], indices[j + 1], indices[j + 2] };
        _ = _c;
        i += 1;
        j += 3;

        // std.debug.print ("vertices: {any}, indices: {any}\n", .{v, c});
    }
}

pub fn createBox (allocator: std.mem.Allocator, w: f32, h: f32, d: f32, numSubdivisions: u32) !MeshData
{
    var mesh_data = try MeshData.init(allocator, 24, 36);

    var v: [24]Vertex = undefined;
    var i: [36]u32 = undefined;

    const w2 = 0.5 * w;
    const h2 = 0.5 * h;
    const d2 = 0.5 * d;

    // Fill in the front face vertex data.
    v[0] = vertex(-w2, -h2, -d2, 0.0, 0.0, -1.0, 1.0, 0.0, 0.0, 0.0, 1.0);
    v[1] = vertex(-w2, h2, -d2, 0.0, 0.0, -1.0, 1.0, 0.0, 0.0, 0.0, 0.0);
    v[2] = vertex(w2, h2, -d2, 0.0, 0.0, -1.0, 1.0, 0.0, 0.0, 1.0, 0.0);
    v[3] = vertex(w2, -h2, -d2, 0.0, 0.0, -1.0, 1.0, 0.0, 0.0, 1.0, 1.0);

    // Fill in the back face vertex data.
    v[4] = vertex(-w2, -h2, d2, 0.0, 0.0, 1.0, -1.0, 0.0, 0.0, 1.0, 1.0);
    v[5] = vertex(w2, -h2, d2, 0.0, 0.0, 1.0, -1.0, 0.0, 0.0, 0.0, 1.0);
    v[6] = vertex(w2, h2, d2, 0.0, 0.0, 1.0, -1.0, 0.0, 0.0, 0.0, 0.0);
    v[7] = vertex(-w2, h2, d2, 0.0, 0.0, 1.0, -1.0, 0.0, 0.0, 1.0, 0.0);

    // Fill in the top face vertex data.
    v[8] = vertex(-w2, h2, -d2, 0.0, 1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 1.0);
    v[9] = vertex(-w2, h2, d2, 0.0, 1.0, 0.0, 1.0, 0.0, 0.0, 0.0, 0.0);
    v[10] = vertex(w2, h2, d2, 0.0, 1.0, 0.0, 1.0, 0.0, 0.0, 1.0, 0.0);
    v[11] = vertex(w2, h2, -d2, 0.0, 1.0, 0.0, 1.0, 0.0, 0.0, 1.0, 1.0);

    // Fill in the bottom face vertex data.
    v[12] = vertex(-w2, -h2, -d2, 0.0, -1.0, 0.0, -1.0, 0.0, 0.0, 1.0, 1.0);
    v[13] = vertex(w2, -h2, -d2, 0.0, -1.0, 0.0, -1.0, 0.0, 0.0, 0.0, 1.0);
    v[14] = vertex(w2, -h2, d2, 0.0, -1.0, 0.0, -1.0, 0.0, 0.0, 0.0, 0.0);
    v[15] = vertex(-w2, -h2, d2, 0.0, -1.0, 0.0, -1.0, 0.0, 0.0, 1.0, 0.0);

    // Fill in the left face vertex data.
    v[16] = vertex(-w2, -h2, d2, -1.0, 0.0, 0.0, 0.0, 0.0, -1.0, 0.0, 1.0);
    v[17] = vertex(-w2, h2, d2, -1.0, 0.0, 0.0, 0.0, 0.0, -1.0, 0.0, 0.0);
    v[18] = vertex(-w2, h2, -d2, -1.0, 0.0, 0.0, 0.0, 0.0, -1.0, 1.0, 0.0);
    v[19] = vertex(-w2, -h2, -d2, -1.0, 0.0, 0.0, 0.0, 0.0, -1.0, 1.0, 1.0);

    // Fill in the right face vertex data.
    v[20] = vertex(w2, -h2, -d2, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 1.0);
    v[21] = vertex(w2, h2, -d2, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 0.0, 0.0);
    v[22] = vertex(w2, h2, d2, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 1.0, 0.0);
    v[23] = vertex(w2, -h2, d2, 1.0, 0.0, 0.0, 0.0, 0.0, 1.0, 1.0, 1.0);

    mesh_data.vertices.appendSliceAssumeCapacity(&v);

    // Fill in the front face index data
    i[0] = 0;
    i[1] = 1;
    i[2] = 2;
    i[3] = 0;
    i[4] = 2;
    i[5] = 3;

    // Fill in the back face index data
    i[6] = 4;
    i[7] = 5;
    i[8] = 6;
    i[9] = 4;
    i[10] = 6;
    i[11] = 7;

    // Fill in the top face index data
    i[12] = 8;
    i[13] = 9;
    i[14] = 10;
    i[15] = 8;
    i[16] = 10;
    i[17] = 11;

    // Fill in the bottom face index data
    i[18] = 12;
    i[19] = 13;
    i[20] = 14;
    i[21] = 12;
    i[22] = 14;
    i[23] = 15;

    // Fill in the left face index data
    i[24] = 16;
    i[25] = 17;
    i[26] = 18;
    i[27] = 16;
    i[28] = 18;
    i[29] = 19;

    // Fill in the right face index data
    i[30] = 20;
    i[31] = 21;
    i[32] = 22;
    i[33] = 20;
    i[34] = 22;
    i[35] = 23;

    mesh_data.indices.appendSliceAssumeCapacity(&i);

    // put a cap on the number of subdivisions.
    const num_subdivisions = @min(numSubdivisions, 6);
    for (0..num_subdivisions) |_|
    {
        try subdivide(allocator, &mesh_data);
    }

    // std.debug.print("box: vertices_len = {d}, indices_len = {d}\n", .{mesh_data.vertices.items.len, mesh_data.indices.items.len});
    return mesh_data;
}

test "test box" {
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    const allocator = gpa.allocator();

    var model = try createBox(allocator, 100, 100, 40, 3);
    defer model.deinit(allocator);

    std.debug.print("testing box\n", .{});

    for (model.vertices.items) |_| {}
    // std.debug.print ("{any}\n", .{v});

    const indices = model.indices.items;
    _ = indices; // autofix
    for (0..model.indices.items.len / 3) |_|
    {
        // std.debug.print("{} {} {}\n", .{indices[i * 3 + 0], indices[i * 3 + 1], indices[i + 3 + 2]});

    }
}

pub fn createSphere (allocator: std.mem.Allocator, r: f32, slice_count: u32, stack_count: u32) !MeshData
{
    var mesh_data = try MeshData.init(allocator, stack_count * slice_count, stack_count * slice_count);

    //
    // Compute the vertices stating at the top pole and moving down the stacks.
    //

    // Poles: note that there will be texture coordinate distortion as there is
    // not a unique point on the texture map to assign to the pole when mapping
    // a rectangular texture onto a sphere.
    const top_vertex = vertex(0, r, 0, 0, 1, 0, 1, 0, 0, 0, 0);
    const bot_vertex = vertex(0, -r, 0, 0, -1, 0, 1, 0, 0, 0, 1);

    try mesh_data.vertices.append(allocator, top_vertex);

    const phi_step = std.math.pi / @as(f32, @floatFromInt(stack_count));
    const theta_step = 2.0 * std.math.pi / @as(f32, @floatFromInt(slice_count));

    // Compute vertices for each stack ring (do not count the poles as rings).
    for (1..stack_count - 1) |i|
    {
        const phi: f32 = @as(f32, @floatFromInt(i)) * phi_step;

        // vertices of ring
        for (0..slice_count) |j|
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
            v.tangent_u = .{
                -r * @sin(phi) * @sin(theta),
                0.0,
                r * @sin(phi) * @cos(theta),
            };
            v.normal = vec3.normalize(v.position);
            v.tex_c = .{ theta / std.math.pi, phi / std.math.pi };

            mesh_data.vertices.appendAssumeCapacity(v);
        }
    }

    mesh_data.vertices.appendAssumeCapacity(bot_vertex);

    //
    // Compute indices for top stack.  The top stack was written first to the vertex buffer
    // and connects the top pole to the first ring.
    //

    for (1..slice_count + 1) |I|
    {
        const i: u32 = @intCast(I);
        mesh_data.indices.appendAssumeCapacity(0);
        mesh_data.indices.appendAssumeCapacity(i + 1);
        mesh_data.indices.appendAssumeCapacity(i);
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

    return mesh_data;
}

test "test Sphere" {
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    const allocator = gpa.allocator();

    var model = try createSphere(allocator, 100, 30, 20);
    defer model.deinit(allocator);

    std.debug.print("testing Sphere\n", .{});

    for (0..model.vertices.items.len) |_| {}
}

fn subdivide (allocator: std.mem.Allocator, mesh_data: *MeshData) !void
{
    var input_copy = MeshData{
        .vertices = try mesh_data.vertices.clone(allocator),
        .indices = try mesh_data.indices.clone(allocator),
    };
    defer input_copy.deinit(allocator);
    // const input_copy = mesh_data;

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

fn midpoint (v0: Vertex, v1: Vertex) Vertex
{
    const p0: Vector3 = v0.position;
    const p1: Vector3 = v1.position;

    const n0: Vector3 = v0.normal;
    const n1: Vector3 = v1.normal;

    const tan0: Vector3 = v0.tangent_u;
    const tan1: Vector3 = v1.tangent_u;

    const tex0: Vector2 = v0.tex_c;
    const tex1: Vector2 = v1.tex_c;

    // Compute the midpoints of all the attributes.  Vectors need to be normalized
    // since linear interpolating can make them not unit length.
    const pos = vec3.multiply(0.5, p0 + p1);
    const normal = vec3.normalize(vec3.multiply(0.5, n0 + n1));
    const tangent = vec3.normalize(vec3.multiply(0.5, tan0 + tan1));
    const tex = vec2.multiply(0.5, tex0 + tex1);

    return Vertex{
        .position = pos,
        .normal = normal,
        .tangent_u = tangent,
        .tex_c = tex,
    };
}

pub fn createGeosphere (allocator: std.mem.Allocator, radius: f32, numSubdivisions: u32) !MeshData
{
    var mesh_data = try MeshData.init(allocator, 12, 60);

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
        1, 4, 0,
        4, 9, 0,
        4, 5, 9,
        8, 5, 4,
        1, 8, 4,
        1, 10, 8,
        10, 3, 8,
        8, 3, 5,
        3, 2, 5,
        3, 7, 2,
        3, 10, 7,
        10, 6, 7,
        6, 11, 7,
        6, 0, 11,
        6, 1, 0,
        10, 1, 6,
        11, 0, 9,
        2, 11, 9,
        5, 2, 9,
        11, 2, 7,
    };

    mesh_data.indices.appendSliceAssumeCapacity(&k);

    try mesh_data.vertices.resize(allocator, 12);
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

        // Derive texture coordinates from spherical coordinates.
        var theta: f32 = std.math.atan2(vertices[i].position[2], vertices[i].position[0]);

        const pi2: f32 = 2 * std.math.pi;
        // Put in [0, 2pi]
        if (theta < 0)
            theta += pi2;

        const phi = std.math.acos(vertices[i].position[1] / radius);

        vertices[i].tex_c[0] = theta / pi2;
        vertices[i].tex_c[1] = phi / pi2;

        // Partial derivative of P with respect to theta
        vertices[i].tangent_u[0] = -radius * @sin(phi) * @sin(theta);
        vertices[i].tangent_u[1] = 0;
        vertices[i].tangent_u[2] = radius * @sin(phi) * @cos(theta);

        const T: Vector3 = mesh_data.vertices.items[i].tangent_u;
        vertices[i].tangent_u = vec3.normalize(T);
    }

    return mesh_data;
}

test "test Geosphere" {
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    const allocator = gpa.allocator();

    var model = try createGeosphere(allocator, 100, 4);
    defer model.deinit(allocator);

    std.debug.print("testing geosphere\n", .{});

    for (0..model.vertices.items.len) |_| {}
}

pub fn createCylinder (allocator: std.mem.Allocator, bottom_radius: f32, top_radius: f32, h: f32, slice_count: u32, stack_count: u32) !MeshData
{
    var mesh_data = try MeshData.init(allocator, stack_count * slice_count, stack_count * slice_count * 6);

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
            _vertex.tex_c = .{ @as(f32, @floatFromInt(j / slice_count)), @as(f32, @floatFromInt(i / stack_count)) };

            // this is unit length
            _vertex.tangent_u = .{ -s, 0, _c };

            const dr = bottom_radius - top_radius;
            const bitangent = [3]f32{ dr * _c, -h, dr * s };

            const T: Vector3 = _vertex.tangent_u;
            const B: Vector3 = bitangent;
            const N: Vector3 = vec3.normalize(vec3.cross(T, B));
            _vertex.normal = N;

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
            mesh_data.indices.appendAssumeCapacity(i * ring_vertex_count + j);
            mesh_data.indices.appendAssumeCapacity((i + 1) * ring_vertex_count + j);
            mesh_data.indices.appendAssumeCapacity((i + 1) * ring_vertex_count + j + 1);

            mesh_data.indices.appendAssumeCapacity(i * ring_vertex_count + j);
            mesh_data.indices.appendAssumeCapacity((i + 1) * ring_vertex_count + j + 1);
            mesh_data.indices.appendAssumeCapacity(i * ring_vertex_count + j + 1);
        }
    }

    try buildCylinderTopCap(allocator, bottom_radius, top_radius, h, slice_count, stack_count, &mesh_data);
    try buildCylinderBottomCap(allocator, bottom_radius, top_radius, h, slice_count, stack_count, &mesh_data);

    return mesh_data;
}

test "test Cylinder" {
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    const allocator = gpa.allocator();

    var model = try createCylinder(allocator, 100, 100, 40, 10, 10);
    defer model.deinit(allocator);

    std.debug.print("testing cylinder\n", .{});

    const indices = model.indices.items;
    var i: usize = 0;
    var j: usize = 0;
    for (0..6) |_| {
        const v = model.vertices.items[i];
        _ = v; // autofix
        const _c = [3]u32{ indices[j + 0], indices[j + 1], indices[j + 2] };
        _ = _c; // autofix
        i += 1;
        j += 3;

        // std.debug.print ("vertices: {any}, indices: {any}\n", .{v, c});
    }
}

fn buildCylinderTopCap (allocator: std.mem.Allocator, bottom_radius: f32, top_radius: f32, h: f32, slice_count: u32, stack_count: u32, mesh_data: *MeshData) !void
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

        try mesh_data.vertices.append(allocator, vertex(x, y, z, 0, 1, 0, 1, 0, 0, u, v));
    }

    // Cap center vertex.
    try mesh_data.vertices.append(allocator, vertex(0, y, 0, 0, 1, 0, 1, 0, 0, 0.5, 0.5));

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

fn buildCylinderBottomCap (allocator: std.mem.Allocator, bottom_radius: f32, top_radius: f32, h: f32, slice_count: u32, stack_count: u32, mesh_data: *MeshData) !void
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

        try mesh_data.vertices.append(allocator, vertex(x, y, z, 0, -1, 0, 1, 0, 0, u, v));
    }

    // cap center vertex.
    try mesh_data.vertices.append(allocator, vertex(0, y, 0, 0, -1, 0, 1, 0, 0, 0.5, 0.5));

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

pub fn createGrid (allocator: std.mem.Allocator, w: f32, d: f32, m: u32, n: u32) !MeshData
{
    const vertex_count = m * n;
    const face_count = (m - 1) * (n - 1) * 2;

    // const mesh_data = try MeshData.init(allocator, vertex_count, face_count * 3);
    var mesh_data = MeshData{};
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

    for (0..m) |i|
    {
        const z = half_depth - @as(f32, @floatFromInt(i)) * dz;

        for (0..n) |j|
        {
            const x = -half_width + @as(f32, @floatFromInt(j)) * dx;

            mesh_data.vertices.items[i * n + j].position = .{ x, 0, z };
            mesh_data.vertices.items[i * n + j].normal = .{ 0, 1, 0 };
            mesh_data.vertices.items[i * n + j].tangent_u = .{ 1, 0, 0 };

            // stretch texture over grid
            mesh_data.vertices.items[i * n + j].tex_c = .{ @as(f32, @floatFromInt(j)) * du, @as(f32, @floatFromInt(i)) * dv };
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

    return mesh_data;
}

test "test grid" {
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    const allocator = gpa.allocator();

    var model = try createGrid(allocator, 100, 100, 40, 3);
    defer model.deinit(allocator);

    std.debug.print("testing grid\n", .{});

    const indices = model.indices.items;
    var i: usize = 0;
    var j: usize = 0;
    for (0..6) |_|
    {
        const v = model.vertices.items[i];
        _ = v; // autofix
        const _c = [3]u32{ indices[j + 0], indices[j + 1], indices[j + 2] };
        _ = _c; // autofix
        i += 1;
        j += 3;

        // std.debug.print ("vertices: {any}, indices: {any}\n", .{v, c});
    }
}

pub fn createQuad (allocator: std.mem.Allocator, x: f32, y: f32, w: f32, h: f32, d: f32) !MeshData
{
    var mesh_data = MeshData{};
    try mesh_data.vertices.resize(allocator, 4);
    try mesh_data.indices.resize(allocator, 6);

    mesh_data.vertices.items[0] = vertex(x, y - h, d, 0, 0, -1, 1, 0, 0, 0, 1);
    mesh_data.vertices.items[1] = vertex(x, y, d, 0, 0, -1, 1, 0, 0, 1, 0);
    mesh_data.vertices.items[2] = vertex(x + w, y, d, 0, 0, -1, 1, 0, 0, 1, 1);
    mesh_data.vertices.items[3] = vertex(x + w, y - h, d, 0, 0, -1, 1, 0, 0, 1, 1);

    mesh_data.indices.items[0] = 0;
    mesh_data.indices.items[1] = 1;
    mesh_data.indices.items[2] = 2;

    mesh_data.indices.items[3] = 0;
    mesh_data.indices.items[4] = 2;
    mesh_data.indices.items[5] = 3;

    return mesh_data;
}

test "test quad" {
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    const allocator = gpa.allocator();

    var model = try createQuad(allocator, 100, 100, 40, 40, 3);
    defer model.deinit(allocator);

    std.debug.print("testing quad\n", .{});

    const indices = model.indices.items;
    var i: usize = 0;
    var j: usize = 0;
    for (0..indices.len / 3) |_|
    {
        const v = model.vertices.items[i];
        _ = v; // autofix
        const _c = [3]u32{ indices[j + 0], indices[j + 1], indices[j + 2] };
        _ = _c; // autofix
        i += 1;
        j += 3;

        // std.debug.print ("vertices: {any}, indices: {any}\n", .{v, c});
    }
}
