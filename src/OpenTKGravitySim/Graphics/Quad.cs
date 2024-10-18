
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Graphics;



internal class Quad
{
    private readonly List<Vector3> verts = [
        new( 1.0f,  1.0f,  0.0f), // Top Right
        new(-1.0f,  1.0f,  0.0f), // Top Left
        new(-1.0f, -1.0f,  0.0f), // Bottom Left
        new( 1.0f, -1.0f,  0.0f)  // Bottom Right
    ];

    private readonly List<uint> indices = [
        3, 0, 1,
        3, 1, 2
    ];

    private readonly VAO vao;
    private readonly VBO<Vector3> vertVBO;
    private readonly IBO ibo;




    public Quad()
    {
        vao = new();
        vertVBO = new(verts);

        vao.Bind();
        vertVBO.Bind();
        vao.LinkToVAO(0, 3, vertVBO);
        vertVBO.UnBind();
        vao.UnBind();

        ibo = new(indices);
    }



    public void Render(ShaderProgram shaderProgram)
    {
        shaderProgram.Bind();
        vao.Bind();
        ibo.Bind();
        
        GL.DrawElements(PrimitiveType.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);
    }



    public void Delete()
    {
        ibo.Delete();
        vertVBO.Delete();
        vao.Delete();
    }
}
