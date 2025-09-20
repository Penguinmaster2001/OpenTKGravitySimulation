
using System.Runtime.InteropServices;
using OpenTK.Graphics.OpenGL4;
using OpenTKGravitySim.Graphics;
using OpenTKGravitySim.Particles;



namespace OpenTKGravitySim.Compute;



public class GpuParticleProcessor : IParticleProcessor, IGlUser
{
    public ComputeShader ComputeShader;
    public bool GlInitialized { get; private set; }
    private readonly IGpuJobManager _gpuJobManager;

    private int _ssbo;



    public GpuParticleProcessor(ComputeShader computeShader, IGpuJobManager gpuJobManager)
    {
        GlUserManager.RegisterGlUser(this);

        _gpuJobManager = gpuJobManager;

        ComputeShader = computeShader;
    }



    public void UpdateParticles(Particle[] refParticles, Particle[] updateParticles, SpacialOctree octree, SimParameters parameters)
    {
        int numParticles = Math.Min(refParticles.Length, updateParticles.Length);

        var computeParticles = new ComputeParticle[numParticles];
        for (int p = 0; p < numParticles; p++)
        {
            computeParticles[p] = new()
            {
                Position = new((float)refParticles[p].Position.X,
                               (float)refParticles[p].Position.Y,
                               (float)refParticles[p].Position.Z,
                               1.0f),
                Velocity = new((float)refParticles[p].Velocity.X,
                               (float)refParticles[p].Velocity.Y,
                               (float)refParticles[p].Velocity.Z,
                               0.0f),
                Attributes = new((float)refParticles[p].Mass,
                                 0.0f,
                                 0.0f,
                                 0.0f),
            };
        }

        var gpuJob = new GpuJob<ComputeParticle[]>(GpuJobFunc(computeParticles, parameters));

        _gpuJobManager.AddJob(gpuJob);

        try
        {
            gpuJob.Completion.Task.Wait();
        }
        catch (Exception e)
            when (e is AggregateException { InnerException: TaskCanceledException } || e is TaskCanceledException)
        {
            return;
        }

        if (!gpuJob.Completion.Task.IsCompleted) return; // The program is probably closing

        computeParticles = gpuJob.Completion.Task.Result;

        // Copy data into update particle array
        for (int p = 0; p < numParticles; p++)
        {
            updateParticles[p] = new(computeParticles[p].Position.Xyz,
                                     computeParticles[p].Velocity.Xyz,
                                     computeParticles[p].Attributes.X);
        }
    }



    private Func<ComputeParticle[]> GpuJobFunc(ComputeParticle[] computeParticles, SimParameters parameters) =>
        () =>
        {
            // Shader
            ComputeShader.Bind();
            ComputeShader.SetUniform1("gravMult", (float)parameters.GravMult);
            ComputeShader.SetUniform1("timeStep", (float)parameters.TimeStep);
            ComputeShader.SetUniform1Int("numParticles", computeParticles.Length);


            // Buffers
            int particleByteSize = Marshal.SizeOf<ComputeParticle>();
            int requiredSize = computeParticles.Length * particleByteSize;

            GL.BindBuffer(BufferTarget.ShaderStorageBuffer, _ssbo);
            GL.GetBufferParameter(BufferTarget.ShaderStorageBuffer, BufferParameterName.BufferSize, out int bufferSize);
            if (bufferSize < requiredSize)
            {
                Console.WriteLine($"Buffer Size: {bufferSize}, Required Size: {requiredSize}");
                GL.BufferData(BufferTarget.ShaderStorageBuffer, requiredSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
            }
            GL.BufferSubData(BufferTarget.ShaderStorageBuffer, 0, requiredSize, computeParticles);
            GL.BindBufferBase(BufferRangeTarget.ShaderStorageBuffer, 0, _ssbo);
            ErrorHelper.CheckGLError();


            // Run compute
            int localSize = 256;
            int groups = (computeParticles.Length + localSize - 1) / localSize;
            GL.DispatchCompute(groups, 1, 1);

            GL.MemoryBarrier(MemoryBarrierFlags.ShaderStorageBarrierBit);

            // Read back data
            GL.GetBufferSubData(BufferTarget.ShaderStorageBuffer, IntPtr.Zero, requiredSize, computeParticles);

            return computeParticles;
        };



    public bool InitializeWithGlContext()
    {
        GL.GenBuffers(1, out _ssbo);

        GlInitialized = true;

        return true;
    }



    public void Delete()
    {
        GL.DeleteBuffer(_ssbo);
        GlInitialized = false;
    }
}
