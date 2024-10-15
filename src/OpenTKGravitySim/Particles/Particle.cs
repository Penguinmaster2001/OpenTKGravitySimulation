
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Particles;



internal class Particle(Vector3 initialPosition, Vector3 initialVelocity, float mass)
{
    public Vector3 Position = initialPosition;
    public Vector3 Velocity = initialVelocity;
    public float Mass = mass;
}
