const std = @import("std");
const dotnet = @import("dotnet");
const c = @import("c.zig");
const r = @import("common.zig");

const string = dotnet.String;
const mat4x4 = dotnet.numerics.mat4x4;
const MeshData = r.MeshData;
const Vertex   = r.Vertex;
const Light    = r.Light;
const Material = r.Material;
const MeshGeometry = r.MeshGeometry;

const assert = std.debug.assert;


// #####################
// shader
// loadShader, loadShaderInfo, ShaderInfo
// #####################

pub const ShaderInfo = struct
{
    samplers: u32,
    storage_textures: u32,
    storage_buffers:  u32,
    uniform_buffers:  u32,
    entry: [:0]const u8,
};

fn loadJson (allocator:std.mem.Allocator, path:[]const u8) []const u8
{
    var buffer: [256]u8 = undefined;
    const idx = dotnet.String.lastIndexOf(path, '.') + 1;
    @memcpy(buffer[0..idx], path[0..idx]);
    @memcpy(buffer[idx..idx + 4], "json");
    const file = buffer[0..idx + 4];

    assert(dotnet.File.exists(file));
    
    const str = dotnet.File.readAll(file, allocator);
    const eof = dotnet.String.indexOf(str, '}');

    return str[0..eof + 1];
}

pub fn loadShaderInfo (path:[]const u8) ShaderInfo
{
    const allocator = std.heap.c_allocator;
    const json = loadJson(allocator, path);  // keep json in memory, because entry references it...
    
    var js = std.json.parseFromSlice(ShaderInfo, allocator, json, .{}) catch @panic("json parser failed");
    defer js.deinit();
    const res = js.value;    
    
    return res;
}

pub fn loadShader (device:?*c.SDL_GPUDevice, path:[]const u8) ?*c.SDL_GPUShader
{    
    const format: u32 = @intCast(c.SDL_GPU_SHADERFORMAT_SPIRV);
    const shader_info = loadShaderInfo(path);

    const stage = blk: {
        if (string.contains(path, "_vs") or string.contains(path, ".vert"))
        {
            break :blk @as(u32, @intCast(c.SDL_GPU_SHADERSTAGE_VERTEX));
        }
        else if (string.contains(path, "_ps") or string.contains(path, ".frag"))
        {
            break :blk @as(u32, @intCast(c.SDL_GPU_SHADERSTAGE_FRAGMENT));
        }
        else
        {
            @panic ("invalid shader stage");
        }       
    };

    var code_size: usize = undefined;
    const code: [*c]u8 = @ptrCast(@alignCast(c.SDL_LoadFile(path.ptr, &code_size)));
    defer c.SDL_free(code);    

    std.debug.print("loaded shader entry: -{s}-\n", .{shader_info.entry.ptr});

    return c.SDL_CreateGPUShader(device, &c.SDL_GPUShaderCreateInfo{
        .code = code,
        .code_size = code_size,
        .format = format,
        .stage = stage,
        .entrypoint = shader_info.entry.ptr,
        .num_samplers = shader_info.samplers,
        .num_uniform_buffers = shader_info.uniform_buffers,
        .num_storage_buffers = shader_info.storage_buffers,
        .num_storage_textures = shader_info.storage_textures,
    });
}

test "test loadShaderInfo" {
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    const allocator = gpa.allocator();
    _ = allocator; // autofix
    
    const ctx = try r.init("some wnd", 800, 600, 0);
    defer r.quit(ctx);
        
    // const shader_info = ShaderInfo{
    //     .samplers = 0,
    //     .storage_textures = 0,
    //     .uniform_buffers = 1,
    //     .storage_buffers = 0,
    //     .entry = "main",
    // };
    // var list = std.ArrayList(u8).init(allocator);
    // defer list.deinit();
    // try std.json.stringify(shader_info, .{.whitespace = .indent_4}, list.writer());
    // std.debug.print("serialize to json\n-{s}-\n", .{list.items});    

    // const str: []const u8 = 
    // \\{
    // \\"samplers": 0,
    // \\"storage_textures": 0,
    // \\"storage_buffers": 0,
    // \\"uniform_buffers": 2,
    // \\"entry": "VS"
    // \\}
    // ;    

    // const js: std.json.Parsed(ShaderInfo) = std.json.parseFromSlice(ShaderInfo, allocator, str, .{}) catch @panic("json parser failed");
    // defer js.deinit();
    // const res = js.value;
    // std.debug.print("deserialized json\nentry:{any}\n", .{res});

    const path = "content/shaders/compiled/color_vs.spv";
    const info = loadShaderInfo(path);
    const shader = loadShader(ctx.device, path);
    std.debug.print("\ndeserialized json: {any}, -- \"{s}\"\n\n", .{info, info.entry});
    defer c.SDL_ReleaseGPUShader(ctx.device, shader);
    assert(shader != null);
}

