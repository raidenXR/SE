namespace SE.Renderer
open System
open OpenTK.Graphics.OpenGL4
open OpenTK.Mathematics

[<AllowNullLiteral>]
type Camera(position:Vector3, aspectRatio:float32) =
    let mutable position = position
    let mutable speed = 1.5f
    let mutable sensitivity = 0.2f
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

    new() = Camera(Vector3(0.5f, 0.5f, 0.5f), 4.f/3.f)

    member this.Position with get() = position and set(value) = position <- value
    member this.Speed with get() = speed and set(value) = speed <- value
    member this.Sensitivity with get() = sensitivity and set(value) = sensitivity <- value

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

    
