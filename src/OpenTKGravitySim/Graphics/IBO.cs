
using System.Threading.Channels;
using OpenTK.Graphics.OpenGL4;

namespace OpenTKTutorial.Graphics;



internal class IBO : GLBO
{
    public IBO(List<uint> data) : base(BufferTarget.ElementArrayBuffer)
    {
        Bind();
        GL.BufferData(BufferTarget.ElementArrayBuffer, data.Count * sizeof(uint), data.ToArray(), BufferUsageHint.StaticDraw);
        UnBind();
    }
}
