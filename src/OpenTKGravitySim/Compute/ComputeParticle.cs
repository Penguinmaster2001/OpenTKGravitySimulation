
using System.Runtime.InteropServices;
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Compute;



[StructLayout(LayoutKind.Sequential)]
struct ComputeParticle
{
    public Vector4 Position;    // 16 bytes
    public Vector4 Velocity;    // 16 bytes
    public Vector4 Attributes;  // 16 bytes
}
// sizeof = 48 bytes
