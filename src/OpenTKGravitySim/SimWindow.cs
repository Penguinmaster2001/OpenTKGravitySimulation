
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using OpenTKGravitySim.Graphics;
using OpenTKGravitySim.Particles;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;



namespace OpenTKGravitySim;



public class SimWindow : GameWindow
{
    public IRenderer Renderer;



    private readonly Camera _camera;

    private readonly Universe _universe;

    private int windowWidth;
    private int windowHeight;



    public SimWindow(int width, int height, Universe universe, IRenderer renderer) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
    {
        windowWidth = width;
        windowHeight = height;

        CenterWindow(new Vector2i(windowWidth, windowHeight));

        _camera = new(windowWidth, windowHeight, new(-750.0f, 250.0f, 0.0f));

        Renderer = renderer;

        _universe = universe;
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

        Renderer.Initialize();

        GL.Enable(EnableCap.DepthTest);
    }



    protected override void OnUnload()
    {
        base.OnUnload();

        Renderer.Delete();
        _universe.Paused = false;
        _universe.Running = false;
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

        if (keyboardState.IsKeyReleased(Keys.P))
        {
            _universe.Paused = !_universe.Paused;
        }
        _camera.Update(keyboardState, mouseState, args);
    }



    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.ClearColor(0.0627f, 0.0666f, 0.1019f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        int windowSizeLocation = Renderer.ShaderProgram.GetUniformLocation("windowSize");

        GL.Uniform2(windowSizeLocation, new Vector2(windowWidth, windowHeight));
        CheckGLError();
        Renderer.ShaderProgram.SetCameraUniforms(_camera);
        CheckGLError();

        Renderer.Render(_universe.Particles);
        CheckGLError(true);

        Context.SwapBuffers();

        // Console.WriteLine($"Simulation time: {universe.SimulationTime}");
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


    int errorCheckNum = 0;
    private void CheckGLError(bool reset = false)
    {
        ErrorCode error = GL.GetError();
        if (error != ErrorCode.NoError)
        {
            Console.WriteLine($"OpenGL error {errorCheckNum}: {error}");
        }

        if (reset) errorCheckNum = 0;
        else errorCheckNum++;
    }
}

