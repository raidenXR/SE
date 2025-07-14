const std = @import("std");
const dotnet = @import("dotnet");
const numerics = dotnet.numerics;
const vec3 = numerics.vec3;
const vec2 = numerics.vec2;
const mat4x4 = numerics.mat4x4;
const string = dotnet.String;
const assets = @import("assets.zig");
const c = @import("c.zig");

const Vector2 = @Vector(2, f32);
const Vector3 = @Vector(3, f32);
const Vector4 = @Vector(4, f32);
const Matrix4x4 = [16]f32;


pub const Vertex = struct
{
    position: Vector3,
    normal:   Vector3 = @splat(0),
    tangent:  Vector3 = @splat(0),
    texcoord: Vector2 = @splat(0),
    color:    Vector4 = @splat(0),

    pub const Kind = enum
    {
        position,
        position_color,
        position_color_u8,
        position_texture,
        vertex,    
    };

    const attributes_0 = [5]c.SDL_GPUVertexAttribute{
        .{
            .buffer_slot = 0,
            .location = 0,
            .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
            .offset = 0,
        },
        .{
            .buffer_slot = 0,
            .location = 1,
            .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
            .offset = 12,
        },
        .{
            .buffer_slot = 0,
            .location = 2,
            .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
            .offset = 24,
        },
        .{
            .buffer_slot = 0,
            .location = 3,
            .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
            .offset = 36,
        },
        .{
            .buffer_slot = 0,
            .location = 4,
            .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
            .offset = 44,
        },
    };

    const attributes_1 = [2]c.SDL_GPUVertexAttribute{
        .{
            .buffer_slot = 0,
            .location = 0,
            .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
            .offset = 0,
        },
        .{
            .buffer_slot = 0,
            .location = 1,
            .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT4,
            .offset = 12,
        },
    };

    const attributes_2 = [2]c.SDL_GPUVertexAttribute{
        .{
            .buffer_slot = 0,
            .location = 0,
            .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
            .offset = 0,
        },
        .{
            .buffer_slot = 0,
            .location = 1,
            .format = c.SDL_GPU_VERTEXELEMENTFORMAT_UBYTE4_NORM,
            .offset = 12,
        },
    };

    const attributes_3 = [2]c.SDL_GPUVertexAttribute{
        .{
            .buffer_slot = 0,
            .location = 0,
            .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT3,
            .offset = 0,
        },
        .{
            .buffer_slot = 0,
            .location = 1,
            .format = c.SDL_GPU_VERTEXELEMENTFORMAT_FLOAT2,
            .offset = 12,
        },
    };

    pub fn attributes (kind:Kind) []const c.struct_SDL_GPUVertexAttribute
    {
        return switch (kind) {
            .position_color_u8 => attributes_2[0..],
            .position_texture => attributes_3[0..],
            .vertex => attributes_0[0..],
            else => @panic("not implemented attributes case"),
        };        
    }

    pub inline fn pitch (kind:Kind) comptime_int
    {
        return switch (kind) {
            .position_color_u8 => @sizeOf(Vector3) + @sizeOf(u8) * 4,
            .position_texture => @sizeOf(Vector3) + @sizeOf(Vector2),
            .vertex => @sizeOf(Vertex),
            else => @panic("not implemented pitch case"),
        };
    }
};

pub const Material = struct
{
    diffuse_albedo: [4]f32,
    fresnel_R0:     [3]f32,
    roughness:      f32,
    mat_transform:  [16]f32,
};

pub const Light = struct
{
    strength:     [3]f32,
    fallof_start: f32,
    direction:    [3]f32,
    fallof_end:   f32,
    position:     [3]f32,
    spot_power:   f32,
};

pub const MeshData = struct
{
    vertices: []Vertex,
    indices:  []u32,

    pub fn deinit (m:MeshData, allocator:std.mem.Allocator) void
    {
        allocator.free(m.vertices);
        allocator.free(m.indices);
    }
};

pub const Submesh = struct
{
    index_count: u32,
    start_index_location: u32,
    base_vertex_location: u32,
};

