
#version 430 core



in vec4 velocity;
in vec4 attributes;



out vec4 fragColor;



void main()
{
    vec2 c = gl_PointCoord - vec2(0.5);
    if (dot(c, c) > 1.0) discard;
    
    fragColor = velocity + attributes;//vec4(0.96, 0.75, 0.43, 1.0);
}