// #####################
// asset 
// loadPixels, freePixels, loadTextureFile, 
// #####################
pub fn loadPixels (path:[]const u8) struct{[]u8, [2]u32}
{
    const l0, const b = r.basepath();
    const l1, const t = r.concatString(b[0..l0], path);
    _ = l1; // autofix

    var w: c_int = undefined;
    var h: c_int = undefined;
    const pixels_data = c.stbi_load(&t, &w, &h, null, 4);
    const img_size = [2]u32{
        @intCast(w),
        @intCast(h),
    };
    const pixels_byte_size = img_size[0] * img_size[1] * 4;

    return .{pixels_data[0..pixels_byte_size], img_size};
}

pub fn freePixels (pixels:[]u8) void
{
    c.stbi_image_free(pixels.ptr);    
}

test "test loadPixels" {
    const pixels, const size = loadPixels("content/textures/texture1.png");
    defer freePixels(pixels);

    std.debug.print("pixels_w: {}, pixels_h: {}\n", .{size[0], size[1]});
}



pub fn loadTextureFile (copypass:?*c.SDL_GPUCopyPass, path:[]const u8) ?*c.SDL_GPUTexture
{
    const pixels, const img_size = loadPixels(path);
    const texture = uploadTexture(copypass, pixels, img_size[0], img_size[1]);
    freePixels(pixels);

    return texture;
}

pub fn loadCubemapTextureSingle () ?*c.SDL_GPUTexture
{
    noreturn;
}

pub fn loadCubemanpTextureFiles () ?*c.SDL_GPUTexture
{
    noreturn;
}




// #####################
// gpu
// #####################
pub fn uploadTexture (device:?*c.SDL_GPUDevice, copy_pass:?*c.SDL_GPUCopyPass, pixels:[]const u8, w:u32, h:u32) ?*c.SDL_GPUTexture
{
    const tex = c.SDL_CreateGPUTexture(device, c.SDL_GPUTextureCreateInfo{
        .type = c.SDL_GPU_TEXTURETYPE_2D,
        .format = c.SDL_GPU_TEXTUREFORMAT_R8G8B8A8_UNORM_SRGB,  // pixels are in sRGB, converted to linear in shaders
        .usage = c.SDL_GPU_TEXTUREUSAGE_SAMPLER,
        .width = w,
        .height = h,
        .layer_count_or_depth = 1,
        .num_levels = 1,
    });

    const tex_transfer_buffer = c.SDL_CreateGPUTransferBuffer(device, c.SDL_GPUTransferBufferCreateInfo{
        .usage = c.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
        .size = @intCast(pixels.len),        
    });    

    var tex_transfer_mem: [*c]u8 = @ptrCast(@alignCast(c.SDL_MapGPUTransferBuffer(device, tex_transfer_buffer, false)));
    @memcpy(tex_transfer_mem[0..pixels.len], pixels);
    c.SDL_UnmapGPUTransferBuffer(device, tex_transfer_buffer);

    c.SDL_UploadToGPUTexture(
        copy_pass,
        c.SDL_GPUTextureTransferInfo{.transfer_buffer = tex_transfer_buffer},
        c.SDL_GPUTextureRegion{.texture = tex, .w = w, .h = h, .d = 1},
        false
    );

    c.SDL_ReleaseGPUTransferBuffer(device, tex_transfer_buffer);

    return tex;
}

pub fn uploadCubemapTextureSides () ?*c.SDL_GPUTexture
{
    noreturn;
}

pub fn uploadCubemapTextureSingle () ?*c.SDL_GPUTexture
{
    noreturn;
}