pub const MeshGeometry = struct
{
    vertex_buffer_cpu: std.ArrayListUnmanaged(Vertex) = .{},
    index_buffer_cpu:  std.ArrayListUnmanaged(u32) = .{},
    vertex_buffer_gpu: ?*c.SDL_GPUBuffer = null,
    index_buffer_gpu:  ?*c.SDL_GPUBuffer = null,
    nv: u32 = 0,
    ni: u32 = 0,
    num_indices: u32 = 0,
    is_uploaded: bool = false,

    pub fn deinit (m:*MeshGeometry, allocator:std.mem.Allocator, ctx:Context) void
    {
        m.vertex_buffer_cpu.deinit(allocator);
        m.index_buffer_cpu.deinit(allocator);
        c.SDL_ReleaseGPUBuffer(ctx.device, m.vertex_buffer_gpu);
        c.SDL_ReleaseGPUBuffer(ctx.device, m.index_buffer_gpu);
        m.is_uploaded = false;
    }

    // copies the mesh_data buffers over its memory and frees the mesh_data instance
    pub fn append (m:*MeshGeometry, allocator:std.mem.Allocator, mesh_data:MeshData) !Submesh
    {
        const bv = m.nv;
        const bi = m.ni;

        try m.vertex_buffer_cpu.appendSlice(allocator, mesh_data.vertices);
        try m.index_buffer_cpu.appendSlice(allocator, mesh_data.indices);
        m.nv += @intCast(mesh_data.vertices.len);
        m.ni += @intCast(mesh_data.indices.len);
        m.num_indices += @intCast(mesh_data.indices.len);

        return Submesh{
            .index_count = @intCast(mesh_data.indices.len),
            .base_vertex_location = bv,
            .start_index_location = bi,
        };
    }

    pub fn upload (m:*MeshGeometry, device:?*c.SDL_GPUDevice) void
    {
        if (m.is_uploaded) return;
        
        const cmdbuf = c.SDL_AcquireGPUCommandBuffer(device);
        const copypass = c.SDL_BeginGPUCopyPass(cmdbuf);

        const vertices: []u8 = std.mem.sliceAsBytes(m.vertex_buffer_cpu.items);
        const indices: []u8  = std.mem.sliceAsBytes(m.index_buffer_cpu.items);
        
        const vertices_byte_size: u32 = @intCast(vertices.len);
        const indices_byte_size: u32  = @intCast(indices.len); 

        const vertex_buffer = c.SDL_CreateGPUBuffer(device, &c.SDL_GPUBufferCreateInfo{
            .usage = c.SDL_GPU_BUFFERUSAGE_VERTEX,
            .size = @intCast(vertices_byte_size),
        });

        const index_buffer = c.SDL_CreateGPUBuffer(device, &c.SDL_GPUBufferCreateInfo{
            .usage = c.SDL_GPU_BUFFERUSAGE_INDEX,
            .size = indices_byte_size,
        });

        const transfer_buffer = c.SDL_CreateGPUTransferBuffer(device, &c.SDL_GPUTransferBufferCreateInfo{
            .usage = c.SDL_GPU_TRANSFERBUFFERUSAGE_UPLOAD,
            .size = @intCast(vertices_byte_size + indices_byte_size),
        });

        var transfer_mem: [*]u8 = @ptrCast(@alignCast(c.SDL_MapGPUTransferBuffer(device, transfer_buffer, false)));
        @memcpy(transfer_mem[0..vertices.len], vertices);
        @memcpy(transfer_mem[vertices.len..vertices.len + indices.len], indices);
        c.SDL_UnmapGPUTransferBuffer(device, transfer_buffer);

        c.SDL_UploadToGPUBuffer(copypass,
            &.{.transfer_buffer = transfer_buffer},
            &.{.buffer = vertex_buffer, .size = @intCast(vertices_byte_size)},
            false
        );
    
        c.SDL_UploadToGPUBuffer(copypass,
            &.{.transfer_buffer = transfer_buffer, .offset = @intCast(vertices_byte_size)},
            &.{.buffer = index_buffer, .size = @intCast(indices_byte_size)},
            false
        );

        c.SDL_ReleaseGPUTransferBuffer(device, transfer_buffer);

        m.vertex_buffer_gpu = vertex_buffer;
        m.index_buffer_gpu  = index_buffer;
        m.num_indices = @intCast(m.index_buffer_cpu.items.len);
        m.is_uploaded = true;        
    }
};

