
namespace OpenTKGravitySim.Graphics;



public interface IRenderer : IGlUser
{
    ShaderProgram ShaderProgram { get; set; }



    void Render<T>(T[] renderables) where T : IRenderable;
}
