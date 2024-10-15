
namespace OpenTKGravitySim;



public class Program
{
    static void Main(string[] args)
    {
        using(SimWindow simWindow = new(1440, 900))
        {
            simWindow.Run();
        }
    }
}
