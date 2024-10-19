
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Particles;



internal class Universe
{
    // public readonly SpacialOctree octreeA;
    // public readonly SpacialOctree octreeB;
    public readonly List<Particle> particleBufferA;
    public readonly List<Particle> particleBufferB;
    public bool UseParticleBufferA = true;
    public bool ExternalReadingBuffer = false;
    public Particle[] Particles => GetPrevParticleBuffer().ToArray();
    public int NumParticles { get; private set; }
    private float timeStep;
    public bool Running;



    public Universe(int numParticles, float size, float timeStep = 0.01f)
    {
        this.timeStep = timeStep;
        NumParticles = numParticles;
        Running = false;

        // octreeA = new(0.1f);
        // octreeB = new(0.1f);

        particleBufferA = new(NumParticles);
        particleBufferB = new(NumParticles);

        AddParticlesEllipse(Vector3.Zero, 2.0f * Vector3.UnitZ, Vector3.One, 1000.0f, size, 100.0f);

        // octreeA.Build(particleBufferA);
        // octreeB.Build(particleBufferB);
    }



    private void AddParticlesEllipse(Vector3 center, Vector3 majorAxis, Vector3 minorAxis, float aveMass, float scale = 100.0f, float maxDistanceOffPlane = 0.0f)
    {
        Random random = new((int) DateTimeOffset.Now.UtcTicks);

        minorAxis /= majorAxis.Length;
        majorAxis.Normalize();

        Vector3 normal = Vector3.Cross(majorAxis, minorAxis);
        normal.Normalize();


        for (int i = 0; i < NumParticles; i++)
        {
            float angle = random.NextSingle() * MathF.Tau;
            float radius = random.NextSingle() * scale;
            float offPlane = (random.NextSingle() - 0.5f) * 2.0f * maxDistanceOffPlane;

            float mass = random.NextSingle() * 2.0f * aveMass;

            Vector3 newPos = center;// + (MathF.Cos(angle) * radius * majorAxis) + (MathF.Sin(angle) * radius * minorAxis) + (offPlane * normal);
            Vector3 velocity = new();// MathF.Sqrt(0.5f * centerMass / newPos.Length) * Vector3.Cross(-newPos, Vector3.UnitY).Normalized();
            Particle newParticle = new(newPos, velocity, mass);

            particleBufferA.Add(newParticle);
            particleBufferB.Add(newParticle);
        }
    }



    public void Run()
    {
        Running = true;

        while (Running)
        {
            // BuildNextTree();

            // for (int i = 0; i < NumParticles; i++)
            // {
            //     StepParticle(i);
            // }
            
            Parallel.For(0, NumParticles, StepParticle);

            while (ExternalReadingBuffer) { }

            SwapBuffers();
        }
    }



    private void StepParticle(int particleIndex)
    {
        List<Particle> prevBuffer = GetPrevParticleBuffer();
        Particle particle = prevBuffer[particleIndex];
        
        Vector3 gravForce = Vector3.Zero;// 10000.0f * GetPrevTree().CalcGravForce(particle.Position);

        for (int otherParticleIndex = 0; otherParticleIndex < NumParticles; otherParticleIndex++)
        {
            if (particleIndex == otherParticleIndex) continue;

            Particle otherParticle = prevBuffer[otherParticleIndex];

            Vector3 direction = otherParticle.Position - particle.Position;
            float distance = MathF.Max(direction.Length, 0.005f);
            direction /= distance;

            gravForce += 100.0f * (otherParticle.Mass / (distance * distance)) * direction;
        }

        Vector3 acceleration = gravForce / particle.Mass;
        // particle.Position += (timeStep * particle.Velocity) + (0.5f * timeStep * timeStep * acceleration);
        // particle.Velocity += timeStep * acceleration;

        List<Particle> nextBuffer = GetNextParticleBuffer();
        nextBuffer[particleIndex] = particle;
    }



    // private void BuildNextTree()
    // {
    //     GetNextTree().Build(GetPrevParticleBuffer());
    // }



    public List<Vector3> GetParticlePositions()
    {
        List<Vector3> particlePositions = GetPrevParticleBuffer().Select(particle => particle.Position).ToList();

        return particlePositions;
    }



    public List<Particle> GetPrevParticleBuffer() => UseParticleBufferA ? particleBufferB : particleBufferA;
    private List<Particle> GetNextParticleBuffer() => UseParticleBufferA ? particleBufferA : particleBufferB;
    // public SpacialOctree GetPrevTree() => UseParticleBufferA ? octreeB : octreeA;
    // private SpacialOctree GetNextTree() => UseParticleBufferA ? octreeA : octreeB;
    private void SwapBuffers() => UseParticleBufferA = !UseParticleBufferA;
}
