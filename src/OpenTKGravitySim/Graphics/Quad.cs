
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
    private int ssbo;




    public Quad(int numParticles)
    {
        vao = new();
        vertVBO = new(verts);

        vao.Bind();
        vertVBO.Bind();
        vao.LinkToVAO(0, 3, vertVBO);
        vertVBO.UnBind();
        vao.UnBind();

        ssbo = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);
        GL.BufferData(BufferTarget.ShaderStorageBuffer, numParticles * Vector3.SizeInBytes, new Vector3[numParticles], BufferUsageHint.StaticDraw);
        
        ibo = new(indices);
    }



    public void Render(ShaderProgram shaderProgram, List<Vector3> particlePositions)
    {
        shaderProgram.Bind();

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);

        GL.GetBufferParameter(BufferTarget.ShaderStorageBuffer, BufferParameterName.BufferSize, out int bufferSize);
        int requiredSize = particlePositions.Count * Vector3.SizeInBytes;
        if (bufferSize < requiredSize)
        {
            // Resize the buffer if necessary
            GL.BufferData(BufferTarget.ShaderStorageBuffer, requiredSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        }
        
        GL.BufferSubData(BufferTarget.ShaderStorageBuffer, 0, particlePositions.Count * Vector3.SizeInBytes, particlePositions.ToArray());
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, ssbo);


        vao.Bind();
        ibo.Bind();
        
        GL.DrawElements(PrimitiveType.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);

        vao.UnBind();
        ibo.UnBind();
    }



    public void Delete()
    {
        ibo.Delete();
        vertVBO.Delete();
        vao.Delete();
    }
}
