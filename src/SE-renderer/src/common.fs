namespace SE.Renderer
open System
open System.Collections.Generic
open System.IO
open System.Drawing
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics

open SkiaSharp
open FFMpegCore
open FFMpegCore.Pipes

open SE
    

[<AllowNullLiteral>]
type Shader(vertex_path:string, fragment_path:string) =
    let mutable handle = 0
    let mutable is_disposed = false
    let uniform_locations = System.Collections.Generic.Dictionary<string,int>()
    
    let dispose () =
        if not is_disposed then
            GL.DeleteProgram(handle)
        is_disposed <- true

    do
        let vs_src = File.ReadAllText(vertex_path)
        let fs_src = File.ReadAllText(fragment_path)
        let vs = GL.CreateShader(ShaderType.VertexShader)
        let fs = GL.CreateShader(ShaderType.FragmentShader)
        GL.ShaderSource(vs, vs_src)
        GL.ShaderSource(fs, fs_src)
        GL.CompileShader(vs)
        GL.CompileShader(fs)

        let mutable vs_compile_success = -1
        let mutable fs_compile_success = -1
        let mutable link_program_success = -1
        GL.GetShader(vs, ShaderParameter.CompileStatus, &vs_compile_success)
        GL.GetShader(fs, ShaderParameter.CompileStatus, &fs_compile_success)
        if vs_compile_success <> (int All.True) then failwith $"Error occured while compiling shader {GL.GetShaderInfoLog(vs)}"
        if fs_compile_success <> (int All.True) then failwith $"Error occured while compiling shader {GL.GetShaderInfoLog(fs)}"

        handle <- GL.CreateProgram()
        GL.AttachShader(handle, vs)
        GL.AttachShader(handle, fs)
        GL.LinkProgram(handle)
        GL.GetProgram(handle, GetProgramParameterName.LinkStatus, &link_program_success)
        if link_program_success <> (int All.True) then failwith $"Error occurred while linking program {handle}"

        GL.DetachShader(handle, vs)
        GL.DetachShader(handle, fs)
        GL.DeleteShader(fs)
        GL.DeleteShader(vs)

        let mutable number_of_uniforms = 0
        GL.GetProgram(handle, GetProgramParameterName.ActiveUniforms, &number_of_uniforms)

        for i in 0..number_of_uniforms - 1 do
            let mutable n0 = 0
            let mutable n1 = Unchecked.defaultof<ActiveUniformType>
            let key = GL.GetActiveUniform(handle, i, &n0, &n1)
            let location = GL.GetUniformLocation(handle, key)
            uniform_locations.Add(key, location)
            // printfn "%s, %d" key location


    interface IDisposable with 
        member this.Dispose() = dispose()

    member this.Dispose() = dispose()
    
    member this.Use() = GL.UseProgram(handle)
    
    member this.GetAttribLocation(attribName:string) = GL.GetAttribLocation(handle, attribName)
    
    member this.SetInt(name:string, data:int) =
        GL.UseProgram(handle)
        GL.Uniform1(uniform_locations[name], data)

    member this.SetFloat(name:string, data:float32) =
        GL.UseProgram(handle)
        GL.Uniform1(uniform_locations[name], data)

    member this.SetMatrix3(name:string, data:Matrix3) =
        let mutable _matrix = data
        GL.UseProgram(handle)
        GL.UniformMatrix3(uniform_locations[name], true, &_matrix)
        
    member this.SetMatrix4(name:string, data:Matrix4) =
        let mutable _matrix = data
        GL.UseProgram(handle)
        GL.UniformMatrix4(uniform_locations[name], true, &_matrix)

    member this.SetVector2(name:string, data:Vector2) =
        GL.UseProgram(handle)
        GL.Uniform2(uniform_locations[name], data)

    member this.SetVector3(name:string, data:Vector3) =
        GL.UseProgram(handle)
        GL.Uniform3(uniform_locations[name], data)

    member this.SetVector4(name:string, data:Vector4) =
        GL.UseProgram(handle)
        GL.Uniform4(uniform_locations[name], data)

    /// copies a buffer object to a shader
    member this.SetBufferObject(data:nativeint, size:int) =
        GL.UseProgram(handle)
        GL.BufferData(BufferTarget.UniformBuffer, size, data, BufferUsageHint.DynamicCopy)
        
    member this.Uniforms() = uniform_locations.Keys

    member this.PrintInfo() =
        printfn "shader.active_uniforms: %d" uniform_locations.Count
        for pair in uniform_locations do printfn "%s, %d" pair.Key pair.Value        
    

