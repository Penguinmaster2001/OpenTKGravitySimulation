
#version 430 core



struct Particle
{
    vec4 position;
    vec4 velocity;
    vec4 mass;
};

// const int particleSizeFloats = 12;



layout(std430, binding = 0) buffer Buf {
    Particle particles[];
};

out vec4 FragColor;

uniform int numParticles;
uniform vec2 windowSize;

uniform mat4 view;
uniform mat4 projection;


float maxDist = 50000.0f;



void main()
{
    mat4 toClipSpace = view * projection;

    for(int i = 0; i < numParticles; i++)
    {
        Particle particle = particles[i];

        vec4 clipSpacePos = particle.position * toClipSpace;

        vec3 ndcPos = clipSpacePos.xyz / clipSpacePos.w;

        if (ndcPos.x >= -1.0 && ndcPos.x <= 1.0 &&
            ndcPos.y >= -1.0 && ndcPos.y <= 1.0 &&
            ndcPos.z >= -1.0 && ndcPos.z <= 1.0)
        {
            vec2 windowCoord = ndcPos.xy * 0.5 + 0.5;

            vec2 coord = gl_FragCoord.xy / windowSize.x;
            vec2 dir = coord - windowCoord;
            float sqrDist = (dir.x * dir.x) + (dir.y * dir.y);

            float depth = 1.0 - (0.5 * (ndcPos.z + 1.0));
            if (sqrDist < depth * depth * (pow(0.1 * particle.mass.x, 0.66)))
            {
                FragColor = vec4((particle.velocity.xyz * 0.01f) + vec3(0.5), 1.0);
                return;
            }
        }
    }
    
    discard;
}
