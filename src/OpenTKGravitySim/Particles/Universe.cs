
using System.Diagnostics;
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Particles;



public class Universe
{
    public readonly SpacialOctree _octree;
    public readonly List<Particle> _particles;
    public List<Particle> Particles => [.. _particles];
    public List<SpacialOctreeNode> LeafNodes => [.. _octree.Leaves];
    public int NumParticles { get; private set; }
    public double GravMult { get; private set; } = 100.0;

    public double TimeStep;
    public bool Running;
    public double SimulationTime = 0.0;
    public Vector3d TotalMomentum;
    public Vector3d TotalVelocity;
    public double TotalMass;
    public Vector3d CenterOfMass;



    public Universe(int numParticles, double size, double timeStep = 0.001)
    {
        TimeStep = timeStep;
        Running = false;

        _octree = new(0.05, 32);

        _particles = new(NumParticles);

        // particleBufferA.Add(new(new(0.0f, 0.0f, 0.0f, 1.0f), Vector4.Zero, 5_000_000.0f));
        // particleBufferB.Add(new(new(0.0f, 0.0f, 0.0f, 1.0f), Vector4.Zero, 5_000_000.0f));
        AddParticlesEllipse(numParticles / 2, new(500.0f, -50.0f, 1000.0f), 0.0 * 0.05 * -Vector3d.UnitX, 2.0f * Vector3.UnitZ, Vector3.UnitY, 50.0, size, size / 50.0);
        AddParticlesEllipse(numParticles / 2, new(-500.0f, 50.0f, 1000.0f), 0.0 * 0.05 * Vector3d.UnitX, 2.0f * Vector3.UnitZ, Vector3.UnitY, 50.0, size, size / 50.0);
        // AddParticlesCluster(numParticles, 3, Vector3d.Zero, 2.0, 1.0, 10.0, size, size / 4.0);
        // AddParticlesEllipse(numParticles / 2, new(-1000.0, 50.0, 0.0), 100.0 *  Vector3d.UnitX, 2.0 * Vector3d.UnitZ, Vector3d.UnitY, 50.0, size, size / 50.0);

        // particleBufferA.Add(new(new(200.0f, 0.0f, 0.0f, 1.0f), new(0.0f, 0.0f, 20.0f, 0.0f), 10.0f));
        // // particleBufferA.Add(new(new(-200.0f, 0.0f, 0.0f, 1.0f), new(0.0f, 20.0f, 0.0f, 0.0f), 10.0f));

        // particleBufferB.Add(new(new(200.0f, 0.0f, 0.0f, 1.0f), new(0.0f, 0.0f, 20.0f, 0.0f), 10.0f));
        // // particleBufferB.Add(new(new(-200.0f, 0.0f, 0.0f, 1.0f), new(0.0f, 20.0f, 0.0f, 0.0f), 10.0f));

        _octree.Build(_particles);

        NumParticles = Math.Min(_particles.Count, _particles.Count);
    }



    private void AddParticlesEllipse(int numParticles, Vector3d center, Vector3d ellipseVelocity, Vector3d majorAxis, Vector3d minorAxis, double aveMass, double scale = 100.0, double maxDistanceOffPlane = 0.0)
    {
        Random random = new((int)DateTimeOffset.Now.UtcTicks);

        minorAxis /= majorAxis.Length;
        majorAxis.Normalize();

        Vector3d normal = Vector3d.Cross(majorAxis, minorAxis);
        normal.Normalize();


        for (int i = 0; i < numParticles; i++)
        {
            double angle = random.NextDouble() * Math.Tau;
            double radius = (0.05 * scale) + (random.NextDouble() * scale);
            double offPlane = (random.NextDouble() - 0.5) * 2.0 * maxDistanceOffPlane;

            double mass = random.NextDouble() * 2.0 * aveMass;

            Vector3d newPos = center + (Math.Cos(angle) * radius * majorAxis) + (Math.Sin(angle) * radius * minorAxis) + (offPlane * normal);
            // Vector4 velocity = new(10.0f * (random.NextSingle() - 0.5f), 10.0f * (random.NextSingle() - 0.5f), 10.0f * (random.NextSingle() - 0.5f), 0.0f) + ellipseVelocity;
            Vector3d velocity = Vector3d.Zero;//ellipseVelocity + Math.Sqrt(0.05 / (newPos - center).Length) * Vector3d.Cross(newPos - center, normal).Normalized();
            Particle newParticle = new(newPos, velocity, mass);

            if (!newParticle.IsValid())
            {
                throw new Exception($"Invalid particle!!! {i}: {newParticle}\n");
            }

            _particles.Add(newParticle);
        }
    }



