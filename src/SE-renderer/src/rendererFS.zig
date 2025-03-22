const c = @import("c.zig");
const std = @import("std");
const common = @import("common.zig");
const dotnet = @import("dotnet");
const numerics = dotnet.numerics;

const ContextError = error
{
    DeviceFailed,
    WindowFailed,
    ClaimWindowFailed,
    PipelineFailed,
    BufferFailed,
};

const Position = extern struct {
    x: f32,
    y: f32,
    z: f32,
};

const Color = extern struct {
    v: u32,
};

fn failwith (comptime fmt:[]const u8, args:anytype) noreturn 
{
    var buffer: [512]u8 = undefined;
    const msg = std.fmt.bufPrintZ(&buffer, fmt, args) catch "failwith buffer overflown\n";
    std.debug.print("{s}", .{msg});
    unreachable;
}

// T must be a pointer
fn write_ptr (comptime T:type, ptr:*[*]u8, value:T) void {
    const ptr_t: *T = @ptrCast(@alignCast(ptr.*));
    ptr_t.* = value;
    ptr.* += @sizeOf(T);
}

fn cast (comptime T:type, ptr:anytype) T
{
    const _ptr:T = @ptrCast(@alignCast(ptr));
    return _ptr;
}

const Context = extern struct 
{
    device: ?*c.SDL_GPUDevice,
    window: ?*c.SDL_Window,
    pipeline: ?*c.SDL_GPUGraphicsPipeline,
    vertex_buffer: ?*c.SDL_GPUBuffer,
    index_buffer: ?*c.SDL_GPUBuffer,
    is_initialized: bool,
    is_disposed: bool,
    running: bool,
};

/// A simple struct to hold data for a surface, mesh
const Mesh = extern struct
{
    positions: [*c]Position,
    colors: [*c]Color,
    indices: [*c]u16,
    len: u32,
};

export fn init (name:[*c]const u8) Context
{
    var s = Context{
        .device = null,
        .window = null,
        .pipeline = null,
        .vertex_buffer = null,
        .index_buffer = null,
        .is_initialized = true,
        .is_disposed = false,
        .running = true,
    };
    
    if (!c.SDL_Init(c.SDL_INIT_VIDEO | c.SDL_INIT_GAMEPAD)) {
        // common.failwith("no example named: {s}\n", .{examplename});
    }

    common.initializeAssetLoader();

    s.device = c.SDL_CreateGPUDevice(
        c.SDL_GPU_SHADERFORMAT_SPIRV | c.SDL_GPU_SHADERFORMAT_DXIL | c.SDL_GPU_SHADERFORMAT_SPIRV,
        false,
        null,
    );
    if (s.device == null) {
        @panic("GPUCreate failed");
    }

    s.window = c.SDL_CreateWindow(name, 640, 480, 0);
    if (s.window == null) {
        failwith ("create window failed: {s}", .{c.SDL_GetError()});
    }

    if (!c.SDL_ClaimWindowForGPUDevice(s.device, s.window)) {
        @panic("GPUClainWindow failed");
    }       

    return s;
}

