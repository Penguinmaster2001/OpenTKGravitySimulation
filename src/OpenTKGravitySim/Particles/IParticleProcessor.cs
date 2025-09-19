
namespace OpenTKGravitySim.Particles;



public interface IParticleProcessor
{
    public void UpdateParticles(Particle[] refParticles, Particle[] updateParticles, SpacialOctree octree, SimParameters parameters);
}
