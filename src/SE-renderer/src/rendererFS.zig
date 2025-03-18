const c = @import("c.zig");
const std = @import("std");
const common = @import("common.zig");
const numerics = @import("numerics.zig");


var device: ?*c.SDL_GPUDevice = null;
var window: ?*c.SDL_Window = null;
var pipeline: ?*c.SDL_GPUGraphicsPipeline = null;
var vertex_buffer: ?*c.SDL_GPUBuffer = null;

var basepath: []const u8 = undefined;

var is_initialized = false;
var is_disposed = false;


const Position = extern struct {
    x: f32,
    y: f32,
    z: f32,
};

const Color = extern struct {
    v: u32,
};

fn write_ptr (comptime T:type, ptr:*[*]u8, value:T) void {
    const ptr_t: *T = @ptrCast(@alignCast(ptr.*));
    ptr_t.* = value;
    ptr.* += @sizeOf(T);
}

export fn init (vertex_size:u32, vertex_buffer_len:u32, pos:[*c]Position, colors:[*c]Color) void {
    const shader_format = c.SDL_GPU_SHADERFORMAT_SPIRV;
    device = c.SDL_CreateGPUDevice(shader_format, false, null) orelse @panic("gpu device failed");
    window = c.SDL_CreateWindow("Sample-Window", 640, 480, 0) orelse @panic("window failed");

    _ = c.SDL_ClaimWindowForGPUDevice(device, window);

    basepath = std.mem.span(c.SDL_GetBasePath());

    const vertex_shader = common.loadShader(device, "PositionColor.vert", 0,0,0,0) orelse @panic("vertex shader failed");
    const fragment_shader = common.loadShader(device, "SolidColor.frag", 0,0,0,0) orelse @panic("fragment shader failed");

    defer c.SDL_ReleaseGPUShader(device, vertex_shader);
    defer c.SDL_ReleaseGPUShader(device,fragment_shader);

    const pipeline_create_info = c.SDL_GPUGraphicsPipelineCreateInfo{
        .target_info = .{
            .num_color_targets = 1,
            .color_target_descriptions = &[_]c.SDL_GPUColorTargetDescription{
                .{.format = c.SDL_GetGPUSwapchainTextureFormat(device, window)},
            },
        },
        .vertex_input_state = c.SDL_GPUVertexInputState{
            .num_vertex_buffers = 1,
            .vertex_buffer_descriptions = &[_]c.SDL_GPUVertexBufferDescription{
                .{
                    .slot = 0,
                    .input_rate = c.SDL_GPU_VERTEXINPUTRATE_VERTEX,
                    .instance_step_rate = 0,
                    .pitch = vertex_size,
                }
            },
            .num_vertex_attributes = 2,
            .vertex_attributes =  &[_]c.SDL_GPUVertexAttribute{
                .{
                    .buffer_slot = 0,
                    .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
                    .location = 0,
                    .offset = 0,
                },
                .{
                    .buffer_slot = 0,
                    .format = c.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM,
                    .location = 1,
                    .offset = 12  // the offset from the previous item // float3
                },
            },
        },
        .primitive_type = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
        .vertex_shader = vertex_shader,
        .fragment_shader = fragment_shader,
    };

    pipeline = c.SDL_CreateGPUGraphicsPipeline(device, &pipeline_create_info) orelse @panic("failed to create pipeline");


    // create vertex buffer;
    vertex_buffer = c.SDL_CreateGPUBuffer(
        device,
        &c.SDL_GPUBufferCreateInfo{
            .usage = c.SDL_GPU_BUFFERUSAGE_VERTEX,
            .size = vertex_size * vertex_buffer_len,
        }
    );

    // to get data into the vertex buffer, we have to use a transfer buffer
    const transfer_buffer = c.SDL_CreateGPUTransferBuffer(
        device,
        &c.SDL_GPUTransferBufferCreateInfo{
            .usage = c.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            .size = vertex_size * vertex_buffer_len,
        }
    );

    var transfer_data: [*]u8 = @ptrCast(@alignCast(c.SDL_MapGPUTransferBuffer(device, transfer_buffer, false)));
    defer c.SDL_UnmapGPUTransferBuffer(device, transfer_buffer);

    for (0..vertex_buffer_len) |i| {
        write_ptr(Position, &transfer_data, pos[i]);
        write_ptr(Color, &transfer_data, colors[i]);
    }

    const upload_cmdbuf = c.SDL_AcquireGPUCommandBuffer(device);
    const copy_pass = c.SDL_BeginGPUCopyPass(upload_cmdbuf);

    c.SDL_UploadToGPUBuffer(
        copy_pass,
        &c.SDL_GPUTransferBufferLocation{
            .transfer_buffer = transfer_buffer,
            .offset = 0,
        },
        &c.SDL_GPUBufferRegion{
            .buffer = vertex_buffer,
            .offset = 0,
            .size = vertex_size * vertex_buffer_len,
        },
        false
    );

    c.SDL_EndGPUCopyPass(copy_pass);
    _ = c.SDL_SubmitGPUCommandBuffer(upload_cmdbuf);
    c.SDL_ReleaseGPUTransferBuffer(device, transfer_buffer);
}

export fn update () void {
    
}

export fn draw (vertex_len:u32) void {
    const cmdbuf = c.SDL_AcquireGPUCommandBuffer(device) orelse @panic("acquireGPUBuffer failed");
    var swapchain_texture: ?*c.SDL_GPUTexture = null;
    _ = c.SDL_WaitAndAcquireGPUSwapchainTexture(cmdbuf, window, &swapchain_texture, null, null);

    if (swapchain_texture != null) {
        const color_target_info = c.SDL_GPUColorTargetInfo{
            .texture = swapchain_texture,
            .clear_color = c.SDL_FColor{.r = 0, .g = 0, .b = 0, .a = 1},
            .load_op = c.SDL_GPU_LOADOP_CLEAR,
            .store_op = c.SDL_GPU_STOREOP_STORE,
        };
        const renderpass = c.SDL_BeginGPURenderPass(cmdbuf, &color_target_info, 1, null);

        c.SDL_BindGPUGraphicsPipeline(renderpass, pipeline);
        c.SDL_BindGPUVertexBuffers(renderpass, 0, &.{.buffer = vertex_buffer, .offset = 0}, 1);
        c.SDL_DrawGPUPrimitives(renderpass, vertex_len, 1, 0, 0);
        c.SDL_EndGPURenderPass(renderpass);
    }
    
    _ = c.SDL_SubmitGPUCommandBuffer(cmdbuf);
}

export fn quit () void {
    c.SDL_ReleaseWindowFromGPUDevice(device, window);
    c.SDL_DestroyWindow(window);
    c.SDL_DestroyGPUDevice(device);
}