pub fn uploadMeshBytes (device:?*c.SDL_GPUDevice, copypass:?c.SDL_GPUCopyPass, vertices:[]u8, indices:[]u8) void
{
    const vertices_byte_size = vertices.len;
    const indices_byte_size  = indices.len; 

    const vertex_buffer = c.SDL_CreateGPUBuffer(device, &c.SDL_GPUBufferCreationInfo{
        .usage = c.SDL_BUFFERUSAGE_VERTEX,
        .size = @intCast(vertices_byte_size),
    });

    const index_buffer = c.SDL_CreateGPUBuffer(device, &c.SDL_GPUBufferCreationInfo{
        .usage = c.SDL_BUFFERUSAGE_INDICES,
        .size = indices_byte_size,
    });

    const transfer_buffer = c.SDL_CreateGPUTransferBuffer(device, &c.SDL_GPUTransferBufferCreateInfo{
        .usage = c.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
        .size = @intCast(vertices_byte_size + indices_byte_size),
    });

    var transfer_mem: [*]u8 = @ptrCast(@alignCast(c.SDL_MapGPUTransferBuffer(device, transfer_buffer, false)));
    @memcpy(transfer_mem, vertices.ptr);
    @memcpy(&transfer_mem[vertices_byte_size], indices.ptr);
    c.SDL_UnmapGPUTransferBuffer(device, transfer_buffer);

    c.SDL_UploadToGPUBuffer(copypass,
        .{.transfer_buffer = transfer_buffer},
        .{.buffer = vertex_buffer, .size = @intCast(vertices_byte_size)},
        false
    );
    
    c.SDL_UploadToGPUBuffer(copypass,
        .{.transfer_buffer = transfer_buffer, .offset = @intCast(vertices_byte_size)},
        .{.buffer = index_buffer, .size = @intCast(indices_byte_size)},
        false
    );

    c.SDL_ReleaseGPUTransferBuffer(device, transfer_buffer);

    // return Mesh{
    //     .vertex_buffer = vertex_buffer,
    //     .index_buffer = index_buffer,
    //     .num_indices = num_indices,
    //     .material = null,
    // };
}

pub fn uploadMesh (copypass:?*c.SDL_GPUCopypass, device:?*c.SDL_GPUDevice, vertices:[]Vertex, indices:[]u32) MeshGeometry
{
    return uploadMeshBytes(device, copypass, std.mem.sliceAsBytes(vertices), std.mem.sliceAsBytes(indices), indices.len);    
}

pub fn wiredPipeline (ctx:r.Context, vs_path:[]const u8, ps_path:[]const u8) ?*c.SDL_GPUGraphicsPipeline
{
    const device = ctx.device;
    const window = ctx.window;

    const vs = loadShader(device, vs_path);
    const ps = loadShader(device, ps_path);
    
    const vertex_attrs = Vertex.attributes(.vertex);    

    const pipeline = c.SDL_CreateGPUGraphicsPipeline(device, &c.SDL_GPUGraphicsPipelineCreateInfo{
        .vertex_shader = vs,
        .fragment_shader = ps,
        .primitive_type = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
        .vertex_input_state = c.SDL_GPUVertexInputState{
            .num_vertex_buffers = 1,
            .vertex_buffer_descriptions = &[_]c.SDL_GPUVertexBufferDescription{
                .{
                    .slot = 0,
                    .input_rate = c.SDL_GPU_VERTEXINPUTRATE_VERTEX,
                    .instance_step_rate = 0,
                    .pitch = Vertex.pitch(.vertex),
                },
            },
            .num_vertex_attributes = @intCast(vertex_attrs.len),
            .vertex_attributes = vertex_attrs.ptr,
        },
        .depth_stencil_state = .{
            .enable_depth_test = true,
            .enable_depth_write = true,
            .compare_op = c.SDL_GPU_COMPAREOP_LESS,
        },
        .rasterizer_state = .{
            .cull_mode = c.SDL_GPU_CULLMODE_BACK,
            .fill_mode = c.SDL_GPU_FILLMODE_LINE,
        },
        .target_info = .{
            .num_color_targets = 1,
            .color_target_descriptions = &[_]c.SDL_GPUColorTargetDescription{
                .{.format = c.SDL_GetGPUSwapchainTextureFormat(device, window)},
            },
        },    
    });

    c.SDL_ReleaseGPUShader(device, vs);
    c.SDL_ReleaseGPUShader(device, ps);

    return pipeline;
}

