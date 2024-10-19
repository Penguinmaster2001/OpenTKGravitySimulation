
using OpenTKGravitySim.Particles;



namespace OpenTKGravitySim;



public class Program
{
    private static readonly Universe universe = new(1000, 1000.0f);



    static void Main(string[] args)
    {
        using (SimWindow simWindow = new(1440, 900, universe))
        {
            Parallel.Invoke(simWindow.Run, universe.Run);
        }
    }
}
