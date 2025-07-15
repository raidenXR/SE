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
const MeshGeometry = r.MeshGeometry;
const SubmeshGeometry = r.SubmeshGeometry;

const Geometry = @import("Geometry.zig");

const print = std.debug.print;
const pi = std.math.pi;

fn updateMainPassCB (camera:r.Camera, main_cb:*r.PassConstants) void
{
	// XMMATRIX view = mCamera.GetView();
	// XMMATRIX proj = mCamera.GetProj();

	// XMMATRIX viewProj = XMMatrixMultiply(view, proj);
	// XMMATRIX invView = XMMatrixInverse(&XMMatrixDeterminant(view), view);
	// XMMATRIX invProj = XMMatrixInverse(&XMMatrixDeterminant(proj), proj);
	// XMMATRIX invViewProj = XMMatrixInverse(&XMMatrixDeterminant(viewProj), viewProj);

	// XMStoreFloat4x4(&mMainPassCB.View, XMMatrixTranspose(view));
	// XMStoreFloat4x4(&mMainPassCB.InvView, XMMatrixTranspose(invView));
	// XMStoreFloat4x4(&mMainPassCB.Proj, XMMatrixTranspose(proj));
	// XMStoreFloat4x4(&mMainPassCB.InvProj, XMMatrixTranspose(invProj));
	// XMStoreFloat4x4(&mMainPassCB.ViewProj, XMMatrixTranspose(viewProj));
	// XMStoreFloat4x4(&mMainPassCB.InvViewProj, XMMatrixTranspose(invViewProj));
	// mMainPassCB.EyePosW = mCamera.GetPosition3f();
	// mMainPassCB.RenderTargetSize = XMFLOAT2((float)mClientWidth, (float)mClientHeight);
	// mMainPassCB.InvRenderTargetSize = XMFLOAT2(1.0f / mClientWidth, 1.0f / mClientHeight);
	// mMainPassCB.NearZ = 1.0f;
	// mMainPassCB.FarZ = 1000.0f;
	// 
    const _view = camera.view;
    const _proj = camera.proj;
    const _view_proj = mat4x4.multiply(_view, _proj);

    var _inv_view: Matrix4x4 = undefined;
    var _inv_proj: Matrix4x4 = undefined;
    var _inv_view_proj: Matrix4x4 = undefined;
    _ = mat4x4.invert(_view, &_inv_view);
    _ = mat4x4.invert(_proj, &_inv_proj);
    _ = mat4x4.invert(_view_proj, &_inv_view_proj);
    
    main_cb.* = r.PassConstants{
        .view      = mat4x4.transpose(_view),
        .inv_view  = mat4x4.transpose(_inv_view),
        .proj      = mat4x4.transpose(_proj),
        .inv_proj  = mat4x4.transpose(_inv_proj),
        .view_proj = mat4x4.transpose(_view_proj),
        .inv_view_proj = mat4x4.transpose(_inv_view_proj),
        .eyes_posW = camera.pos,
        .render_target_size = .{1280.0, 720.0},
        .inv_render_target_size = .{1.0 / 1280.0, 1.0 / 720.0},
        .nearZ = 1.0,
        .farZ  = 1000.0,
    };
}

var prev_pos: Vector3 = @splat(0);
fn cameraPrint (camera:r.Camera) void
{
    if (!dotnet.numerics.vec3.equal(prev_pos, camera.pos))
    {
        prev_pos = camera.pos;
        std.debug.print("camera pos: [{d}, {d}, {d}]\n", .{camera.pos[0], camera.pos[1], camera.pos[2]});    
    }    
}

