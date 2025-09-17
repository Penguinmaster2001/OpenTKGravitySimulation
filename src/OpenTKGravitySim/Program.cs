
using OpenTKGravitySim.Graphics;
using OpenTKGravitySim.Particles;



namespace OpenTKGravitySim;



public class Program
{
    private static readonly Universe universe = new(10000, 500.0);



    static void Main()
    {
        string vertexShaderPath = "Shaders/oneQuad.vert";
        string fragmentShaderPath = "Shaders/RenderableRenderer.frag";
        var shaderProgram = new ShaderProgram(vertexShaderPath, fragmentShaderPath);
        var quad = new Quad(shaderProgram);


        using SimWindow simWindow = new(1440, 900, universe, quad);
        Parallel.Invoke(simWindow.Run, universe.Run);
    }
}
