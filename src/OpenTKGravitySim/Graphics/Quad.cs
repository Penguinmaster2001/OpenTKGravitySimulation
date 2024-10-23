
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Graphics;



public interface IRenderable
{
    RenderObject ToRenderObject();
}



[Serializable]
public struct RenderObject(Vector4 position, Vector4 velocity, Vector4 attributes) : IRenderable
{
    public Vector4 Position = position;
    public Vector4 Velocity = velocity;
    public Vector4 Attributes = attributes;
    public static int SizeInBytes => Marshal.SizeOf<RenderObject>();

    public readonly RenderObject ToRenderObject()
    {
        return this;
    }
}



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
    private List<RenderObject> renderObjects;




    public Quad()
    {
        renderObjects = new(1000);
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



    public void Render<T>(ShaderProgram shaderProgram, Camera camera, List<T> renderables) where T : IRenderable
    {
        int renderObjectCount = Math.Min(1000, renderables.Count);
        int maxIterations = Math.Min(10 * renderObjectCount, renderables.Count);
        renderObjects.Clear();
        uint i = 0;
        while (renderObjects.Count < renderObjectCount && i < maxIterations)
        {
            RenderObject renderObject = renderables[(int) ((i * 110503u) % (uint) renderables.Count)].ToRenderObject();
            Vector4 clipSpace = renderObject.Position * camera.ViewMatrix * camera.ProjectionMatrix;
            if (clipSpace.X >= -clipSpace.W && clipSpace.X <= clipSpace.W &&
                clipSpace.Y >= -clipSpace.W && clipSpace.Y <= clipSpace.W &&
                clipSpace.Z >= -clipSpace.W && clipSpace.Z <= clipSpace.W)
            {
                renderObject.Position = clipSpace / clipSpace.W;
                renderObjects.Add(renderObject);
            }
            i++;
        }
        renderObjects.Sort(Comparer<RenderObject>.Create((a, b) => a.Position.Z.CompareTo(b.Position.Z)));
        renderObjectCount = renderObjects.Count;

        int requiredSize = renderObjectCount * RenderObject.SizeInBytes;

        shaderProgram.Bind();
        shaderProgram.SetUniform1Int("numRenderables", renderObjectCount);

        GL.BindBuffer(BufferTarget.ShaderStorageBuffer, ssbo);

        GL.GetBufferParameter(BufferTarget.ShaderStorageBuffer, BufferParameterName.BufferSize, out int bufferSize);
        if (bufferSize < requiredSize)
        {
            Console.WriteLine($"Buffer Size: {bufferSize}, Required Size: {requiredSize}");
            GL.BufferData(BufferTarget.ShaderStorageBuffer, requiredSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
        }
        
        GL.BufferSubData(BufferTarget.ShaderStorageBuffer, 0, requiredSize, renderObjects.ToArray());
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
        ibo.Delete();
        vertVBO.Delete();
        vao.Delete();
    }
}
