
namespace OpenTKGravitySim.Graphics;



public interface IGlUser
{
    bool GlInitialized { get; }



    bool InitializeWithGlContext();



    void Delete();
}
