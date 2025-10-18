namespace SE.Renderer
open System
open System.IO
open System.Drawing
open StbImageSharp
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics


type Model(vertices: array<float32>, indices: array<uint32>) =
    let mutable scale = Matrix4.Identity

    member this.Vertices with get() = vertices
    member this.Indices with get() = indices
    member this.Stride with get() = 10 * sizeof<float32>
    member this.Attrib0 with get() = 0 * sizeof<float32>
    member this.Attrib1 with get() = 3 * sizeof<float32>
    member this.Attrib2 with get() = 6 * sizeof<float32>
    member this.VerticesBufferSize with get() = vertices.Length * sizeof<float32>
    member this.IndicesBufferSize with get() = indices.Length * sizeof<uint32>

    member this.Scale with get() = scale and set(value) = scale <- value

    member this.Print() =
        let v = vertices
        let i = indices
        let mutable j = 0
        while j < 100 do
            printfn "[%g, %g, %g], [%g, %g, %g]" v[j + 0] v[j + 1] v[j + 2] v[j + 3] v[j + 4] v[j + 5]
            j <- j + 10

        j <- 0
        while j < 30 do
            printfn "[%d, %d, %d]" i[j + 0] i[j + 1] i[j + 2]
            j <- j + 3

    member this.SaveAsTxt(path:string) =
        use fs = System.IO.File.CreateText(path)
        fs.WriteLine("Vertices")
        let mutable n = 0
        for v in vertices do
            if n = 10 then
                fs.Write("\n")
                n <- 0
            fs.Write($"{v}, ")
            n <- n + 1
        fs.WriteLine("\nIndices")
        n <- 0
        for v in indices do
            if n = 3 then
                fs.Write("\n")
                n <- 0
            fs.Write($"{v}, ")
            n <- n + 1

    
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
            
            
[<AllowNullLiteral>]        
type Texture(gl_handle:int) =
    member this.Handle = gl_handle

    static member loadFromFile (path:string) =
        let _handle = GL.GenTexture()
        GL.ActiveTexture(TextureUnit.Texture0)
        GL.BindTexture(TextureTarget.Texture2D, _handle)
        StbImage.stbi_set_flip_vertically_on_load(1)
        use stream = File.OpenRead(path)
        let image = ImageResult.FromStream(stream, ColorComponents.RedGreenBlueAlpha)
        let _rgba = OpenTK.Graphics.OpenGL4.PixelFormat.Rgba
        let _rgba_internal = OpenTK.Graphics.OpenGL4.PixelInternalFormat.Rgba
        GL.TexImage2D(TextureTarget.Texture2D, 0, _rgba_internal, image.Width, image.Height, 0, _rgba, PixelType.UnsignedByte, image.Data)
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int TextureMinFilter.Linear));
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int TextureMagFilter.Linear));
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int TextureWrapMode.Repeat));
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int TextureWrapMode.Repeat));
        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);
        Texture(_handle);

    member this.Use(_unit:TextureUnit) =
        GL.ActiveTexture(_unit)
        GL.BindTexture(TextureTarget.Texture2D, this.Handle)
        
