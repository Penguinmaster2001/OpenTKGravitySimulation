
using System.Runtime.InteropServices;
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Particles;



[Serializable, StructLayout(LayoutKind.Sequential)]
internal struct Particle(Vector3 initialPosition, Vector3 initialVelocity, float mass)
{
    public Vector3 Position = initialPosition;
    public Vector3 Velocity = initialVelocity;
    public float Mass = mass;



    public static int SizeInBytes => Marshal.SizeOf<Particle>();



    internal static Particle FromByteArray(byte[] data, int offset)
    {
        int size = SizeInBytes;
        if (offset + size > data.Length)
        {
            Console.WriteLine($"{offset} out of range");
            return new();
        }
        IntPtr ptr = Marshal.AllocHGlobal(size);
        try
        {
            Marshal.Copy(data, offset, ptr, size);
            return Marshal.PtrToStructure<Particle>(ptr);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }
}
