
#version 430 core



uniform mat4 view;
uniform mat4 projection;

layout(location = 0) in vec4 inPosition;
layout(location = 1) in vec4 inVelocity;
layout(location = 2) in vec4 inAttributes;

out vec4 velocity;
out vec4 attributes;
out float cameraDist;
out float size;



void main()
{
    vec4 viewPos = inPosition * view;
    cameraDist = max(0.0001, length(viewPos.xyz));

    // clip space position
    vec4 clipSpacePos = projection * viewPos;
    gl_Position = clipSpacePos;

    velocity = inVelocity;
    attributes = inAttributes;

    float baseSize = inAttributes.x;
    size = 10.0 * baseSize / cameraDist;
    gl_PointSize = max(1.0, size);
}
