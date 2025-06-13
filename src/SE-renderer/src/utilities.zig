const std = @import("std");
const dotnet = @import("dotnet");
const numerics = dotnet.numerics;
const string = dotnet.String;
const c = @import("c.zig");
const GeometryGenerator = @import("GeometryGenerator.zig");
const Camera = @import("Camera.zig");


pub const Context = struct
{
    device: ?*c.SDL_GPUDevice,
    window: ?*c.SDL_Window,        
};

/// creates a gpu_device and a window 
pub fn init (wnd_name:[*c]const u8, w:u32, h:u32, window_flags:c.SDL_WindowFlags) !Context 
{
    _ = c.SDL_Init( c.SDL_INIT_VIDEO );
    
    const device = c.SDL_CreateGPUDevice(
        c.SDL_GPU_SHADERFORMAT_SPIRV | c.SDL_GPU_SHADERFORMAT_DXIL | c.SDL_GPU_SHADERFORMAT_MSL,
        false,
        null);

    if (device == null) return error.DeviceFailed;

    const wnd_w: c_int = @intCast(w);
    const wnd_h: c_int = @intCast(h);
    const window = c.SDL_CreateWindow(wnd_name, wnd_w, wnd_h, window_flags);

    if (window == null) return error.WindowFailed;
    
    if(!c.SDL_ClaimWindowForGPUDevice(device, window)) return error.ClaimWindowFailed;

    return Context{
        .device = device,
        .window = window,
    };
}


pub fn quit (ctx:Context) void
{
    c.SDL_ReleaseWindowFromGPUDevice(ctx.device, ctx.window);
    c.SDL_DestroyWindow(ctx.window);
    c.SDL_DestroyGPUDevice(ctx.device);    
}

pub fn getBasepath () struct{[256:0]u8, usize}
{
    var bpath_buffer: [256:0]u8 = undefined;
    @memset (&bpath_buffer, '\x00');
    var bpath_slice  = std.fs.cwd().realpath(".", &bpath_buffer) catch @panic("basepath_buffer overflown");
    bpath_slice.len += 1;
    bpath_slice[bpath_slice.len - 1] = '/';

    @memset(bpath_buffer[bpath_slice.len..], '\x00');
    
    return .{bpath_buffer, bpath_slice.len};
}

fn getFullpath (filename:[]const u8) struct{[*c]const u8, [1024:0]u8}
{
    var basepath_slice, const bpath_len = getBasepath();
    const basepath = basepath_slice[0..bpath_len];

    var fullpath_buffer: [1024:0]u8   = undefined;
    fullpath_buffer[1021] = '\x00';
    const fullpath = std.fmt.bufPrintZ(&fullpath_buffer, "{s}{s}", .{basepath, filename}) catch @panic ("buffprintZ failed");
    
    return .{fullpath.ptr, fullpath_buffer};
}

test "test getBasepath" {
    const b, const l = getBasepath();
    std.debug.print("getbasepath: -{s}-\n", .{b[0..l]});
}

pub fn loadShader (
    device: ?*c.SDL_GPUDevice, 
    shader_file_path: []const u8,
    entry_point: [*c]const u8,
    sampler_count: u32,
    uniform_buffer_count: u32,
    storage_buffer_count: u32,
    storage_texture_count: u32,
    ) ?*c.SDL_GPUShader 
{    
    const stage = blk: {
        if (string.contains(shader_file_path, "_vs") or string.contains(shader_file_path, ".vert"))
        {
            break :blk c.SDL_GPU_SHADERSTAGE_VERTEX;
        }
        else if (string.contains(shader_file_path, "_ps") or string.contains(shader_file_path, ".frag"))
        {
            break :blk c.SDL_GPU_SHADERSTAGE_FRAGMENT;
        }
        else
        {
            @panic ("invalid shader stage");
        }       
    };

    const fullpath, _ = getFullpath(shader_file_path);
    const format   = c.SDL_GPU_SHADERFORMAT_SPIRV;   

    var code_size: usize = undefined;
    const code = c.SDL_LoadFile(fullpath, &code_size);
    defer c.SDL_free(code);
    errdefer c.SDL_free(code);
    
    if (code == null) {
        @panic ("failed to load shader from disk: {s}\n");
    }

    const shader_info = c.SDL_GPUShaderCreateInfo{
        .code = @ptrCast(code),
        .code_size = code_size,
        .entrypoint = entry_point,
        .format = @intCast(format),
        .stage = @intCast(stage),
        .num_samplers = sampler_count,
        .num_uniform_buffers = uniform_buffer_count,
        .num_storage_buffers = storage_buffer_count,
        .num_storage_textures = storage_texture_count,
    };

    const shader = c.SDL_CreateGPUShader(device, &shader_info);
    if (shader == null) {
        std.debug.print("failed to create shader\n", .{});
        c.SDL_free(code);
        return null;
    }
    
    return shader;
}


