
namespace OpenTKGravitySim.Particles;



public class CpuParticleProcessor : IParticleProcessor
{
    public void UpdateParticles(Particle[] refParticles, Particle[] updateParticles, SpacialOctree octree, SimParameters parameters)
    {
        int numParticles = Math.Min(refParticles.Length, updateParticles.Length);
        Parallel.For(0, numParticles, (pi) => StepParticle(pi, refParticles, updateParticles, octree, parameters));
    }



    private void StepParticle(int particleIndex, Particle[] refParticles, Particle[] updateParticles, SpacialOctree octree, SimParameters parameters)
    {
        var particle = refParticles[particleIndex];

        var force = parameters.GravMult * octree.CalcGravForce(particle.Position);

        var acceleration = force / particle.Mass;

        if (acceleration.Length > parameters.MaxAcceleration)
        {
            acceleration = parameters.MaxAcceleration * acceleration.Normalized();
        }

        particle.Position += (parameters.TimeStep * particle.Velocity) + (0.5 * parameters.TimeStep * parameters.TimeStep * acceleration);
        particle.Velocity += parameters.TimeStep * acceleration;

        if (particle.Velocity.Length > parameters.MaxVelocity)
        {
            particle.Velocity = parameters.MaxVelocity * particle.Velocity.Normalized();
        }

        updateParticles[particleIndex] = particle;
    }
}
