const c = @import("c.zig");
const common = @import("common.zig");
const Context = common.Context;

pub fn init (context:*Context) bool
{
    return common.commonInit(context, c.SDL_WINDOW_RESIZABLE);
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
        return false;
    }

    var swapchain_texture: ?*c.SDL_GPUTexture = null;
    if (!c.SDL_WaitAndAcquireGPUSwapchainTexture(cmdbuf, context.window, &swapchain_texture, null, null)) {
        return false;
    }

    if (swapchain_texture != null) {
        const color_target_info = c.SDL_GPUColorTargetInfo{
            .texture =  swapchain_texture,
            .clear_color = c.SDL_FColor{.r = 0.3, .g = 0.4, .b = 0.5, .a = 1.0},
            .load_op = c.SDL_GPU_LOADOP_CLEAR,
            .store_op = c.SDL_GPU_STOREOP_STORE,
        };

        const render_pass = c.SDL_BeginGPURenderPass(cmdbuf, &color_target_info, 1, null);
        c.SDL_EndGPURenderPass(render_pass);
    }

    _ = c.SDL_SubmitGPUCommandBuffer(cmdbuf);

    return true;
}

pub fn quit (context:*Context) void 
{
    common.commonQuit(context);
}

pub const ClearScreen_Example = common.Example{
    .name = "ClearScreen",
    .init = init,
    .update = update,
    .draw = draw,
    .quit = quit,
};
