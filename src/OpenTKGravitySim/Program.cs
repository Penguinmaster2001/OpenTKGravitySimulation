
using OpenTKGravitySim.Graphics;
using OpenTKGravitySim.Particles;



namespace OpenTKGravitySim;



public class Program
{
    private static readonly Universe universe = new(5_000, 500.0);



    static void Main()
    {
        string vertexShaderPath = "Shaders/glPoints.vert";
        string fragmentShaderPath = "Shaders/glPoints.frag";
        var shaderProgram = new ShaderProgram(vertexShaderPath, fragmentShaderPath);
        var renderer = new PointRenderer(shaderProgram, universe.NumParticles);

        Console.WriteLine($"num particles: {universe.NumParticles}");


        using SimWindow simWindow = new(1440, 900, universe, renderer);
        Parallel.Invoke(simWindow.Run, universe.Run);
    }
}
