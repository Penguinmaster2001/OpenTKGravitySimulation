
using OpenTK.Graphics.OpenGL4;



namespace OpenTKTutorial.Graphics;



internal abstract class GLBO
{
    public int ID { get; protected set; }
    public BufferTarget BufferTarget { get; }



    public GLBO(BufferTarget bufferTarget)
    {
        ID = GL.GenBuffer();
        BufferTarget = bufferTarget;
    }



    public void Bind() => GL.BindBuffer(BufferTarget, ID);
    public void UnBind() => GL.BindBuffer(BufferTarget, 0);
    public void Delete() => GL.DeleteBuffer(ID);
}