[<AllowNullLiteral>]
type Camera(position:Vector3, aspectRatio:float32) =
    let mutable position = position
    let mutable aspect_ratio = max aspectRatio 1f
    let mutable front = -Vector3.UnitZ
    let mutable up = Vector3.UnitY
    let mutable right = Vector3.UnitX

    let mutable pitch = 0.0f
    let mutable yaw = -MathHelper.PiOver2
    let mutable fov = MathHelper.PiOver2

    let updateVectors() =
        front.X <- MathF.Cos(pitch) * MathF.Cos(yaw)
        front.Y <- MathF.Sin(pitch)
        front.Z <- MathF.Cos(pitch) * MathF.Sin(yaw)

        front <- Vector3.Normalize(front)
        right <- Vector3.Normalize(Vector3.Cross(front, Vector3.UnitY))
        up <- Vector3.Normalize(Vector3.Cross(right, front))

    member this.Position with get() = position and set(value) = position <- value

    member this.AspectRatio
        with private get() = aspect_ratio
        and set(value:float32) = aspect_ratio <- if value > 0f then value else 1f
        
    member this.Front with get() = front
    member this.Up with get() = up
    member this.Right with get() = right

    member this.Pitch
        with get() = MathHelper.RadiansToDegrees(pitch)
        and set(value:float32) =
            let angle = MathHelper.Clamp(value, -89f, 89f)
            pitch <- MathHelper.DegreesToRadians(angle)
            updateVectors()

    member this.Yaw
        with get() = MathHelper.RadiansToDegrees(yaw)
        and set(value:float32) =
            yaw <- MathHelper.DegreesToRadians(value)
            updateVectors()

    member this.Fov
        with get() = MathHelper.RadiansToDegrees(fov)
        and set(value:float32) =
            let angle = MathHelper.Clamp(value, 1f, 90f)
            fov <- MathHelper.DegreesToRadians(angle)

    member this.GetViewMatrix() = Matrix4.LookAt(position, position + front, up)

    member this.GetProjectionMatrix() = Matrix4.CreatePerspectiveFieldOfView(fov, aspect_ratio, 0.01f, 100f)
            
            
// [<AllowNullLiteral>]        
// type Texture(gl_handle:int) =
//     member this.Handle = gl_handle

//     static member loadFromFile (path:string) =
//         let _handle = GL.GenTexture()
//         GL.ActiveTexture(TextureUnit.Texture0)
//         GL.BindTexture(TextureTarget.Texture2D, _handle)
//         StbImage.stbi_set_flip_vertically_on_load(1)
//         use stream = File.OpenRead(path)
//         let image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha)
//         let _rgba = OpenTK.Graphics.OpenGL4.PixelFormat.Rgba
//         let _rgba_internal = OpenTK.Graphics.OpenGL4.PixelInternalFormat.Rgba
//         GL.TexImage2D(TextureTarget.Texture2D, 0, _rgba_internal, image.Width, image.Height, 0, _rgba, PixelType.UnsignedByte, image.Data)
//         GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int TextureMinFilter.Linear));
//         GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int TextureMagFilter.Linear));
//         GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int TextureWrapMode.Repeat));
//         GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int TextureWrapMode.Repeat));
//         GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
//         Texture(_handle);

    // member this.Use(_unit:TextureUnit) =
    //     GL.ActiveTexture(_unit)
    //     GL.BindTexture(TextureTarget.Texture2D, this.Handle)
        

