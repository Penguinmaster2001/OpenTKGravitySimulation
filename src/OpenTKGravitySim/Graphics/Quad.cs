
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




    public Quad()
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



    public void Render(ShaderProgram shaderProgram, Particle[] particles)
    {
        // foreach (Particle particle in particles)
        // {
        //     Console.WriteLine($"{particle.Position}, {particle.Velocity}, {particle.Mass}");
        // }

        shaderProgram.Bind();

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);

        GL.GetBufferParameter(BufferTarget.ShaderStorageBuffer, BufferParameterName.BufferSize, out int bufferSize);
        int requiredSize = particles.Length * Particle.SizeInBytes;
        if (bufferSize < requiredSize)
        {
            Console.WriteLine($"Buffer Size: {bufferSize}, Required Size: {requiredSize}");
            GL.BufferData(BufferTarget.ShaderStorageBuffer, requiredSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        }
        
        GL.BufferSubData(BufferTarget.ShaderStorageBuffer, 0, particles.Length * Particle.SizeInBytes, particles);
        // IntPtr ptr = GL.MapBuffer(BufferTarget.ShaderStorageBuffer, BufferAccess.ReadOnly);
        // if (ptr != IntPtr.Zero)
        // {
        //     byte[] data = new byte[requiredSize];
        //     System.Runtime.InteropServices.Marshal.Copy(ptr, data, 0, requiredSize);
        //     GL.UnmapBuffer(BufferTarget.ShaderStorageBuffer);

        //     // Process the data as needed, for example, print it
        //     for (int i = 0; i < data.Length; i += Particle.SizeInBytes)
        //     {
        //         // Assuming Particle has a method to create an instance from a byte array
        //         Particle particle = Particle.FromByteArray(data, i);
        //         Console.WriteLine($"{particle.Position}, {particle.Velocity}, {particle.Mass}");
        //     }
        // }
        GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, ssbo);
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);


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
