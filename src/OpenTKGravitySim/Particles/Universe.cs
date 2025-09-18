
using System.Diagnostics;
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Particles;



public class Universe
{
    public readonly SpacialOctree _octree;
    public List<Particle> _refParticles => _useBufA ? _particleBufA : _particleBufB;
    public List<Particle> _updateParticles => _useBufA ? _particleBufB : _particleBufA;
    private bool _useBufA = true;
    private readonly List<Particle> _particleBufA;
    private readonly List<Particle> _particleBufB;
    public List<Particle> Particles => [.. _refParticles];
    public List<SpacialOctreeNode> LeafNodes => [.. _octree.Leaves];
    public int NumParticles { get; private set; }
    public double GravMult { get; private set; } = 5000.0;

    public double TimeStep;
    public bool Running;
    public bool Paused = true;
    public double SimulationTime = 0.0;
    public Vector3d TotalMomentum;
    public Vector3d TotalVelocity;
    public double TotalMass;
    public Vector3d CenterOfMass;



    public Universe(int numParticles, double size, double timeStep = 0.01)
    {
        TimeStep = timeStep;
        Running = false;

        _octree = new(0.5);

        _particleBufA = new(NumParticles);
        _particleBufB = new(NumParticles);

        AddParticlesEllipse(numParticles / 2, new(500.0f, -50.0f, 1000.0f), 0.0 * 0.05 * -Vector3d.UnitX, 2.0f * Vector3.UnitZ, Vector3.UnitY, 20.0, size, size / 50.0);
        AddParticlesEllipse(numParticles / 2, new(-500.0f, 50.0f, 1000.0f), 0.0 * 0.05 * Vector3d.UnitX, 2.0f * Vector3.UnitZ, Vector3.UnitY, 20.0, size, size / 50.0);

        _octree.Build(_refParticles);

        NumParticles = Math.Min(_refParticles.Count, _updateParticles.Count);

        BalanceParticles();
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
            double radius = (0.3 * scale) + (random.NextDouble() * scale);
            double offPlane = (random.NextDouble() - 0.5) * 2.0 * maxDistanceOffPlane;

            double mass = random.NextDouble() * 2.0 * aveMass;

            Vector3d newPos = center + (Math.Cos(angle) * radius * majorAxis) + (Math.Sin(angle) * radius * minorAxis) + (offPlane * normal);
            // Vector4 velocity = new(10.0f * (random.NextSingle() - 0.5f), 10.0f * (random.NextSingle() - 0.5f), 10.0f * (random.NextSingle() - 0.5f), 0.0f) + ellipseVelocity;
            Vector3d velocity = ellipseVelocity + Math.Sqrt(5 / (newPos - center).Length) * Vector3d.Cross(newPos - center, normal).Normalized();
            Particle newParticle = new(newPos, velocity, mass);

            if (!newParticle.IsValid())
            {
                throw new Exception($"Invalid particle!!! {i}: {newParticle}\n");
            }

            _particleBufA.Add(newParticle);
            _particleBufB.Add(newParticle);
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

                _refParticles.Add(newParticle);
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

        for (int particle = 0; particle < _refParticles.Count; particle++)
        {
            TotalMomentum += _refParticles[particle].Mass * _refParticles[particle].Velocity;
            TotalVelocity += _refParticles[particle].Velocity;
            CenterOfMass += _refParticles[particle].Mass * _refParticles[particle].Position;
            TotalMass += _refParticles[particle].Mass;
        }
        CenterOfMass /= TotalMass;

        _useBufA = !_useBufA;
    }



    private void BalanceParticles()
    {
        UpdateGlobals();

        for (int p = 0; p < NumParticles; p++)
        {
            var particle = _refParticles[p];
            particle.Position -= CenterOfMass;
            _refParticles[p] = particle;

            particle = _updateParticles[p];
            particle.Position -= CenterOfMass;
            _updateParticles[p] = particle;
        }
    }



    private readonly Stopwatch _particleStopwatch = new();
    public void Run()
    {
        Running = true;
        // var stopwatch = new System.Diagnostics.Stopwatch();
        // stopwatch.Start();

        while (Running)
        {
            if (Paused) continue;

            // BuildNextTree();

            // for (int i = 0; i < NumParticles; i++)
            // {
            //     StepParticle(i);
            // }

            BalanceParticles();

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

            Console.WriteLine($"tree:\t\t{_buildTreeStopwatch.Elapsed}");
            Console.WriteLine($"particles:\t{_particleStopwatch.Elapsed}");

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
        Particle particle = _refParticles[particleIndex];

        // Vector3d gravForce = Vector3d.Zero;
        // for (int otherParticleIndex = 0; otherParticleIndex < NumParticles; otherParticleIndex++)
        // {
        //     if (particleIndex == otherParticleIndex) continue;

        //     Particle otherParticle = _refParticles[otherParticleIndex];

        //     Vector3d direction = otherParticle.Position - particle.Position;
        //     double distance = direction.Length;
        //     direction /= distance;

        //     gravForce += GravMult * (otherParticle.Mass / (distance * distance)) * direction;
        // }

        var octreeGF = GravMult * _octree.CalcGravForce(particle.Position);
        // var ngfN = gravForce.Normalized();
        // var ogfN = octreeGF.Normalized();

        // if ((ngfN - ogfN).Length > 0.01)
        // {
        //     Paused = true;
        //     Console.WriteLine($"{particleIndex}:\n{gravForce.Length:0.000}\n{octreeGF.Length:0.000}\n{ngfN.X:0.000}\t{ngfN.Y:0.000}\t{ngfN.Z:0.000}\n{ogfN.X:0.000}\t{ogfN.Y:0.000}\t{ogfN.Z:0.000}\n{(ngfN - ogfN).Length:0.000}\n\n\n");
        // }

        var force = octreeGF;
        if (force.Length > GravMult * 100.0)
        {
            force = GravMult * 100.0 * force.Normalized();
        }

        Vector3d acceleration = force / particle.Mass;

        particle.Position += new Vector3d((TimeStep * particle.Velocity) + (0.5 * TimeStep * TimeStep * acceleration));
        particle.Velocity += new Vector3d(TimeStep * acceleration);

        // (Vector3d pos, Vector3d vel) = EulerIntegration(particle.Position, particle.Velocity);
        // particle.Velocity -= TotalVelocity / NumParticles;
        // particle.Position -= CenterOfMass;

        // if (!particle.IsValid())
        // {
        //     throw new Exception($"Invalid particle!!! {particleIndex}: {particle}\n");
        // }

        _updateParticles[particleIndex] = particle;


        // (Vector3d, Vector3d) EulerIntegration(Vector3d pos, Vector3d vel)
        // {
        //     (vel, Vector3d acc) = Derivatives(pos, vel);
        //     pos += (TimeStep * vel) + (0.5 * TimeStep * TimeStep * acc);
        //     vel += TimeStep * acc;
        //     return (pos, vel);
        // }


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
        // (Vector3d velocity, Vector3d acceleration) Derivatives(Vector3d position, Vector3d velocity)
        // {
        //     return (velocity, Vector3d.Clamp(GravMult * _octree.CalcGravForce(position) / particle.Mass, 1000.0 * -Vector3d.One, 1000.0 * Vector3d.One));
        // }
    }



    private readonly Stopwatch _buildTreeStopwatch = new();
    private void BuildNextTree()
    {
        _buildTreeStopwatch.Restart();
        _octree.Build(_refParticles);
        _buildTreeStopwatch.Stop();
    }
}
