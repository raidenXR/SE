const c = @import("c.zig");
const std = @import("std");

pub const Context = struct
{
    example_name: []const u8,
    base_path: []const u8,
    window: ?*c.SDL_Window,
    device: ?*c.SDL_GPUDevice,
    left_pressed: bool,
    right_pressed: bool,
    down_pressed: bool,
    up_pressed: bool,
    delta_time: f32,
};

pub const PositionVertex = struct
{
    x: f32,
    y: f32,
    z: f32,

    pub fn init (x:f32, y:f32, z:f32) PositionVertex
    {
        return PositionVertex{.x = x, .y = y, .z = z};
    }
};

pub const PositionColorVertex = struct
{
    x: f32,
    y: f32,
    z: f32,
    r: u8,
    g: u8,
    b: u8,
    a: u8,

    pub fn init (x:f32, y:f32, z:f32, r:u8, g:u8, b:u8, a:u8) PositionColorVertex 
    {
        return PositionColorVertex{.x = x, .y = y, .z = z, .r = r, .g = g, .b = b, .a = a};
    }
};

pub const PositionTextureVertex = struct
{
    x: f32,
    y: f32,
    z: f32,
    u: f32,
    v: f32,
};


pub const Example = struct
{
    name: [:0]const u8,
    init: *const fn(*Context) bool,
    update: *const fn(*Context) bool,
    draw: *const fn(*Context) bool,
    quit: *const fn(*Context) void,
};

pub fn failwith (comptime fmt:[]const u8, args:anytype) noreturn 
{
    var buffer: [512]u8 = undefined;
    const msg = std.fmt.bufPrintZ(&buffer, fmt, args) catch "failwith buffer overflown\n";
    std.debug.print("{s}", .{msg});
    unreachable;
}

pub fn commonInit (context:*Context, windowFlags:c.SDL_WindowFlags) bool
{
    context.device = c.SDL_CreateGPUDevice(
        c.SDL_GPU_SHADERFORMAT_SPIRV | c.SDL_GPU_SHADERFORMAT_DXIL | c.SDL_GPU_SHADERFORMAT_MSL,
        false,
        null);

    if (context.device == null) {
        @panic("GPUCreate failed");
    }

    context.window = c.SDL_CreateWindow("SDL_Window", 640, 480, windowFlags);
    // var buffer: [256]u8 = undefined;
    // const wnd_name = std.fmt.bufPrintZ(&buffer, "{s}", .{context.example_name}) catch @panic("buffer overflown");
    // std.debug.print("{s}\n", .{wnd_name});
    // context.window = c.SDL_CreateWindow(context.example_name.ptr, 640, 480, windowFlags);
    if (context.window == null) {
        failwith ("create window failed: {s}", .{c.SDL_GetError()});
    }

    if (!c.SDL_ClaimWindowForGPUDevice(context.device, context.window)) {
        @panic("GPUClainWindow failed");
    }

    return true;
}


pub fn commonQuit (context:*Context) void 
{
    c.SDL_ReleaseWindowFromGPUDevice(context.device, context.window);
    c.SDL_DestroyWindow(context.window);
    c.SDL_DestroyGPUDevice(context.device);
}

// var basepath: [*c]const u8 = undefined;
var basepath: []const u8 = undefined;

pub fn initializeAssetLoader () void
{
    basepath = std.mem.span(c.SDL_GetBasePath());
    std.debug.print("basepath: {s}\n", .{basepath});
}

