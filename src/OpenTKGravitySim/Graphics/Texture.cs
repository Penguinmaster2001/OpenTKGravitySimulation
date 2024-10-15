
using OpenTK.Graphics.OpenGL4;
using StbImageSharp;



namespace OpenTKTutorial.Graphics;



internal class Texture
{
    public int ID;



    static Texture()
    {
        StbImage.stbi_set_flip_vertically_on_load(1);
    }



    public Texture(string filePath)
    {
        ID = GL.GenTexture();

        GL.ActiveTexture(TextureUnit.Texture0);
        Bind();

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) TextureMagFilter.Nearest);

        ImageResult theRock = ImageResult.FromStream(File.OpenRead(filePath), ColorComponents.RedGreenBlueAlpha);

        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, theRock.Width, theRock.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, theRock.Data);
        UnBind();
    }



    public void Bind() => GL.BindTexture(TextureTarget.Texture2D, ID);
    public void UnBind() => GL.BindTexture(TextureTarget.Texture2D, 0);
    public void Delete() => GL.DeleteTexture(ID);
}
