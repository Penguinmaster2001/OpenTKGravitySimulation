
using System.Numerics;
using OpenTK.Mathematics;



namespace OpenTKGravitySim.Particles;



internal class Universe
{
    private readonly SpacialOctree[] octrees;
    public readonly List<Particle>[] particleBuffers;
    public int ReadBufferIndex;
    public const int NumBuffers = 6;
    public bool ExternalReadingBuffer = false;
    public List<Particle> Particles => GetParticleBuffer().ToList();
    public List<SpacialOctreeNode> LeafNodes => GetTree().Leaves.ToList();
    public int NumParticles { get; private set; }
    public double TimeStep;
    public bool Running;
    public double SimulationTime = 0.0;
    public Vector3d TotalMomentum;
    public Vector3d TotalVelocity;
    public double TotalMass;
    public Vector3d CenterOfMass;
    public double GravMultiplier = 1.0;



    public Universe(int numParticles, double size, double timeStep = 0.01)
    {
        this.TimeStep = timeStep;
        Running = false;
        ReadBufferIndex = 0;

        octrees = new SpacialOctree[NumBuffers];
        particleBuffers = new List<Particle>[NumBuffers];
        for (int buffer = 0; buffer < NumBuffers; buffer++)
        {
            octrees[buffer] = new(0.25, 20);
            particleBuffers[buffer] = new(numParticles);
        }

        particleBufferA.Add(new(new(0.0f, 0.0f, 0.0f, 1.0f), Vector4d.Zero, 5_000_000.0f));
        particleBufferB.Add(new(new(0.0f, 0.0f, 0.0f, 1.0f), Vector4d.Zero, 5_000_000.0f));
        AddParticlesEllipse(numParticles / 2, new(500.0, -50.0, 1000.0), 2.0f * -Vector3d.UnitX, 2.0f * Vector3d.UnitZ, Vector3d.UnitY, 50.0f, size, size / 50.0f);
        AddParticlesEllipse(numParticles / 2, new(-500.0, 50.0, 1000.0), 2.0f *  Vector3d.UnitX, 2.0f * Vector3d.UnitZ, Vector3d.UnitY, 50.0f, size, size / 50.0f);
        AddParticlesCluster(numParticles, 3, Vector3d.Zero, 2.0, 1.0, 10.0, size, size / 4.0);
        AddParticlesEllipse(numParticles / 2, new(-1000.0, 50.0, 0.0), 100.0 *  Vector3d.UnitX, 2.0 * Vector3d.UnitZ, Vector3d.UnitY, 50.0, size, size / 50.0);

        particleBufferA.Add(new(new(200.0f, 0.0f, 0.0f, 1.0f), new(0.0f, 0.0f, 20.0f, 0.0f), 10.0f));
        // particleBufferA.Add(new(new(-200.0f, 0.0f, 0.0f, 1.0f), new(0.0f, 20.0f, 0.0f, 0.0f), 10.0f));

        particleBufferB.Add(new(new(200.0f, 0.0f, 0.0f, 1.0f), new(0.0f, 0.0f, 20.0f, 0.0f), 10.0f));
        // particleBufferB.Add(new(new(-200.0f, 0.0f, 0.0f, 1.0f), new(0.0f, 20.0f, 0.0f, 0.0f), 10.0f));

        // GenerateStarSystem(1, Vector3d.Zero, Vector3d.Zero, Vector3d.UnitY, 100_000.0, 0.99, 100.0);
        
        for (int buffer = 0; buffer < NumBuffers; buffer++)
        {
            octrees[buffer].Build(particleBuffers[buffer]);
        }

        NumParticles = GetParticleBuffer().Count;
    }



