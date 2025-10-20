
using OpenTK.Graphics.OpenGL4;



namespace OpenTKGravitySim.Graphics;



public class GlSettings
{
    public List<EnableCap> Settings { get; set; }

    public Action? OtherSettings { get; set; }



    public GlSettings(Action? otherSettings = null, params IEnumerable<EnableCap> settings)
    {
        OtherSettings = otherSettings;
        Settings = [.. settings];
    }



    public GlSettings(params IEnumerable<EnableCap> settings)
    {
        OtherSettings = null;
        Settings = [.. settings];
    }



    public void Apply()
    {
        foreach (var settings in Settings)
        {
            GL.Enable(settings);

            OtherSettings?.Invoke();
        }
    }
}
