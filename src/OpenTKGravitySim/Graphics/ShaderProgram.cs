
using OpenTK.Graphics.OpenGL4;



namespace OpenTKGravitySim.Graphics;



public class ShaderProgram : BaseShaderProgram
{
    public string VertexShaderPath;
    public string FragmentShaderPath;



    public ShaderProgram(string vertexShaderPath, string fragmentShaderPath) : base()
    {
        VertexShaderPath = vertexShaderPath;
        FragmentShaderPath = fragmentShaderPath;
    }



    public override bool InitializeWithGlContext()
    {
        var succeeded = true;
        if (IsCompiled)
        {
            Delete();
        }

        ID = GL.CreateProgram();

        if (!CompileShader(ShaderType.VertexShader, LoadShaderSource(VertexShaderPath), out int vertexShader))
        {
            succeeded = false;
        }

        if (!CompileShader(ShaderType.FragmentShader, LoadShaderSource(FragmentShaderPath), out int fragmentShader))
        {
            succeeded = false;
        }

        if (succeeded)
        {
            GL.AttachShader(ID, vertexShader);
            GL.AttachShader(ID, fragmentShader);

            GL.LinkProgram(ID);
        }

        GL.GetProgram(ID, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetProgramInfoLog(ID);
            Delete();
            var msg = $"Shader program linking failed\nFragment shader path: {FragmentShaderPath}, vertex shader path: {VertexShaderPath}\n{infoLog}\n";
            Console.WriteLine(msg);

            succeeded = false;
        }

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        if (succeeded)
        {
            IsCompiled = true;
            Bind();
            return true;
        }

        Delete();
        return false;
    }
}
