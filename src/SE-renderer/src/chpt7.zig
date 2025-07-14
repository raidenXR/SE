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

pub fn toString (v:r.Vertex, buffer:[]u8) []const u8
{
    const p = v.position;
    const C = v.color;
    return std.fmt.bufPrint(buffer, "pos: [{d}, {d}, {d}],  color: [{d}, {d}, {d}, {d}]", .{p[0], p[1], p[2], C[0], C[1], C[2], C[3]})
        catch @panic("std.fmt.printBuff() failed");
}

// pub fn state (s:*@This()) r.State
// {
//     return r.State{
//         .ptr = s,
//         .initialFn = initialize,
//         .updateFn = update,
//         .drawFn = draw,
//     };
// }


var ctx: r.Context = undefined;
var pipeline: ?*c.SDL_GPUGraphicsPipeline = null;
var mesh_geometries: std.StringHashMap(Geometry.MeshGeometry) = undefined;
// var render_items: std.ArrayList(RenderItem) = undefined;

var main_cb: r.PassConstants = .default;
var obj_cb:  r.ObjectConstants = .default;

var is_running = true;

// Camera
// -----------------------------------------
var world = mat4x4.identity;
var view  = mat4x4.identity;
var proj  = mat4x4.identity;
var world_view_proj = mat4x4.identity;
var eyes_pos = Vector3{0, 0, 0};

var theta: f32 = 1.5 * pi;
var phi:   f32 = 0.2 * pi;
var radius: f32 = 15.0;
// -----------------------------------------

const w = 1240;
const h = 720;
const aspect_ratio = w / h;

// fn initialize (allocator:std.mem.Allocator) !void
// {   
//     mesh_geometries = std.StringHashMap(Geometry.MeshGeometry).init(std.heap.c_allocator);
//     // render_items = std.ArrayList(RenderItem).init(std.heap.c_allocator);
//     try buildShapeGeometry(allocator);
//     try buildRenderItems(allocator);
//     try buildPSO(allocator, "shaders/color_vs.spv", "shaders/color_ps.spv");
// }


fn update () void
{
    var gt = GameTimer.default;
    onKeyboardInput(&gt);
    updateCamera(&gt);
    // animateMaterials();    
    updateObjectCBS(&gt);
    // updateMaterials();
    updateMainPassCB(&gt);
}

// test "test cbpass" {
//     var gpa = std.heap.GeneralPurposeAllocator(.{}){};
//     const allocator = gpa.allocator();
//     mesh_geometries = std.StringHashMap(Geometry.MeshGeometry).init(std.heap.c_allocator);
//     defer mesh_geometries.deinit();
//     render_items = std.ArrayList(RenderItem).init(std.heap.c_allocator);
//     defer render_items.deinit();

//     var gt: GameTimer = .default;
//     ctx = try r.init("renderer window", 1240, 720, 0);
//     defer r.quit(ctx);
//     // defer mesh_geometries.deinit(allocator);
//     // defer render_items.deinit(allocator);

//     try buildShapeGeometry(allocator);
//     try buildRenderItems(allocator);
//     try buildPSO(allocator, "shaders/compiled/color_vs.spv", "shaders/compiled/color_ps.spv");
//     var buffer: [512]u8 = undefined;

//     update(&gt);

//     // if (true) return;
//     if (false) return;

//     const ptr: [*]f32 = @ptrCast(@alignCast(&main_cb));
//     const len = @sizeOf(r.PassConstants) / @sizeOf(f32);

//     const File = dotnet.File;
//     var fs = File.create("cbPass_log.txt");
//     defer fs.close();

//     File.writeLine(fs, "CB pass");
//     for (0..len) |i|
//     {
//         const str = try std.fmt.bufPrint(&buffer, "{d}", .{ptr[i]});
//         File.writeLine(fs, str);
//     }        

//     var box  = try Geometry.createBox(allocator, 20, 20, 20, 3);
//     var grid = try Geometry.createGrid(allocator, 100, 100, 5, 5);
//     var quad = try Geometry.createQuad(allocator, 5, 12, 20, 30, 2);
//     var sphere = try Geometry.createSphere(allocator, 40, 12, 12);
//     var cylinder = try Geometry.createCylinder(allocator, 50, 40, 10, 8, 12);
    
