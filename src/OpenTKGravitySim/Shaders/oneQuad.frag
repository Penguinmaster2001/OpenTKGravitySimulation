
#version 430 core

out vec4 FragColor;

uniform int numParticles;
uniform vec2 windowSize;

uniform mat4 view;
uniform mat4 projection;

struct Particle
{
    vec3 position;
    vec3 velocity;
    float mass;
};

layout(std430, binding = 0) buffer Particles {
    Particle particles[];
};



void main()
{
    float nearestZ = 1.01;
    float nearestDepth = 0.0;
    mat4 toClipSpace = view * projection;

    for(int i = 0; i < numParticles; i++)
    {
        Particle particle = particles[i];
        vec4 clipSpacePos = vec4(particle.position, 1.0) * toClipSpace;

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
            if (ndcPos.z < nearestZ && sqrDist < 5.0 * depth * depth * particle.mass)
            {
                nearestDepth = depth;
                nearestZ = ndcPos.z;
            }
        }
    }

    if (nearestZ >= 1.0) discard;

    FragColor = vec4(vec3(5000.0 * nearestDepth), 1.0);
}
