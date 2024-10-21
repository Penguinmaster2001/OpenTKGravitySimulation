
using System.Runtime.InteropServices;
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Particles;



[Serializable, StructLayout(LayoutKind.Sequential)]
internal struct Particle(Vector4 initialPosition, Vector4 initialVelocity, float mass)
{
    public Vector4 Position = initialPosition;
    public Vector4 Velocity = initialVelocity;
    public Vector4 Mass = new(mass, 0.0f, 0.0f, 0.0f);



    public static int SizeInBytes => Marshal.SizeOf<Particle>();



    public readonly bool IsValid()
    {
        return !float.IsNaN(Position.X) && !float.IsNaN(Position.Y) && !float.IsNaN(Position.Z) && !float.IsNaN(Position.W) &&
               !float.IsNaN(Velocity.X) && !float.IsNaN(Velocity.Y) && !float.IsNaN(Velocity.Z) && !float.IsNaN(Velocity.W) &&
               !float.IsNaN(    Mass.X) && !float.IsNaN(    Mass.Y) && !float.IsNaN(    Mass.Z) && !float.IsNaN(    Mass.W);
    }



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



    public override readonly string ToString()
    {
        return $"Pos: {Position}, Vel: {Velocity}, Mass: {Mass}";
    }
}
