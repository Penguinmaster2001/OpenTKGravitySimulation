
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
    public Particle[] Particles => GetPrevParticleBuffer().Take(Math.Min(NumParticles, 1000)).ToArray();
    public int NumParticles { get; private set; }
    private float timeStep;
    public bool Running;
    public float SimulationTime = 0.0f;



    public Universe(int numParticles, float size, float timeStep = 0.01f)
    {
        this.timeStep = timeStep;
        Running = false;

        octreeA = new(1.0f, 10);
        octreeB = new(1.0f, 10);

        particleBufferA = new(NumParticles);
        particleBufferB = new(NumParticles);

        // particleBufferA.Add(new(new(0.0f, 0.0f, 0.0f, 1.0f), Vector4.Zero, 5_000_000.0f));
        // particleBufferB.Add(new(new(0.0f, 0.0f, 0.0f, 1.0f), Vector4.Zero, 5_000_000.0f));
        // AddParticlesEllipse(numParticles, Vector3.Zero, 2.0f * Vector3.UnitZ, Vector3.UnitY, 10.0f, size, size / 5.0f);
        AddParticlesCluster(numParticles, 10, Vector3.Zero, 10.0f, 1.0f, 10.0f, size, size / 100.0f);

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
            // Vector4 velocity = new(10.0f * (random.NextSingle() - 0.5f), 10.0f * (random.NextSingle() - 0.5f), 10.0f * (random.NextSingle() - 0.5f), 0.0f);
            Vector4 velocity = new(MathF.Sqrt(10_000.0f / newPos.Length) * Vector3.Cross(-(newPos.Xyz - center), normal).Normalized(), 0.0f);
            Particle newParticle = new(newPos, velocity, mass);

            particleBufferA.Add(newParticle);
            particleBufferB.Add(newParticle);
        }
    }



    private void AddParticlesCluster(int numParticles, int numClusters, Vector3 center, float aveClusterSpeed, float aveParticleSpeed, float aveMass, float globalSize = 1000.0f, float clusterSize = 10.0f)
    {
        Random random = new((int) DateTimeOffset.Now.UtcTicks);

        int particlesPerCluster = numParticles / numClusters;

        for (int cluster = 0; cluster < numClusters; cluster++)
        {
            Vector3 clusterCenter = (0.5f * globalSize * RandomVector3(random)) + center;
            Vector3 clusterVelocity = aveClusterSpeed * RandomVector3(random);

            for (int particle = 0; particle < particlesPerCluster; particle++)
            {
                Vector3 particlePosition = (0.5f * clusterSize * RandomVector3(random)) + clusterCenter;
                Vector3 particleVelocity = (aveParticleSpeed * RandomVector3(random)) + clusterVelocity;
                float mass = random.NextSingle() * 2.0f * aveMass;
                Particle newParticle = new(new(particlePosition, 1.0f), new(particleVelocity, 0.0f), mass);

                particleBufferA.Add(newParticle);
                particleBufferB.Add(newParticle);
            }
        }
    }



    private static Vector3 RandomVector3(Random random)
    {
        return (2.0f * new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle())) - Vector3.One;
    }




    public void Run()
    {
        Running = true;
        // var stopwatch = new System.Diagnostics.Stopwatch();
        // stopwatch.Start();

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
            // if (SimulationTime > 10.0f) Running = false;

            SwapBuffers();
        }

        // stopwatch.Stop();
        // Console.WriteLine($"Time for 10 seconds: {stopwatch.ElapsedMilliseconds} ms");
    }



    private void StepParticle(int particleIndex)
    {
        List<Particle> prevBuffer = GetPrevParticleBuffer();
        Particle particle = prevBuffer[particleIndex];
        
        // Vector3 gravForce = 1000.0f * GetPrevTree().CalcGravForce(particle.Position.Xyz);

        // for (int otherParticleIndex = 0; otherParticleIndex < NumParticles; otherParticleIndex++)
        // {
        //     if (particleIndex == otherParticleIndex) continue;

        //     Particle otherParticle = prevBuffer[otherParticleIndex];

        //     Vector3 direction = otherParticle.Position.Xyz - particle.Position.Xyz;
        //     float distance = MathF.Max(direction.Length, 0.005f);
        //     direction /= distance;

        //     gravForce += 1000.0f * (otherParticle.Mass.X / (distance * distance)) * direction;
        // }

        // Vector3 acceleration = gravForce / particle.Mass.X;

        // particle.Position += new Vector4((timeStep * particle.Velocity.Xyz) + (0.5f * timeStep * timeStep * acceleration), 0.0f);
        // particle.Velocity += new Vector4(timeStep * acceleration, 0.0f);

        (Vector3 pos, Vector3 vel) = RK4Integration(particle.Position.Xyz, particle.Velocity.Xyz);
        particle.Position = new(pos, 1.0f);
        particle.Velocity = new(vel, 0.0f);

        if (!particle.IsValid())
        {
            throw new Exception($"Invalid particle!!! {particleIndex}: {particle}\n");
        }

        List<Particle> nextBuffer = GetNextParticleBuffer();
        nextBuffer[particleIndex] = particle;


        (Vector3, Vector3) EulerIntegration(Vector3 pos, Vector3 vel)
        {
            (vel, Vector3 acc) = Derivatives(pos, vel);
            pos += (timeStep * vel) + (0.5f * timeStep * timeStep * acc);
            vel += timeStep * acc;
            return (pos, vel);
        }


        (Vector3, Vector3) RK4Integration(Vector3 pos, Vector3 vel)
        {
            (Vector3 k1Pos, Vector3 k1Vel) = Derivatives(pos, vel);
            (Vector3 k2Pos, Vector3 k2Vel) = Derivatives(pos + (0.5f * timeStep * k1Pos), vel + (0.5f * timeStep * k1Vel));
            (Vector3 k3Pos, Vector3 k3Vel) = Derivatives(pos + (0.5f * timeStep * k2Pos), vel + (0.5f * timeStep * k2Vel));
            (Vector3 k4Pos, Vector3 k4Vel) = Derivatives(pos + (timeStep * k3Pos), vel + (timeStep * k3Vel));
            pos += (timeStep / 6.0f) * (k1Pos + (2.0f * k2Pos) + (2.0f * k3Pos) + k4Pos);
            vel += (timeStep / 6.0f) * (k1Vel + (2.0f * k2Vel) + (2.0f * k3Vel) + k4Vel);
            return (pos, vel);
        }


        // Returns derivatives of position and velocity as (velocity, acceleration)
        (Vector3, Vector3) Derivatives(Vector3 position, Vector3 velocity)
        {
            return (velocity, Vector3.Clamp(1000.0f * GetPrevTree().CalcGravForce(position) / particle.Mass.X, 100.0f * -Vector3.One,  100.0f * Vector3.One));
        }
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
