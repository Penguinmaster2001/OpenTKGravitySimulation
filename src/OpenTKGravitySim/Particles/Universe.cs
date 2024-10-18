
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Particles;



internal class Universe
{
    public readonly List<Particle> particleBufferA;
    public readonly List<Particle> particleBufferB;
    public bool UseParticleBufferA = true;
    public int NumParticles { get; private set; }
    private float timeStep;
    public bool Running;



    public Universe(int numParticles, float size, float timeStep = 0.001f)
    {
        this.timeStep = timeStep;
        NumParticles = numParticles;
        Running = false;

        particleBufferA = new(NumParticles);
        particleBufferB = new(NumParticles);

        AddParticlesEllipse(Vector3.Zero, Vector3.UnitX, 2.0f * Vector3.UnitZ, 1000.0f, size, 100.0f);
    }



    private void AddParticlesEllipse(Vector3 center, Vector3 majorAxis, Vector3 minorAxis, float aveMass, float scale = 100.0f, float maxDistanceOffPlane = 0.0f)
    {
        Random random = new((int) DateTimeOffset.Now.UtcTicks);

        majorAxis.Normalize();
        minorAxis.Normalize();

        Vector3 normal = Vector3.Cross(majorAxis, minorAxis);
        
        if (MathHelper.ApproximatelyEqual(normal.LengthSquared, 1.0f, 2))
        {
            Console.WriteLine("Major and minor axis of ellipse are not orthogonal");
            normal.Normalize();
        }


        for (int i = 0; i < NumParticles; i++)
        {
            float angle = random.NextSingle() * MathF.Tau;
            float radius = (random.NextSingle() - 0.5f) * 2.0f * scale;
            float offPlane = random.NextSingle() * maxDistanceOffPlane;

            float mass = random.NextSingle() * 2.0f * aveMass;

            Vector3 newPos = center + (MathF.Cos(angle) * radius * majorAxis) + (MathF.Sin(angle) * radius * minorAxis) + (offPlane * normal);
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
            Parallel.For(0, NumParticles, StepParticle);

            SwapParticleBuffers();
        }
    }



    private void StepParticle(int particleIndex)
    {
        List<Particle> prevBuffer = GetPrevParticleBuffer();
        Particle particle = prevBuffer[particleIndex];
        
        Vector3 gravForce = Vector3.Zero;

        for (int otherParticleIndex = 0; otherParticleIndex < NumParticles; otherParticleIndex++)
        {
            if (particleIndex == otherParticleIndex) continue;

            Particle otherParticle = prevBuffer[otherParticleIndex];

            Vector3 direction = otherParticle.Position - particle.Position;
            float distance = direction.Length;
            direction /= distance;

            gravForce += 100.0f * (otherParticle.Mass / (distance * distance)) * direction;
        }

        Vector3 acceleration = gravForce / particle.Mass;
        particle.Velocity += timeStep * acceleration;
        particle.Position += timeStep * particle.Velocity;

        List<Particle> nextBuffer = GetNextParticleBuffer();
        nextBuffer[particleIndex] = particle;
    }



    public List<float> GetParticlePositions()
    {
        List<float> particlePositions = new(NumParticles * 3);

        List<Particle> unusedBuffer = GetPrevParticleBuffer();
        
        for (int i = 0; i < NumParticles; i++)
        {
            particlePositions.Add(unusedBuffer[i].Position.X);
            particlePositions.Add(unusedBuffer[i].Position.Y);
            particlePositions.Add(unusedBuffer[i].Position.Z);
        }

        return particlePositions;
    }



    private List<Particle> GetPrevParticleBuffer() => UseParticleBufferA ? particleBufferB : particleBufferA;
    private List<Particle> GetNextParticleBuffer() => UseParticleBufferA ? particleBufferA : particleBufferB;
    private void SwapParticleBuffers() => UseParticleBufferA = !UseParticleBufferA;
}