//     defer box.deinit(allocator);
//     defer grid.deinit(allocator);
//     defer quad.deinit(allocator);
//     defer sphere.deinit(allocator);
//     defer cylinder.deinit(allocator);

//     var model = box;
//     var indices = box.indices.items;

//     File.writeLine(fs, "\nbox");
//     for (model.vertices.items) |v| {
//         File.writeLine(fs, toString(v, &buffer));    
//     }
//     for (0..indices.len / 3) |i| {
//         const str = try std.fmt.bufPrint(&buffer, "{}  {}  {}", .{indices[i * 3 + 0], indices[i * 3 + 1], indices[i * 3 + 2]});
//         File.writeLine(fs, str);
//     }

//     model = grid;
//     indices = grid.indices.items;
//     File.writeLine(fs, "\ngrid");
//     for (model.vertices.items) |v| {
//         File.writeLine(fs, toString(v, &buffer));    
//     }
//     for (0..indices.len / 3) |i| {
//         const str = try std.fmt.bufPrint(&buffer, "{}  {}  {}", .{indices[i * 3 + 0], indices[i * 3 + 1], indices[i * 3 + 2]});
//         File.writeLine(fs, str);
//     }

//     model = sphere;
//     indices = sphere.indices.items;
//     File.writeLine(fs, "\nsphere");
//     for (model.vertices.items) |v| {
//         File.writeLine(fs, toString(v, &buffer));    
//     }
//     for (0..indices.len / 3) |i| {
//         const str = try std.fmt.bufPrint(&buffer, "{}  {}  {}", .{indices[i * 3 + 0], indices[i * 3 + 1], indices[i * 3 + 2]});
//         File.writeLine(fs, str);
//     }
    
//     model = cylinder;
//     indices = cylinder.indices.items;
//     File.writeLine(fs, "\nCylinder");
//     for (model.vertices.items) |v| {
//         File.writeLine(fs, toString(v, &buffer));    
//     }
//     for (0..indices.len / 3) |i| {
//         const str = try std.fmt.bufPrint(&buffer, "{}  {}  {}", .{indices[i * 3 + 0], indices[i * 3 + 1], indices[i * 3 + 2]});
//         File.writeLine(fs, str);
//     }
// }

fn draw () void
{
    var gt = GameTimer.default;
    _ = &gt;
    const cmdbuf = c.SDL_AcquireGPUCommandBuffer( ctx.device );

    var swapchain_texture: ?*c.SDL_GPUTexture = null;
    if (!c.SDL_WaitAndAcquireGPUSwapchainTexture( cmdbuf, ctx.window, &swapchain_texture, null, null)) 
    {
        @panic ("WaitAndAcquiredGPUSwaptexture failed\n");
    }

    if (swapchain_texture != null)
    {
        const color_target_info = c.SDL_GPUColorTargetInfo{
            .texture = swapchain_texture,
            .clear_color = c.SDL_FColor{.r = 0.1, .g = 0.2, .b = 0.2, .a = 1},
            .load_op = c.SDL_GPU_LOADOP_CLEAR,
            .store_op = c.SDL_GPU_STOREOP_STORE,
        };

        const renderpass = c.SDL_BeginGPURenderPass( cmdbuf, &color_target_info, 1, null );
        _ = renderpass; // autofix
        
        // drawRenderItems(cmdbuf, renderpass);
    }

    _ = c.SDL_SubmitGPUCommandBuffer( cmdbuf );
}

fn onKeyboardInput (gt:*const GameTimer) void
{
    _ = gt;
}

fn updateCamera (gt:*const GameTimer) void
{
    _ = gt;
    eyes_pos[0] = radius * @sin(phi) * @cos(theta);
    eyes_pos[2] = radius * @sin(phi) * @sin(theta);
    eyes_pos[1] = radius * @cos(phi);

    const pos = Vector3{eyes_pos[0], eyes_pos[1], eyes_pos[2]};
    const target = Vector3{0,0,0};
    const up = Vector3{0, 1, 0};

    view = mat4x4.createLookAt(pos, target, up);    
    view[15] = 1;
}