pub fn loadImage (image_filepath:[]const u8) ?*c.SDL_Surface
{
    const fullpath, _ = getFullpath(image_filepath);   
        
    const result = c.SDL_LoadBMP(fullpath);
    // var pixel_format = c.SDL_PIXELFORMAT_ARGB8888;

    return result;
}

/// creates the Shaders & Pipeline
pub fn createPipeline (ctx:Context, vs:?*c.SDL_GPUShader, ps:?*c.SDL_GPUShader, gs:?*c.SDL_GPUShader, cs:?*c.SDL_GPUShader) ?*c.SDL_GPUGraphicsPipeline
{
    const device = ctx.device;
    const window = ctx.window;
    // const vertex_shader = loadShader( device, vertex_shader_name, 0, 1, 0, 0);
    // const fragment_shader = loadShader( device, fragment_shader_name, 0, 1, 0, 0);
    
    var pipeline_info = c.SDL_GPUGraphicsPipelineCreateInfo{
        .target_info = .{
            .num_color_targets = 1,
            .color_target_descriptions = &[_]c.SDL_GPUColorTargetDescription{
                .{
                    .format = c.SDL_GetGPUSwapchainTextureFormat(device, window),
                    .blend_state = c.SDL_GPUColorTargetBlendState{
                        .enable_blend = true,
                        .src_color_blendfactor = c.SDL_GPU_BLENDFACTOR_ONE,
                        .dst_color_blendfactor = c.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_COLOR,
                        .color_blend_op = c.SDL_GPU_BLENDOP_ADD,
                        .src_alpha_blendfactor = c.SDL_GPU_BLENDFACTOR_ONE,
                        .dst_alpha_blendfactor = c.SDL_GPU_BLENDFACTOR_ONE_MINUS_SRC_ALPHA,
                        .alpha_blend_op = c.SDL_GPU_BLENDOP_ADD,
                    }
                },
            }
        },
        .vertex_input_state = c.SDL_GPUVertexInputState{
            .num_vertex_buffers = 1,
            .vertex_buffer_descriptions = &[_]c.SDL_GPUVertexBufferDescription{
                .{
                    .slot = 0,
                    .input_rate = c.SDL_GPU_VERTEXINPUTRATE_VERTEX,
                    .instance_step_rate = 0,
                    .pitch = @sizeOf(GeometryGenerator.Vertex),
                }
            },
            .num_vertex_attributes = 4,
            .vertex_attributes = &[_]c.SDL_GPUVertexAttribute{
                .{
                    .buffer_slot = 0,
                    .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
                    .location = 0,
                    .offset = 0,
                },
                .{
                    .buffer_slot = 0,
                    .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
                    .location = 1,
                    .offset = @sizeOf([3]f32),
                },
                .{
                    .buffer_slot = 0,
                    .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
                    .location = 2,
                    .offset = @sizeOf([3]f32),
                },
                .{
                    .buffer_slot = 0,
                    .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
                    .location = 3,
                    .offset = @sizeOf([2]f32),
                },                
            }            
        },
        .primitive_type = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
        .vertex_shader = vs,
        .fragment_shader = ps,
    };

    if (vs != null) c.SDL_ReleaseGPUShader( device, vs );
    if (ps != null) c.SDL_ReleaseGPUShader( device, ps );
    if (cs != null) c.SDL_ReleaseGPUShader( device, cs );
    if (gs != null) c.SDL_ReleaseGPUShader( device, gs );

    return c.SDL_CreateGPUGraphicsPipeline(device, &pipeline_info);
}

pub fn getSize (comptime T: type, slice: []T) u32
{
    const a: usize = @sizeOf(T);
    const b = slice.len;

    return @as(u32, @intCast(a * b));
}


