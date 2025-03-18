const c = @import("c.zig");
const common = @import("common.zig");
const Context = common.Context;

var fill_pipeline: ?*c.SDL_GPUGraphicsPipeline = null;
var line_pipeline: ?*c.SDL_GPUGraphicsPipeline = null;
var small_viewport = c.SDL_GPUViewport{.x = 160, .y = 120, .w = 320, .h = 240, .min_depth = 0.1, .max_depth = 1.0};
var scissor_rect = c.SDL_Rect{.x = 320, .y = 240, .w = 320, .h = 240};

var use_wireframemode = false;
var use_small_viewport = false;
var use_scissor_rect = false;

pub fn init (context:*Context) bool
{
    const result = common.commonInit(context, 0);
    if (!result) {
        return result;
    }

    // create the shaders
    const vertex_shader = common.loadShader(context.device, "RawTriangle.vert", 0,0,0,0);
    if (vertex_shader == null) {
        @panic("failed to create vertex shader");
    }

    const fragment_shader = common.loadShader(context.device, "SolidColor.frag", 0,0,0,0);
    if (fragment_shader == null) {
        @panic("failed to create fragment shader");
    }

    // create the pipelines
    var pipeline_create_info = c.SDL_GPUGraphicsPipelineCreateInfo{
        .target_info =  .{
            .num_color_targets = 1,
            .color_target_descriptions = &[_]c.SDL_GPUColorTargetDescription{
                .{.format = c.SDL_GetGPUSwapchainTextureFormat(context.device, context.window)},
            },
        },
        .primitive_type = c.SDL_GPU_PRIMITIVETYPE_TRIANGLELIST,
        .vertex_shader = vertex_shader,
        .fragment_shader = fragment_shader,
    };

    pipeline_create_info.rasterizer_state.fill_mode = c.SDL_GPU_FILLMODE_FILL;
    fill_pipeline = c.SDL_CreateGPUGraphicsPipeline(context.device, &pipeline_create_info);
    if (fill_pipeline == null) {
        @panic("failed to create fill pipeline");
    }

    pipeline_create_info.rasterizer_state.fill_mode = c.SDL_GPU_FILLMODE_LINE;
    line_pipeline = c.SDL_CreateGPUGraphicsPipeline(context.device, &pipeline_create_info);
    if (line_pipeline == null) {
        @panic("failed to create line pipeline");
    }

    // clean up shader resources
    c.SDL_ReleaseGPUShader(context.device, vertex_shader);
    c.SDL_ReleaseGPUShader(context.device, fragment_shader);

    // finally, print instructions

    return true;
}

pub fn update (context:*Context) bool
{
    if (context.left_pressed) {
        use_wireframemode = !use_wireframemode;
    }
    if (context.down_pressed) {
        use_small_viewport = !use_small_viewport;
    }
    if (context.right_pressed) {
        use_scissor_rect = !use_scissor_rect;
    }

    return true;
}

pub fn draw (context:*Context) bool
{
    const cmdbuf = c.SDL_AcquireGPUCommandBuffer(context.device);
    if (cmdbuf == null) {
        @panic("AcquireGPUCommandBuffer failed\n");
    }

    var swapchain_texture: ?*c.SDL_GPUTexture = undefined;
    if (!c.SDL_WaitAndAcquireGPUSwapchainTexture(cmdbuf, context.window, &swapchain_texture, null, null)) {
        @panic("WaitAndAcquiredGPUSwapChainTexture failed");
    }

    if (swapchain_texture != null) {
        const color_target_info = c.SDL_GPUColorTargetInfo{
            .texture = swapchain_texture,
            .clear_color = c.SDL_FColor{.r = 0.0, .g = 0.0, .b = 0.0, .a = 1.0},
            .load_op = c.SDL_GPU_LOADOP_LOAD,
            .store_op = c.SDL_GPU_STOREOP_STORE,
        };

        const render_pass = c.SDL_BeginGPURenderPass(cmdbuf, &color_target_info, 1, null);
        c.SDL_BindGPUGraphicsPipeline(render_pass, if (use_wireframemode) line_pipeline else fill_pipeline);
        
        if (use_small_viewport) {
            c.SDL_SetGPUViewport(render_pass, &small_viewport);    
        }
        if (use_scissor_rect) {
            c.SDL_SetGPUScissor(render_pass, &scissor_rect);
        }

        c.SDL_DrawGPUPrimitives(render_pass, 3, 1, 0, 0);
        c.SDL_EndGPURenderPass(render_pass);
    }

    _ = c.SDL_SubmitGPUCommandBuffer(cmdbuf);

    return true;
}

pub fn quit (context:*Context) void
{
    c.SDL_ReleaseGPUGraphicsPipeline(context.device, fill_pipeline);
    c.SDL_ReleaseGPUGraphicsPipeline(context.device, line_pipeline);

    use_wireframemode = false;
    use_small_viewport = false;
    use_scissor_rect = false;

    common.commonQuit(context);
}

pub const BasicTriangle_Example = common.Example{
    .name = "BasicTriangle",
    .init = init,
    .update = update,
    .draw = draw,
    .quit = quit,
};

