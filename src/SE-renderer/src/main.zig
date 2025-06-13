const std = @import("std");
const c = @import("c.zig");
const dotnet = @import("dotnet");
const mat4x4 = dotnet.numerics.mat4x4;
const numerics = dotnet.numerics;
const GeometryGenerator = @import("GeometryGenerator.zig");
const Camera = @import("Camera.zig");
const Utils = @import("utilities.zig");

const print = std.debug.print;

var is_running = true;

var world = mat4x4.identity;
var view  = mat4x4.identity;
var proj  = mat4x4.identity;
var world_view_proj = mat4x4.identity;

var theta: f32 = 1.5 * std.math.pi;
var phi:   f32 = std.math.pi / 4.0;
var radius: f32 = 5.0;

const w = 1240;
const h = 720;
const aspect_ratio = w / h;


fn update () void
{
    // convert spherical to cartesian coordinates.
    const x = radius * @sin(phi) * @cos(theta);
    const z = radius * @sin(phi) * @sin(theta);
    const y = radius * @cos(phi);

    // build view matrix;
    const pos    = [3]f32{x, y, z};
    const target = [3]f32{0, 0, 0}; 
    const up     = [3]f32{0, 1, 0};

    view = mat4x4.createLookAt(pos, target, up);
    world_view_proj = mat4x4.multiply(mat4x4.multiply(world, view), proj);
}

fn draw () void
{
}

pub fn main () !void
{
    proj = mat4x4.createPerspectiveFieldOfView(0.25 * std.math.pi, aspect_ratio, 1, 1000);

    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    const allocator = gpa.allocator();

    var camera = Camera.default;    

    const ctx= try Utils.init("renderer window", 1240, 720, 0);
    defer Utils.quit(ctx);

    const vs = Utils.loadShader(ctx.device, "shaders/color_vs.spv", "VS", 0, 1, 0, 0);
    defer c.SDL_ReleaseGPUShader(ctx.device, vs);
    const ps = Utils.loadShader(ctx.device, "shaders/color_ps.spv", "PS", 0, 1, 0, 0);
    defer c.SDL_ReleaseGPUShader(ctx.device, ps);
    const pipeline = Utils.createPipeline( ctx, vs, ps, null, null);

    var box = try GeometryGenerator.createBox( allocator, 100, 100, 100, 4);
    defer box.deinit( allocator );

    var geo = GeometryGenerator.MeshGeometry{
        .name = "box_example",
        .pipeline = pipeline,
        .vertex_byte_stride = @sizeOf(GeometryGenerator.Vertex),
        .index_format = c.SDL_GPU_INDEXELEMENTSIZE_32BIT,
    };
    defer geo.deinit( allocator, ctx.device );

    try geo.vertex_buffer_cpu.appendSlice( allocator, box.vertices.items );
    try geo.index_buffer_cpu.appendSlice( allocator, box.indices.items );


    geo.vertex_buffer_byte_size += Utils.getSize(GeometryGenerator.Vertex, box.vertices.items);
    geo.total_indices_len += @intCast(box.indices.items.len);


    Utils.createAndUploadBuffers( ctx.device, &geo );   


    while (is_running)
    {
        var evt: c.SDL_Event = undefined;

        update();

        while (c.SDL_PollEvent(&evt))
        {
            if (evt.type == c.SDL_EVENT_QUIT)
            {
                is_running = false;
            }
            else if (evt.type == c.SDL_EVENT_KEY_DOWN)
            {
                if (evt.key.key == c.SDLK_LEFT)
                {
                    camera.pos[0] -= 1;
                }
                if (evt.key.key == c.SDLK_RIGHT)
                {
                    camera.pos[0] += 1;
                }

                if (evt.key.key == c.SDLK_UP)
                {
                    camera.pos[1] += 1;
                }
                if (evt.key.key == c.SDLK_DOWN)
                {
                    camera.pos[1] -= 1;
                }
            }
        }

        const cmdbuf = c.SDL_AcquireGPUCommandBuffer( ctx.device );
    
        var swapchain_texture: ?*c.SDL_GPUTexture = null;
        if (!c.SDL_WaitAndAcquireGPUSwapchainTexture( cmdbuf, ctx.window, &swapchain_texture, null, null)) 
        {
            @panic ("WaitAndAcquiredGPUSwaptexture failed\n");
        }

        if (swapchain_texture != null)
        {
            // var camera = Camera{};

            var color_target_info = c.SDL_GPUColorTargetInfo{
                .texture = swapchain_texture,
                .clear_color = c.SDL_FColor{.r = 0.3, .g = 0.5, .b = 0.4, .a = 1},
                .load_op = c.SDL_GPU_LOADOP_CLEAR,
                .store_op = c.SDL_GPU_STOREOP_STORE,
            };

            const renderpass = c.SDL_BeginGPURenderPass( cmdbuf, &color_target_info, 1, null );

            c.SDL_BindGPUVertexBuffers( renderpass, 0, &c.SDL_GPUBufferBinding{.buffer = geo.vertex_buffer_gpu, .offset = 0}, 1);
            c.SDL_BindGPUIndexBuffer( renderpass, &c.SDL_GPUBufferBinding{.buffer = geo.index_buffer_gpu, .offset = 0}, c.SDL_GPU_INDEXELEMENTSIZE_32BIT);
            c.SDL_PushGPUVertexUniformData( cmdbuf, 0, &world_view_proj, @sizeOf([16]f32) );
            c.SDL_BindGPUGraphicsPipeline( renderpass, geo.pipeline );
            c.SDL_DrawGPUIndexedPrimitives( renderpass, geo.total_indices_len, 1, 0, 0, 0);
            c.SDL_EndGPURenderPass( renderpass );
        }

        _ = c.SDL_SubmitGPUCommandBuffer( cmdbuf );
    }
}

test "run all tests" {
    std.testing.refAllDecls(@This());
}

