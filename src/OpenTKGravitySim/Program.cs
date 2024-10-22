
using OpenTKGravitySim.Particles;



namespace OpenTKGravitySim;



public class Program
{
    private static readonly Universe universe = new(10_000, 1000.0f);



    static void Main(string[] args)
    {
        universe.Run();
        // using (SimWindow simWindow = new(1440, 900, universe))
        // {
        //     Parallel.Invoke(simWindow.Run, universe.Run);
        // }
    }
}
