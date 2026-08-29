namespace SE.Renderer

open System
open System.Numerics
open SkiaSharp

type Colormap =
    | Spring = 0
    | Summer = 1
    | Autumn = 2
    | Winter = 3
    | Gray = 4
    | Hot = 5
    | Cool = 6
    | Jet = 7


// [<Struct>]
// type SKColor(r:byte, g:byte, b:byte) =
//     member this.R = r
//     member this.G = g
//     member this.B = b
//     member this.A = 255uy


module Colormaps =

    let [<Literal>] MAP_SIZE = 64
    let [<Literal>] ALPHA = 255uy

    let spring () =
        let buffer = Array.zeroCreate<SKColor> MAP_SIZE
        for i in 0..MAP_SIZE - 1 do
            let lerp = (float i) / (float MAP_SIZE)
            let r = 255uy
            let g = byte (255. * lerp)
            let b = byte (255uy - g)
            buffer[i] <- SKColor(r,g,b)
        buffer

    let summer () =
        let buffer = Array.zeroCreate<SKColor> MAP_SIZE
        for i in 0..MAP_SIZE - 1 do
            let lerp = (float i) / (float MAP_SIZE)
            let r = byte (255. * lerp)
            let g = byte (255. * 0.5 * (1. + lerp))
            let b = byte (255. - 0.4)
            buffer[i] <- SKColor(r,g,b)
        buffer

    let autumn () =
        let buffer = Array.zeroCreate<SKColor> MAP_SIZE
        for i in 0..MAP_SIZE - 1 do
            let lerp = (float i) / (float MAP_SIZE)
            let r = 255uy
            let g = byte (255. * lerp)
            let b = 0uy
            buffer[i] <- SKColor(r,g,b)
        buffer
        
    let winter () =
        let buffer = Array.zeroCreate<SKColor> MAP_SIZE
        for i in 0..MAP_SIZE - 1 do
            let lerp = (float i) / (float MAP_SIZE)
            let r = 0uy
            let g = byte (255. * lerp)
            let b = byte (255. * (1. - 0.5 + lerp))
            buffer[i] <- SKColor(r,g,b)
        buffer

    let gray () =
        let buffer = Array.zeroCreate<SKColor> MAP_SIZE
        for i in 0..MAP_SIZE - 1 do
            let lerp = (float i) / (float MAP_SIZE)
            let r = byte (255. * lerp)
            let g = byte (255. * lerp)
            let b = byte (255. * lerp)
            buffer[i] <- SKColor(r,g,b)
        buffer

    let hot () =
        let buffer = Array.zeroCreate<SKColor> MAP_SIZE
        for n in 0..MAP_SIZE - 1 do
            let lerp = (float n) / (float MAP_SIZE)
            let n1 = int (3.0 * float MAP_SIZE / 8.0)
            let i = int ((float MAP_SIZE - 1.0) * lerp)

            let r = if i < n1 then (1.0 * (float i + 1.0) / float n1) else 1.0
            let g = if i < n1 then 0.0 else (if (i >= n1 && i < 2 * n1) then (1.0 * (float i + 1. - float n1) / float n1) else 1.0)
            let b = if i < 2 * n1 then 0.0 else (1.0 * (float i + 1. - 2. * float n1) / (float MAP_SIZE - 2.0 * float n1))
            buffer[n] <- SKColor(byte (r * 255.), byte (g * 255.), byte (b * 255.))
        buffer

    let cool () =
        let buffer = Array.zeroCreate<SKColor> MAP_SIZE
        for n in 0..MAP_SIZE - 1 do
            let lerp = (float n) / (float MAP_SIZE)
            let i = int (float (MAP_SIZE - 1) * lerp)
            let _array = 1.0 * (float i) / (float MAP_SIZE - 1.0)

            let r = byte (255. * _array)
            let g = byte (255. * (1. - _array))
            let b = 255uy
            buffer[n] <- SKColor(r, g, b)
        buffer


    let jet () =
        let buffer : SKColor[] = Array.zeroCreate MAP_SIZE
        let n = int (Math.Ceiling(float MAP_SIZE / 4.0))
        let cMatrix = Array2D.zeroCreate<float> MAP_SIZE 3

        let nMod = 0
        let array1 = Array.zeroCreate<float> (3 * n - 1)
        let red = Array.zeroCreate<int> array1.Length
        let green = Array.zeroCreate<int> array1.Length
        let blue = Array.zeroCreate<int> array1.Length

        for i = 0 to array1.Length - 1 do
            array1[i] <-
                if i < n then
                    float (i + 1) / float n
                elif i < 2 * n - 1 then
                    1.0
                else
                    float (3 * n - 1 - i) / float n

            green[i] <- int (Math.Ceiling(float n / 2.0)) - nMod + i
            red[i] <- green[i] + n
            blue[i] <- green[i] - n

        let nb =
            blue
            |> Array.filter (fun value -> value > 0)
            |> Array.length

        for i = 0 to MAP_SIZE - 1 do

            for j = 0 to red.Length - 1 do
                if i = red[j] && red[j] < MAP_SIZE then
                    cMatrix[i, 0] <- array1[i - red[0]]

            for j = 0 to green.Length - 1 do
                if i = green[j] && green[j] < MAP_SIZE then
                    cMatrix[i, 1] <- array1[i - green[0]]

            for j = 0 to blue.Length - 1 do
                if i = blue[j] && blue[j] >= 0 then
                    cMatrix[i, 2] <-
                        array1[array1.Length - 1 - nb + i]

        for i = 0 to MAP_SIZE - 1 do
            let redValue =
                byte (Math.Clamp(int (cMatrix[i, 0] * 255.0), 0, 255))

            let greenValue =
                byte (Math.Clamp(int (cMatrix[i, 1] * 255.0), 0, 255))

            let blueValue =
                byte (Math.Clamp(int (cMatrix[i, 2] * 255.0), 0, 255))

            // Make black/zero entries transparent.
            let alpha =
                if redValue = 0uy && greenValue = 0uy && blueValue = 0uy then
                    0uy
                else
                    255uy

            buffer[i] <- SKColor(redValue, greenValue, blueValue, alpha)

        buffer


