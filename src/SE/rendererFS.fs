module LibSErenderer
    open System
    open System.Runtime.InteropServices
    open System.Diagnostics
        
    [<Literal>]
    let libname = "SE-rendereri.so"

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

    [<DllImport(libname)>]
    extern void init(uint vertex_size, uint vertex_buffer_len, Position[] pos, Color[] colors);

    [<DllImport(libname)>]
    extern void update();

    [<DllImport(libname)>]
    extern void draw(uint vertex_len);

    [<DllImport(libname)>]
    extern void quit();