fn updateObjectCBS (gt:*const GameTimer) void
{
    _ = gt;
    // for (render_items.items) |e|
    // {
    //     obj_cb.world = mat4x4.transpose(e.world);
    // }
}
fn updateMainPassCB (gt:*const GameTimer) void
{
    const _view = view;
    const _proj = proj;

    const _view_proj = mat4x4.multiply(_view, _proj);
    var _inv_view: Matrix4x4 = undefined;
    var _inv_proj: Matrix4x4 = undefined;
    var _inv_view_proj: Matrix4x4 = undefined;
    _ = mat4x4.invert(_view, &_inv_view);
    _ = mat4x4.invert(_proj, &_inv_proj);
    _ = mat4x4.invert(_view_proj, &_inv_view_proj);
    
    main_cb = r.PassConstants{
        .view      = mat4x4.transpose(_view),
        .inv_view  = mat4x4.transpose(_inv_view),
        .proj      = mat4x4.transpose(_proj),
        .inv_proj  = mat4x4.transpose(_inv_proj),
        .view_proj = mat4x4.transpose(_view_proj),
        .inv_view_proj = mat4x4.transpose(_inv_view_proj),
        .eyes_posW = eyes_pos,
        .pad1 = 0,
        .render_target_size = .{1280.0, 720.0},
        .inv_render_target_size = .{1.0 / 1280.0, 1.0 / 720.0},
        .nearZ = 1.0,
        .farZ  = 1000.0,
        .total_time = gt.totalTime(),
        .delta_time = gt.deltaTime(),
    };
}


fn buildShapeGeometry (allocator:std.mem.Allocator) !void
{    
    var box      = try Geometry.createBox(allocator, 1.5, 0.5, 1.5, 3);
    var grid     = try Geometry.createGrid(allocator, 20, 30, 60, 40);
    var sphere   = try Geometry.createSphere(allocator, 0.5, 20, 20);
    var cylinder = try Geometry.createCylinder(allocator, 0.5, 0.3, 3, 20, 20);

    defer box.deinit(allocator);
    defer grid.deinit(allocator);
    defer sphere.deinit(allocator);
    defer cylinder.deinit(allocator);

    // line 586
    const box_vertex_offset: usize = 0;
    const grid_vertex_offset = box.vertices.items.len;
    const sphere_vertex_offset = grid_vertex_offset + grid.vertices.items.len;
    const cylinder_vertex_offset = sphere_vertex_offset + sphere.vertices.items.len;

    const box_index_offset: usize = 0;
    const grid_index_offset = box.indices.items.len;
    const sphere_index_offset = grid_index_offset + grid.indices.items.len;
    const cylinder_index_offset = sphere_index_offset + sphere.indices.items.len;

    // Define the SubmeshGeometry that cover different 
    // regions of the vertex/index buffers.
    const box_submesh = SubmeshGeometry{
        .index_count = @intCast(box.indices.items.len),
        .start_index_location = @intCast(box_index_offset),
        .base_vertex_location = @intCast(box_vertex_offset),
    };
    const grid_submesh = SubmeshGeometry{
        .index_count = @intCast(grid.indices.items.len),
        .start_index_location = @intCast(grid_index_offset),
        .base_vertex_location = @intCast(grid_vertex_offset),
    };
    const sphere_submesh = SubmeshGeometry{
        .index_count = @intCast(sphere.indices.items.len),
        .start_index_location = @intCast(sphere_index_offset),
        .base_vertex_location = @intCast(sphere_vertex_offset),
    };
    const cylinder_submesh = SubmeshGeometry{
        .index_count = @intCast(cylinder.indices.items.len),
        .start_index_location = @intCast(cylinder_index_offset),
        .base_vertex_location = @intCast(cylinder_vertex_offset),
    }; 

    const total_vertex_count =
        box.vertices.items.len +
        grid.vertices.items.len +
        sphere.vertices.items.len +
        cylinder.vertices.items.len;

    var vertices = std.ArrayListUnmanaged(Vertex){};
    try vertices.resize(allocator, total_vertex_count);

    var k: usize = 0;
    for (box.vertices.items) |i|
    {
        vertices.items[k].position= i.position;
        vertices.items[k].color = .{0.3, 0.3, 0.3, 1.0};
        k += 1;
    }

    for (grid.vertices.items) |i|
    {
        vertices.items[k].position = i.position;
        vertices.items[k].color = .{0.3, 0.3, 0.3, 1.0};
        k += 1;
    }

    for (sphere.vertices.items) |i|
    {
        vertices.items[k].position = i.position;
        vertices.items[k].color = .{0.3, 0.3, 0.3, 1.0};
        k += 1;
    }

    for (cylinder.vertices.items) |i|
    {
        vertices.items[k].position = i.position;
        vertices.items[k].color = .{0.3, 0.3, 0.3, 1.0};
        k += 1;
    }

    var indices = std.ArrayListUnmanaged(u32){};
    try indices.appendSlice(allocator, box.indices.items);
    try indices.appendSlice(allocator, grid.indices.items);
    try indices.appendSlice(allocator, sphere.indices.items);
    try indices.appendSlice(allocator, cylinder.indices.items);

    const vb_byte_size = vertices.items.len * @sizeOf(Vertex);
    const ib_byte_size = indices.items.len * @sizeOf(u32);

    var geo = Geometry.MeshGeometry{
        .name = "geo",
        // .vertex_buffer_cpu = vertices,
        // .index_buffer_cpu = indices,
        .total_indices_len = @intCast(indices.items.len),
        .vertex_byte_stride = @sizeOf(Vertex),
        .vertex_buffer_byte_size = @intCast(vb_byte_size),
        .index_buffer_byte_size = @intCast(ib_byte_size),
    };
    
    // const vertices_size = @sizeOf(Vertex) * vertices.items.len;
    // const indices_size  = @sizeOf(u32) * indices.items.len;
    // geo.vertex_buffer_gpu = r.createBlob(ctx.device, c.SDL_GPU_BUFFERUSAGE_VERTEX, vertices_size);
    // geo.index_buffer_gpu  = r.createBlob(ctx.device, c.SDL_GPU_BUFFERUSAGE_INDEX, indices_size);
    // r.copyMemory(ctx.device, geo.vertex_buffer_gpu, vertices.items.ptr, vertices_size);
    // r.copyMemory(ctx.device, geo.index_buffer_gpu, indices.items.ptr, indices_size);
    
    try geo.drawArgs.put(allocator, "box", box_submesh);
    try geo.drawArgs.put(allocator, "grid", grid_submesh);
    try geo.drawArgs.put(allocator, "sphere", sphere_submesh);
    try geo.drawArgs.put(allocator, "cylinder", cylinder_submesh);
    
    try mesh_geometries.put("geo", geo);
}