pub const RenderItem = struct
{
    world:         Matrix4x4,
    tex_transform: Matrix4x4 = mat4x4.identity,
    submesh:       Submesh,
    geometry:      *MeshGeometry,
    idx_material:  i32,
    idx_texture:   i32,
    primitive:     c_int = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
};



pub const Camera = struct
{
	// // Camera coordinate system with coordinates relative to world space.
	// DirectX::XMFLOAT3 mPosition = { 0.0f, 0.0f, 0.0f };
	// DirectX::XMFLOAT3 mRight = { 1.0f, 0.0f, 0.0f };
	// DirectX::XMFLOAT3 mUp = { 0.0f, 1.0f, 0.0f };
	// DirectX::XMFLOAT3 mLook = { 0.0f, 0.0f, 1.0f };

	// // Cache frustum properties.
	// float mNearZ = 0.0f;
	// float mFarZ = 0.0f;
	// float mAspect = 0.0f;
	// float mFovY = 0.0f;
	// float mNearWindowHeight = 0.0f;
	// float mFarWindowHeight = 0.0f;

	// bool mViewDirty = true;

	// // Cache View/Proj matrices.
	// DirectX::XMFLOAT4X4 mView = MathHelper::Identity4x4();
	// DirectX::XMFLOAT4X4 mProj = MathHelper::Identity4x4();
	// 
    pos:    Vector3 = .{0, 0, 0},
    right:  Vector3 = .{1, 0, 0},
    up:     Vector3 = .{0, 1, 0},
    look:   Vector3 = .{0, 0, 1},
    view:   Matrix4x4 = mat4x4.identity,
    proj:   Matrix4x4 = mat4x4.identity,
    near_z: f32 = 0,
    far_z:  f32 = 0,
    aspect: f32 = 0,
    fov_y:  f32 = 0,
    near_window_h: f32 = 0,
    far_window_h:  f32 = 0,

    view_dirty: bool = true,


    pub fn fovX (cm:Camera) f32
    {
        const half_width = 0.5 * nearWindowWidth(cm);

        return 0.2 * std.math.atan(half_width / cm.near_z);
    }    
    
    // Get near and far plane dimensions in view space coordinates.
    pub fn nearWindowWidth (cm:Camera) f32
    {
        return cm.aspect * cm.near_window_h;
    }

    pub fn farWindowWidth (cm:Camera) f32
    {
        return cm.aspect * cm.far_window_h;
    }

    // Set frustrum
    pub fn setLens (cm:*Camera, fov_y:f32, aspect:f32, zn:f32, zf:f32) void
    {
        // cache properties
        cm.fov_y = fov_y;
        cm.aspect = aspect;
        cm.near_z = zn;
        cm.far_z = zf;

        cm.near_window_h = 2 * cm.near_z * @tan(0.5 * cm.fov_y);
        cm.far_window_h  = 2 * cm.far_z * @tan(0.5 * cm.fov_y);

        cm.proj = mat4x4.createPerspectiveFieldOfView(cm.fov_y, cm.aspect, cm.near_z, cm.far_z);
    }

    // define camera space via LookAt parameters
    pub fn lookAt (cm:*Camera, pos:Vector3, target:Vector3, world_up:Vector3) void
    {
        const L = vec3.normalize(target - pos);
        const R = vec3.normalize(vec3.cross(world_up, L));
        const U = vec3.cross(L, R);

        cm.pos = pos;
        cm.look = L;
        cm.right = R;
        cm.up = U;

        cm.view_dirty = true;
    }

    // strafe/walk the camera a distance d.
    pub fn strafe (cm:*Camera, d:f32) void
    {
        const s: Vector3 = .{d, d, d};
        const r: Vector3 = cm.right;
        const p: Vector3 = cm.pos;
    
        cm.pos = (s * r) + p;
        cm.view_dirty = true;
    }

    pub fn walk (cm:*Camera, d:f32) void
    {
        const s: Vector3 = .{d, d, d};
        const l: Vector3 = cm.look;
        const p: Vector3 = cm.pos;

        cm.pos = (s * l) + p;
        cm.view_dirty = true;
    }

    // rotate the camera
    pub fn pitch (cm:*Camera, angle:f32) void
    {    
    	// Rotate up and look vector about the right vector.

    	const R = mat4x4.createRotationXFromCenterPoint(angle, c.right);
	
    	cm.up   = vec3.transformNormal(cm.up, R);
    	cm.look = vec3.transformNormal(cm.look, R);

    	cm.view_dirty = true; 
    }

    pub fn rotateY (cm:*Camera, angle:f32) void
    {
        // Rotate the basis vectors about the right vector.
    
        const R = mat4x4.createRotationY(angle);

        cm.up   = vec3.transformNormal(cm.up, R);
        cm.look = vec3.transformNormal(cm.look, R);

        cm.view_dirty = true;    
    }

    // After modifying camera position/orientation, call to rebuild the view matrix.
    pub fn updateViewMatrix (cm:*Camera) void
    {
        if (cm.view_dirty)
        {
            var R: Vector3 = cm.right;
            var U: Vector3 = cm.up;
            var L: Vector3 = cm.look;
            const P: Vector3 = cm.pos;

    		// Keep camera's axes orthogonal to each other and of unit length.
            L = vec3.normalize(L);
            U = vec3.normalize(vec3.cross(L, R));
		
            // U, L already ortho-normal, so no need to normalize cross product.
            R = vec3.cross(U, L);

            // Fill in the view matrix entries.
            const x = -(P * R)[0];
            const y = -(P * U)[0];
            const z = -(P * L)[0];

            cm.right = R;
            cm.up = U;
            cm.look = L;

            cm.view = Matrix4x4{
                cm.right[0], cm.right[1], cm.right[2], x,
                cm.up[0], cm.up[1], cm.up[2], y,
                cm.look[0], cm.look[1], cm.look[2], z,
                0, 0, 0, 1,            
            };

            cm.view_dirty = false;
        }
    }
};