pub fn loadShader (
    device: ?*c.SDL_GPUDevice, 
    shader_file_name: []const u8,
    sampler_count: u32,
    uniform_buffer_count: u32,
    storage_buffer_count: u32,
    storage_texture_count: u32,
    ) ?*c.SDL_GPUShader 
{
    var stage: c.SDL_GPUShaderStage = undefined;
    if (c.SDL_strstr(shader_file_name.ptr, ".vert") != null) {
        stage = c.SDL_GPU_SHADERSTAGE_VERTEX;
    }            
    else if (c.SDL_strstr(shader_file_name.ptr, ".frag") != null) {
        stage = c.SDL_GPU_SHADERSTAGE_FRAGMENT;
    }
    else {
        failwith ("invalid shader stage!\n", .{});
    }

    var fullpath_buffer: [1024]u8 = undefined;
    var fullpath: []const u8 = undefined;
    const backend_formats = c.SDL_GetGPUShaderFormats(device);
    var format = c.SDL_GPU_SHADERFORMAT_INVALID;
    var entrypoint: [*c]const u8 = undefined;

    if (backend_formats & c.SDL_GPU_SHADERFORMAT_SPIRV > 0) {
        fullpath = std.fmt.bufPrintZ(&fullpath_buffer, "{s}Content/Shaders/Compiled/SPIRV/{s}.spv", .{basepath, shader_file_name}) catch @panic("bufPrintZ failed");
        format = c.SDL_GPU_SHADERFORMAT_SPIRV;
        entrypoint = "main";
    }
    else if (backend_formats & c.SDL_GPU_SHADERFORMAT_MSL > 0) {
        fullpath = std.fmt.bufPrintZ(&fullpath_buffer, "{s}Content/Shaders/Compiled/MSL/{s}.msl", .{basepath, shader_file_name}) catch @panic("bufPrintZ failed");
        format = c.SDL_GPU_SHADERFORMAT_MSL;
        entrypoint = "main0";        
    }
    else if (backend_formats & c.SDL_GPU_SHADERFORMAT_DXIL > 0) {
        fullpath = std.fmt.bufPrintZ(&fullpath_buffer, "{s}Content/Shaders/Compiled/DXIL/{s}.dxil", .{basepath, shader_file_name}) catch @panic("bufPrintZ failed");
        format = c.SDL_GPU_SHADERFORMAT_SPIRV;
        entrypoint = "main";        
    }
    else {
        @panic("unrecongnized backend shader format");
    }

    var code_size: usize = undefined;
    const code = c.SDL_LoadFile(fullpath.ptr, &code_size);
    if (code == null) {
        failwith ("failed to load shader from disk: {s}\n", .{fullpath});
    }

    const shader_info = c.SDL_GPUShaderCreateInfo{
        .code = @ptrCast(code),
        .code_size = code_size,
        .entrypoint = entrypoint,
        .format = @intCast(format),
        .stage = stage,
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
    
    c.SDL_free(code);
    return shader;
}


pub fn createComputePipeline (
    device: ?*c.SDL_GPUDevice,
    shader_filename: []const u8,
    create_info: ?*c.SDL_GPUComputePipeline,
    ) ?c.SDL_GPUComputePipeline 
{
    _ = shader_filename; // autofix
    var fullpath: [256]u8 = undefined;
    _ = &fullpath;
    const backend_formats = c.SDL_GetGPUShaderFormats (device);
    var format = c.SDL_GPU_SHADERFORMAT_INVALID;
    var entrypoint: [*c]u8 = undefined;

    if (backend_formats and c.SDL_GPU_SHADERFORMAT_SPIRV) {
        format = c.SDL_GPU_SHADERFORMAT_SPIRV;
        entrypoint = "main";
    }
    else if (backend_formats and c.SDL_GPU_SHADERFORMAT_MSL) {
        format = c.SDL_GPU_SHADERFORMAT_MSL;
        entrypoint = "main";
    }
    else {
        failwith ("unrecongnized backed shader format", .{});
        return null;
    }

    var codesize: usize = 0;
    const code = c.SDL_LoadFile(fullpath, &codesize);
    if (code == null) {
        failwith ("failed to load compute shader from disk! {s}", .{fullpath});
        return null;
    }

    // make a copy of the create data, then overwrite the parts we need
    var new_create_info = create_info.*;
    new_create_info.code = code;
    new_create_info.code_size = codesize;
    new_create_info.entrypoint = entrypoint;
    new_create_info.format = format;

    const pipeline = c.SDL_CreateGPUComputePipeline(device, &new_create_info);
    if (pipeline == null) {
        failwith ("failed to create compute pipeline", .{});
        c.SDL_free(code);
        return null;
    }    
    c.SDL_free(code);
    return pipeline;
}

pub fn loadImage (
    image_filename:[]const u8, 
    desired_channels:u32
    ) ?*c.SDL_Surface 
{
    var fullpath_buffer: [1024]u8 = undefined;
    var fullpath: []const u8 = undefined;
    var result: ?*c.SDL_Surface = null;
    var format: c.SDL_PixelFormat = undefined;

    fullpath = std.fmt.bufPrintZ(&fullpath_buffer, "{s}Content/Images/{s}", .{basepath, image_filename}) catch @panic("bufPrintZ failed");

    result = c.SDL_LoadBMP(fullpath.ptr);
    if (result == null) {
        failwith("failed to load BMP: {s}", .{c.SDL_GetError()});
    }

    if (desired_channels == 4) {
        format = c.SDL_PIXELFORMAT_ABGR8888;
    } 
    else {
        _ = c.SDL_DestroySurface(result);
        return null;
    }
    if (result.?.format != format) {
        const next = c.SDL_ConvertSurface(result, format);
        c.SDL_DestroySurface(result);
        result = next;
    }

    return result;
}

pub fn loadHDRImage (image_filename:[]const u8, pwidth:u32, pheight:u32, pchannels:u32, desired_channels:u32) *f32
{
    _ = image_filename; // autofix
    var fullpath: [256]u8 = undefined;
    // _ = c.SDL_snprintf(&fullpath, @sizeOf(fullpath), "{s}Content/Images/{s}", .{basepath, image_filename});
    return c.stbi_loadf(&fullpath, pwidth, pheight, pchannels, desired_channels);
}

pub const ASTCHeader = struct
{
    magic: [4]u8,
    blockX: u8,
    blockY: u8,
    blockZ: u8,
    dimX: [3]u8,
    dimY: [3]u8,
    dimZ: [3]u8,
};

pub const DDS_PixelFormat = struct
{
    dwsize: u32,
    dwflags: u32,
    dwfourcc: u32,
    dwRGBBit_count: u32,
    dwRBitmask: i32,
    dwGBitmask: i32,
    dwBBitmask: i32,
    dwABitmask: i32,
};

pub const DDS_Header = struct
{
    dwmagic: u32,
    dwsize: u32,
    dwflags: u32,
    dwheight: u32,
    dwwidth: u32,
    dwpitchor_linearsize: u32,
    dwdepth: u32,
    dwmipmap_count: u32,
    dwreserved: [11]u32,
    ddspf: DDS_PixelFormat,
    dwcaps: u32,
    dwcaps2: u32,
    dwcaps3: u32,
    dwcaps4: u32,
    dwreserved2: u32
};

pub const DDS_HeaderDXT10 = struct
{
    dxgiFormat: u32,
    resourceDimension: u32,
    miscFlag: u32,
    arraySize: usize,
    miscFlags2: usize,
};

pub fn loadASTCImage (image_filename:[]const u8, pwidth:*u32, pheight:*u32, pimage_datalenght:*u32) *anyopaque
{
    _ = image_filename; // autofix
    var fullpath: [256]u8 = undefined;
    // _ = c.SDL_snprintf(&fullpath, @sizeOf(fullpath), "{s}Content/Images/{s}", .{basepath, image_filename});

    var filesize: usize = undefined;
    const filecontents = c.SDL_LoadFile(&fullpath, &filesize);
    if (filecontents == null) {
        // c.SDL_assert ();
        return null;
    }

    const header: *ASTCHeader = @ptrCast(filecontents);
    if (header.magic[0] != 0x13 or header.magic[1] != 0xAB or header.magic[2] != 0xA1 or header.magic[3] != 0x5C) {
        // c.SDL_assert ()
        return null;
    }

    // get image dimensions in texels
    pwidth.* = header.dimX[0] + (header.dimX[1] << 8) + (header.dimX[2] << 16);
    pheight.* = header.dimX[0] + (header.dimY[1] << 8) + (header.dimY[2] << 16);

    // get the size of the texture data
    const block_count_x = (pwidth.* + header.blockX - 1) / header.blockX;
    const block_count_y = (pheight.* + header.blockY - 1) / header.blockY;
    pimage_datalenght.* = block_count_x + block_count_y * 16;

    const data = c.SDL_malloc(pimage_datalenght.*);
    _ = c.SDL_memcpy(data, @as([*c]const u8, @ptrCast(filecontents)) + @sizeOf(ASTCHeader), pimage_datalenght.*);
    c.SDF_free(filecontents);
    
    return data;
}

pub fn loadDDSImage (image_filename:[]const u8, format:c.SDL_GPUTextureFormat, pwidth:*u32, pheight:*u32, pimage_datalength:*u32) ?*anyopaque
{
    _ = image_filename; // autofix
    _ = format;
    var fullpath: [256]u8 = undefined;
    // _ = c.SDL_snprintf(&fullpath, @sizeOf(fullpath), "{s}Content/Image/{s}", .{basepath, image_filename});
    var filesize: usize = undefined;
    const filecontents = c.SDL_LoadFile(&fullpath, &filesize);
    if (filecontents == null) {
        // c.SDL_assert ()
        return null;
    }
    const header: *DDS_Header = @ptrCast(filecontents);
    if (header.dwmagic != 0x20534444) {
        // c.SDL_assert ()
        return null;
    }

    const has_dx10header = header.ddspf.dwflags == 0x4 and header.ddspf.dwfourcc == 0x30315844;

    pwidth.* = header.dwwidth;
    pheight.* = header.dwheight;
    pimage_datalength.* = header.dwpitchor_linearsize;

    const data = c.SDL_malloc(pimage_datalength.*);
    _ = c.SDL_memcpy(data, @as([*c]const u8, @ptrCast(filecontents)) + @sizeOf(DDS_Header) + (if (has_dx10header) @sizeOf(DDS_HeaderDXT10) else 0), pimage_datalength.*);
    c.SDL_free(filecontents);

    return data;
}
