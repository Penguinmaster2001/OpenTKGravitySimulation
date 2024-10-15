
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

using OpenTKTutorial.Graphics;

using OpenTKGravitySim.Particles;



namespace OpenTKGravitySim;



internal class SimWindow : GameWindow
{
    private readonly Camera camera;

    private readonly Universe universe;

    private int windowWidth;
    private int windowHeight;

    private ShaderProgram? shaderProgram;
    private string vertexShaderPath = "Shaders/default.vert";
    private string fragmentShaderPath = "Shaders/default.frag";



    public SimWindow(int width, int height) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
    {
        windowWidth = width;
        windowHeight = height;

        CenterWindow(new Vector2i(windowWidth, windowHeight));

        camera = new(windowWidth, windowHeight, Vector3.Zero);

        universe = new(300, 1000.0f);
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

        universe.Delete();
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
        universe.Update(camera, args);
    }



    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.ClearColor(0.0627f, 0.0666f, 0.1019f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        // Transformation matrices
        Matrix4 model = Matrix4.Identity;
        Matrix4 view = camera.ViewMatrix;
        Matrix4 projection = camera.ProjectionMatrix;

        if (shaderProgram is not null)
        {
            int modelLocation = GL.GetUniformLocation(shaderProgram.ID, "model");
            int viewLocation = GL.GetUniformLocation(shaderProgram.ID, "view");
            int projectionLocation = GL.GetUniformLocation(shaderProgram.ID, "projection");

            GL.UniformMatrix4(modelLocation, true, ref model);
            GL.UniformMatrix4(viewLocation, true, ref view);
            GL.UniformMatrix4(projectionLocation, true, ref projection);

            universe.Render(shaderProgram);
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
}

