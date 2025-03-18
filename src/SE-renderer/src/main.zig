const std = @import("std");
const c = @import("c.zig");
const common = @import("common.zig");
// const ClearScrean = @import("ClearScreen.zig");
// const BasicTriangle = @import("BasicTriangle.zig");
// const BasicVertexBuffer = @import("BasicVertexBuffer.zig");
// const BlitCube = @import("BlitCube.zig");


const samples_names = [_][]const u8 {
    "ClearScreen",
    "BasicTriangle",
    "BasicVertexBuffer",
};

const examples = [_]*const common.Example{
    &@import("ClearScreen.zig").ClearScreen_Example,
    &@import("BasicTriangle.zig").BasicTriangle_Example,
    &@import("BasicVertexBuffer.zig").BasicVertexBuffer_Example,
    &@import("BlitCube.zig").BlitCube_Example,
};

const idx = 2;


pub fn main () !void
{
    var context: common.Context = undefined;
    var quit = false;
    var last_time: f32 = 0.0;

    if (!c.SDL_Init(c.SDL_INIT_VIDEO | c.SDL_INIT_GAMEPAD)) {
        // common.failwith("no example named: {s}\n", .{examplename});
    }

    common.initializeAssetLoader();

    const ticks = c.SDL_GetTicks();
    var newtime: f32 = @floatFromInt(ticks);
    newtime /= 1000.0;
    context.delta_time = newtime - last_time;
    last_time = newtime;
    
    _ = examples[idx].init(&context);

    while (!quit) {
        var evt: c.SDL_Event = undefined;
        while (c.SDL_PollEvent(&evt)) 
        {
            if (evt.type == c.SDL_EVENT_QUIT) {
                quit = true;
            }
            if (quit) {
                // c.SDL_zero(&context);
                examples[idx].quit(&context);
                break;
            }
            
            if (!examples[idx].update(&context)) {
                @panic("update failed");
            }    
            if (!examples[idx].draw(&context)) {
                @panic("draw failed");
            }         
        }
    }
}