module Shaders =
    let shaders_map = new System.Collections.Generic.Dictionary<string,Shader>()

    let load (shaders:list<string*string*string>) =
        for (tag,vs,fs) in shaders do
            shaders_map.Add(tag, new Shader(vs,fs))

    let unload () =
        for pair in shaders_map do
            pair.Value.Dispose()
        shaders_map.Clear()            

    let init (tag:string, vs_path:string, fs_path:string) =
        let shader = new Shader(vs_path, fs_path)
        shaders_map.Add(tag, shader)
        shader

    let deinit (tag:string) =
        let mutable shader: Shader = null
        match shaders_map.TryGetValue(tag, &shader) with
        | true  -> shader.Dispose()
        | false -> ()

    let get (tag:string) =
        shaders_map[tag]
    

type SKBitmapFrame(bmp:SKBitmap, pixels:narray<byte>) =
    let Source = bmp

    do
        if bmp.ColorType <> SKColorType.Bgra8888 then
            printfn "colortype: %A" bmp.ColorType
            failwith "only 'bgra' colortype is supported"

    interface IDisposable with
        member this.Dispose() =
            Source.Dispose()
            pixels.Dispose()

    interface IVideoFrame with
        member this.Width = Source.Width
        member this.Height = Source.Height
        member this.Format = "bgra"
    
        member this.Serialize(pipe:System.IO.Stream) =
            pipe.Write(Source.Bytes, 0, Source.Bytes.Length)

        member this.SerializeAsync(pipe:System.IO.Stream, token:System.Threading.CancellationToken) =
            pipe.WriteAsync(Source.Bytes, 0, Source.Bytes.Length, token)

    member this.Bitmap = Source
    member this.Pixels = pixels

        

module VideoCapture =
    
    let capture_frame (frames:ResizeArray<IVideoFrame>) (wnd:SE_Window) =
        let size = wnd.FramebufferSize
        let w = size.X
        let h = size.Y
        // use pixels = NativeArray.rent<byte> (w*h*4)
        let pixels = NativeArray.create<byte> (w*h*4)  // This leaks memory, use regular arrays, DO NOT POOL
        // let pixels = Array.zeroCreate<byte> (w*h*4)
        GL.ReadPixels(0, 0, w, h, PixelFormat.Bgra, PixelType.UnsignedByte, pixels.ToInt())

        let bitmap = new SKBitmap(w, h, SKColorType.Bgra8888, SKAlphaType.Premul)
        // let bit = new SKBitmap(pixels)
        // use pixels_ptr = fixed pixels
        let success = bitmap.InstallPixels(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Premul), pixels.ToInt(), w*4)

        if not success then
            failwith "failed to install pixels on SKBitmap"
        
        let frame = new SKBitmapFrame(bitmap, pixels)
        frames.Add(frame :> IVideoFrame)


    let create_video_from_frames (path:string) (frames:seq<IVideoFrame>) (wnd:SE_Window) =
        if System.IO.File.Exists(path) then
            System.IO.File.Delete(path)
        if System.IO.File.Exists("./tmp.png") then
            System.IO.File.Delete("./tmp.png")

        let size = wnd.FramebufferSize
        printfn "framebuffer: (%d, %d)" size.X size.Y
        // save last frame as image
        printfn "image png conversion"
    
        let last_frame = (Seq.last frames) :?> SKBitmapFrame
        let bmp = last_frame.Bitmap
        use tmp_img = SKImage.FromBitmap(bmp)
        use tmp_dat = tmp_img.Encode(SKEncodedImageFormat.Png, 80)
        use tmp_stm = System.IO.File.OpenWrite("./tmp.png")
        tmp_dat.SaveTo(tmp_stm)
        tmp_stm.Close()
        printfn "image png saved"

        let source = new RawVideoPipeSource(frames, FrameRate = 30)
        let success = FFMpegArguments
                        .FromPipeInput(source)
                        .OutputToFile(path, true, (fun options -> options.WithVideoCodec("libvpx-vp9").WithVideoFilters(fun filter -> filter.Mirror(Enums.Mirroring.Vertical) |> ignore) |> ignore))
                        // .ProcessSynchronously()

        printfn "start processing video conversion on %d frames" (Seq.length frames)
        let s = success.ProcessSynchronously()
        // success
        let str = if s then "video conversion done!" else "video conversion failed"
        printfn "%s" str
        

