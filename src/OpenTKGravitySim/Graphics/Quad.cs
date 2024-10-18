
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTKGravitySim.Particles;



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
        
        ibo = new(indices);
    }



    public void Render(ShaderProgram shaderProgram, List<Particle> particles)
    {
        shaderProgram.Bind();

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);

        GL.GetBufferParameter(BufferTarget.ShaderStorageBuffer, BufferParameterName.BufferSize, out int bufferSize);
        int requiredSize = particles.Count * Particle.SizeInBytes;
        if (bufferSize < requiredSize)
        {
            GL.BufferData(BufferTarget.ShaderStorageBuffer, requiredSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        }
        
        GL.BufferSubData(BufferTarget.ShaderStorageBuffer, 0, particles.Count * Particle.SizeInBytes, particles.ToArray());
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
