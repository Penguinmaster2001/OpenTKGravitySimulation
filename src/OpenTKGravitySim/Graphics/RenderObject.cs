
using System.Runtime.InteropServices;
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Graphics;

[Serializable]
public struct RenderObject(Vector4 position, Vector4 velocity, Vector4 attributes) : IRenderable
{
    public Vector4 Position = position;
    public Vector4 Velocity = velocity;
    public Vector4 Attributes = attributes;
    public static int SizeInBytes => Marshal.SizeOf<RenderObject>();

    public readonly RenderObject ToRenderObject()
    {
        return this;
    }
}
