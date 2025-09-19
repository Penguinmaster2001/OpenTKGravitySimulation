
using OpenTKGravitySim.Graphics;
using OpenTKGravitySim.Particles;



namespace OpenTKGravitySim;



public class Program
{
    static void Main()
    {
        var parameters = new SimParameters()
        {
            GravMult = 500.0,
            TimeStep = 0.01,
        };

        var particleProcessor = new CpuParticleProcessor();
        var universe = new Universe(10_000, 500.0, parameters, particleProcessor);

        string vertexShaderPath = "Shaders/glPoints.vert";
        string fragmentShaderPath = "Shaders/glPoints.frag";
        var shaderProgram = new ShaderProgram(vertexShaderPath, fragmentShaderPath);
        var renderer = new PointRenderer(shaderProgram, universe.NumParticles);

        Console.WriteLine($"num particles: {universe.NumParticles}");


        using SimWindow simWindow = new(1440, 900, universe, renderer);
        Parallel.Invoke(simWindow.Run, universe.Run);
    }
}
