
#version 330 core

out vec4 FragColor;

uniform vec3 positions[1000];
// uniform float numParticles;
uniform vec2 windowSize;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

void main()
{
    for(int i = 0; i < 50; i++)
    {
        // Transform the position into clip space
        vec4 clipSpacePos = vec4(positions[i], 1.0) * model * view * projection;

        // Perform the perspective divide to get normalized device coordinates (NDC)
        vec3 ndcPos = clipSpacePos.xyz / clipSpacePos.w;

        // Check if the position is within the NDC range [-1, 1]
        if (ndcPos.x >= -1.0 && ndcPos.x <= 1.0 &&
            ndcPos.y >= -1.0 && ndcPos.y <= 1.0 &&
            ndcPos.z >= -1.0 && ndcPos.z <= 1.0)
        {
            // Convert to window coordinates (assuming a normalized coordinate system [-1, 1])
            vec2 windowCoord = ndcPos.xy * 0.5 + 0.5;

            // Calculate the distance from the current fragment to the transformed position
            vec2 coord = gl_FragCoord.xy / windowSize.x;
            float dist = distance(coord, windowCoord);

            // If the distance is within a certain threshold, color the fragment
            float depth = 1.0 - (0.5 * (ndcPos.z + 1.0));
            if (dist < 0.1 * depth + 0.01) // Adjust the threshold as needed
            {
                FragColor = vec4(1.0, 0.0, depth, 1.0);
                return;
            }
        }
    }

    discard;
}
