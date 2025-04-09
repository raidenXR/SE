using System;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace librendererFS;

public static unsafe class librendererFS
{
    const string libname = "librendererFS";

    [StructLayout(LayoutKind.Sequential)]
    public struct Position
    {
        public float x,
        public float y,
        public float z,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Color
    {
        public int v,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Context
    {
        public nint device,
        public nint window,
        public nint pipeline,
        public nint vertex_buffer,
        public nint index_buffer,
        public bool is_initialized,
        public bool is_disposed,
        public bool running,
    }

    ///<summary>
    /// A simple struct to hold data for a surface, mesh
    ///</summary>
    [StructLayout(LayoutKind.Sequential)]
    public struct Mesh
    {
        public Position[] positions,
        public Color[] colors,
        public u16[] indices,
        public int len,
    }

    [DllImport(libname)]
    public static extern unsafe Context init ();

    ///<summary>
    /// pitch is the len of the buffer description i.e sizeOf PositionColorVertex
    ///</summary>
    [DllImport(libname)]
    public static extern unsafe void createVertexBuffer (nint s, Position[] pos, Color[] colors, int vertex_size);

    [DllImport(libname)]
    public static extern unsafe void update (nint s);

    [DllImport(libname)]
    public static extern unsafe void quit ();

    [DllImport(libname)]
    public static extern unsafe void draw (nint context);

}

