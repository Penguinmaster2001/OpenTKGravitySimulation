
#version 430 core



in vec4 velocity;
in vec4 attributes;
in float cameraDist;
in float size;



out vec4 fragColor;



void main()
{
    vec2 c = gl_PointCoord - vec2(0.5);
    float grad = dot(c, c);
    if (grad > 0.25) discard;

    float intensity = min(2.0, 100000.0 * attributes.x / (cameraDist * cameraDist));
    float alpha = min(size, 1.0) * 4.0 * (0.25 - grad);
    vec3 color = vec3(0.96, 0.75, 0.43);
    
    fragColor = vec4(intensity * color, alpha);
}
