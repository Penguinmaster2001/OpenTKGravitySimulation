
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;

using OpenTKTutorial.Graphics;



namespace OpenTKGravitySim.Particles;



internal class Universe
{
    private List<Particle> particles;
    private float timeStep;
    private readonly List<Vector3> verts;
    private readonly List<uint> indices;

    private VAO vao;
    private VBO<Vector3> vertexVbo;
    private IBO ibo;



    public Universe(int numParticles, float size, float timeStep = 0.001f)
    {
        this.timeStep = timeStep;

        Random random = new((int) DateTimeOffset.Now.UtcTicks);

        particles = new(numParticles + 1);
        verts = new(3 * numParticles);
        indices = [];
        
        float centerMass = 1_000_000.0f;
        Particle centerParticle = new(Vector3.Zero, Vector3.Zero, centerMass);
        particles.Add(centerParticle);

        verts.AddRange([Vector3.Zero, Vector3.Zero, Vector3.Zero]);

        for (int i = 0; i < numParticles; i++)
        {
            Vector3 newPos = size * 2.0f * (new Vector3(random.NextSingle(), random.NextSingle(), random.NextSingle()) - new Vector3(0.5f));
            Vector3 velocity = MathF.Sqrt(0.5f * centerMass / newPos.Length) * Vector3.Cross(-newPos, Vector3.UnitY).Normalized();
            Particle newParticle = new(newPos, velocity, 500.0f * random.NextSingle());

            verts.AddRange([Vector3.Zero, Vector3.Zero, Vector3.Zero]);

            particles.Add(newParticle);
        }


        vao = new();
        vao.Bind();

        vertexVbo = new(verts);
        vertexVbo.Bind();
        vao.LinkToVAO(0, 3, vertexVbo);
        vertexVbo.UnBind();

        vao.UnBind();
    }



    private List<Vector3> VertsFromParticle(Particle particle, Camera camera)
    {
        float radius = MathF.Cbrt(particle.Mass);
        indices.AddRange([(uint)indices.Count, (uint)indices.Count + 1, (uint)indices.Count + 2]);

        // Vector3 toCamera = (camera.Position - particle.Position).Normalized();
        Vector3 bottomDir = camera.right;//Vector3.Cross(toCamera, camera.up).Normalized();
        Vector3 upDir = camera.up;//Vector3.Cross(toCamera, bottomDir).Normalized();
        Vector3 bottomLeft = (-radius * bottomDir) - (0.5f * MathF.Sqrt(3.0f) * radius * upDir);
        Vector3 bottomRight = (radius * bottomDir) - (0.5f * MathF.Sqrt(3.0f) * radius * upDir);
        Vector3 top = 0.5f * MathF.Sqrt(3.0f) * radius * upDir;

        return [
            particle.Position + bottomLeft,
            particle.Position + bottomRight,
            particle.Position + top
        ];
    }



    public void Update(Camera camera, FrameEventArgs args)
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

        verts.Clear();

        foreach (Particle particle in particles)
        {
            verts.AddRange(VertsFromParticle(particle, camera));
        }

        vertexVbo.SubData(verts);
    }



    public void Render(ShaderProgram shaderProgram)
    {
        vao.Bind();
        if (ibo == null)
        {
            ibo = new IBO(indices);
        }
        else
        {
            ibo.Bind();
        }

        shaderProgram.Bind();
        vao.Bind();
        ibo.Bind();
        
        GL.DrawElements(PrimitiveType.Points, indices.Count, DrawElementsType.UnsignedInt, 0);
    }



    public void Delete()
    {
        ibo.Delete();
        vertexVbo.Delete();
        vao.Delete();
    }
}
