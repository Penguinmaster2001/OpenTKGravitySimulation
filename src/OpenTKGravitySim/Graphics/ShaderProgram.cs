
using OpenTK.Graphics.OpenGL4;



namespace OpenTKTutorial.Graphics;



internal class ShaderProgram
{
    public int ID;

    public ShaderProgram(string vertexShaderPath, string fragmentShaderPath)
    {
        ID = GL.CreateProgram();

        int vertexShader  = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vertexShader, LoadShaderSource(vertexShaderPath));
        GL.CompileShader(vertexShader);

        int fragmentShader  = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fragmentShader, LoadShaderSource(fragmentShaderPath));
        GL.CompileShader(fragmentShader);

        GL.AttachShader(ID, vertexShader);
        GL.AttachShader(ID, fragmentShader);

        GL.LinkProgram(ID);

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);
    }



    public static string LoadShaderSource(string filePath)
    {
        string shaderSource = "";

        try
        {
            using (StreamReader reader = new(filePath))
            {
                shaderSource = reader.ReadToEnd();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine($"Failed to load shader!!\nFilepath: {filePath}\n{e.Message}");
        }

        return shaderSource;
    }



    public void Bind() => GL.UseProgram(ID);
    public void UnBind() => GL.UseProgram(0);
    public void Delete() => GL.DeleteShader(ID);
}
