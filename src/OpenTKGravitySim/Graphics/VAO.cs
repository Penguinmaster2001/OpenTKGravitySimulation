
using OpenTK.Graphics.OpenGL4;



namespace OpenTKTutorial.Graphics;



internal class VAO
{
    public int ID;



    public VAO()
    {
        ID = GL.GenVertexArray();
    }



    public void LinkToVAO(int location, int size, GLBO vbo)
    {
        Bind();
        vbo.Bind();

        GL.VertexAttribPointer(location, size, VertexAttribPointerType.Float, false, 0, 0);
        GL.EnableVertexAttribArray(location);

        vbo.UnBind();
        UnBind();
    }



    public void Bind() => GL.BindVertexArray(ID);
    public void UnBind() => GL.BindVertexArray(0);
    public void Delete() => GL.DeleteVertexArray(ID);
}
