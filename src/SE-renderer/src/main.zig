const std = @import("std");
const c = @import("c.zig");


pub fn main () !void
{
    var wnd: ?*c.SDL_Window = null;
    defer c.SDL_DestroyWindow (wnd);
    var renderer: ?*c.SDL_Renderer = null;
    defer c.SDL_DestroyRenderer (renderer);
    
    _ = c.SDL_SetAppMetadata ("Example clear", "1.0", "com.example");

    if (!c.SDL_Init (c.SDL_INIT_VIDEO))
    {
        // c.SDL_Log ("couldn't initialize SDL: {s}\n", .{c.SDL_GetError()});
        // return c.SDL_APP_FAILURE;
        @panic ("SDL_init failed");
    }
    defer c.SDL_Quit();

    if (!c.SDL_CreateWindowAndRenderer ("examples", 640, 480, 0, &wnd, &renderer))
    {
        // c.SDL_Log ("couldn't create window/renderer: {s}\n", .{c.SDL_GetError()});
        // return c.SDL_APP_FAILURE;
        @panic ("SDL_create_window and renderer failed");
    }


    var running = true;
    
    while (running)
    {
        var event: c.SDL_Event = undefined;
        while (c.SDL_PollEvent (&event))
        {
            if (event.type == c.SDL_EVENT_QUIT) running = false;
        }

        const now: f64 = @floatFromInt(c.SDL_GetTicks() / 1000);
        const r: f32 = @floatCast(0.5 + 0.5 * c.SDL_sin(now));
        const g: f32 = @floatCast(0.5 + 0.5 * c.SDL_sin(now + c.SDL_PI_D * 2.0 / 3.0));
        const b: f32 = @floatCast(0.5 + 0.5 * c.SDL_sin(now + c.SDL_PI_D * 4 / 3.0));

        _ = c.SDL_SetRenderDrawColorFloat (renderer, r, g, b, c.SDL_ALPHA_OPAQUE_FLOAT);            
        _ = c.SDL_RenderClear (renderer);
        _ = c.SDL_RenderPresent (renderer);
    }
}
