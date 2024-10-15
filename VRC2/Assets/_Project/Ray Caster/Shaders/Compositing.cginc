#ifndef COMPOSITING_CGINC
#define COMPOSITING_CGINC

//Linear interpolation between two colors for transfer function
float4 Monolinear(float4 color1, float4 color2, float sample, float p1, float p2)
{
    float4 color;
    float dist1 = sample - p1;
    float dist2 = p2 - sample;

    color = (dist2 / (dist1 + dist2)) * color1 + (dist1 / (dist1 + dist2)) * color2;
    return color;
}

/**
 * Transfer function.
 * tx is transfer density x
 * cx is transfer color x
 */
float4 Transfer(float dens, float t1, float t2, float t3, float t4, float t5, float4 c1, float4 c2, float4 c3,
                float4 c4, float4 c5)
{
    float4 color = float4(0, 0, 0, 0);
    if (dens >= t5)
    {
        color = c5;
    }
    else if (dens >= t4)
    {
        color = Monolinear(c4, c5, dens, t4, t5);
        // color = _Transfer4c;
    }
    else if (dens >= t3)
    {
        // color = _Transfer3c;
        color = Monolinear(c3, c4, dens, t3, t4);
    }
    else if (dens >= t2)
    {
        // color = _Transfer2c;
        color = Monolinear(c2, c3, dens, t2, t3);
    }
    else if (dens >= t1)
    {
        // color = _Transfer1c;
        color = Monolinear(c1, c2, dens, t1, t2);
    }
    return color;
}


//Compositing Functions

float4 Accumulate(float4 color, float opacityCutoff, float stepSize, float4 newDens,float t1, float t2, float t3, float t4, float t5, float4 c1, float4 c2, float4 c3,
                float4 c4, float4 c5)
{
    float4 newColor = Transfer(newDens,t1,t2,t3,t4,t5,c1,c2,c3,c4,c5);

    newColor.a = 1 - pow((1 - newColor.a), (stepSize / 0.1f));

    float test = color.a + (1.0 - color.a) * newColor.a;

    if (test <= opacityCutoff)
    {
        color = color + (1.0 - color.a) * newColor;
    }
    return color;
}


float Average(float dens, float newDens, int sampleCount)
{
    dens = dens + (newDens - dens) / sampleCount;
    return dens;
}

float Maximum(float dens, float newDens)
{
    dens = max(dens, newDens);
    return dens;
}

float First(float dens, float newDens, float targetDens)
{
    float epsilon = 0.05;
    if (newDens <= targetDens + epsilon && newDens >= targetDens - epsilon)
    {
        dens = targetDens;
    }
    return dens;
}

float4 First_lit(float4 color, float newDens, float targetDens, float3 samplePosition)
{
    float epsilon = 0.05;
    if (newDens <= targetDens + epsilon && newDens >= targetDens - epsilon)
    {
        color.a = targetDens;
        //flag to terminate ray and record surface point
        color.rgb = samplePosition;
    }
    return color;
}

#endif
