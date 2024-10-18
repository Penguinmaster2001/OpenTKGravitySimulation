
using OpenTKGravitySim.Particles;



namespace OpenTKGravitySim;



public class Program
{
    private static readonly Universe universe = new(1000, 500.0f);
    private static readonly object lockObject = new();



    static void Main(string[] args)
    {
        using (SimWindow simWindow = new(1440, 900, universe, lockObject))
        {
            Parallel.Invoke(simWindow.Run, universe.Run);
        }
    }
}
