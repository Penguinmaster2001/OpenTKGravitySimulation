
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


        for (int i = 0; i < numParticles; i++)
        {
            double angle = random.NextDouble() * Math.Tau;
            double radius = (0.3 * scale) + (random.NextDouble() * scale);
            double offPlane = (random.NextDouble() - 0.5) * 2.0 * maxDistanceOffPlane;

            double mass = random.NextDouble() * 2.0 * aveMass;

            Vector3d newPos = center + (Math.Cos(angle) * radius * majorAxis) + (Math.Sin(angle) * radius * minorAxis) + (offPlane * normal);
            Vector3d velocity = ellipseVelocity + Math.Sqrt(25 / (newPos - center).Length) * Vector3d.Cross(newPos - center, normal).Normalized();
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

                RefParticles.Add(newParticle);
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
        if (force.Length > GravMult * 100.0)
        {
            force = GravMult * 100.0 * force.Normalized();
        }

        Vector3d acceleration = force / particle.Mass;

        particle.Position += new Vector3d((TimeStep * particle.Velocity) + (0.5 * TimeStep * TimeStep * acceleration));
        particle.Velocity += new Vector3d(TimeStep * acceleration);

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
