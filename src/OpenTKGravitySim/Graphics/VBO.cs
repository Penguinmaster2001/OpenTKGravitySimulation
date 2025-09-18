
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;



namespace OpenTKGravitySim.Graphics;



internal class VBO<T> : GLBO where T : struct
{
    public VBO() : base(BufferTarget.ArrayBuffer) { }

    public VBO(T[] data) : base(BufferTarget.ArrayBuffer)
    {
        Bind();
        GL.BufferData(BufferTarget.ArrayBuffer, data.Length * Marshal.SizeOf<T>(), data, BufferUsageHint.StaticDraw);
        UnBind();
    }



    public void SubData(List<T> data) => SubData(data.ToArray());



    public void SubData(T[] data)
    {
        Bind();
        GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, data.Length * Marshal.SizeOf<T>(), data);
        UnBind();
    }
}
