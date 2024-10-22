
using System.Runtime.InteropServices;
using OpenTK.Mathematics;
using OpenTKGravitySim.Graphics;



namespace OpenTKGravitySim.Particles;



[Serializable]
internal struct Particle(Vector3 initialPosition, Vector3 initialVelocity, float mass) : IRenderable
{
    public Vector3 Position = initialPosition;
    public Vector3 Velocity = initialVelocity;
    public float Mass = mass;



    public static int SizeInBytes => Marshal.SizeOf<Particle>();



    public readonly bool IsValid()
    {
        return !float.IsNaN(Position.X) && !float.IsNaN(Position.Y) && !float.IsNaN(Position.Z) &&
               !float.IsNaN(Velocity.X) && !float.IsNaN(Velocity.Y) && !float.IsNaN(Velocity.Z) &&
               !float.IsNaN(    Mass);
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



    public RenderObject ToRenderObject()
    {
        return new RenderObject() {
            Position = new(Position, 1.0f),
            Velocity = new(Velocity, 1.0f),
            Attributes = new(Mass, 0.0f, 0.0f, 0.0f)
        };
    }
}