    private void AddParticlesCluster(int numParticles, int numClusters, Vector3d center, double aveClusterSpeed, double aveParticleSpeed, double aveMass, double globalSize = 1000.0, double clusterSize = 10.0)
    {
        Random random = new((int)DateTimeOffset.Now.UtcTicks);

        int particlesPerCluster = numParticles / numClusters;

        for (int cluster = 0; cluster < numClusters; cluster++)
        {
            Vector3d clusterCenter = (0.5 * globalSize * RandomVector3(random)) + center;
            Vector3d clusterVelocity = aveClusterSpeed * RandomVector3(random);

            for (int particle = 0; particle < particlesPerCluster; particle++)
            {
                Vector3d particlePosition = (0.5 * clusterSize * RandomVector3(random)) + clusterCenter;
                Vector3d particleVelocity = (aveParticleSpeed * RandomVector3(random)) + clusterVelocity;
                double mass = random.NextDouble() * 2.0 * aveMass;
                Particle newParticle = new(particlePosition, particleVelocity, mass);

                _particles.Add(newParticle);
            }
        }
    }



    private static Vector3d RandomVector3(Random random)
    {
        return (2.0 * new Vector3d(random.NextDouble(), random.NextDouble(), random.NextDouble())) - Vector3d.One;
    }



    private void UpdateGlobals()
    {
        TotalMomentum = Vector3d.Zero;
        TotalVelocity = Vector3d.Zero;
        CenterOfMass = Vector3d.Zero;
        TotalMass = 0.0;

        for (int particle = 0; particle < _particles.Count; particle++)
        {
            TotalMomentum += _particles[particle].Mass * _particles[particle].Velocity;
            TotalVelocity += _particles[particle].Velocity;
            CenterOfMass += _particles[particle].Mass * _particles[particle].Position;
            TotalMass += _particles[particle].Mass;
        }
        CenterOfMass /= TotalMass;
    }




    private readonly Stopwatch _particleStopwatch = new();
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

            UpdateGlobals();

            BuildNextTree();

            _particleStopwatch.Restart();
            Parallel.For(0, NumParticles, StepParticle);
            _particleStopwatch.Stop();

            // Parallel.Invoke(BuildNextTree, () =>
            // {
            //     _particleStopwatch.Restart();
            //     Parallel.For(0, NumParticles, StepParticle);
            //     _particleStopwatch.Stop();
            // });

            Console.WriteLine($"tree:\t\t{_buildTreeStopwatch.ElapsedMilliseconds}");
            Console.WriteLine($"particles:\t{_particleStopwatch.ElapsedMilliseconds}");

            // Parallel.For(0, NumParticles, StepParticle);

            // while (ExternalReadingBuffer) { }

            SimulationTime += TimeStep;
            // if (SimulationTime > 10.0f) Running = false;
        }

        // stopwatch.Stop();
        // Console.WriteLine($"Time for 10 seconds: {stopwatch.ElapsedMilliseconds} ms");
    }



    private void StepParticle(int particleIndex)
    {
        Particle particle = _particles[particleIndex];

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

        (Vector3d pos, Vector3d vel) = EulerIntegration(particle.Position, particle.Velocity);
        // float massProportion = particle.Mass / TotalMass;
        particle.Velocity = vel - (TotalVelocity / NumParticles);
        particle.Position = pos - CenterOfMass;

        if (!particle.IsValid())
        {
            throw new Exception($"Invalid particle!!! {particleIndex}: {particle}\n");
        }

        _particles[particleIndex] = particle;


        (Vector3d, Vector3d) EulerIntegration(Vector3d pos, Vector3d vel)
        {
            (vel, Vector3d acc) = Derivatives(pos, vel);
            pos += (TimeStep * vel) + (0.5 * TimeStep * TimeStep * acc);
            vel += TimeStep * acc;
            return (pos, vel);
        }


        // (Vector3d, Vector3d) RK4Integration(Vector3d pos, Vector3d vel)
        // {
        //     (Vector3d k1Pos, Vector3d k1Vel) = Derivatives(pos, vel);
        //     (Vector3d k2Pos, Vector3d k2Vel) = Derivatives(pos + (0.5 * timeStep * k1Pos), vel + (0.5 * timeStep * k1Vel));
        //     (Vector3d k3Pos, Vector3d k3Vel) = Derivatives(pos + (0.5 * timeStep * k2Pos), vel + (0.5 * timeStep * k2Vel));
        //     (Vector3d k4Pos, Vector3d k4Vel) = Derivatives(pos + (timeStep * k3Pos), vel + (timeStep * k3Vel));
        //     pos += (timeStep / 6.0) * (k1Pos + (2.0 * k2Pos) + (2.0 * k3Pos) + k4Pos);
        //     vel += (timeStep / 6.0) * (k1Vel + (2.0 * k2Vel) + (2.0 * k3Vel) + k4Vel);
        //     return (pos, vel);
        // }


        // Returns derivatives of position and velocity as (velocity, acceleration)
        (Vector3d velocity, Vector3d acceleration) Derivatives(Vector3d position, Vector3d velocity)
        {
            return (velocity, Vector3d.Clamp(GravMult * _octree.CalcGravForce(position) / particle.Mass, 1000.0 * -Vector3d.One, 1000.0 * Vector3d.One));
        }
    }



    private readonly Stopwatch _buildTreeStopwatch = new();
    private void BuildNextTree()
    {
        _buildTreeStopwatch.Restart();
        _octree.Build(_particles);
        _buildTreeStopwatch.Stop();
    }
}
