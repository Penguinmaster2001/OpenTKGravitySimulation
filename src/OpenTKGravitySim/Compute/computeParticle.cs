
using System.Runtime.InteropServices;
using OpenTK.Mathematics;



[StructLayout(LayoutKind.Sequential)]
struct Particle
{
    public Vector4 position; // 16 bytes
    public Vector4 attr;     // 16 bytes
}
// sizeof = 32 bytes
