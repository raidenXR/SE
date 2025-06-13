const c = @cImport({
    @cInclude("SDL3/SDL.h");
    @cInclude("SDL3/SDL_main.h");
    // @cInclude("lib64/SDL3_shadercross.h");
});

pub usingnamespace(c);
