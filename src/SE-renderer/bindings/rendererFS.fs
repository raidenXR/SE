open System
open System.Runtime.InteropServices
open System.Diagnostics

module librendererFS = 
    let [<Literal>] libname = "librendererFS"

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type Position = { 
        x: float32
        y: float32
        z: float32
    }

    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type Color = { 
        v: int
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

    /// A simple struct to hold data for a surface, mesh
    [<Struct; StructLayout(LayoutKind.Sequential)>]
    type Mesh = { 
        positions: Position[]
        colors: Color[]
        indices: u16[]
        len: int
    }

    [<DllImport(libname)>]
    extern Context init ();

    /// pitch is the len of the buffer description i.e sizeOf PositionColorVertex
    [<DllImport(libname)>]
    extern void createVertexBuffer (nativeint s, Position[] pos, Color[] colors, int vertex_size);

    [<DllImport(libname)>]
    extern void update (nativeint s);

    [<DllImport(libname)>]
    extern void quit ();

    [<DllImport(libname)>]
    extern void draw (nativeint context);


