
#version 330 core

out vec4 FragColor;

uniform vec3 positions[1000];
uniform int numParticles;
uniform vec2 windowSize;

uniform mat4 view;
uniform mat4 projection;



void main()
{
    float nearestZ = 1.01;
    float nearestDepth = 0.0;
    for(int i = 0; i < numParticles; i++)
    {
        vec4 clipSpacePos = vec4(positions[i], 1.0) * view * projection;

        vec3 ndcPos = clipSpacePos.xyz / clipSpacePos.w;

        if (ndcPos.x >= -1.0 && ndcPos.x <= 1.0 &&
            ndcPos.y >= -1.0 && ndcPos.y <= 1.0 &&
            ndcPos.z >= -1.0 && ndcPos.z <= 1.0)
        {
            vec2 windowCoord = ndcPos.xy * 0.5 + 0.5;

            vec2 coord = gl_FragCoord.xy / windowSize.x;
            float dist = distance(coord, windowCoord);

            float depth = 1.0 - (0.5 * (ndcPos.z + 1.0));
            if (dist < 5.0 * depth)
            {
                if (ndcPos.z < nearestZ)
                {
                    nearestDepth = depth;
                    nearestZ = ndcPos.z;
                }
            }
        }
    }

    if (nearestZ >= 1.0) discard;

    FragColor = vec4(vec3(1000.0 * nearestDepth), 1.0);
}
