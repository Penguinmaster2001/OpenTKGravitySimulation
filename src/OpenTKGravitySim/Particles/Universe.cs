
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

        Random random = new((int) DateTimeOffset.Now.UtcTicks);

        particleBufferA = new(numParticles);
        particleBufferB = new(numParticles);
        
        float centerMass = 1_000_000.0f;
        Particle centerParticle = new(Vector3.Zero, Vector3.Zero, centerMass);
        particleBufferA.Add(centerParticle);
        particleBufferB.Add(centerParticle);

        for (int i = 1; i < numParticles; i++)
        {
            Vector3 newPos = size * 2.0f * (new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle()) - new Vector3(0.5f));
            Vector3 velocity = MathF.Sqrt(0.5f * centerMass / newPos.Length) * Vector3.Cross(-newPos, Vector3.UnitY).Normalized();
            Particle newParticle = new(newPos, velocity, 500.0f * random.NextSingle());

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
