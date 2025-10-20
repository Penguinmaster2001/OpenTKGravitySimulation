
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using OpenTKGravitySim.Graphics;
using OpenTKGravitySim.Particles;



namespace OpenTKGravitySim;



public class SimWindow : GameWindow
{
    public IRenderer Renderer;

    private GlSettings _glSettings;



    private readonly IGpuJobManager _gpuJobManager;



    private readonly Camera _camera;

    private readonly Universe _universe;

    private int windowWidth;
    private int windowHeight;



    public SimWindow(int width, int height, Universe universe, IRenderer renderer, GlSettings glSettings, IGpuJobManager gpuJobManager) : base(GameWindowSettings.Default, NativeWindowSettings.Default)
    {
        windowWidth = width;
        windowHeight = height;

        CenterWindow(new Vector2i(windowWidth, windowHeight));

        _camera = new(windowWidth, windowHeight, new(-750.0f, 250.0f, 0.0f));

        Renderer = renderer;

        _universe = universe;

        _glSettings = glSettings;

        _gpuJobManager = gpuJobManager;
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

        if (!GlUserManager.GlContextInitialized())
        {
            Console.WriteLine("Failed to init renderer");

            Close();
        }

        // GL.Enable(EnableCap.DepthTest);
        // GL.Enable(EnableCap.VertexProgramPointSize);
        // GL.Enable(EnableCap.Blend);
        // GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        _glSettings.Apply();
    }



    protected override void OnUnload()
    {
        base.OnUnload();

        GlUserManager.Delete();
        _universe.Paused = false;
        _universe.Running = false;

        // The universe will queue another job on the gpu that is never run, so we must cancel it
        // TODO: Do this better
        _gpuJobManager.CancelJobs();
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

        if (keyboardState.IsKeyReleased(Keys.Space))
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

        Renderer.ShaderProgram.SetCameraUniforms(_camera);

        _gpuJobManager.RunJobs();

        Renderer.Render(_universe.RefParticles);

        Context.SwapBuffers();
    }
}
