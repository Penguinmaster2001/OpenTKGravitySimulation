
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Graphics;



public class PointRenderer : IRenderer
{
    private VAO? _vao;

    private readonly uint[] _indices;
    private IBO? _ibo;

    private readonly Vector4[] _positions;
    private VBO<Vector4>? _positionVBO;

    private readonly Vector4[] _velocities;
    private VBO<Vector4>? _velocityVBO;

    private readonly Vector4[] _attributes;
    private VBO<Vector4>? _attributeVBO;

    private readonly int _numRenderables;



    public ShaderProgram ShaderProgram { get; set; }




    public PointRenderer(ShaderProgram shaderProgram, int numRenderables)
    {
        ShaderProgram = shaderProgram;
        _numRenderables = numRenderables;

        _indices = new uint[_numRenderables];
        for (uint r = 0; r < _numRenderables; r++)
        {
            _indices[r] = r;
        }

        _positions = new Vector4[numRenderables];
        _velocities = new Vector4[numRenderables];
        _attributes = new Vector4[numRenderables];
    }



    public void Render<T>(T[] renderables) where T : IRenderable
    {
        if (_vao is null || _ibo is null || _positionVBO is null || _velocityVBO is null || _attributeVBO is null)
        {
            Console.WriteLine("Renderer not initialized!");
            return;
        }

        int renderObjectCount = Math.Min(renderables.Length, _numRenderables);

        for (int i = 0; i < renderObjectCount; i++)
        {
            var renderObject = renderables[i].ToRenderObject();

            _positions[i] = renderObject.Position;
            _velocities[i] = renderObject.Velocity;
            _attributes[i] = renderObject.Attributes;
        }

        _positionVBO.SubData(_positions);
        _velocityVBO.SubData(_velocities);
        _attributeVBO.SubData(_attributes);

        ShaderProgram.Bind();

        _vao.Bind();
        _ibo.Bind();

        GL.DrawElements(PrimitiveType.Points, renderObjectCount, DrawElementsType.UnsignedInt, 0);
    
        _ibo.UnBind();
        _vao.UnBind();
    }



    public void Delete()
    {
        _attributeVBO?.Delete();
        _velocityVBO?.Delete();
        _positionVBO?.Delete();

        _ibo?.Delete();

        _vao?.Delete();
        
        ShaderProgram.Delete();
    }



    public bool Initialize()
    {
        _vao = new();

        _positionVBO = new(_positions);
        _velocityVBO = new(_velocities);
        _attributeVBO = new(_attributes);
        _ibo = new(_indices);

        _vao.Bind();

        _positionVBO.Bind();
        _vao.LinkToVAO(0, 4, _positionVBO);
        _positionVBO.UnBind();

        _velocityVBO.Bind();
        _vao.LinkToVAO(1, 4, _velocityVBO);
        _velocityVBO.UnBind();

        _attributeVBO.Bind();
        _vao.LinkToVAO(2, 4, _attributeVBO);
        _attributeVBO.UnBind();

        _vao.UnBind();

        return ShaderProgram.Initialize();
    }
}
