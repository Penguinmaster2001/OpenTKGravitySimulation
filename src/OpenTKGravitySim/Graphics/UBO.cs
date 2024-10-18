
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;



namespace OpenTKGravitySim.Graphics;



internal class UBO<T> : GLBO where T : struct
{
    public UBO(List<T> data) : base(BufferTarget.UniformBuffer)
    {
        Bind();
        GL.BufferData(BufferTarget.UniformBuffer, data.Count * Marshal.SizeOf<T>(), data.ToArray(), BufferUsageHint.StaticDraw);
        UnBind();
    }



    public void SubData(List<T> data)
    {
        Bind();
        GL.BufferSubData(BufferTarget.UniformBuffer, IntPtr.Zero, data.Count * Marshal.SizeOf<T>(), data.ToArray());
        UnBind();
    }



    public override void Bind() => GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, ID);
    public override void UnBind() => GL.BindBufferBase(BufferRangeTarget.UniformBuffer, 0, 0);
}
