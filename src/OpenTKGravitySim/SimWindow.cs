
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

using OpenTKGravitySim.Graphics;

using OpenTKGravitySim.Particles;
using ErrorCode = OpenTK.Graphics.OpenGL4.ErrorCode;



namespace OpenTKGravitySim;



internal class SimWindow : GameWindow
{
    private readonly Camera camera;
    private Quad windowQuad;

    private readonly Universe universe;
    private List<Vector3> particlePositions;

    private int windowWidth;
    private int windowHeight;

    private ShaderProgram shaderProgram;
    private string vertexShaderPath = "Shaders/oneQuad.vert";
    private string fragmentShaderPath = "Shaders/oneQuad.frag";
    // private string fragmentShaderPath = "Shaders/raymarching.frag";



    public SimWindow(int width, int height, Universe universe) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
    {
        windowWidth = width;
        windowHeight = height;

        CenterWindow(new Vector2i(windowWidth, windowHeight));

        camera = new(windowWidth, windowHeight, Vector3.Zero);
        shaderProgram = new();

        windowQuad = new(universe.NumParticles);

        this.universe = universe;
        particlePositions = [];
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

        shaderProgram.CreateNewProgram(vertexShaderPath, fragmentShaderPath);
        
        GL.Enable(EnableCap.DepthTest);
    }



    protected override void OnUnload()
    {
        base.OnUnload();

        windowQuad.Delete();
        shaderProgram.Delete();
        universe.Running = false;
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

        particlePositions = universe.GetParticlePositions();
    }



    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);

        GL.ClearColor(0.0627f, 0.0666f, 0.1019f, 1.0f);
        GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);

        int windowSizeLocation = shaderProgram.GetUniformLocation("windowSize");

        GL.Uniform2(windowSizeLocation, new Vector2(windowWidth, windowHeight));
        CheckGLError();
        shaderProgram.SetUniform1Int("numParticles", universe.NumParticles);
        CheckGLError();
        shaderProgram.SetCameraUniforms(camera);
        CheckGLError();

        windowQuad.Render(shaderProgram, particlePositions);
        CheckGLError(true);

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