    private void GenerateStarSystem(int numPlanets, Vector3d center, Vector3d velocity, Vector3d orbitPlaneNormal, double starMass, double starMassProportion, double minPlanetOrbitSpacing)
    {
        Random random = new((int) DateTimeOffset.Now.UtcTicks);

        orbitPlaneNormal.Normalize();
        double angleOffX = Vector3d.CalculateAngle(orbitPlaneNormal, Vector3d.UnitX);
        Vector3d axis1 =  0.25 * Math.PI < angleOffX && angleOffX < 0.75 * Math.PI ? Vector3d.Cross(orbitPlaneNormal, Vector3d.UnitX) : Vector3d.Cross(orbitPlaneNormal, Vector3d.UnitZ);
        Vector3d axis2 = Vector3d.Cross(orbitPlaneNormal, axis1);


        Particle star = new(center, velocity, starMass);
        AddToBuffers(star);
        double totalAllowedPlanetMass = starMass * (1.0 - starMassProportion);
        double[] planetMasses = new double[numPlanets];
        
        double planetMassMultiplier = 0.0;
        for (int planetIndex = 0; planetIndex < numPlanets; planetIndex++)
        {
            double planetMass = random.NextDouble();
            planetMassMultiplier += planetMass;
            planetMasses[planetIndex] = planetMass;
        }
        planetMassMultiplier = totalAllowedPlanetMass / planetMassMultiplier;

        double orbitRadius = 0.0;
        for (int planetIndex = 0; planetIndex < numPlanets; planetIndex++)
        {
            orbitRadius += minPlanetOrbitSpacing * (1.0 + 0.1 * random.NextDouble());
            double startAngle = 2.0 * random.NextDouble() * Math.PI;
            double planetMass = planetMasses[planetIndex] * planetMassMultiplier;
            Vector3d startPosition = center + (Math.Cos(startAngle) * orbitRadius * axis1) + (Math.Sin(startAngle) * orbitRadius * axis2);
            Vector3d toStar = center - startPosition;
            double orbitSpeed = Math.Sqrt(GravMultiplier * (planetMass + starMass) / toStar.Length);
            Vector3d orbitVelocity = orbitSpeed * Vector3d.Cross(toStar, orbitPlaneNormal).Normalized();

            Particle planet = new(startPosition, orbitVelocity, planetMass);
            AddToBuffers(planet);
        }
    }



    private void AddParticlesEllipse(int numParticles, Vector3d center, Vector3d ellipseVelocity, Vector3d majorAxis, Vector3d minorAxis, double aveMass, double scale = 100.0, double maxDistanceOffPlane = 0.0)
    {
        Random random = new((int) DateTimeOffset.Now.UtcTicks);

        minorAxis /= majorAxis.Length;
        majorAxis.Normalize();

        Vector3d normal = Vector3d.Cross(majorAxis, minorAxis);
        normal.Normalize();


        for (int i = 0; i < numParticles; i++)
        {
            double angle = random.NextDouble() * Math.Tau;
            double radius = (0.1 + random.NextDouble()) * scale;
            double offPlane = (random.NextDouble() - 0.5) * 2.0 * maxDistanceOffPlane;

            double mass = random.NextDouble() * 2.0 * aveMass;

            Vector3d newPos = center + (Math.Cos(angle) * radius * majorAxis) + (Math.Sin(angle) * radius * minorAxis) + (offPlane * normal);
            // Vector4 velocity = new(10.0f * (random.NextSingle() - 0.5f), 10.0f * (random.NextSingle() - 0.5f), 10.0f * (random.NextSingle() - 0.5f), 0.0f) + ellipseVelocity;
            Vector3d velocity = ellipseVelocity;// + MathF.Sqrt(10.0f / (newPos - center).Length) * Vector3.Cross(newPos - center, normal).Normalized();
            Particle newParticle = new(newPos, velocity, mass);

            if (!newParticle.IsValid())
            {
                throw new Exception($"Invalid particle!!! {i}: {newParticle}\n");
            }

            AddToBuffers(newParticle);
        }
    }


