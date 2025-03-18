const c = @import("c.zig");
const common = @import("common.zig");
const numerics = @import("numerics.zig");
const Context = common.Context;

var pipeline: ?*c.SDL_GPUGraphicsPipeline = null;
var vertex_buffer: ?*c.SDL_GPUBuffer = null;
var index_buffer: ?*c.SDL_GPUBuffer = null;
var source_texture: ?*c.SDL_GPUTexture = null;
var destination_texture: ?*c.SDL_GPUTexture = null;
var sampler: ?*c.SDL_GPUSampler = null;

var cam_pos = numerics.Vector3{0,0,4};

pub fn init (context:*Context) bool
{
    const result = common.commonInit(context, 0);
    if (!result) {
        return result;
    }

    // create the shaders
    const vertex_shader = common.loadShader(context.device, "Skybox.vert", 0,1,0,0);
    if (vertex_shader == null) {
        @panic("failed to create vertex shader");
    }

    const fragment_shader = common.loadShader(context.device, "Skybox.frag", 1, 0, 0,0);
    if (fragment_shader == null) {
        @panic("failed to create fragment shader");
    }

    var pipeline_create_info = c.SDL_GPUGraphicsPipelineCreateInfo{
        .target_info = .{
            .num_color_targets = 1,
            .color_target_descriptions = &[_]c.SDL_GPUColorTargetDescription{
                .{.format = c.SDL_GetGPUSwapchainTextureFormat(context.device, context.window)},
            }
        },
        .vertex_input_state = c.SDL_GPUVertexInputState{
            .num_vertex_buffers = 1,
            .vertex_buffer_descriptions = &[_]c.SDL_GPUVertexBufferDescription{
                .{
                    .slot = 0,
                    .input_rate = c.SDL_GPU_VERTEXINPUTRATE_VERTEX,
                    .instance_step_rate = 0,
                    .pitch = @sizeOf(common.PositionVertex),
                },                    
            },
            .num_vertex_attributes = 1,
            .vertex_attributes = &[_]c.SDL_GPUVertexAttribute{
                .{
                    .buffer_slot = 0,
                    .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
                    .location = 0,
                    .offset = 0,
                },
            },                
        },
        .primitive_type = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
        .vertex_shader = vertex_shader,
        .fragment_shader = fragment_shader,
    };

    pipeline = c.SDL_CreateGPUGraphicsPipeline(context.device, &pipeline_create_info);
    c.SDL_ReleaseGPUShader(context.device, vertex_shader);
    c.SDL_ReleaseGPUShader(context.device, fragment_shader);

    // create the GPU resources
    vertex_buffer = c.SDL_CreateGPUBuffer(
        context.device,
        &c.SDL_GPUBufferCreateInfo{
            .usage = c.SDL_GPU_BUFFERUSAGE_VERTEX,
            .size = @sizeOf(common.PositionVertex) * 24,
        }
    );

    index_buffer = c.SDL_CreateGPUBuffer(
        context.device,
        &c.SDL_GPUBufferCreateInfo{
            .usage = c.SDL_GPU_BUFFERUSAGE_INDEX,
            .size = @sizeOf(u16) * 36,
        }
    );

    source_texture = c.SDL_CreateGPUTexture(context.device, &c.SDL_GPUTextureCreateInfo{
        .format = c.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
        .type = c.SDL_GPU_TEXTURETYPE_CUBE,
        .width = 32,
        .height = 32,
        .layer_count_or_depth = 6,
        .num_levels = 1,
        .usage = c.SDL_GPU_TEXTUREUSAGE_SAMPLER | c.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET
    });

    destination_texture = c.SDL_CreateGPUTexture(context.device, &c.SDL_GPUTextureCreateInfo{
        .format = c.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM,
        .type = c.SDL_GPU_TEXTURETYPE_CUBE,
        .width = 32,
        .height = 32,
        .layer_count_or_depth = 6,
        .num_levels = 1,
        .usage = c.SDL_GPU_TEXTUREUSAGE_SAMPLER | c.SDL_GPU_TEXTUREUSAGE_COLOR_TARGET        
    });

    sampler = c.SDL_CreateGPUSampler(context.device, &c.SDL_GPUSamplerCreateInfo{
        .min_filter = c.SDL_GPU_FILTER_NEAREST,
        .mag_filter = c.SDL_GPU_FILTER_NEAREST,
        .mipmap_mode = c.SDL_GPU_SAMPLERMIPMAPMODE_NEAREST,
        .address_mode_u = c.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
        .address_mode_v = c.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
        .address_mode_w = c.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
    });

    // set up buffer data
    const buffer_transfer_buffer = c.SDL_CreateGPUTransferBuffer(
        context.device,
        &c.SDL_GPUTransferBufferCreateInfo{
            .usage = c.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            .size = (@sizeOf(common.PositionVertex) * 24) + (@sizeOf(u16) * 36)
        }
    );

    var buffer_transfer_data: [*]common.PositionVertex = @ptrCast(@alignCast(c.SDL_MapGPUTransferBuffer(
        context.device,
        buffer_transfer_buffer,
        false
    )));

    buffer_transfer_data[0] = common.PositionVertex.init(-10, -10, -10);
    buffer_transfer_data[1] = common.PositionVertex.init(10, -10, -10);
    buffer_transfer_data[2] = common.PositionVertex.init(10, 10, -10);
    buffer_transfer_data[3] = common.PositionVertex.init(-10, 10, -10);
    
    buffer_transfer_data[4] = common.PositionVertex.init(-10, -10, 10);
    buffer_transfer_data[5] = common.PositionVertex.init(10, -10, 10);
    buffer_transfer_data[6] = common.PositionVertex.init(10, 10, 10);
    buffer_transfer_data[7] = common.PositionVertex.init(-10, 10, 10);

    buffer_transfer_data[8] = common.PositionVertex.init(-10, -10, -10);
    buffer_transfer_data[9] = common.PositionVertex.init(-10, 10, -10);
    buffer_transfer_data[10] = common.PositionVertex.init(-10, 10, 10);
    buffer_transfer_data[11] = common.PositionVertex.init(-10, -10, 10);

    buffer_transfer_data[12] = common.PositionVertex.init(10, -10, -10);
    buffer_transfer_data[13] = common.PositionVertex.init(10, 10, -10);
    buffer_transfer_data[14] = common.PositionVertex.init(10, 10, 10);
    buffer_transfer_data[15] = common.PositionVertex.init(10, -10, 10);

    buffer_transfer_data[16] = common.PositionVertex.init(-10, -10, -10);
    buffer_transfer_data[17] = common.PositionVertex.init(-10, -10, 10);
    buffer_transfer_data[18] = common.PositionVertex.init(10, -10, 10);
    buffer_transfer_data[19] = common.PositionVertex.init(10, -10, -10);

    buffer_transfer_data[20] = common.PositionVertex.init(-10, 10, -10);
    buffer_transfer_data[21] = common.PositionVertex.init(-10, 10, 10);
    buffer_transfer_data[22] = common.PositionVertex.init(10, 10, 10);
    buffer_transfer_data[23] = common.PositionVertex.init(10, 10, -10);

    const index_data: [*]u16 = @ptrCast(@alignCast(&buffer_transfer_data[24]));
    const indices = [_]u16{
        0,1,2,0,2,3,
        6,5,4,7,6,4,
        8,9,10,8,10,11,
        14,13,12,15,14,12,
        16,17,18,16,18,19,
        22,21,20,23,22,20,
    };

    _ = c.SDL_memcpy(@ptrCast(index_data), @ptrCast(&indices), indices.len);

    c.SDL_UnmapGPUTransferBuffer(context.device, buffer_transfer_buffer);

    // set up texture data
    const bytes_per_image = 32 * 32 * 4;
    const texture_transfer_buffer = c.SDL_CreateGPUTransferBuffer(
        context.device,
        &c.SDL_GPUTransferBufferCreateInfo{
            .usage = c.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            .size = bytes_per_image * 6,
        }
    );

    const texture_transfer_data: [*]u8 = @ptrCast(@alignCast(c.SDL_MapGPUTransferBuffer(
        context.device,
        texture_transfer_buffer,
        false
    )));

    const image_names = [_][]const u8 {
        "cube0.bmp", "cube1.bmp", "cube2.bmp",
        "cube3.bmp", "cube4.bmp", "cube5.bmp",
    };

    for (image_names, 0..) |image, I| {
        const i: u32 = @intCast(I);
        const image_data_ = common.loadImage(image, 4);
        if (image_data_) |image_data| {
             _ = c.SDL_memcpy(texture_transfer_data + (bytes_per_image * i), image_data.pixels, bytes_per_image);
            c.SDL_DestroySurface(image_data);            
        }
        else {
            @panic("could not load image data");            
        }
    }
    c.SDL_UnmapGPUTransferBuffer(context.device, texture_transfer_buffer);

    // unload the transfer data to the GPU buffers
    const cmdbuf = c.SDL_AcquireGPUCommandBuffer(context.device);
    const copy_pass = c.SDL_BeginGPUCopyPass(cmdbuf);

    c.SDL_UploadToGPUBuffer(
        copy_pass,
        &c.SDL_GPUTransferBufferLocation{
            .transfer_buffer = buffer_transfer_buffer,
            .offset = 0,
        },
        &c.SDL_GPUBufferRegion{
            .buffer = vertex_buffer,
            .offset = 0,
            .size = @sizeOf(common.PositionVertex) * 24,
        },
        false
    );

    c.SDL_UploadToGPUBuffer(
        copy_pass,
        &c.SDL_GPUTransferBufferLocation{
            .transfer_buffer = buffer_transfer_buffer,
            .offset = @sizeOf(common.PositionVertex) * 24,
        },
        &c.SDL_GPUBufferRegion{
            .buffer = index_buffer,
            .offset = 0,
            .size = @sizeOf(u16) * 36,
        },
        false
    );

    for (0..6) |I| {
        const i: u32 = @intCast(I);
        c.SDL_UploadToGPUTexture(
            copy_pass,
            &c.SDL_GPUTextureTransferInfo{
                .transfer_buffer = texture_transfer_buffer,
                .offset = bytes_per_image * i,
            },
            &c.SDL_GPUTextureRegion{
                .texture = source_texture,
                .layer = i,
                .w = 32,
                .h = 32,
                .d = 1,
            },
            false
        );
    }

    c.SDL_EndGPUCopyPass(copy_pass);

    // blit to destination texture
    // this serves no real purpose other that demostrating cube-cube blits are possible
    for (0..6) |I| {
        const i: u32 = @intCast(I);
        c.SDL_BlitGPUTexture(
            cmdbuf,
            &c.SDL_GPUBlitInfo{
                .source = .{
                    .texture = source_texture, 
                    .layer_or_depth_plane = i,
                    .w = 32,
                    .h = 32,
                },
                .destination = .{
                    .texture = destination_texture,
                    .layer_or_depth_plane = i,
                    .w = 32,
                    .h = 32,
                },
                .load_op = c.SDL_GPU_LOADOP_DONT_CARE,
                .filter = c.SDL_GPU_FILTER_LINEAR,
            }
        );
    }

    c.SDL_ReleaseGPUTransferBuffer(context.device, buffer_transfer_buffer);
    c.SDL_ReleaseGPUTransferBuffer(context.device, texture_transfer_buffer);

    _ = c.SDL_SubmitGPUCommandBuffer(cmdbuf);

    return true;
}

