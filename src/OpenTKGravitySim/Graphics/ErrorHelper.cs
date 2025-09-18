
using OpenTK.Graphics.OpenGL;



namespace OpenTKGravitySim.Graphics;



public static class ErrorHelper
{
    public static int ErrorCheckNum { get; private set; } = 0;
    public static void CheckGLError(bool reset = false)
    {
        ErrorCode error = GL.GetError();
        if (error != ErrorCode.NoError)
        {
            Console.WriteLine($"OpenGL error {ErrorCheckNum}: {error}");
        }

        if (reset) ErrorCheckNum = 0;
        else ErrorCheckNum++;
    }
}
