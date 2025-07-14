const std = @import("std");
const c = @import("c.zig");
const dotnet = @import("dotnet");
const numerics = dotnet.numerics;
const mat4x4 = numerics.mat4x4;

const Vector3 = numerics.Vector3;
const Vector4 = numerics.Vector4;
const Matrix4x4 = numerics.Matrix4x4;

const r = @import("common.zig");
const assets = @import("assets.zig");

const Vertex = r.Vertex;
const MeshData = r.MeshData;
const SubmeshGeometry = r.SubmeshGeometry;

const Geometry = @import("Geometry.zig");
const MeshGeometry = Geometry.MeshGeometry;

const GameTimer = @import("GameTimer.zig");

const print = std.debug.print;
const pi = std.math.pi;

const Self = @This();

pipeline: ?*c.SDL_GPUGraphicsPipeline = null,
box_mesh: r.Mesh = undefined,
car_mesh: r.Mesh = undefined,
box_mesh_data: r.MeshData = undefined,
car_mesh_data: r.MeshData = undefined,
camera: r.Camera = undefined,

var ctx: r.Context = undefined;

fn initialize (ptr:*anyopaque, allocator:std.mem.Allocator) !void
{
    var s: *Self = @ptrCast(@alignCast(ptr));
    s.ctx = try r.init("oexample", 1280, 720, 0);
    s.box_mesh_data = try Geometry.createBox(allocator, 40, 40, 1, 3);
    s.car_mesh_data = try assets.loadModel(allocator, "content/models/car.txt");
    
    const cmdbuf   = c.SDL_AcquireGPUCommandBuffer(ctx.device);
    const copypass = c.SDL_BeginGPUCopyPass(cmdbuf);    

    const bytes = std.mem.sliceAsBytes;

    s.box_mesh = assets.uploadMeshBytes(ctx.device, copypass, bytes(s.box_mesh_data.vertices.items), bytes(s.box_mesh_data.indices.items), s.box_mesh_data.indices.items.len);
    s.car_mesh = assets.uploadMeshBytes(ctx.device, copypass, bytes(s.car_mesh_data.vertices.items), bytes(s.car_mesh_data.indices.items), s.car_mesh_data.indices.items.len);

    s.camera = r.Camera{};
}

fn deinit (ptr:*anyopaque, allocator:std.mem.Allocator) void
{
    _ = allocator; // autofix
    var s: *Self = @ptrCast(@alignCast(ptr));
    _ = &s;    
}

fn update (ptr:*anyopaque) void
{
    var s: *Self = @ptrCast(@alignCast(ptr));
    _ = &s;
    
}

fn draw (ptr:*anyopaque, cmdbuffer:?*c.SDL_GPUCommandBuffer, renderpass:?*c.SDL_GPURenderPass) void
{
    var s: *Self = @ptrCast(@alignCast(ptr));
    _ = &s;
    _ = cmdbuffer; // autofix
    
    c.SDL_BindGPUGraphicsPipeline(renderpass, s.pipeline);
    c.SDL_BindGPUVertexBuffers(renderpass, 0, &.{.buffer = s.box_mesh.vertex_buffer, .offset = 0}, 1);
    c.SDL_BindGPUIndexBuffer(renderpass, &.{.buffer = s.box_mesh.index_buffer, .offset = 1}, c.SDL_GPU_INDEXELEMENTSIZE_32BIT);
    // c.SDL_PushGPUVertexUniformData(cmdbuffer, 0, &{}, 0);   
    c.SDL_DrawGPUIndexedPrimitives(renderpass, s.box_mesh.num_indices, 1, 0, 0, 0);
}

pub fn state (s:*Self) r.State
{
    return r.State{
        .ptr = s,
        .initFn = initialize,
        .deinitFn = deinit,
        .updateFn = update,
        .drawFn = draw,
    };
}