/// pitch is the len of the buffer description i.e sizeOf PositionColorVertex
export fn createVertexBuffer (s:*Context, pos:[*]Position, colors:[*]Color, vertex_size:u32, vertex_buffer_len:u32) void 
{
    // create the shaders
    const vertex_shader = common.loadShader(s.device, "PositionColor.vert", 0,0,0,0);
    if (vertex_shader == null) {
        @panic("failed to create vertex shader");
    }

    const fragment_shader = common.loadShader(s.device, "SolidColor.frag", 0,0,0,0);
    if (fragment_shader == null) {
        @panic("failed to create fragment shader");
    }

    defer c.SDL_ReleaseGPUShader(s.device, vertex_shader);
    defer c.SDL_ReleaseGPUShader(s.device, fragment_shader);    

    // create the pipeline 
    const pipeline_create_info = c.SDL_GPUGraphicsPipelineCreateInfo{
        .target_info = .{
            .num_color_targets = 1,
            .color_target_descriptions = &[_]c.SDL_GPUColorTargetDescription{
                .{.format = c.SDL_GetGPUSwapchainTextureFormat(s.device, s.window)},
            },
        },
        // this is set up to match the vertex shader layout
        .vertex_input_state = c.SDL_GPUVertexInputState{ 
            .num_vertex_buffers = 1,
            .vertex_buffer_descriptions = &[_]c.SDL_GPUVertexBufferDescription{
                .{
                    .slot = 0,
                    .input_rate = c.SDL_GPU_VERTEXINPUTRATE_VERTEX,
                    .instance_step_rate = 0,
                    .pitch = vertex_size              
                },                
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
                    .format = c.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM,
                    .location = 1,
                    .offset = @sizeOf(f32) * 3,
                },
            },
        },
        .primitive_type = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
        .vertex_shader = vertex_shader,
        .fragment_shader = fragment_shader,
    };

    s.pipeline = c.SDL_CreateGPUGraphicsPipeline(s.device, &pipeline_create_info);
    if (s.pipeline == null) {
        @panic("failed to create pipeline"); 
    }
    
    // create vertex buffer
    s.vertex_buffer = c.SDL_CreateGPUBuffer(
        s.device,
        &c.SDL_GPUBufferCreateInfo{
            .usage = c.SDL_GPU_BUFFERUSAGE_VERTEX,
            .size = vertex_size * vertex_buffer_len,
        }
    );
    if (s.vertex_buffer == null) {
        @panic("CreateVertexBuffer failed");
    }

    // to get data into the vertex buffer, we have to use a transfer buffer
    const transfer_buffer_create_info = c.SDL_GPUTransferBufferCreateInfo{
        .usage = c.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
        .size = 3 * 16
    };
    const transfer_buffer = c.SDL_CreateGPUTransferBuffer(s.device, &transfer_buffer_create_info);

    // map gpu transfer buffer
    // copy the memory with defined (from descriptions layout)
    // *anyopaque --> *void
    const transfer_data_ptr = c.SDL_MapGPUTransferBuffer(s.device, transfer_buffer, false);
    var transfer_data = cast([*]u8, transfer_data_ptr);
    defer c.SDL_UnmapGPUTransferBuffer(s.device, transfer_buffer);

    const N: usize = @intCast(vertex_buffer_len);
    for (0..N) |i| {
        write_ptr(Position, &transfer_data, pos[i]);
        write_ptr(Color, &transfer_data, colors[i]);            
    }

    // upload the transfer data to the vertex buffer
    const upload_cmdbuf = c.SDL_AcquireGPUCommandBuffer(s.device);
    const copy_pass = c.SDL_BeginGPUCopyPass(upload_cmdbuf);

	const transfer_buffer_location = c.SDL_GPUTransferBufferLocation{
	    .transfer_buffer = transfer_buffer,
	    .offset = 0,
	};    	
	const buffer_region = c.SDL_GPUBufferRegion{
	    .buffer = s.vertex_buffer,
	    .offset = 0,
	    .size = (@sizeOf(numerics.Vector3) + @sizeOf(u32)) * vertex_buffer_len,
	};

    c.SDL_UploadToGPUBuffer(copy_pass, &transfer_buffer_location, &buffer_region, false);
    c.SDL_EndGPUCopyPass(copy_pass);
    _ = c.SDL_SubmitGPUCommandBuffer(upload_cmdbuf);
    c.SDL_ReleaseGPUTransferBuffer(s.device, transfer_buffer);
}

export fn update (s:*Context, vertex_len:u32) void
{
    while (s.running) {
        var evt: c.SDL_Event = undefined;
        while (c.SDL_PollEvent(&evt)) 
        {
            if (evt.type == c.SDL_EVENT_QUIT) {
                s.running = false;
            }
            if (!s.running) {
                quit(s);
                break;
            }            
            draw(s, vertex_len); 
        }
    }    
}

export fn quit (s:*Context) void
{
    if (!s.is_disposed) {
        c.SDL_ReleaseGPUGraphicsPipeline(s.device, s.pipeline);
        c.SDL_ReleaseGPUBuffer(s.device, s.vertex_buffer);
    
        c.SDL_ReleaseWindowFromGPUDevice(s.device, s.window);
        c.SDL_DestroyWindow(s.window);
        c.SDL_DestroyGPUDevice(s.device);            
    }
    s.running = false;
    s.is_disposed = true;
}


export fn draw (context:*Context, vertex_len:u32) void 
{
    const cmdbuf = c.SDL_AcquireGPUCommandBuffer(context.device);
    if (cmdbuf == null) {
        @panic("acquireGPUBuffer failed");        
    }
    var swapchain_texture: ?*c.SDL_GPUTexture = null;
    _ = c.SDL_WaitAndAcquireGPUSwapchainTexture(cmdbuf, context.window, &swapchain_texture, null, null);

    if (swapchain_texture != null) {
        const color_target_info = c.SDL_GPUColorTargetInfo{
            .texture = swapchain_texture,
            .clear_color = c.SDL_FColor{.r = 0, .g = 0, .b = 0, .a = 1},
            .load_op = c.SDL_GPU_LOADOP_CLEAR,
            .store_op = c.SDL_GPU_STOREOP_STORE,
        };
        const renderpass = c.SDL_BeginGPURenderPass(cmdbuf, &color_target_info, 1, null);

        c.SDL_BindGPUGraphicsPipeline(renderpass, context.pipeline);
        c.SDL_BindGPUVertexBuffers(renderpass, 0, &.{.buffer = context.vertex_buffer, .offset = 0}, 1);
        c.SDL_DrawGPUPrimitives(renderpass, vertex_len, 1, 0, 0);
        c.SDL_EndGPURenderPass(renderpass);
    }
    
    _ = c.SDL_SubmitGPUCommandBuffer(cmdbuf);
}