type Colorbar(colormap:Colormap, z_min:float, z_max:float) =
    let colormap = match colormap with
                    | Colormap.Spring -> Colormaps.spring ()
                    | Colormap.Summer -> Colormaps.summer ()
                    | Colormap.Autumn -> Colormaps.autumn ()
                    | Colormap.Winter -> Colormaps.winter ()
                    | Colormap.Gray   -> Colormaps.gray ()
                    | Colormap.Hot    -> Colormaps.hot ()
                    | Colormap.Cool   -> Colormaps.cool ()
                    | Colormap.Jet    -> Colormaps.jet ()
                    | _ -> failwith "not implemented yet"

    let vertices = Array.zeroCreate<SKPoint> (Colormaps.MAP_SIZE * 4)
    let colors   = Array.zeroCreate<SKColor> (Colormaps.MAP_SIZE * 4)
    let indices  = Array.zeroCreate<uint16> (Colormaps.MAP_SIZE * 6)

    let tick_pts  = Array.zeroCreate<SKPoint> (Colormaps.MAP_SIZE * 2)
    let label_pts = Array.zeroCreate<SKPoint> Colormaps.MAP_SIZE
    let label_vals = Array.zeroCreate<float32> Colormaps.MAP_SIZE
    let mutable label_pts_slice = Memory<SKPoint>(label_pts)

    let mutable w = 800f
    let mutable h = 600f
    let mutable zmin = float32 z_min
    let mutable zmax = float32 z_max
    // let mutable b: Model3.Bounds = {xmin = 0; xmax = 1; ymin = 0; ymax = 1; zmin = 0; zmax = 1}
    // let mutable border = SKRect()
    let mutable is_disposed = false

    let paint = new SKPaint(
        Color = SKColors.Black,
        StrokeWidth = 2f,
        IsAntialias = true,
        TextSize = 16f
    )

    interface IDisposable with
        member this.Dispose() =
            if not is_disposed then
                paint.Dispose()
            is_disposed <- true

    member x.Bounds
        with get() = (zmin,zmax)
        and set(value) =
            zmin <- fst value
            zmax <- snd value

    member x.Colormap
        with get() = ReadOnlySpan<SKColor>(colormap)

    member x.Dispose() = (x :> IDisposable).Dispose()
    
    member x.W 
        with get() = w
        and set(value) = w <- value

    member x.H 
        with get() = h
        and set(value) = h <- value

    member this.Item
        with get(z) =
            if z < zmin then failwith "z is less than zmin"
            if z > zmax then failwith "z is greater than zmax"
            let value = (z - zmin) / (zmax - zmin)
            let c = colormap[int ((float32 (Colormaps.MAP_SIZE - 1)) * value)]
            Vector4(float32 c.Red, float32 c.Green, float32 c.Blue, float32 c.Alpha)
    

    member x.Update () =
        let transform = Matrix3x2.CreateTranslation(0.7f, 0.0f) * Matrix3x2.CreateScale(0.5f, 0.5f)
        let pos = Vector2.Transform(Vector2(1.0f, 0.5f), transform)
        let x = pos.X
        let dx = 0.01f
        let mutable y = pos.Y
        let mutable dy = (1f / 64f)
        let mutable value = 0f

        let mutable n = 0
        let mutable j = 0
        let mutable i = 0
        let mutable c = 0
        let mutable m = 0
        while m < vertices.Length do
            vertices[m + 0] <- SKPoint(w * x, h * (1f - y))
            vertices[m + 1] <- SKPoint(w * (x + dx), h * (1f - y))
            vertices[m + 2] <- SKPoint(w * (x + dx), h * (1f - y - dy))
            vertices[m + 3] <- SKPoint(w * x, h * (1f - y - dy))

            indices[i + 0] <- uint16 (m + 0)
            indices[i + 1] <- uint16 (m + 1)
            indices[i + 2] <- uint16 (m + 3)
            indices[i + 3] <- uint16 (m + 3)
            indices[i + 4] <- uint16 (m + 1)
            indices[i + 5] <- uint16 (m + 2)

            colors[c + 0] <- colormap[int ((float32 (Colormaps.MAP_SIZE - 1)) * value)]
            colors[c + 1] <- colormap[int ((float32 (Colormaps.MAP_SIZE - 1)) * value)]
            colors[c + 2] <- colormap[int ((float32 (Colormaps.MAP_SIZE - 1)) * value)]
            colors[c + 3] <- colormap[int ((float32 (Colormaps.MAP_SIZE - 1)) * value)]

            if m % 48 = 0 then 
                label_vals[n] <- (float32 value) * (zmax - zmin) + zmin
                label_pts[n+0] <-  SKPoint(w * (x + dx + 0.01f), h * (1f - y))
                tick_pts[j + 0] <- SKPoint(w * (x + dx + 0.00f), h * (1f - y) - 9f)
                tick_pts[j + 1] <- SKPoint(w * (x + dx + 0.01f), h * (1f - y) - 9f)
                n <- n + 1
                j <- j + 2
            
            value <- value + dy
            y <- y + (dy / 2f)
            
            m <- m + 4
            i <- i + 6
            c <- c + 4
        label_pts_slice <- Memory<SKPoint>(label_pts, 0, n)


    member this.Draw(canvas:SKCanvas) =
        canvas.DrawVertices(SKVertexMode.Triangles, vertices, null, colors, indices, paint)
        canvas.DrawPoints(SKPointMode.Lines, tick_pts, paint)
        let slice = label_pts_slice.Span
        for i in 0..slice.Length - 1 do
        canvas.DrawText(label_vals[i].ToString("N3"), slice[i].X, slice[i].Y, paint)


    member this.AsTexture (w:float32, h:float32) =
        let transform = Matrix3x2.CreateTranslation(-0.5f, -0.5f) * Matrix3x2.CreateScale(0.8f, 0.8f)
        let pos = Vector2.Transform(Vector2.One, transform)
        let x = pos.X
        let dx = 0.16f
        // let mutable y = 20.f
        let mutable y = pos.Y
        let mutable dy = (1f / 64f)
        let mutable value = 0f

        let mutable n = 0
        let mutable j = 0
        let mutable i = 0
        let mutable c = 0
        let mutable m = 0
        while m < vertices.Length do
            vertices[m + 0] <- SKPoint(w * x, h * (1f - y))
            vertices[m + 1] <- SKPoint(w * (x + dx), h * (1f - y))
            vertices[m + 2] <- SKPoint(w * (x + dx), h * (1f - y - dy))
            vertices[m + 3] <- SKPoint(w * x, h * (1f - y - dy))

            indices[i + 0] <- uint16 (m + 0)
            indices[i + 1] <- uint16 (m + 1)
            indices[i + 2] <- uint16 (m + 3)
            indices[i + 3] <- uint16 (m + 3)
            indices[i + 4] <- uint16 (m + 1)
            indices[i + 5] <- uint16 (m + 2)

            colors[c + 0] <- colormap[int ((float32 (Colormaps.MAP_SIZE - 1)) * value)]
            colors[c + 1] <- colormap[int ((float32 (Colormaps.MAP_SIZE - 1)) * value)]
            colors[c + 2] <- colormap[int ((float32 (Colormaps.MAP_SIZE - 1)) * value)]
            colors[c + 3] <- colormap[int ((float32 (Colormaps.MAP_SIZE - 1)) * value)]

            if m % 48 = 0 then 
                label_vals[n] <- (float32 value) * (zmax - zmin) + zmin
                label_pts[n+0] <-  SKPoint(w * (x + dx + 0.05f), h * (1f - y))
                tick_pts[j + 0] <- SKPoint(w * (x + dx + 0.00f), h * (1f - y) - 9f)
                tick_pts[j + 1] <- SKPoint(w * (x + dx + 0.05f), h * (1f - y) - 9f)
                n <- n + 1
                j <- j + 2
            
            value <- value + dy
            y <- y + (dy / 2f)
            
            m <- m + 4
            i <- i + 6
            c <- c + 4
        label_pts_slice <- Memory<SKPoint>(label_pts, 0, n)
        
        let imageinfo = new SKImageInfo(int w, int h, SKColorType.Rgba8888, SKAlphaType.Premul)
        use bitmap = new SKBitmap(imageinfo)
        // use font = new SKFont()
        use canvas = new SKCanvas(bitmap)

        canvas.Clear(SKColors.Transparent)
        paint.Color <- SKColors.White
        this.Draw(canvas)
        paint.Style <- SKPaintStyle.Stroke
        canvas.DrawRect(SKRect(x*w, h*(1.f-pos.Y), (x+dx)*w, (1.f-y-dy/2.f)*h), paint)
        paint.Color <- SKColors.Black
        paint.Style <- SKPaintStyle.StrokeAndFill
        Texture.create bitmap 0.7f -0.7f 0.3f 1.8f


    // member this.AsMesh () =
    //     let L = 10
    //     let mutable vb = SE.NativeArray.create<float32> (Colormaps.MAP_SIZE*L)
    //     for i in 0..Colormaps.MAP_SIZE-1 do
    //         vb[i*L+0] <- vertices[i].X / w
    //         vb[i*L+1] <- vertices[i].Y / h
    //         vb[i*L+2] <- 0.f  // z-value should be 0
    //         vb[i*L+3] <- 0.f  // normal vector
    //         vb[i*L+4] <- 0.f
    //         vb[i*L+5] <- 0.f
    //         vb[i*L+6] <- float32(colors[i].Red) / 255.f
    //         vb[i*L+7] <- float32(colors[i].Green) / 255.f
    //         vb[i*L+8] <- float32(colors[i].Blue) / 255.f
    //         vb[i*L+9] <- float32(colors[i].Alpha) / 255.f

    //     let ib  = indices |> Array.map (fun i -> uint32 i) |> SE.NativeArray.ofArray
    //     let mesh: SE.Spatial.Mesh = {vertices = vb; indices = ib; L = L}
    //     mesh

        
        

    