pub const State = struct
{
    ptr: *anyopaque,
    initFn:   *const fn (s:*anyopaque, allocator:std.mem.Allocator) anyerror!void,
    deinitFn: *const fn (s:*anyopaque, allocator:std.mem.Allocator) void,
    updateFn: *const fn (s:*anyopaque) void,
    drawFn:   *const fn (s:*anyopaque, cmdbuffer:?*c.SDL_GPUCommandBuffer, renderpass:?*c.SDL_GPURenderPass) void,

    pub fn init (s:*State, allocator:std.mem.Allocator) !void
    {
        try s.initFn(s.ptr, allocator);
    }

    pub fn deinit (s:*State, allocator:std.mem.Allocator) void
    {
        s.deinitFn(allocator);
    }

    pub fn update (s:*State) void
    {
        s.updateFn(s.ptr);
    }

    pub fn draw (s:*State, cmdbuffer:?*c.SDL_GPUCommandBuffer, renderpass:?*c.SDL_GPURenderPass) void
    {
        s.drawFn(s.ptr, cmdbuffer, renderpass);
    }
};

pub const ObjectConstants = struct
{
    world:         Matrix4x4,
    // tex_transform: Matrix4x4,


    pub const default = ObjectConstants{
        .world = mat4x4.identity,
        // .tex_transform = mat4x4.identity,
    };
};