    private void AddParticlesCluster(int numParticles, int numClusters, Vector3d center, double aveClusterSpeed, double aveParticleSpeed, double aveMass, double globalSize = 1000.0, double clusterSize = 10.0)
    {
        Random random = new((int) DateTimeOffset.Now.UtcTicks);

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

                AddToBuffers(newParticle);
            }
        }
    }



    private static Vector3d RandomVector3(Random random)
    {
        return (2.0 * new Vector3d(random.NextDouble(), random.NextDouble(), random.NextDouble())) - Vector3d.One;
    }



    private void AddToBuffers(Particle particle)
    {
        for (int buffer = 0; buffer < NumBuffers; buffer++)
        {
            Console.WriteLine($"Adding {particle} to buffer {buffer}");
            particleBuffers[buffer].Add(particle);
            Console.WriteLine($"In buffer at {buffer}: {particleBuffers[buffer].Last()}");
        }
    }




    private void UpdateGlobals()
    {
        TotalMomentum = new();
        TotalVelocity = new();
        CenterOfMass = new();
        TotalMass = 0.0;
        GetParticleBuffer(0).ForEach(p =>{
            TotalMomentum += p.Mass * p.Velocity;
            TotalVelocity += p.Velocity;
            CenterOfMass += p.Mass * p.Position;
            TotalMass += p.Mass;
        });
        CenterOfMass /= TotalMass;
    }




    public void Run()
    {
        Running = true;
        // var stopwatch = new System.Diagnostics.Stopwatch();
        // stopwatch.Start();
        int i = 0;

        while (i < 10 && Running)
        {            
            UpdateGlobals();
            // Parallel.Invoke(() => BuildNextTree(0), () => Parallel.For(0, NumParticles, particleIndex => StepParticle(particleIndex, TimeStep, 0)));
            // Parallel.Invoke(() => BuildNextTree(1), () => Parallel.For(0, NumParticles, particleIndex => StepParticle(particleIndex, 0.5 * TimeStep, 1)));
            // Parallel.Invoke(() => BuildNextTree(2), () => Parallel.For(0, NumParticles, particleIndex => StepParticle(particleIndex, 0.5 * TimeStep, 2)));
            // Parallel.Invoke(() => BuildNextTree(3), () => Parallel.For(0, NumParticles, particleIndex => StepParticle(particleIndex, TimeStep, 3)));
            BuildNextTree(0);
            Parallel.For(0, NumParticles, particleIndex => StepParticle(particleIndex, TimeStep, 0));
            BuildNextTree(1);
            Parallel.For(0, NumParticles, particleIndex => StepParticle(particleIndex, 0.5 * TimeStep, 1));
            BuildNextTree(2);
            Parallel.For(0, NumParticles, particleIndex => StepParticle(particleIndex, 0.5 * TimeStep, 2));
            BuildNextTree(3);
            Parallel.For(0, NumParticles, particleIndex => StepParticle(particleIndex, TimeStep, 3));

            Parallel.For(0, NumParticles, particleIndex => CombineParticleSamples(particleIndex, TimeStep));

            SimulationTime += TimeStep;
            // if (SimulationTime > 10.0f) Running = false;

            Particle p0 = GetParticleBuffer()[0];
            Particle p1 = GetParticleBuffer()[1];
            Vector3d toOther = p0.Position - p1.Position;
            Vector3d expGravForce = toOther.Normalized() * GravMultiplier * (p0.Mass + p1.Mass) / toOther.LengthSquared;
            Console.WriteLine($"0 Expected acceleration: {expGravForce / p0.Mass}");
            Console.WriteLine($"0 Actual acceleration: {p0.Acceleration}");
            Console.WriteLine($"1 Expected acceleration: {expGravForce / p1.Mass}");
            Console.WriteLine($"1 Actual acceleration: {p1.Acceleration}");
            Console.WriteLine($"Values: {p0}\n{p1}\n{toOther}\n{expGravForce}\n\n");
            IncrementBuffers(5);
            i++;
        }

        // stopwatch.Stop();
        // Console.WriteLine($"Time for 10 seconds: {stopwatch.ElapsedMilliseconds} ms");
    }



    /// <summary>
    /// Update particles in 5 based on 0, 1, 2, 3, and 4
    /// Uses RK4
    /// </summary>
    /// <param name="particleIndex"></param>
    /// <param name="timeStep"></param>
    private void CombineParticleSamples(int particleIndex, double timeStep)
    {
        Particle particle = GetParticleBuffer(0)[particleIndex];
        Particle k1Particle = GetParticleBuffer(1)[particleIndex];
        Particle k2Particle = GetParticleBuffer(2)[particleIndex];
        Particle k3Particle = GetParticleBuffer(3)[particleIndex];
        Particle k4Particle = GetParticleBuffer(4)[particleIndex];

        double particleMass = particle.Mass;
        particle.Acceleration = particle.Velocity;
        particle = (timeStep / 6.0) * (k1Particle + (2.0 * k2Particle) + (2.0 * k3Particle) + k4Particle);
        particle.Mass = particleMass;
        particle.Acceleration = (particle.Velocity - particle.Acceleration) / timeStep;
        Console.WriteLine($"{particleIndex} s Actual acceleration: {particle.Acceleration}");
        GetParticleBuffer(5)[particleIndex] = particle;
    }



    /// <summary>
    /// Update particles in 1 based on particles from 0
    /// </summary>
    /// <param name="particleIndex"></param>
    /// <param name="timeStep"></param>
    /// <param name="bufferOffset"></param>
    /// <exception cref="Exception"></exception>
    private void StepParticle(int particleIndex, double timeStep, int bufferOffset = 0)
    {
        Particle particle = GetParticleBuffer(bufferOffset)[particleIndex];

        (Vector3d pos, Vector3d vel) = RK4Integration(particle.Position, particle.Velocity);
        particle.Velocity = vel - (TotalVelocity / NumParticles);
        particle.Position = pos - CenterOfMass;

        if (!particle.IsValid())
        {
            Running = false;
            throw new Exception($"Invalid particle!!! {particleIndex}: {particle}\n");
        }

        GetParticleBuffer(bufferOffset + 1)[particleIndex] = particle;


        (Vector3d, Vector3d) EulerIntegration(Vector3d pos, Vector3d vel)
        {
            (vel, Vector3d acc) = Derivatives(pos, vel);
            pos += (timeStep * vel) + (0.5 * timeStep * timeStep * acc);
            vel += timeStep * acc;
            return (pos, vel);
        }


        (Vector3d, Vector3d) RK4Integration(Vector3d pos, Vector3d vel)
        {
            (Vector3d k1Pos, Vector3d k1Vel) = Derivatives(pos, vel);
            (Vector3d k2Pos, Vector3d k2Vel) = Derivatives(pos + (0.5 * timeStep * k1Pos), vel + (0.5 * timeStep * k1Vel));
            (Vector3d k3Pos, Vector3d k3Vel) = Derivatives(pos + (0.5 * timeStep * k2Pos), vel + (0.5 * timeStep * k2Vel));
            (Vector3d k4Pos, Vector3d k4Vel) = Derivatives(pos + (timeStep * k3Pos), vel + (timeStep * k3Vel));
            pos += (timeStep / 6.0) * (k1Pos + (2.0 * k2Pos) + (2.0 * k3Pos) + k4Pos);
            vel += (timeStep / 6.0) * (k1Vel + (2.0 * k2Vel) + (2.0 * k3Vel) + k4Vel);
            return (pos, vel);
        }


        // Returns derivatives of position and velocity as (velocity, acceleration)
        (Vector3d, Vector3d) Derivatives(Vector3d position, Vector3d velocity)
        {
            Vector3d acc = GravMultiplier * GetTree().CalcGravForce(position) / particle.Mass;
            return (velocity, acc);
        }
    }



    /// <summary>
    /// Build the tree at 0 based on particle buffer -1
    /// </summary>
    /// <param name="offset"></param>
    private void BuildNextTree(int offset = 0)
    {
        GetTree(offset).Build(GetParticleBuffer(offset));
    }



    public List<Particle> GetParticleBuffer(int offset = 0) => particleBuffers[((offset % NumBuffers) + NumBuffers + ReadBufferIndex) % NumBuffers];
    private SpacialOctree GetTree(int offset = 0) => octrees[((offset % NumBuffers) + NumBuffers + ReadBufferIndex) % NumBuffers];

    /// <summary>
    /// Rotate buffer at amount to 0
    /// </summary>
    /// <param name="amount"></param>
    private void IncrementBuffers(int amount = 1) => ReadBufferIndex = ((amount % NumBuffers) + NumBuffers + ReadBufferIndex) % NumBuffers;
}