pub fn coloredPipeline (ctx:r.Context, vs_path:[]const u8, ps_path:[]const u8) ?*c.SDL_GPUGraphicsPipeline
{
    const device = ctx.device;
    const window = ctx.window;

    const vs = loadShader(device, vs_path);
    const ps = loadShader(device, ps_path);
    
    const vertex_attrs = Vertex.attributes(.vertex);    

    const pipeline = c.SDL_CreateGPUGraphicsPipeline(device, &c.SDL_GPUGraphicsPipelineCreateInfo{
        .vertex_shader = vs,
        .fragment_shader = ps,
        .primitive_type = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
        .vertex_input_state = c.SDL_GPUVertexInputState{
            .num_vertex_buffers = 1,
            .vertex_buffer_descriptions = &[_]c.SDL_GPUVertexBufferDescription{
                .{
                    .slot = 0,
                    .input_rate = c.SDL_GPU_VERTEXINPUTRATE_VERTEX,
                    .instance_step_rate = 0,
                    .pitch = Vertex.pitch(.vertex),
                },
            },
            .num_vertex_attributes = @intCast(vertex_attrs.len),
            .vertex_attributes = vertex_attrs.ptr,
        },
        .target_info = .{
            .num_color_targets = 1,
            .color_target_descriptions = &[_]c.SDL_GPUColorTargetDescription{
                .{.format = c.SDL_GetGPUSwapchainTextureFormat(device, window)},
            },
        },    
    });    
    
    c.SDL_ReleaseGPUShader(device, vs);
    c.SDL_ReleaseGPUShader(device, ps);

    return pipeline;
}

pub fn texturedPipeline (ctx:r.Context, vs_path:[]const u8, ps_path:[]const u8) ?*c.SDL_GPUGraphicsPipeline
{
    const device = ctx.device;
    const window = ctx.window;

    const vs = loadShader(device, vs_path);
    const ps = loadShader(device, ps_path);

    const vertex_attrs = Vertex.attributes(.vertex);    

    const pipeline = c.SDL_CreateGPUGraphicsPipeline(device, &c.SDL_GPUGraphicsPipelineCreateInfo{
        .vertex_shader = vs,
        .fragment_shader = ps,
        .primitive_type = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
        .vertex_input_state = c.SDL_GPUVertexInputState{
            .num_vertex_buffers = 1,
            .vertex_buffer_descriptions = &[_]c.SDL_GPUVertexBufferDescription{
                .{
                    .slot = 0,
                    .input_rate = c.SDL_GPU_VERTEXINPUTRATE_VERTEX,
                    .instance_step_rate = 0,
                    .pitch = Vertex.pitch(.vertex),
                },
            },
            .num_vertex_attributes = @intCast(vertex_attrs.len),
            .vertex_attributes = vertex_attrs.ptr,
        },
        .target_info = .{
            .num_color_targets = 1,
            .color_target_descriptions = &[_]c.SDL_GPUColorTargetDescription{
                .{.format = c.SDL_GetGPUSwapchainTextureFormat(device, window)},
            },
        },    
    });    
    
    c.SDL_ReleaseGPUShader(device, vs);
    c.SDL_ReleaseGPUShader(device, ps);

    return pipeline;
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

pub const lights = std.StaticStringMap(Light).initComptime(.{
    .{"default", Light{
        .strength = .{ 0.5, 0.5, 0.5 },
        .fallof_start = 1,
        .direction = .{ 0, -1, 0 },
        .fallof_end = 10,
        .position = .{ 0, 0, 0 },
        .spot_power = 64,
    }},    
});

pub const materials = std.StaticStringMap(Material).initComptime(.{
    .{"default", Material{
        .diffuse_albedo = .{1, 1, 1, 1},
        .fresnel_R0 = .{0.01, 0.01, 0.01},
        .roughness = 0.25,
        .mat_transform = mat4x4.identity, 
    }},    
    .{"brick", Material{
        .diffuse_albedo = .{1, 1, 1, 1},
        .fresnel_R0 = .{0.02, 0.02, 0.02},
        .roughness = 0.1,
        .mat_transform = mat4x4.identity,
    }},
    .{"stone", Material{
        .diffuse_albedo = .{1, 1, 1, 1},
        .fresnel_R0 = .{0.05, 0.05, 0.05},
        .roughness = 0.3,
        .mat_transform = mat4x4.identity,        
    }},
    .{"tile", Material{
        .diffuse_albedo = .{1, 1, 1, 1},
        .fresnel_R0 = .{0.02, 0.02, 0.02},
        .roughness = 0.3,
        .mat_transform = mat4x4.identity,                
    }},    
});


test "test static string maps" {
    const light0 = lights.get("default");
    if (light0) |l| std.debug.print("{any}\n", .{l});
    
    const mat0 = materials.get("default");
    _ = mat0; // autofix
    const mat1 = materials.get("tile");
    if (mat1) |m| std.debug.print("{any}\n", .{m});
}

