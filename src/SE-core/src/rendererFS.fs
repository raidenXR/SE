module LibSErenderer
    open System
    open System.Runtime.InteropServices
    open System.Diagnostics
        
    [<Literal>]
    let libname = "./native/libSE-renderer.so"

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type Position = {
        X: float32
        Y: float32
        Z: float32
    }

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type Color = {
        V: uint
    }
    
    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type Context = {
        device: nativeint
        window: nativeint
        pipeline: nativeint
        vertex_buffer: nativeint
        index_buffer: nativeint
        is_initialized: bool
        is_disposed: bool
        running: bool
    }

    [<DllImport(libname)>]
    extern Context init(string name);
    
    [<DllImport(libname)>]
    extern void createVertexBuffer(Context* s, Position[] pos, Color[] colors, uint32 vertex_len, uint32 vertex_buffer_len);    

    [<DllImport(libname)>]
    extern void update(Context* s, uint32 vertex_len);

    [<DllImport(libname)>]
    extern void draw(Context* s, uint32 vertex_len);

    [<DllImport(libname)>]
    extern void quit(Context* s);

