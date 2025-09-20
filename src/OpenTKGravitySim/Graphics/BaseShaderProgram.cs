
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Graphics;



public abstract class BaseShaderProgram : IGlUser
{
    public int ID { get; protected set; } = -1;
    public bool IsCompiled { get; protected set; } = false;
    public bool GlInitialized { get; protected set; } = false;

    public BaseShaderProgram()
    {
        GlUserManager.RegisterGlUser(this);
    }



    public virtual bool InitializeWithGlContext()
    {
        GlInitialized = true;
        return true;
    }




    public void Bind() => GL.UseProgram(ID);
    public void UnBind() => GL.UseProgram(0);
    public void Delete()
    {
        IsCompiled = false;
        GL.DeleteProgram(ID);
        ID = -1;
        GlInitialized = false;
    }



    public int GetUniformLocation(string uniformName)
    {
        if (!IsCompiled) return -1;

        int location = GL.GetUniformLocation(ID, uniformName);

        return location;
    }



    public void SetUniform(string uniformName, Action<int> setUniformAction)
    {
        int location = GetUniformLocation(uniformName);

        if (location >= 0)
        {
            setUniformAction(location);
        }
    }



    protected static bool CompileShader(ShaderType type, string code, out int shader)
    {
        shader = GL.CreateShader(type);

        GL.ShaderSource(shader, code);

        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetShaderInfoLog(shader);
            string msg = $"Shader {type} compilation failed!!\n\n\n{infoLog}\n\n\n{code}";
            Console.WriteLine(msg);

            return false;
        }

        return true;
    }



    public static string LoadShaderSource(string filePath)
    {
        string shaderSource = "";

        using (StreamReader reader = new(filePath))
        {
            shaderSource = reader.ReadToEnd();
        }

        return shaderSource;
    }



    public void SetUniformMatrix4(string uniformName, Matrix4 matrix) => SetUniform(uniformName, location => GL.UniformMatrix4(location, true, ref matrix));
    public void SetUniform1(string uniformName, float val) => SetUniform(uniformName, location => GL.Uniform1(location, val));
    public void SetUniform1Int(string uniformName, int val) => SetUniform(uniformName, location => GL.Uniform1(location, val));
    public void SetUniform2(string uniformName, Vector2 vector) => SetUniform(uniformName, location => GL.Uniform2(location, vector));
    public void SetUniform3(string uniformName, Vector3 vector) => SetUniform(uniformName, location => GL.Uniform3(location, vector));
    public void SetCameraUniforms(Camera camera)
    {
        SetUniform3("cameraForward", camera.forward);
        SetUniform3("cameraUp", camera.up);
        SetUniform3("cameraRight", camera.right);
        SetUniform3("cameraPos", camera.Position);
        SetUniformMatrix4("view", camera.ViewMatrix);
        SetUniformMatrix4("projection", camera.ProjectionMatrix);
        SetUniform1("fov", camera.FOV);
    }
}
