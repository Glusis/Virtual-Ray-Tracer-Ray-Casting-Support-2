#ifndef LIGHTING_CGINC
#define LIGHTING_CGINC

    //All non-trivial Phong lighting components
        
    //simple diffuse lighting
    float diffuse(float3 lightPos, float3 normal, float lightStrength, float diffuseConstant)
    {
        return dot(lightPos,normal) * lightStrength * diffuseConstant;
    }
    //simple specular lighting
    float specular(float3 lightPos, float3 normal, float lightStrength, float specConstant, float shininess)
    {
        return pow(dot(lightPos,normal), shininess) * lightStrength *specConstant;
    }

#endif