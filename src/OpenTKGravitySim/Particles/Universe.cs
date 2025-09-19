
using System.Diagnostics;
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Particles;



public class Universe
{
    public readonly SpacialOctree _octree;
    public List<Particle> RefParticles => _useBufA ? _particleBufA : _particleBufB;
    public List<Particle> UpdateParticles => _useBufA ? _particleBufB : _particleBufA;
    private bool _useBufA = true;
    private readonly List<Particle> _particleBufA;
    private readonly List<Particle> _particleBufB;
    public List<Particle> Particles => [.. RefParticles];
    public List<SpacialOctreeNode> LeafNodes => [.. _octree.Leaves];
    public int NumParticles { get; private set; }
    public double GravMult { get; private set; } = 500.0;

    public double TimeStep;
    public bool Running;
    public bool Paused = true;
    public double SimulationTime = 0.0;
    public Vector3d TotalMomentum;
    public Vector3d TotalVelocity;
    public double TotalMass;
    public Vector3d CenterOfMass;

    private readonly Random _random = new();



    public Universe(int numParticles, double size, double timeStep = 0.001)
    {
        TimeStep = timeStep;
        Running = false;

        _octree = new(0.1);

        _particleBufA = new(NumParticles);
        _particleBufB = new(NumParticles);

        double weight = 1.1;
        int numClusters = 5;
        double geoFactor = (1.0 - Math.Pow(weight, numClusters)) / (1.0 - weight);
        for (int i = 0; i < numClusters; i++)
        {
            var clusterSize = Math.Pow(weight, i) / geoFactor;
            var clusterRadius = size * Math.Sqrt(clusterSize);
            var clusterStars = (int)Math.Floor(numParticles * clusterSize);
            var center = 0.5 * size * RandomVector3(_random);

            Console.WriteLine($"{i}: size: {clusterSize}, stars: {clusterStars}");

            if (_random.NextDouble() < 0.3)
            {
                AddParticlesEllipse(clusterStars, center, Vector3d.Zero,
                    RandomVector3(_random).Normalized(), RandomVector3(_random),
                    20.0, clusterRadius, clusterRadius / 100.0);
            }
            else
            {
                AddParticlesCluster(clusterStars, center, 0.0, 10.0, 20.0, clusterRadius);
            }
        }

        // AddParticlesEllipse(numParticles / 2, new(500.0f, -50.0f, 1000.0f), 0.0 * 0.05 * -Vector3d.UnitX, 2.0f * Vector3.UnitZ, Vector3.UnitY, 20.0, size, size / 50.0);
        // AddParticlesEllipse(numParticles / 2, new(-500.0f, 50.0f, 1000.0f), 0.0 * 0.05 * Vector3d.UnitX, 2.0f * Vector3.UnitZ, Vector3.UnitY, 20.0, size, size / 50.0);

        // AddParticlesEllipse(numParticles, Vector3d.Zero, Vector3d.Zero, 2.0f * Vector3.UnitZ, Vector3.UnitY, 20.0, size, 0.0);

        _octree.Build(RefParticles);

        NumParticles = Math.Min(RefParticles.Count, UpdateParticles.Count);

        BalanceParticles();
    }



    private void AddParticlesEllipse(int numParticles, Vector3d center, Vector3d ellipseVelocity, Vector3d majorAxis, Vector3d minorAxis, double aveMass, double scale = 100.0, double maxDistanceOffPlane = 0.0)
    {
        Random random = new((int)DateTimeOffset.Now.UtcTicks);

        minorAxis /= majorAxis.Length;
        majorAxis.Normalize();

        Vector3d normal = Vector3d.Cross(majorAxis, minorAxis);
        normal.Normalize();

        var centerParticle = new Particle(center, ellipseVelocity, aveMass * numParticles);
        _particleBufA.Add(centerParticle);
        _particleBufB.Add(centerParticle);

        for (int i = 0; i < numParticles - 1; i++)
        {
            double angle = random.NextDouble() * Math.Tau;
            double radius = (0.1 * scale) + (random.NextDouble() * scale);
            double offPlane = (random.NextDouble() - 0.5) * 2.0 * maxDistanceOffPlane;

            double mass = random.NextDouble() * 2.0 * aveMass;

            Vector3d newPos = center + (Math.Cos(angle) * radius * majorAxis) + (Math.Sin(angle) * radius * minorAxis) + (offPlane * normal);
            Vector3d velocity = ellipseVelocity + Math.Sqrt(10.0 * aveMass * numParticles / (newPos - center).Length) * Vector3d.Cross(newPos - center, normal).Normalized();
            Particle newParticle = new(newPos, velocity, mass);

            _particleBufA.Add(newParticle);
            _particleBufB.Add(newParticle);
        }
    }