/// Create & Upload Scene Index and Vertex Buffers
pub fn createMeshGeometryBuffers (device: ?*c.SDL_GPUDevice, mesh_geometry:*GeometryGenerator.MeshGeometry, copy_pass:?*c.SDL_GPUCopyPass) void
{
    const vertices = mesh_geometry.vertex_buffer_cpu.items;
    const indices  = mesh_geometry.index_buffer_cpu.items;

    const vertices_size = getSize(GeometryGenerator.Vertex, vertices);
    const indices_size  = getSize(u32, indices);
    const total_size    = vertices_size + indices_size;
        
    const vertex_buffer = c.SDL_CreateGPUBuffer( 
        device, 
        &c.SDL_GPUBufferCreateInfo{
            .usage = c.SDL_GPU_BUFFERUSAGE_VERTEX,
            .size = vertices_size,
        }
    );

    const index_buffer = c.SDL_CreateGPUBuffer(
        device,
        &c.SDL_GPUBufferCreateInfo{
            .usage = c.SDL_GPU_BUFFERUSAGE_INDEX,
            .size = indices_size,
        }
    );

    const buffer_transfer_buffer = c.SDL_CreateGPUTransferBuffer(
        device, 
        &c.SDL_GPUTransferBufferCreateInfo{
            .usage = c.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            .size = total_size,
        }
    );

    const transfer_data_ptr = c.SDL_MapGPUTransferBuffer(device, buffer_transfer_buffer, false);
    var transfer_buffer_ptr: [*]u8 = @ptrCast(@alignCast(transfer_data_ptr));

    var vertex_data: [*]GeometryGenerator.Vertex = @ptrCast(@alignCast(transfer_buffer_ptr));
    for (vertices, 0..) |v, n|
    {
        vertex_data[n] = v;
    }

    transfer_buffer_ptr += vertices_size;

    var index_data: [*]u32 = @ptrCast(@alignCast(transfer_data_ptr));
    for (indices, 0..) |i, n|
    {
        index_data[n] = i;
    }

    c.SDL_UnmapGPUTransferBuffer( device, buffer_transfer_buffer);
    
    // upload the transfer data to the GPU buffers
    // const upload_cmd_buffer = c.SDL_AcquireGPUCommandBuffer( device );
    // const copy_pass = c.SDL_BeginGPUCopyPass( upload_cmd_buffer );

    c.SDL_UploadToGPUBuffer(
        copy_pass,
        &c.SDL_GPUTransferBufferLocation{
            .transfer_buffer = buffer_transfer_buffer,
            .offset = 0,
        },
        &c.SDL_GPUBufferRegion{
            .buffer = vertex_buffer,
            .offset = 0,
            .size = vertices_size,
        },
        false
    );

    c.SDL_UploadToGPUBuffer(
        copy_pass,
        &c.SDL_GPUTransferBufferLocation{
            .transfer_buffer = buffer_transfer_buffer,
            .offset = vertices_size,
        },
        &c.SDL_GPUBufferRegion{
            .buffer = index_buffer,
            .offset = 0,
            .size = indices_size,
        },
        false
    );


    // c.SDL_EndGPUCopyPass( copy_pass );
    // _ = c.SDL_SubmitGPUCommandBuffer( upload_cmd_buffer );

    mesh_geometry.vertex_buffer_gpu = vertex_buffer;
    mesh_geometry.index_buffer_gpu = index_buffer;
    mesh_geometry.transfer_buffer = buffer_transfer_buffer;
    mesh_geometry.total_indices_len += indices_size;
}


pub fn uploadTexture (device: ?*c.SDL_GPUDevice, texture:?*c.SDL_GPUTexture, surface:?*c.SDL_Surface, copy_pass:?*c.SDL_GPUCopyPass) void
{
    const w: u32 = @intCast(surface.?.w);
    const h: u32 = @intCast(surface.?.h);
    const s: u32 = (w * h * 4);
    
    const texture_transfer_buffer = c.SDL_CreateGPUTransferBuffer(
        device,
        &c.SDL_GPUTransferBufferCreateInfo{
            .usage = c.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            .size = s,            
        }
    );

    const texture_transfer_data  = c.SDL_MapGPUTransferBuffer(device, texture_transfer_buffer, false);
    var texture_tranfer_ptr: [*]u8 = @ptrCast(@alignCast(texture_transfer_data));

    const pixels: [*]u8 = @ptrCast(@alignCast(surface.?.pixels)); 
    @memcpy(texture_tranfer_ptr[0..s], pixels[0..s]);
    c.SDL_UnmapGPUTransferBuffer( device, texture_transfer_buffer );
    
    c.SDL_UploadToGPUTexture(
        copy_pass,
        &c.SDL_GPUTextureTransferInfo{
            .transfer_buffer = texture_transfer_buffer,
            .offset = 0,
        },
        &c.SDL_GPUTextureRegion{
            .texture = texture,
            .w = w,
            .h = h,
            .d = 1,
        },
        false
    );

    c.SDL_ReleaseGPUTransferBuffer( device, texture_transfer_buffer );
}