pub fn main () !void
{
    var gpa = std.heap.GeneralPurposeAllocator(.{}){};
    const allocator = gpa.allocator();

    const ctx = try r.init("renderer window", 1240, 720, 0);
    defer r.quit(ctx);
    var is_running = true;

    var cbpass = r.PassConstants.default;
    var camera = r.Camera{};
    camera.pos = .{0, 2, -15};
    camera.setLens(0.25 * pi, 1280.0 / 720.0, 1.0, 1000.0);
    camera.updateViewMatrix();

    const pixels, _ = assets.loadPixels("content/textures/texture0.png");
    defer assets.freePixels(pixels);

    var geo = MeshGeometry{};
    defer geo.deinit(allocator, ctx);

    const car_model = try Geometry.loadModel(allocator, "content/models/car.txt");
    defer car_model.deinit(allocator);    
    for (car_model.vertices) |*v| v.color = .{0.5, 0.5, 0.5, 1.0};

    const pipeline = assets.wiredPipeline(ctx, "content/shaders/color_vs.spv", "content/shaders/color_ps.spv");
    const submesh = try geo.append(allocator, car_model);
    geo.upload(ctx.device);

    const ritem = r.RenderItem{
        .world = mat4x4.multiply(mat4x4.createScale(2,2,2), mat4x4.createTranslation(.{0.5, 0.5, 0.0})),
        .tex_transform = mat4x4.identity,
        .submesh = submesh,
        .primitive = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
        .idx_material = -1,
        .idx_texture = -1,
        .geometry = &geo,
    };

    const num_indices: u32 = @intCast(car_model.vertices.len / 4);
    std.debug.print("num_indices: {}\n", .{num_indices});
    std.debug.print("world: {any}\n", .{ritem.world});
    std.debug.print("submesh: {any}\n", .{ritem.submesh});
    
    while (is_running)
    {
        var evt: c.SDL_Event = undefined;

        while (c.SDL_PollEvent(&evt))
        {
            if (evt.type == c.SDL_EVENT_QUIT)
            {
                is_running = false;
            }

            if (evt.type == c.SDL_EVENT_KEY_DOWN)
            {
                if (evt.key.key == c.SDLK_ESCAPE) is_running = false;
                if (evt.key.key == c.SDLK_DOWN) camera.walk(-3);
                if (evt.key.key == c.SDLK_UP)   camera.walk(3);
                if (evt.key.key == c.SDLK_LEFT)  camera.strafe(-3);
                if (evt.key.key == c.SDLK_RIGHT) camera.strafe(3);
                camera.updateViewMatrix();

                cameraPrint(camera);                
            }

            //   if((btnState & MK_LBUTTON) != 0)
            //   {
            	// // Make each pixel correspond to a quarter of a degree.
            	// float dx = XMConvertToRadians(0.25f*static_cast<float>(x - mLastMousePos.x));
            	// float dy = XMConvertToRadians(0.25f*static_cast<float>(y - mLastMousePos.y));

            	// mCamera.Pitch(dy);
            	// mCamera.RotateY(dx);
            //   }

            //   mLastMousePos.x = x;
            //   mLastMousePos.y = y;
            
            updateMainPassCB(camera, &cbpass);


            const cmdbuf = c.SDL_AcquireGPUCommandBuffer(ctx.device);
            var swapchain_texture: ?*c.SDL_GPUTexture = undefined;
            if (c.SDL_AcquireGPUSwapchainTexture(cmdbuf, ctx.window, &swapchain_texture, null, null))
            {
                const color_target_info = c.SDL_GPUColorTargetInfo{
                    .texture = swapchain_texture,
                    .clear_color = c.SDL_FColor{.r = 0.3, .g = 0.3, .b = 0.5, .a = 1},
                    .load_op = c.SDL_GPU_LOADOP_CLEAR,
                    .store_op = c.SDL_GPU_STOREOP_STORE,
                };

                const renderpass = c.SDL_BeginGPURenderPass(cmdbuf, &color_target_info, 1, null);
                c.SDL_BindGPUGraphicsPipeline(renderpass, pipeline);
                c.SDL_BindGPUVertexBuffers(renderpass, 0, &.{.buffer = ritem.geometry.vertex_buffer_gpu, .offset = 0}, 1);
                c.SDL_BindGPUIndexBuffer(renderpass, &.{.buffer = ritem.geometry.index_buffer_gpu, .offset = 0}, c.SDL_GPU_INDEXELEMENTSIZE_32BIT);

                c.SDL_PushGPUVertexUniformData(cmdbuf, 0, &.{ritem.world, ritem.tex_transform}, 2 * @sizeOf(Matrix4x4));
                c.SDL_PushGPUVertexUniformData(cmdbuf, 1, &cbpass, @sizeOf(r.PassConstants));                
                
                c.SDL_PushGPUFragmentUniformData(cmdbuf, 0, &.{ritem.world, ritem.tex_transform}, 2 * @sizeOf(Matrix4x4));
                c.SDL_PushGPUFragmentUniformData(cmdbuf, 0, &cbpass, @sizeOf(r.PassConstants));
                
                // c.SDL_DrawGPUIndexedPrimitives(renderpass, num_indices, 1, 0, 0, 0);
                // c.SDL_DrawGPUIndexedPrimitives(renderpass, num_indices, 1, ritem.submesh.start_index_location, ritem.submesh.base_vertex_location, 0);                
                c.SDL_EndGPURenderPass(renderpass);
            }
            _ = c.SDL_SubmitGPUCommandBuffer(cmdbuf);
        }
    }
}

test "run all tests" {
    std.testing.refAllDecls(@This());
}


