
#version 430 core



uniform mat4 view;
uniform mat4 projection;
uniform float fov;
uniform vec3 cameraPos;

layout(location = 0) in vec4 inPosition;
layout(location = 1) in vec4 inVelocity;
layout(location = 2) in vec4 inAttributes;

out vec4 velocity;
out vec4 attributes;
out float cameraDist;
out float size;
out float redshift;



void main()
{
    velocity = inVelocity;
    attributes = inAttributes;

    vec4 viewPos = inPosition * view;
    cameraDist = max(0.0001, length(viewPos.xyz));

    vec3 dir = cameraPos - inPosition.xyz;
    redshift = dot(velocity.xyz, dir) / (length(dir) * 1000.0);

    // clip space position
    vec4 clipSpacePos = projection * viewPos;
    gl_Position = clipSpacePos;

    size = 100.0 * ( 1.0 / tan(0.00872664625997 * fov)) * pow(inAttributes.x, 1.0 / 3.0) / cameraDist;
    gl_PointSize = max(1.0, size);
}
