
#version 330 core

out vec4 FragColor;

void main()
{
    vec2 coord = gl_PointCoord * 2.0 - 1.0;
    float dist = dot(coord, coord);

    if (dist > 1.0)
        discard;

    FragColor = vec4(1.0f, 0.0f, 1.0f, 1.0f);
}
