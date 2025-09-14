
using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using OpenTKGravitySim.Graphics;



namespace OpenTKGravitySim.Particles;



[Serializable]
public struct Particle(Vector3d initialPosition, Vector3d initialVelocity, double mass) : IRenderable
{
    public Vector3d Position = initialPosition;
    public Vector3d Velocity = initialVelocity;
    public double Mass = mass;



    public static int SizeInBytes => Marshal.SizeOf<Particle>();



    public readonly bool IsValid()
    {
        return !double.IsNaN(Position.X) && !double.IsNaN(Position.Y) && !double.IsNaN(Position.Z) &&
               !double.IsNaN(Velocity.X) && !double.IsNaN(Velocity.Y) && !double.IsNaN(Velocity.Z) &&
               !double.IsNaN(Mass);
    }



    public static Particle FromByteArray(byte[] data, int offset)
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



    public RenderObject ToRenderObject()
    {
        return new RenderObject() {
            Position = new((Vector3) Position, 1.0f),
            Velocity = new((Vector3) Velocity, 1.0f),
            Attributes = new((float) Mass, 0.0f, 0.0f, 0.0f)
        };
    }
}
