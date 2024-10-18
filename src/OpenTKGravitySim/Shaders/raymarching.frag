
#version 330 core

out vec4 FragColor;

uniform int numParticles;
uniform vec3 positions[1000];

uniform vec2 windowSize;
uniform vec3 cameraForward;
uniform vec3 cameraRight;
uniform vec3 cameraUp;
uniform vec3 cameraPos;


int numValidPositions = 0;
vec3 validPositions[1000];

float farClip = 5000.0;



struct Ray
{
    vec3 direction;
    vec3 origin;
};



const int max_steps = 50;




float sdSphere(vec3 point, vec3 center, float radius)
{
    return length(point - center) - radius;
}



float sdScene(vec3 point, Ray ray)
{
    int prevNumValid = numValidPositions;
    numValidPositions = 0;
    float minDist = farClip;
    for(int i = 0; i < prevNumValid; i++)
    {
        vec3 pos = validPositions[i];

        vec3 dir = pos - point;
        float dist = length(dir);

        if (dist > farClip || dot(ray.direction, dir / dist) < 0.9) continue;
        
        validPositions[numValidPositions++] = pos;
        float sphere = dist - 10.0;//sdSphere(point, pos, 10.0);

        if (sphere < minDist)
        {
            minDist = sphere;
        }
        
    }

    return minDist;
}



void main()
{
    vec2 uv = ((2.0 * gl_FragCoord.xy) - windowSize.xy) / min(windowSize.x, windowSize.y);

    vec3 direction = (uv.x * cameraRight) + (uv.y * cameraUp) + (1.0 * cameraForward);

    Ray ray;
    ray.direction = normalize(direction);
    ray.origin = cameraPos;

    for(int i = 0; i < numParticles; i++)
    {
        vec3 pos = positions[i];

        vec3 dir = pos - cameraPos;
        float dist = length(dir);

        if (dist > farClip || dot(cameraForward, dir / dist) < 0.5) continue;

        validPositions[numValidPositions++] = positions[i];
    }

    // Raymarch loop
    float distanceTraveled = 0.0;
    int step;
    for(step = 0; step < max_steps; step++)
    {
        vec3 point = ray.origin + (ray.direction * distanceTraveled);

        float sceneDistance = sdScene(point, ray);

        distanceTraveled += sceneDistance;

        if (sceneDistance < 0.1) break;

        if (numValidPositions <= 0 || distanceTraveled > farClip)
        {
            discard;
        }
    }
    
    FragColor = vec4(vec3(1.0 - (distanceTraveled / farClip)), 1.0);
}
