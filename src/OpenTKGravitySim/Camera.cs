
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.GraphicsLibraryFramework;



namespace OpenTKGravitySim;



internal class Camera
{
    // TODO: These should be from another class that manages the screen
    private float _screenWidth;
    public float ScreenWidth
    {
        get => _screenWidth;
        set
        {
            _screenWidth = value;
            UpdateProjectionMatrix();
        }
    }

    private float _screenHeight;
    public float ScreenHeight
    {
        get => _screenHeight;
        set
        {
            _screenHeight = value;
            UpdateProjectionMatrix();
        }
    }
    
    private double _fov = 110.0;
    public double FOV
    {
        get => _fov;
        set
        {
            _fov = MathHelper.Clamp(value, 0.1, 179.9);
            UpdateProjectionMatrix();
        }
    }

    private float nearClip = 1.0f;
    private float farClip = 50_000.0f;
    private double sensitivity = 100.0f;
    private float MaxPitch = 89.99f;
    private float MinPitch = -89.99f;

    public double MovementSpeed { get; private set; } = 1000.0f;
    public Vector3 Velocity { get; private set; }
    private bool firstMove = true;
    public Vector2 mouseLastPos;
    public double mouseSmoothFactor = 0.2;

    public Matrix4 ViewMatrix => Matrix4.LookAt(Position, Position + forward, up);

    private Matrix4 _projectionMatrix;
    public Matrix4 ProjectionMatrix { get => _projectionMatrix; private set => _projectionMatrix = value; }

    // TODO: This should be an affine matrix
    public Vector3 Position { get; private set; }
    
    /// <summary>
    /// Yaw and pitch in degrees
    /// </summary>
    public Vector2 RotationEuler;
    public Vector3 up = Vector3.UnitY;
    public Vector3 forward = Vector3.UnitZ;
    public Vector3 right = Vector3.UnitX;



    public Camera(float screenWidth, float screenHeight, Vector3 position)
    {
        _screenWidth = screenWidth;
        _screenHeight = screenHeight;
        UpdateProjectionMatrix();
        Velocity = Vector3.Zero;
        Position = position;
        RotationEuler = Vector2.Zero;
    }



    private void UpdateProjectionMatrix()
    {
        ProjectionMatrix = Matrix4.CreatePerspectiveFieldOfView((float) MathHelper.DegreesToRadians(FOV),
                                                                ScreenWidth / ScreenHeight,
                                                                nearClip,
                                                                farClip);
    }



    public void InputController(KeyboardState keyboardState, MouseState mouseState, double frameDelta)
    {
        Vector3 keyboardDirection = Vector3.Zero;

        if (keyboardState.IsKeyDown(Keys.W))
        {
            keyboardDirection += forward;
        }
        if (keyboardState.IsKeyDown(Keys.S))
        {
            keyboardDirection += -forward;
        }
        if (keyboardState.IsKeyDown(Keys.D))
        {
            keyboardDirection += right;
        }
        if (keyboardState.IsKeyDown(Keys.A))
        {
            keyboardDirection += -right;
        }
        if (keyboardState.IsKeyDown(Keys.E))
        {
            keyboardDirection += up;
        }
        if (keyboardState.IsKeyDown(Keys.Q))
        {
            keyboardDirection += -up;
        }

        if (keyboardDirection.LengthSquared > 0.0)
        {
            keyboardDirection.Normalize();
        }

        double scrollAmount = frameDelta * -mouseState.ScrollDelta.Y;
        if (keyboardState.IsKeyDown(Keys.LeftControl))
        {
            FOV += 50.0f * scrollAmount;
        }
        else if (keyboardState.IsKeyDown(Keys.LeftShift))
        {
            sensitivity += 10.0 * sensitivity * scrollAmount;
            sensitivity = MathHelper.Clamp(sensitivity, 1.0f, 10_000.0f);
        }
        else
        {
            MovementSpeed += 5.0 * MovementSpeed * scrollAmount;
            MovementSpeed = MathHelper.Clamp(MovementSpeed, 10.0, 100_000.0);
        }

        Velocity = (float) MovementSpeed * keyboardDirection;

        Vector2 mouseCurPos = new(mouseState.X, -mouseState.Y);
        if (firstMove)
        {
            mouseLastPos = mouseCurPos;
            firstMove = false;
        }
        else
        {
            Vector2 smoothMousePos = Vector2.Lerp(mouseLastPos, mouseCurPos, (float) Math.Max(1.0, 500.0 * Math.Max(frameDelta, 0.002) * mouseSmoothFactor));
            Vector2 mouseDelta = smoothMousePos - mouseLastPos;
            mouseLastPos = smoothMousePos;

            RotationEuler += (Vector2) (Math.Max(frameDelta, 0.002) * (FOV / 180.0) * sensitivity * (Vector2d) mouseDelta);

            if (RotationEuler.Y > MaxPitch) RotationEuler.Y = MaxPitch;
            else if (RotationEuler.Y < MinPitch) RotationEuler.Y = MinPitch;
        }
    }



    private void UpdateVectors(float frameDelta)
    {
        Position += frameDelta * Velocity;

        forward.X = MathF.Cos(MathHelper.DegreesToRadians(RotationEuler.Y)) * MathF.Cos(MathHelper.DegreesToRadians(RotationEuler.X));
        forward.Y = MathF.Sin(MathHelper.DegreesToRadians(RotationEuler.Y));
        forward.Z = MathF.Cos(MathHelper.DegreesToRadians(RotationEuler.Y)) * MathF.Sin(MathHelper.DegreesToRadians(RotationEuler.X));
        forward.Normalize();

        right = Vector3.Normalize(Vector3.Cross(forward, Vector3.UnitY));

        up = Vector3.Normalize(Vector3.Cross(right, forward));
    }



    public void Update(KeyboardState keyboardState, MouseState mouseState, FrameEventArgs e)
    {
        float frameDelta = (float) e.Time;

        InputController(keyboardState, mouseState, e.Time);

        UpdateVectors(frameDelta);
    }
}
