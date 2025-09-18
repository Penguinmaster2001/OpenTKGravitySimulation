
#version 430 core



uniform mat4 view;
uniform mat4 projection;

layout(location = 0) in vec4 inPosition;
layout(location = 1) in vec4 inVelocity;
layout(location = 2) in vec4 inAttributes;

out vec4 velocity;
out vec4 attributes;



void main()
{
    velocity = inVelocity;
    attributes = inAttributes;
    gl_Position = inPosition * view * projection;
    gl_PointSize = 100 * inAttributes.x;
}
