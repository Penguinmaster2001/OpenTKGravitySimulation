
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Graphics;



internal class ShaderProgram
{
    public int ID = -1;
    public bool IsCompiled { get; private set; } = false;



    public ShaderProgram(string vertexShaderPath, string fragmentShaderPath)
    {
        CreateNewProgram(vertexShaderPath, fragmentShaderPath);
    }



    public void CreateNewProgram(string vertexShaderPath, string fragmentShaderPath)
    {
        if (IsCompiled)
        {
            Delete();
        }

        ID = GL.CreateProgram();

        int vertexShader = CompileShader(ShaderType.VertexShader, LoadShaderSource(vertexShaderPath));
        int fragmentShader = CompileShader(ShaderType.FragmentShader, LoadShaderSource(fragmentShaderPath));

        GL.AttachShader(ID, vertexShader);
        GL.AttachShader(ID, fragmentShader);

        GL.LinkProgram(ID);

        GL.GetProgram(ID, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetProgramInfoLog(ID);
            Delete();
            throw new Exception($"Shader program linking failed\nFragment shader path: {fragmentShaderPath}, vertex shader path: {vertexShaderPath}\n{infoLog}\n{LoadShaderSource(fragmentShaderPath)}");
        }

        GL.DeleteShader(vertexShader);
        GL.DeleteShader(fragmentShader);

        IsCompiled = true;
        Bind();
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
    }



    private static int CompileShader(ShaderType type, string code)
    {
        int shader = GL.CreateShader(type);
        GL.ShaderSource(shader, code);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string infoLog = GL.GetShaderInfoLog(shader);
            throw new Exception($"Shader {type} compilation failed!!\n{infoLog}");
        }

        return shader;
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



    public void Bind() => GL.UseProgram(ID);
    public void UnBind() => GL.UseProgram(0);
    public void Delete()
    {
        IsCompiled = false;
        ID = -1;
        GL.DeleteProgram(ID);
    }
}