// test "test BuildShapeGeometry" {
    // var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    // const allocator = gpa.allocator();
    // mesh_geometries = std.StringHashMap(MeshGeometry).init(std.heap.c_allocator);
    // defer mesh_geometries.deinit();
    // render_items = std.ArrayList(RenderItem).init(std.heap.c_allocator);
    // defer render_items.deinit();

    // var gt: GameTimer = .default;
    // ctx = try r.init("renderer window", 1240, 720, 0);
    // defer r.quit(ctx);
    // // defer mesh_geometries.deinit(allocator);
    // // defer render_items.deinit(allocator);

    // try buildShapeGeometry(allocator);
    // try buildRenderItems(allocator);
    // try buildPSO(allocator, "shaders/compiled/color_vs.spv", "shaders/compiled/color_ps.spv");
    // var buffer: [512]u8 = undefined;

    // update(&gt);

    // try buildShapeGeometry(allocator);

    // // if (true) return;
    // if (false) return;
    
    // if (mesh_geometries.get("geo")) |geo|
    // {
    //     const File = dotnet.File;
    //     // const vertices = geo.vertex_buffer_cpu.items;
    //     const v_ptr: [*]Vertex = @ptrCast(@alignCast(geo.vertex_buffer_gpu));
    //     const vertices = v_ptr[0..geo.vertex_buffer_byte_size / geo.vertex_byte_stride];
    //     // const indices = geo.index_buffer_cpu.items;
    //     const i_ptr: [*]u32 = @ptrCast(@alignCast(geo.index_buffer_gpu));
    //     const indices = i_ptr[0..geo.index_buffer_byte_size / @sizeOf(u32)];

    //     var fs = File.create("buildShapeGeometry_log.txt");
    //     defer fs.close();

    //     std.debug.print("\n#### begin ShapeGeometry iteration\nvertices: {}, indices: {}\n\n", .{vertices.len, indices.len});
    //     for (vertices) |v| {
    //         File.writeLine(fs, toString(v, &buffer));            
    //     }
    //     for (0..indices.len / 3) |i| {
    //         const str = try std.fmt.bufPrint(&buffer, "{}  {}  {}", .{indices[i * 3 + 0], indices[i * 3 + 1], indices[i * 3 + 2]});
    //         File.writeLine(fs, str);
    //     }
    // }
    // else
    // {
    //     std.debug.print("mesh_geometries has no geo entry\n", .{});
    // }
