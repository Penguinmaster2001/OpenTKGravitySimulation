
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

using OpenTKGravitySim.Graphics;

using OpenTKGravitySim.Particles;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;
using OpenTK.Platform.Windows;



namespace OpenTKGravitySim;



internal class SimWindow : GameWindow
{
    private readonly Camera camera;
    private Quad windowQuad;

    private readonly Universe universe;

    private int windowWidth;
    private int windowHeight;

    private ShaderProgram? shaderProgram;
    private string vertexShaderPath = "Shaders/oneQuad.vert";
    // private string fragmentShaderPath = "Shaders/oneQuad.frag";
    private string fragmentShaderPath = "Shaders/raymarching.frag";



    public SimWindow(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
    {
        windowWidth = width;
        windowHeight = height;

        CenterWindow(new Vector2i(windowWidth, windowHeight));

        camera = new(windowWidth, windowHeight, Vector3.Zero);

        windowQuad = new();

        universe = new(100, 1000.0f);
    }



    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);

        windowWidth = e.Width;
        windowHeight = e.Height;
        GL.Viewport(0, 0, windowWidth, windowHeight);
    }



    protected override void OnLoad()
    {
        base.OnLoad();

        shaderProgram = new(vertexShaderPath, fragmentShaderPath);
        
        GL.Enable(EnableCap.DepthTest);
        // GL.FrontFace(FrontFaceDirection.Cw);
        // GL.Enable(EnableCap.CullFace);
        // GL.CullFace(CullFaceMode.Back);
    }



    protected override void OnUnload()
    {
        base.OnUnload();

        windowQuad.Delete();
        shaderProgram?.Delete();
    }



    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        MouseState mouseState = MouseState;
        KeyboardState keyboardState = KeyboardState;

        if (keyboardState.IsKeyReleased(Keys.Escape))
        {
            CursorState = CursorState == CursorState.Grabbed ? CursorState.Normal : CursorState.Grabbed;
        }
        camera.Update(keyboardState, mouseState, args);
        universe.Update(args);
    }



    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.ClearColor(0.0627f, 0.0666f, 0.1019f, 1.0f);
        CheckGLError();
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        CheckGLError();
        // Transformation matrices
        Matrix4 model = Matrix4.Identity;
        Matrix4 view = camera.ViewMatrix;
        Matrix4 projection = camera.ProjectionMatrix;

        if (shaderProgram is not null)
        {
            int positionsLocation = shaderProgram.GetUniformLocation("positions");
            CheckGLError();
            int windowSizeLocation = shaderProgram.GetUniformLocation("windowSize");
            CheckGLError();

            GL.Uniform3(positionsLocation, universe.NumParticles, universe.ParticlePositions.ToArray());
            CheckGLError();
            GL.Uniform2(windowSizeLocation, new Vector2(windowWidth, windowHeight));
            shaderProgram.SetUniform1Int("numParticles", universe.NumParticles);
            shaderProgram.SetCameraUniforms(camera);

            windowQuad.Render(shaderProgram);
        }
        else
        {
            throw new Exception($"shaderProgram is null!!");
        }


        Context.SwapBuffers();
    }



    public static string LoadShaderSource(string filePath)
    {
        string shaderSource = "";

        try
        {
            using (StreamReader reader = new(filePath))
            {
                shaderSource = reader.ReadToEnd();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load shader!!\nFilepath: {filePath}\n{e.Message}");
        }

        return shaderSource;
    }



    private static void CheckGLError()
    {
        ErrorCode error = GL.GetError();
        if (error != ErrorCode.NoError)
        {
            // throw new Exception($"OpenGL error: {error}");
            Console.WriteLine($"OpenGL error: {error}");
        }
    }
}

