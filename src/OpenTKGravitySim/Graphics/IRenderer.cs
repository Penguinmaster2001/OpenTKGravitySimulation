
namespace OpenTKGravitySim.Graphics;



public interface IRenderer
{
    ShaderProgram ShaderProgram { get; set; }



    void Render<T>(T[] renderables) where T : IRenderable;



    bool Initialize();



    void Delete();
}
