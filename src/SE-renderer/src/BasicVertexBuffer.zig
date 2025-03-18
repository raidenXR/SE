const c = @import("c.zig");
const common = @import("common.zig");
const Context = common.Context;

var pipeline: ?*c.SDL_GPUGraphicsPipeline = undefined;
var vertex_buffer: ?*c.SDL_GPUBuffer = undefined;

pub fn init (context:*Context) bool 
{
    const result = common.commonInit(context, 0);
    if (!result) {
        return result;
    }

    // create the shaders
    const vertex_shader = common.loadShader(context.device, "PositionColor.vert", 0,0,0,0);
    if (vertex_shader == null) {
        @panic("failed to create vertex shader");
    }

    const fragment_shader = common.loadShader(context.device, "SolidColor.frag", 0,0,0,0);
    if (fragment_shader == null) {
        @panic("failed to create fragment shader");
    }


    // create the pipeline 
    const pipeline_create_info = c.SDL_GPUGraphicsPipelineCreateInfo{
        .target_info = .{
            .num_color_targets = 1,
            .color_target_descriptions = &[_]c.SDL_GPUColorTargetDescription{
                c.SDL_GPUColorTargetDescription{.format = c.SDL_GetGPUSwapchainTextureFormat(context.device, context.window)},
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
                    .pitch = @sizeOf(common.PositionColorVertex),                
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

    pipeline = c.SDL_CreateGPUGraphicsPipeline(context.device, &pipeline_create_info);
    if (pipeline == null) {
        @panic("failed to create pipeline"); 
    }

    c.SDL_ReleaseGPUShader(context.device, vertex_shader);
    c.SDL_ReleaseGPUShader(context.device, fragment_shader);

    // create vertex buffer
    vertex_buffer = c.SDL_CreateGPUBuffer(
        context.device,
        &c.SDL_GPUBufferCreateInfo{
            .usage = c.SDL_GPU_BUFFERUSAGE_VERTEX,
            .size = @sizeOf(common.PositionColorVertex) * 3,
        }
    );

    // to get data into the vertex buffer, we have to use a transfer buffer
    const transfer_buffer = c.SDL_CreateGPUTransferBuffer(
        context.device,
        &c.SDL_GPUTransferBufferCreateInfo{
            .usage = c.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            .size = @sizeOf(common.PositionColorVertex) * 3,
        }
    );
    
    var transfer_data: [*]common.PositionColorVertex = @ptrCast(@alignCast(c.SDL_MapGPUTransferBuffer(
        context.device,
        transfer_buffer,
        false,
    )));

    transfer_data[0] = common.PositionColorVertex.init(-1, -1, 0, 255, 0, 0, 255);
    transfer_data[1] = common.PositionColorVertex.init(1, -1, 0, 0, 255, 0, 255);
    transfer_data[2] = common.PositionColorVertex.init(0, 1, 0, 0, 0, 255, 255);

    c.SDL_UnmapGPUTransferBuffer(context.device, transfer_buffer);

    // upload the transfer data to the vertex buffer
    const upload_cmdbuf = c.SDL_AcquireGPUCommandBuffer(context.device);
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
            .size = @sizeOf(common.PositionColorVertex) * 3,
        },
        false,
    );

    c.SDL_EndGPUCopyPass(copy_pass);
    _ = c.SDL_SubmitGPUCommandBuffer(upload_cmdbuf);
    c.SDL_ReleaseGPUTransferBuffer(context.device, transfer_buffer);

    return true;
}

pub fn update (context:*Context) bool 
{
    _ = context;
    return true;
}

pub fn draw (context:*Context) bool
{
    const cmdbuf = c.SDL_AcquireGPUCommandBuffer(context.device);
    if (cmdbuf == null) {
        common.failwith("aqcuireGPUBuffer failed {s}", .{c.SDL_GetError()});
        return false;
    }

    var swapchain_texture: ?*c.SDL_GPUTexture = undefined;
    if (!c.SDL_WaitAndAcquireGPUSwapchainTexture(cmdbuf, context.window, &swapchain_texture, null, null)) {
        common.failwith("waitAndAqcuireSwapchainTexture failed {s}", .{c.SDL_GetError()});
        return false;
    }

    if (swapchain_texture != null) {
        var color_target_info = c.SDL_GPUColorTargetInfo{
            .texture = swapchain_texture,
            .clear_color = c.SDL_FColor{.r = 0.0, .g = 0.0, .b = 0.0, .a = 1.0},
            .load_op = c.SDL_GPU_LOADOP_CLEAR,
            .store_op = c.SDL_GPU_STOREOP_STORE,            
        };

        const renderpass = c.SDL_BeginGPURenderPass(
            cmdbuf,
            &color_target_info,
            1,
            null,
        );

        c.SDL_BindGPUGraphicsPipeline(renderpass, pipeline);
        c.SDL_BindGPUVertexBuffers(renderpass, 0, &c.SDL_GPUBufferBinding{.buffer = vertex_buffer, .offset = 0}, 1);
        c.SDL_DrawGPUPrimitives(renderpass, 3, 1, 0, 0);

        c.SDL_EndGPURenderPass(renderpass);
    }

    _ = c.SDL_SubmitGPUCommandBuffer(cmdbuf);

    return true;
}

pub fn quit (context:*Context) void 
{
    c.SDL_ReleaseGPUGraphicsPipeline(context.device, pipeline);
    c.SDL_ReleaseGPUBuffer(context.device, vertex_buffer);

    common.commonQuit(context);
}

pub const BasicVertexBuffer_Example = common.Example{
    .name = "BasicVertexBuffer",
    .init = init,
    .update = update,
    .draw = draw,
    .quit = quit,    
};
