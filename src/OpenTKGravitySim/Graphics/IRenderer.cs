
namespace OpenTKGravitySim.Graphics;



public interface IRenderer
{
    ShaderProgram ShaderProgram { get; set; }



    void Render<T>(List<T> renderables) where T : IRenderable;



    void Initialize();



    void Delete();
}
