
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
    float nearestZ = 1.01;
    float nearestDepth = 0.0;
    vec3 nearestVel = vec3(0.0);
    mat4 toClipSpace = view * projection;

    for(int i = 0; i < numParticles; i++)
    {
        Particle particle = particles[i];
        // int offset = particleSizeFloats * i;
        // Particle particle = Particle(
        //                              vec4(particles[offset + 0], particles[offset + 1], particles[offset + 2], 1.0),
        //                              vec4(particles[offset + 4], particles[offset + 5], particles[offset + 6], 0.0),
        //                              particles[offset + 8],
        //                              0.0, 0.0, 0.0);

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
            if (ndcPos.z < nearestZ && sqrDist < 5.0 * depth * depth * (pow(0.01 * particle.mass.x, 0.66)))
            {
                nearestDepth = depth;
                nearestZ = ndcPos.z;
                nearestVel = particle.velocity.xyz;
            }
        }
    }

    if (nearestZ >= 1.0) discard;
    // maxDist * nearestDepth * 
    FragColor = vec4((nearestVel * 0.01f) + vec3(0.5), 1.0);
}
