
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Particles;



internal class Universe
{
    public readonly SpacialOctree octreeA;
    public readonly SpacialOctree octreeB;
    public readonly List<Particle> particleBufferA;
    public readonly List<Particle> particleBufferB;
    public bool UseParticleBufferA = true;
    public bool ExternalReadingBuffer = false;
    public Particle[] Particles => GetPrevParticleBuffer().Take(Math.Min(NumParticles, 5000)).ToArray();
    public int NumParticles { get; private set; }
    private float timeStep;
    public bool Running;
    public float SimulationTime = 0.0f;



    public Universe(int numParticles, float size, float timeStep = 0.01f)
    {
        this.timeStep = timeStep;
        Running = false;

        octreeA = new(1.0f);
        octreeB = new(1.0f);

        particleBufferA = new(NumParticles);
        particleBufferB = new(NumParticles);

        particleBufferA.Add(new(new(0.0f, 0.0f, 0.0f, 1.0f), Vector4.Zero, 50_000.0f));
        particleBufferB.Add(new(new(0.0f, 0.0f, 0.0f, 1.0f), Vector4.Zero, 50_000.0f));
        AddParticlesEllipse(numParticles, Vector3.Zero, 2.0f * Vector3.UnitZ, Vector3.UnitY, 1.0f, size, size / 10.0f);

        // particleBufferA.Add(new(new(200.0f, 0.0f, 0.0f, 1.0f), new(0.0f, 0.0f, 20.0f, 0.0f), 10.0f));
        // // particleBufferA.Add(new(new(-200.0f, 0.0f, 0.0f, 1.0f), new(0.0f, 20.0f, 0.0f, 0.0f), 10.0f));

        // particleBufferB.Add(new(new(200.0f, 0.0f, 0.0f, 1.0f), new(0.0f, 0.0f, 20.0f, 0.0f), 10.0f));
        // // particleBufferB.Add(new(new(-200.0f, 0.0f, 0.0f, 1.0f), new(0.0f, 20.0f, 0.0f, 0.0f), 10.0f));

        octreeA.Build(particleBufferA);
        octreeB.Build(particleBufferB);

        NumParticles = Math.Min(particleBufferA.Count, particleBufferB.Count);
    }



    private void AddParticlesEllipse(int numParticles, Vector3 center, Vector3 majorAxis, Vector3 minorAxis, float aveMass, float scale = 100.0f, float maxDistanceOffPlane = 0.0f)
    {
        Random random = new((int) DateTimeOffset.Now.UtcTicks);

        minorAxis /= majorAxis.Length;
        majorAxis.Normalize();

        Vector3 normal = Vector3.Cross(majorAxis, minorAxis);
        normal.Normalize();


        for (int i = 0; i < numParticles; i++)
        {
            float angle = random.NextSingle() * MathF.Tau;
            float radius = (0.2f + (random.NextSingle() * 0.8f)) * scale;
            float offPlane = (random.NextSingle() - 0.5f) * 2.0f * maxDistanceOffPlane;

            float mass = random.NextSingle() * 2.0f * aveMass;

            Vector4 newPos = new(center + (MathF.Cos(angle) * radius * majorAxis) + (MathF.Sin(angle) * radius * minorAxis) + (offPlane * normal), 1.0f);
            Vector4 velocity = new(MathF.Sqrt(25_000.0f / newPos.Length) * Vector3.Cross(-(newPos.Xyz - center), normal).Normalized(), 0.0f);
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
            
            Parallel.Invoke(BuildNextTree, () => Parallel.For(0, NumParticles, StepParticle));
            // Parallel.For(0, NumParticles, StepParticle);

            // while (ExternalReadingBuffer) { }

            SimulationTime += timeStep;

            SwapBuffers();
        }
    }



    private void StepParticle(int particleIndex)
    {
        List<Particle> prevBuffer = GetPrevParticleBuffer();
        Particle particle = prevBuffer[particleIndex];
        
        Vector3 gravForce = 1000.0f * GetPrevTree().CalcGravForce(particle.Position.Xyz);

        // for (int otherParticleIndex = 0; otherParticleIndex < NumParticles; otherParticleIndex++)
        // {
        //     if (particleIndex == otherParticleIndex) continue;

        //     Particle otherParticle = prevBuffer[otherParticleIndex];

        //     Vector3 direction = otherParticle.Position.Xyz - particle.Position.Xyz;
        //     float distance = MathF.Max(direction.Length, 0.005f);
        //     direction /= distance;

        //     gravForce += 1000.0f * (otherParticle.Mass.X / (distance * distance)) * direction;
        // }

        Vector3 acceleration = gravForce / particle.Mass.X;
        particle.Position += new Vector4((timeStep * particle.Velocity.Xyz) + (0.5f * timeStep * timeStep * acceleration), 0.0f);
        particle.Velocity += new Vector4(timeStep * acceleration, 0.0f);

        if (!particle.IsValid())
        {
            Console.WriteLine($"{particleIndex}: {particle}\n");
        }

        List<Particle> nextBuffer = GetNextParticleBuffer();
        nextBuffer[particleIndex] = particle;
    }



    private void BuildNextTree()
    {
        GetNextTree().Build(GetPrevParticleBuffer());
    }



    public List<Vector4> GetParticlePositions()
    {
        List<Vector4> particlePositions = GetPrevParticleBuffer().Select(particle => particle.Position).ToList();

        return particlePositions;
    }



    public List<Particle> GetPrevParticleBuffer() => UseParticleBufferA ? particleBufferB : particleBufferA;
    private List<Particle> GetNextParticleBuffer() => UseParticleBufferA ? particleBufferA : particleBufferB;
    public SpacialOctree GetPrevTree() => UseParticleBufferA ? octreeB : octreeA;
    private SpacialOctree GetNextTree() => UseParticleBufferA ? octreeA : octreeB;
    private void SwapBuffers() => UseParticleBufferA = !UseParticleBufferA;
}
