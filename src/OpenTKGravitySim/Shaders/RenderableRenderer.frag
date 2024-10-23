
#version 430 core



struct Renderable
{
    vec4 position;
    vec4 velocity;
    vec4 attributes;
};

// const int particleSizeFloats = 12;



layout(std430, binding = 0) buffer Buf {
    Renderable renderables[];
};

out vec4 FragColor;

uniform int numRenderables;
uniform vec2 windowSize;

uniform mat4 view;
uniform mat4 projection;


float maxDist = 50000.0f;



void main()
{
    mat4 toClipSpace = view * projection;
    // FragColor = vec4(0.0);
    {
    vec4 clipSpacePos = vec4(0.0, 0.0, 0.0, 1.0) * toClipSpace;

    vec3 ndcPos = clipSpacePos.xyz / clipSpacePos.w;

    if (ndcPos.x >= -1.0 && ndcPos.x <= 1.0 &&
        ndcPos.y >= -1.0 && ndcPos.y <= 1.0 &&
        ndcPos.z >= -1.0 && ndcPos.z <= 1.0)
    {
        vec2 windowCoord = ndcPos.xy * 0.5 + 0.5;

        vec2 coord = gl_FragCoord.xy / windowSize.x;
        vec2 dir = coord - windowCoord;
        float sqrDist = (dir.x * dir.x) + (dir.y * dir.y);

        if (sqrDist < 0.00001)
        {
            FragColor = vec4(1.0, 1.0, 1.0, 1.0);
            return;
        }
    }

    clipSpacePos = vec4(10.0, 0.0, 0.0, 1.0) * toClipSpace;

    ndcPos = clipSpacePos.xyz / clipSpacePos.w;

    if (ndcPos.x >= -1.0 && ndcPos.x <= 1.0 &&
        ndcPos.y >= -1.0 && ndcPos.y <= 1.0 &&
        ndcPos.z >= -1.0 && ndcPos.z <= 1.0)
    {
        vec2 windowCoord = ndcPos.xy * 0.5 + 0.5;

        vec2 coord = gl_FragCoord.xy / windowSize.x;
        vec2 dir = coord - windowCoord;
        float sqrDist = (dir.x * dir.x) + (dir.y * dir.y);

        if (sqrDist < 0.00001)
        {
            FragColor = vec4(1.0, 0.0, 0.0, 1.0);
            return;
        }
    }

    clipSpacePos = vec4(0.0, 10.0, 0.0, 1.0) * toClipSpace;

    ndcPos = clipSpacePos.xyz / clipSpacePos.w;

    if (ndcPos.x >= -1.0 && ndcPos.x <= 1.0 &&
        ndcPos.y >= -1.0 && ndcPos.y <= 1.0 &&
        ndcPos.z >= -1.0 && ndcPos.z <= 1.0)
    {
        vec2 windowCoord = ndcPos.xy * 0.5 + 0.5;

        vec2 coord = gl_FragCoord.xy / windowSize.x;
        vec2 dir = coord - windowCoord;
        float sqrDist = (dir.x * dir.x) + (dir.y * dir.y);

        if (sqrDist < 0.00001)
        {
            FragColor = vec4(0.0, 1.0, 0.0, 1.0);
            return;
        }
    }

    clipSpacePos = vec4(0.0, 0.0, 10.0, 1.0) * toClipSpace;

    ndcPos = clipSpacePos.xyz / clipSpacePos.w;

    if (ndcPos.x >= -1.0 && ndcPos.x <= 1.0 &&
        ndcPos.y >= -1.0 && ndcPos.y <= 1.0 &&
        ndcPos.z >= -1.0 && ndcPos.z <= 1.0)
    {
        vec2 windowCoord = ndcPos.xy * 0.5 + 0.5;

        vec2 coord = gl_FragCoord.xy / windowSize.x;
        vec2 dir = coord - windowCoord;
        float sqrDist = (dir.x * dir.x) + (dir.y * dir.y);

        if (sqrDist < 0.00001)
        {
            FragColor = vec4(0.0, 0.0, 1.0, 1.0);
            return;
        }
    }
    }

    for(int i = 0; i < numRenderables; i++)
    {
        Renderable renderable = renderables[i];

        vec4 clipSpacePos = renderable.position;// * toClipSpace;

        vec3 ndcPos = clipSpacePos.xyz;// / clipSpacePos.w;

        vec2 windowCoord = ndcPos.xy * 0.5 + 0.5;

        vec2 coord = gl_FragCoord.xy / windowSize.x;
        vec2 dir = coord - windowCoord;
        float sqrDist = (dir.x * dir.x) + (dir.y * dir.y);

        float depth = 1.0 - (0.5 * (ndcPos.z + 1.0));
        if (sqrDist < depth * depth * 0.1 * renderable.attributes.x)
        {
            // FragColor = vec4((renderable.velocity.xyz * 0.01f) + vec3(0.5), 1.0);
            FragColor = vec4(0.5, vec2(renderable.attributes.x / renderable.attributes.y), 1.0);
            return;
            // FragColor += vec4(0.5, vec2(renderable.attributes.x / renderable.attributes.y), 1.0) * depth * depth * 0.1 * renderable.attributes.x / sqrDist;
        }
    }
    
    // if (FragColor == vec4(0.0)) discard;
    discard;
}
