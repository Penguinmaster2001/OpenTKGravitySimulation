
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Graphics;



public class Quad : IRenderer
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

    private VAO? vao;
    private VBO<Vector3>? vertVBO;
    private IBO? ibo;
    private int ssbo;
    private readonly List<RenderObject> _renderObjects = new(300);



    public ShaderProgram ShaderProgram { get; set; }




    public Quad(ShaderProgram shaderProgram)
    {
        ShaderProgram = shaderProgram;
    }


    Random random = new Random();
    public void Render<T>(List<T> renderables) where T : IRenderable
    {
        if (vao is null || vertVBO is null || ibo is null) return;

        // foreach (Particle particle in particles)
        // {
        //     Console.WriteLine($"{particle.Position}, {particle.Velocity}, {particle.Mass}");
        // }
        int renderObjectCount = Math.Min(300, renderables.Count);
        _renderObjects.Clear();

        uint offset = (uint)random.Next();
        for (uint i = 0; i < renderObjectCount; i++)
        {
            RenderObject renderObject = renderables[(int)((offset + i * 110503u) % (uint)renderables.Count)].ToRenderObject();
            _renderObjects.Add(renderObject);
        }

        // RenderObject[] renderObjects = renderables.Take(renderObjectCount).Select(renderable => renderable.ToRenderObject()).ToArray();
        int requiredSize = renderObjectCount * RenderObject.SizeInBytes;

        ShaderProgram.Bind();
        ShaderProgram.SetUniform1Int("numRenderables", renderObjectCount);

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);

        GL.GetBufferParameter(BufferTarget.ShaderStorageBuffer, BufferParameterName.BufferSize, out int bufferSize);
        if (bufferSize < requiredSize)
        {
            Console.WriteLine($"Buffer Size: {bufferSize}, Required Size: {requiredSize}");
            GL.BufferData(BufferTarget.ShaderStorageBuffer, requiredSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        }

        GL.BufferSubData(BufferTarget.ShaderStorageBuffer, 0, requiredSize, _renderObjects.ToArray());
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


        vao.Bind();
        ibo.Bind();

        GL.DrawElements(PrimitiveType.Triangles, indices.Count, DrawElementsType.UnsignedInt, 0);

        vao.UnBind();
        ibo.UnBind();
        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, 0);
    }



    public void Delete()
    {
        ibo?.Delete();
        vertVBO?.Delete();
        vao?.Delete();
        ShaderProgram.Delete();
    }

    public bool Initialize()
    {
        vao = new();
        vertVBO = new([.. verts]);

        vao.Bind();
        vertVBO.Bind();
        vao.LinkToVAO(0, 3, vertVBO);
        vertVBO.UnBind();
        vao.UnBind();

        ssbo = GL.GenBuffer();

        ibo = new([.. indices]);

        return ShaderProgram.Initialize();
    }
}
