
namespace OpenTKGravitySim.Graphics;



public static class GlUserManager
{
    private static readonly List<IGlUser> _glUsers = [];



    public static void RegisterGlUser(IGlUser glUser) => _glUsers.Add(glUser);



    public static bool GlContextInitialized()
    {
        bool success = true;
        foreach (var glUser in _glUsers)
        {
            if (glUser.GlInitialized) continue;
            success &= glUser.InitializeWithGlContext();
        }

        return success;
    }



    public static void Delete()
    {
        foreach (var glUser in _glUsers)
        {
            glUser.Delete();
        }
    }
}