    private void AddParticlesCluster(int numParticles, Vector3d center, double aveClusterSpeed, double aveParticleSpeed, double aveMass, double clusterSize = 10.0)
    {
        Random random = new((int)DateTimeOffset.Now.UtcTicks);

        Vector3d clusterVelocity = aveClusterSpeed * RandomVector3(random);

        for (int particle = 0; particle < numParticles; particle++)
        {
            Vector3d particlePosition = (0.5 * clusterSize * RandomVector3(random)) + center;
            Vector3d particleVelocity = (aveParticleSpeed * RandomVector3(random)) + clusterVelocity;
            double mass = random.NextDouble() * 2.0 * aveMass;
            Particle newParticle = new(particlePosition, particleVelocity, mass);

            _particleBufA.Add(newParticle);
            _particleBufB.Add(newParticle);
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

        for (int particle = 0; particle < RefParticles.Count; particle++)
        {
            TotalMomentum += RefParticles[particle].Mass * RefParticles[particle].Velocity;
            TotalVelocity += RefParticles[particle].Velocity;
            CenterOfMass += RefParticles[particle].Mass * RefParticles[particle].Position;
            TotalMass += RefParticles[particle].Mass;
        }
        CenterOfMass /= TotalMass;

        _useBufA = !_useBufA;
    }



    private void BalanceParticles()
    {
        UpdateGlobals();

        for (int p = 0; p < NumParticles; p++)
        {
            var particle = RefParticles[p];
            particle.Position -= CenterOfMass;
            RefParticles[p] = particle;

            particle = UpdateParticles[p];
            particle.Position -= CenterOfMass;
            UpdateParticles[p] = particle;
        }
    }



    private readonly Stopwatch _particleStopwatch = new();
    public void Run()
    {
        Running = true;

        while (Running)
        {
            if (Paused) continue;

            BalanceParticles();

            BuildNextTree();

            _particleStopwatch.Restart();
            Parallel.For(0, NumParticles, StepParticle);
            _particleStopwatch.Stop();

            Console.WriteLine($"tree:\t\t{_buildTreeStopwatch.Elapsed}");
            Console.WriteLine($"particles:\t{_particleStopwatch.Elapsed}");

            SimulationTime += TimeStep;
        }
    }



    private void StepParticle(int particleIndex)
    {
        Particle particle = RefParticles[particleIndex];

        var octreeGF = GravMult * _octree.CalcGravForce(particle.Position);

        var force = octreeGF;

        Vector3d acceleration = force / particle.Mass;

        if (acceleration.Length > 5_000.0)
        {
            acceleration = 5_000.0 * acceleration.Normalized();
        }

        particle.Position += new Vector3d((TimeStep * particle.Velocity) + (0.5 * TimeStep * TimeStep * acceleration));
        particle.Velocity += new Vector3d(TimeStep * acceleration);

        if (particle.Velocity.Length > 5_000.0)
        {
            particle.Velocity = 5_000.0 * particle.Velocity.Normalized();
        }

        UpdateParticles[particleIndex] = particle;
    }



    private readonly Stopwatch _buildTreeStopwatch = new();
    private void BuildNextTree()
    {
        _buildTreeStopwatch.Restart();
        _octree.Build(RefParticles);
        _buildTreeStopwatch.Stop();
    }
}