pub fn update (context:*Context) bool
{
    if (context.left_pressed or context.right_pressed) {
        cam_pos[2] *= -1;
    }

    return true;
}

pub fn draw (context:*Context) bool
{
    const cmdbuf = c.SDL_AcquireGPUCommandBuffer(context.device);
    if (cmdbuf == null) {
        common.failwith("acquireGPUCommandBuffer failed {s}\n", .{c.SDL_GetError()});
    }

    var swapchain_texture:?*c.SDL_GPUTexture = null;
    if (!c.SDL_WaitAndAcquireGPUSwapchainTexture(cmdbuf, context.window, &swapchain_texture, null, null)) {
        common.failwith("waitandAcquireGPUSwapchainTexture failed: {s}\n", .{c.SDL_GetError()});
    }

    if (swapchain_texture != null) {
        const proj = numerics.mat4x4.createPerspectiveFieldOfView(75.0 * c.SDL_PI_F / 180.0, 640.0 / 480.0, 0.01, 100);

        const view = numerics.mat4x4.createLookAt(cam_pos, numerics.Vector3{0,0,0}, numerics.Vector3{0,1,0});

        const viewproj = numerics.mat4x4.multiply(view, proj);

        const color_target_info = c.SDL_GPUColorTargetInfo{
            .texture = swapchain_texture,
            .clear_color = c.SDL_FColor{.r = 0.0, .g = 0.0, .b = 0.0, .a = 1.0},
            .load_op = c.SDL_GPU_LOADOP_CLEAR,
            .store_op = c.SDL_GPU_STOREOP_STORE,
        };

        const render_pass = c.SDL_BeginGPURenderPass(cmdbuf, &color_target_info, 1, null);

        c.SDL_BindGPUGraphicsPipeline(render_pass, pipeline);
        c.SDL_BindGPUVertexBuffers(render_pass, 0, &c.SDL_GPUBufferBinding{.buffer = vertex_buffer, .offset = 0}, 1);
        c.SDL_BindGPUIndexBuffer(render_pass, &c.SDL_GPUBufferBinding{.buffer = index_buffer, .offset = 0}, c.SDL_GPU_INDEXELEMENTSIZE_16BIT);
        c.SDL_PushGPUVertexUniformData(cmdbuf, 0, @ptrCast(&viewproj), viewproj.len);
        c.SDL_DrawGPUIndexedPrimitives(render_pass, 36, 1, 0, 0,0);

        c.SDL_EndGPURenderPass(render_pass);        
    }
    
    _ = c.SDL_SubmitGPUCommandBuffer(cmdbuf);

    return true;
}

pub fn quit (context:*Context) void
{
    c.SDL_ReleaseGPUGraphicsPipeline(context.device, pipeline);
    c.SDL_ReleaseGPUBuffer(context.device, vertex_buffer);
    c.SDL_ReleaseGPUBuffer(context.device, index_buffer);
    c.SDL_ReleaseGPUTexture(context.device, source_texture);
    c.SDL_ReleaseGPUTexture(context.device, destination_texture);
    c.SDL_ReleaseGPUSampler(context.device, sampler);

    cam_pos[2] = c.SDL_fabsf(cam_pos[2]);

    common.commonQuit(context);
}

pub const BlitCube_Example = common.Example{
    .name = "BlitCube",
    .init = init,
    .update = update,
    .draw = draw,
    .quit = quit,
};