pub fn createSamplers (device:?*c.SDL_GPUDevice) [6]?*c.SDL_GPUSampler
{
    var samplers: [6]?*c.SDL_GPUSampler = undefined;

    // PointClamp
    samplers[0] = c.SDL_CreateGPUSampler(device, &c.SDL_GPUSamplerCreateInfo{
        .min_filter  = c.SDL_GPU_FILTER_NEAREST,
        .mag_filter  = c.SDL_GPU_FILTER_NEAREST,
        .mipmap_mode = c.SDL_GPU_SAMPLERMIPMAPMODE_NEAREST,
        .address_mode_u = c.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
        .address_mode_v = c.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
        .address_mode_w = c.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
    });

    // PointWrap
    samplers[1] = c.SDL_CreateGPUSampler(device, &c.SDL_GPUSamplerCreateInfo{
		.min_filter  = c.SDL_GPU_FILTER_NEAREST,
		.mag_filter  = c.SDL_GPU_FILTER_NEAREST,
		.mipmap_mode = c.SDL_GPU_SAMPLERMIPMAPMODE_NEAREST,
		.address_mode_u = c.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
		.address_mode_v = c.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
		.address_mode_w = c.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,    
    });

    // LinearClamp
    samplers[2] = c.SDL_CreateGPUSampler(device, &c.SDL_GPUSamplerCreateInfo{
		.min_filter  = c.SDL_GPU_FILTER_LINEAR,
		.mag_filter  = c.SDL_GPU_FILTER_LINEAR,
		.mipmap_mode = c.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR,
		.address_mode_u = c.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
		.address_mode_v = c.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
		.address_mode_w = c.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,    
    });

    // LinearWrap
    samplers[3] = c.SDL_CreateGPUSampler(device, &c.SDL_GPUSamplerCreateInfo{
        .min_filter  = c.SDL_GPU_FILTER_LINEAR,
		.mag_filter  = c.SDL_GPU_FILTER_LINEAR,
		.mipmap_mode = c.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR,
		.address_mode_u = c.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
		.address_mode_v = c.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
		.address_mode_w = c.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
    });
    
    // AnisotropicClamp
    samplers[4] = c.SDL_CreateGPUSampler(device, &c.SDL_GPUSamplerCreateInfo{
        .min_filter  = c.SDL_GPU_FILTER_LINEAR,
		.mag_filter  = c.SDL_GPU_FILTER_LINEAR,
		.mipmap_mode = c.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR,
		.address_mode_u = c.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
		.address_mode_v = c.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
		.address_mode_w = c.SDL_GPU_SAMPLERADDRESSMODE_CLAMP_TO_EDGE,
		.enable_anisotropy = true,
		.max_anisotropy = 4
    });
    
    // AnisotropicWrap
    samplers[5] = c.SDL_CreateGPUSampler(device, &c.SDL_GPUSamplerCreateInfo{
        .min_filter  = c.SDL_GPU_FILTER_LINEAR,
		.mag_filter  = c.SDL_GPU_FILTER_LINEAR,
		.mipmap_mode = c.SDL_GPU_SAMPLERMIPMAPMODE_LINEAR,
		.address_mode_u = c.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
		.address_mode_v = c.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
		.address_mode_w = c.SDL_GPU_SAMPLERADDRESSMODE_REPEAT,
		.enable_anisotropy = true,
		.max_anisotropy = 4
    });
    
    return samplers;
}


pub fn draw (device: ?*c.SDL_GPUDevice, window: ?*c.SDL_Window, mesh_geometry: GeometryGenerator.MeshGeometry, background_color: [4]f32) void
{
    const cmdbuf = c.SDL_AcquireGPUCommandBuffer( device );
    
    var swapchain_texture: ?*c.SDL_GPUTexture = null;
    if (!c.SDL_WaitAndAcquireGPUSwapchainTexture( cmdbuf, window, &swapchain_texture, null, null)) 
    {
        @panic ("WaitAndAcquiredGPUSwaptexture failed\n");
    }

    const color = c.SDL_FColor{
        .r = background_color[0],
        .g = background_color[1],
        .b = background_color[2],
        .a = background_color[3],
    };

    if (swapchain_texture != null)
    {
        // var camera = Camera{};

        var color_target_info = c.SDL_GPUColorTargetInfo{
            .texture = swapchain_texture,
            .clear_color = color,
            .load_op = c.SDL_GPU_LOADOP_CLEAR,
            .store_op = c.SDL_GPU_STOREOP_STORE,
        };

        const renderpass = c.SDL_BeginGPURenderPass( cmdbuf, &color_target_info, 1, null );

        c.SDL_BindGPUVertexBuffers( renderpass, 0, &c.SDL_GPUBufferBinding{.buffer = mesh_geometry.vertex_buffer_gpu, .offset = 0}, 1);
        c.SDL_BindGPUIndexBuffer( renderpass, &c.SDL_GPUBufferBinding{.buffer = mesh_geometry.index_buffer_gpu, .offset = 0}, c.SDL_GPU_INDEXELEMENTSIZE_32BIT);
        c.SDL_BindGPUGraphicsPipeline( renderpass, mesh_geometry.pipeline );
        c.SDL_DrawGPUIndexedPrimitives( renderpass, mesh_geometry.total_indices_len, 1, 0, 0, 0);
        c.SDL_EndGPURenderPass( renderpass );
    }

    _ = c.SDL_SubmitGPUCommandBuffer( cmdbuf );
}