// }

fn buildPSO (allocator:std.mem.Allocator, vs_path:[]const u8, ps_path:[]const u8) !void
{
    _ = allocator; // autofix
    const vs = assets.loadShader(ctx.device, vs_path);
    const ps = assets.loadShader(ctx.device, ps_path);

    const pipeline_create_info = c.SDL_GPUGraphicsPipelineCreateInfo{
        .target_info = c.SDL_GPUGraphicsPipelineTargetInfo{
            .num_color_targets = 1,
            .color_target_descriptions = &[_]c.SDL_GPUColorTargetDescription{
                .{.format = c.SDL_GetGPUSwapchainTextureFormat(ctx.device, ctx.window)},                    
            }                
        },
        .vertex_input_state = c.SDL_GPUVertexInputState{
            .num_vertex_buffers = 1,
            .vertex_buffer_descriptions = &[_]c.SDL_GPUVertexBufferDescription{
                .{
                    .slot = 0,
                    .input_rate = c.SDL_GPU_VERTEXINPUTRATE_VERTEX,
                    .instance_step_rate = 0,
                    .pitch = @sizeOf(Vertex),
                }
            },
            .num_vertex_attributes = 2,
            .vertex_attributes = &[_]c.SDL_GPUVertexAttribute{
                .{
                    .buffer_slot = 0,
                    .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
                    .location = 0,
                    .offset = 0,
                },
                .{
                    .buffer_slot = 0,
                    .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
                    .location = 1,
                    .offset = 12,
                },
            }            
        },
        .primitive_type = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
        .vertex_shader = vs,
        .fragment_shader = ps,
    };

    pipeline = c.SDL_CreateGPUGraphicsPipeline(ctx.device, &pipeline_create_info);

    c.SDL_ReleaseGPUShader(ctx.device, vs);
    c.SDL_ReleaseGPUShader(ctx.device, ps);

    // var _iter = mesh_geometries.valueIterator();
    // while (_iter.next()) |g|
    // {
    //     _ = g; // autofix
    //     // r.createMeshGeometryBuffers(ctx.device, g);        
    // }

}

// fn buildRenderItems (allocator:std.mem.Allocator) !void
// {
//     _ = allocator; // autofix
//     const geo = mesh_geometries.getPtr("geo").?;
    
//     const box_item = RenderItem{
//         .world = mat4x4.multiply(mat4x4.createScale(2,2,2), mat4x4.createTranslation(.{0, 0.5, 0})),
//         // .tex_transform = mat4x4.createScale(1,1,1),
//         // .mat = materials.get("mat0").?,
//         .geo = geo,
//         .primitive_type = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
//         .index_count = geo.drawArgs.get("box").?.index_count,
//         .start_index_location = geo.drawArgs.get("box").?.start_index_location,
//         .base_vertex_location = geo.drawArgs.get("box").?.base_vertex_location,
//     };

//     const grid_item = RenderItem{
//         .world = mat4x4.multiply(mat4x4.createScale(2,2,2), mat4x4.createTranslation(.{0, 0.5, 0})),
//         // .tex_transform = mat4x4.createScale(.{1,1,1}),
//         // .mat = materials.get("mat0").?,
//         .geo = geo,
//         .primitive_type = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
//         .index_count = geo.drawArgs.get("grid").?.index_count,
//         .start_index_location = geo.drawArgs.get("grid").?.start_index_location,
//         .base_vertex_location = geo.drawArgs.get("grid").?.base_vertex_location,
//     };

//     // const skull_item = RenderItem{
//     //     .world = mat4x4.multiply(mat4x4.createScale(2,2,2), mat4x4.createTranslation(.{0, 0.5, 0})),
//     //     // .tex_transform = mat4x4.createScale(1,1,1),
//     //     // .mat = materials.get("mat0").?,
//     //     .geo = mesh_geometries.get("shape").?,
//     //     .primitive_type = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
//     //     .index_count = mesh_geometries.get("shape").?.total_indices_len,
//     //     .start_index_location = shape.drawArgs.get("box").?.start_index_location,
//     //     .base_vertex_location = shape.drawArgs.get("box").?.base_vertex_location,
//     // };