pub const PassConstants = struct
{
    view:          Matrix4x4,
    inv_view:      Matrix4x4,
    proj:          Matrix4x4,
    inv_proj:      Matrix4x4,
    view_proj:     Matrix4x4,
    inv_view_proj: Matrix4x4,
    eyes_posW:     Vector3,
    pad0:          f32 = 0,
    render_target_size:     [2]f32,
    inv_render_target_size: [2]f32,
    nearZ:         f32,
    farZ:          f32,
    pad1:          f32 = 0,
    pad2:          f32 = 0,
    // ambient_light: [4]f32,
    // lights:        [16]Light,


    pub const default = PassConstants{
        .view      = mat4x4.identity,
        .inv_view  = mat4x4.identity,
        .proj      = mat4x4.identity,
        .inv_proj  = mat4x4.identity,
        .view_proj = mat4x4.identity,
        .inv_view_proj = mat4x4.identity,
        .eyes_posW     = .{0, 0, 0},
        .pad0          = 0,
        .render_target_size = .{0, 0},
        .inv_render_target_size = .{0, 0},
        .nearZ = 0,
        .farZ  = 0,
        .pad1 = 0,
        .pad2 = 0,
        // .ambient_light = .{0, 0, 0, 1},
        // .lights = undefined,        
    };
};


pub const Context = struct
{
    device: ?*c.SDL_GPUDevice,
    window: ?*c.SDL_Window,        
};

/// creates a gpu_device and a window 
pub fn init (wnd_name:[*c]const u8, w:c_int, h:c_int, window_flags:c.SDL_WindowFlags) !Context 
{
    _ = c.SDL_Init( c.SDL_INIT_VIDEO );
    
    const device = c.SDL_CreateGPUDevice(
        // c.SDL_GPU_SHADERFORMAT_SPIRV | c.SDL_GPU_SHADERFORMAT_DXIL | c.SDL_GPU_SHADERFORMAT_MSL,
        c.SDL_GPU_SHADERFORMAT_SPIRV,
        false,
        null);

    if (device == null) return error.DeviceFailed;

    const window = c.SDL_CreateWindow(wnd_name, w, h, window_flags);

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

pub fn concatString (a:[]const u8, b:[]const u8) struct{usize, [1024:0]u8}
{
    var buffer: [1024:0]u8 = undefined;
    // @memset(&buffer, '\x00');

    const str = std.fmt.bufPrintZ(&buffer, "{s}{s}", .{a, b}) catch @panic("concatString failed");

    return .{str.len, buffer};
}


var _basepath: [256]u8 = undefined;
 
pub fn basepath () struct{usize, [256:0]u8}
{
    var buffer: [256:0]u8 = undefined;
    // @memset (&buffer, '\x00');

    const str = std.fs.cwd().realpath(".", &buffer) catch @panic("basepath failed");
    buffer[str.len + 0] = '/';
    buffer[str.len + 1] = '\x00';
    
    return .{str.len + 1, buffer};
}

pub fn fullpath (filename:[]const u8) struct{usize, [1024:0]u8}
{
    const l, const b = basepath();
    return concatString(b[0..l], filename);    
}

fn strfn () struct{[]const u8, [256:0]u8}
{
    var buffer: [256:0]u8 = undefined;
    @memset(&buffer, '\x00');

    const str: []const u8 = "some str for testing";
    @memcpy(buffer[0..str.len], str);

    return .{buffer[0..str.len], buffer};
}

test "test string helper functions" {
    var buffer: [256]u8 = undefined;
    const path = try std.fs.cwd().realpath(".", &buffer);
    const l0, const b = basepath();
    const l1, const f = fullpath("content/models/car.txt");
    const l2, const s = concatString("str A +", "str B");

    const str, _ = strfn();

    std.debug.print("path: -{s}-\n", .{path});
    std.debug.print("basepath: -{s}-\n", .{b[0..l0]});
    std.debug.print("fullpath: -{s}-\n", .{f[0..l1]});    
    std.debug.print("concat_s: -{s}-\n", .{s[0..l2]});    
    
    std.debug.print("str fn: -{s}-\n", .{str});    
}


