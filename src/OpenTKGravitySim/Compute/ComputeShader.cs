
using OpenTK.Graphics.OpenGL4;
using OpenTKGravitySim.Graphics;



namespace OpenTKGravitySim.Compute;



public class ComputeShader : BaseShaderProgram
{
    public string ComputeShaderPath;



    public ComputeShader(string computeShaderPath)
    {
        GlUserManager.RegisterGlUser(this);

        ComputeShaderPath = computeShaderPath;
    }



    public override bool InitializeWithGlContext()
    {
        var succeeded = true;
        if (IsCompiled)
        {
            Delete();
        }

        ID = GL.CreateProgram();

        if (!CompileShader(ShaderType.ComputeShader, LoadShaderSource(ComputeShaderPath), out int computeShader))
        {
            succeeded = false;
        }

        if (succeeded)
        {
            GL.AttachShader(ID, computeShader);
            GL.LinkProgram(ID);
        }

        GL.GetProgram(ID, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetProgramInfoLog(ID);
            Delete();
            var msg = $"Shader program linking failed\nCompute shader path: {ComputeShaderPath}\n{infoLog}\n";
            Console.WriteLine(msg);

            succeeded = false;
        }

        GL.DeleteShader(computeShader);

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
