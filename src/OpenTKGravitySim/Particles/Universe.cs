
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

using OpenTKGravitySim.Graphics;



namespace OpenTKGravitySim.Particles;



internal class Universe
{
    public readonly List<Particle> particles;

    public List<float> ParticlePositions;
    public int NumParticles { get; private set; }
    private float timeStep;



    public Universe(int numParticles, float size, float timeStep = 0.001f)
    {
        this.timeStep = timeStep;

        Random random = new((int) DateTimeOffset.Now.UtcTicks);

        particles = new(numParticles);
        
        float centerMass = 1_000_000.0f;
        Particle centerParticle = new(Vector3.Zero, Vector3.Zero, centerMass);
        particles.Add(centerParticle);

        for (int i = 1; i < numParticles; i++)
        {
            Vector3 newPos = size * 2.0f * (new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle()) - new Vector3(0.5f));
            Vector3 velocity = MathF.Sqrt(0.5f * centerMass / newPos.Length) * Vector3.Cross(-newPos, Vector3.UnitY).Normalized();
            Particle newParticle = new(newPos, velocity, 500.0f * random.NextSingle());

            particles.Add(newParticle);
        }

        NumParticles = particles.Count;


        ParticlePositions = new(particles.Count * 3);
        
        for (int i = 0; i < particles.Count; i++)
        {
            ParticlePositions.Add(particles[i].Position.X);
            ParticlePositions.Add(particles[i].Position.Y);
            ParticlePositions.Add(particles[i].Position.Z);
        }
    }



    public void Update(FrameEventArgs args)
    {
        float frameDelta = (float) args.Time;

        int iterations = (int) MathF.Ceiling(frameDelta / timeStep);


        for (int iteration = 0; iteration < iterations; iteration++)
        {
            foreach (Particle particle in particles)
            {
                Vector3 gravForce = Vector3.Zero;
                
                foreach (Particle otherParticle in particles)
                {
                    if (particle == otherParticle) continue;
                    Vector3 direction = otherParticle.Position - particle.Position;
                    float distance = direction.Length;
                    direction /= distance;

                    gravForce += 100.0f * (otherParticle.Mass / (distance * distance)) * direction;
                }

                Vector3 acceleration = gravForce / particle.Mass;
                particle.Velocity += timeStep * acceleration;
                particle.Position += timeStep * particle.Velocity;
            }
        }


        ParticlePositions = new(particles.Count * 3);
        
        for (int i = 0; i < particles.Count; i++)
        {
            ParticlePositions.Add(particles[i].Position.X);
            ParticlePositions.Add(particles[i].Position.Y);
            ParticlePositions.Add(particles[i].Position.Z);
        }
    }
}
