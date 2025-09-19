
using OpenTK.Mathematics;



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
        Particle particle = refParticles[particleIndex];

        var octreeGF = parameters.GravMult * octree.CalcGravForce(particle.Position);

        var force = octreeGF;

        Vector3d acceleration = force / particle.Mass;

        if (acceleration.Length > 5_000.0)
        {
            acceleration = 5_000.0 * acceleration.Normalized();
        }

        particle.Position += new Vector3d((parameters.TimeStep * particle.Velocity) + (0.5 * parameters.TimeStep * parameters.TimeStep * acceleration));
        particle.Velocity += new Vector3d(parameters.TimeStep * acceleration);

        if (particle.Velocity.Length > 5_000.0)
        {
            particle.Velocity = 5_000.0 * particle.Velocity.Normalized();
        }

        updateParticles[particleIndex] = particle;
    }
}
