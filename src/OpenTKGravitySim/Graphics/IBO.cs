
using OpenTK.Graphics.OpenGL4;



namespace OpenTKGravitySim.Graphics;



internal class IBO : GLBO
{
    public IBO(uint[] data) : base(BufferTarget.ElementArrayBuffer)
    {
        Bind();
        GL.BufferData(BufferTarget.ElementArrayBuffer, data.Length * sizeof(uint), data.ToArray(), BufferUsageHint.StaticDraw);
        UnBind();
    }
}
