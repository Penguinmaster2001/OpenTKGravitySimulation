
#version 430 core

#define GAMMA 0.8



in vec4 velocity;
in vec4 attributes;
in float cameraDist;
in float size;
in float redshift;



out vec4 fragColor;



// inputs: vec3 colorRGB (0..1), float z (>=0)
const vec3 lambdaRGB = vec3(610.0, 540.0, 460.0); // approximate nm for R,G,B peaks

// Gaussian spectral response for R,G,B when mapping spectrum -> RGB
float gauss(float lam, float mu, float sigma) {
    float x = (lam - mu) / sigma;
    return exp(-0.5*x*x);
}

// Convert RGB -> spectral amplitudes at three wavelengths (simple proxy)
vec3 rgbToSpec(vec3 rgb) {
    // assume each channel represents amplitude near its center wavelength
    return rgb;
}

// Resample spectral amplitudes after redshift
vec3 redshiftRGB(vec3 rgb, float z) {
    // map RGB to spectral amplitudes
    vec3 spec = rgbToSpec(rgb);

    // shift wavelengths
    vec3 lamShifted = lambdaRGB * (1.0 + z);

    // now reconstruct RGB by integrating shifted spectral peaks against RGB sensor curves
    // approximate sensor curves with Gaussians centered at original lambdaRGB
    float sigma = 40.0; // width in nm, tweak for softer/harder mixing

    // compute how much each shifted spectral peak contributes to each RGB sensor
    mat3 M;
    for (int i = 0; i < 3; ++i) {
        for (int j = 0; j < 3; ++j) {
            // contribution of source peak i (at lamShifted[i]) to sensor j (center lambdaRGB[j])
            M[j][i] = gauss(lamShifted[i], lambdaRGB[j], sigma);
        }
    }

    // combine: newRGB_j = sum_i M[j][i] * spec[i]
    vec3 outC;
    outC.r = dot(M[0], spec);
    outC.g = dot(M[1], spec);
    outC.b = dot(M[2], spec);

    // normalize / gamma-correct as desired
    outC = clamp(outC, 0.0, 1.0);

    return outC;
}



void main()
{
    vec2 c = gl_PointCoord - vec2(0.5);
    float grad = dot(c, c);
    if (grad > 0.25) discard;

    float intensity = 10.0 * min(2.0, 1000.0 * attributes.x / cameraDist);
    float alpha = min(size, 1.0) * 4.0 * (0.25 - grad);
    vec3 color = redshiftRGB(vec3(1.0, 0.81, 0.62), redshift);
    
    fragColor = vec4(intensity * color, alpha);
}