//     try render_items.append(box_item);
//     try render_items.append(grid_item);
//     // try render_items.append(skull_item);


//     for (0..5) |I|
//     {
//         const i: f32 = @floatFromInt(I);
//         const left_cyl_world  = mat4x4.createTranslation(.{-0.5, 1.5, -10.0 + i * 5.0});
//         const right_cyl_world = mat4x4.createTranslation(.{0.5, 1.5, -10.0 + i * 5.0});
        
//         const left_sphere_world  = mat4x4.createTranslation(.{-0.5, 3.5, -10.0 + i * 5.0});
//         const right_sphere_world = mat4x4.createTranslation(.{0.5, 3.5, -10.0 + i * 5.0});

//         const left_cyl_ritem = RenderItem{
//             .world = right_cyl_world,
//             .geo = geo,
//             .primitive_type = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
//             .index_count = geo.drawArgs.get("cylinder").?.index_count,
//             .start_index_location = geo.drawArgs.get("cylinder").?.start_index_location,
//             .base_vertex_location = geo.drawArgs.get("cylinder").?.base_vertex_location,
//         };

//         const right_cyl_ritem = RenderItem{
//             .world = left_cyl_world,
//             .geo = geo,
//             .primitive_type = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
//             .index_count = geo.drawArgs.get("cylinder").?.index_count,
//             .start_index_location = geo.drawArgs.get("cylinder").?.start_index_location,
//             .base_vertex_location = geo.drawArgs.get("cylinder").?.base_vertex_location,            
//         };

//         const left_sphere_item = RenderItem{
//             .world = right_sphere_world,
//             .geo = geo,
//             .primitive_type = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
//             .index_count = geo.drawArgs.get("sphere").?.index_count,
//             .start_index_location = geo.drawArgs.get("sphere").?.start_index_location,
//             .base_vertex_location = geo.drawArgs.get("sphere").?.base_vertex_location,                        
//         };

//         const right_sphere_item = RenderItem{
//             .world = left_sphere_world,
//             .geo = geo,
//             .primitive_type = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
//             .index_count = geo.drawArgs.get("sphere").?.index_count,
//             .start_index_location = geo.drawArgs.get("sphere").?.start_index_location,
//             .base_vertex_location = geo.drawArgs.get("sphere").?.base_vertex_location,                                    
//         };

//         try render_items.append(left_cyl_ritem);     
//         try render_items.append(right_cyl_ritem);     
//         try render_items.append(left_sphere_item);     
//         try render_items.append(right_sphere_item);     
//     }
// }


// fn drawRenderItems (cmdbuf:?*c.SDL_GPUCommandBuffer, renderpass:?*c.SDL_GPURenderPass) void
// {
//     const obj_cb_size = @sizeOf(r.PassConstants);
//     // const mat_cb_size = @sizeOf(r.Material);
//     const main_cb_size = @sizeOf(r.ObjectConstants);

//     for (render_items.items) |ri|
//     {
//         c.SDL_BindGPUGraphicsPipeline(renderpass, ri.geo.pipeline);
//         c.SDL_BindGPUVertexBuffers(renderpass, 0, &.{.buffer = ri.geo.vertex_buffer_gpu, .offset = ri.base_vertex_location}, 1);        
//         c.SDL_BindGPUIndexBuffer(renderpass, &.{.buffer = ri.geo.index_buffer_gpu, .offset = ri.start_index_location}, c.SDL_GPU_INDEXELEMENTSIZE_32BIT);
//         c.SDL_PushGPUVertexUniformData(cmdbuf, 0, &main_cb, main_cb_size);
//         c.SDL_PushGPUVertexUniformData(cmdbuf, 1, &obj_cb, obj_cb_size);
//         // c.SDL_PushGPUFragmentUniformData(cmdbuf, 0, &mat_cb, mat_cb_size);
//         c.SDL_DrawGPUIndexedPrimitives(renderpass, ri.index_count, 1, 0, 0, 0);
//         // c.SDL_EndGPURenderPass(renderpass);
//     }
// }


