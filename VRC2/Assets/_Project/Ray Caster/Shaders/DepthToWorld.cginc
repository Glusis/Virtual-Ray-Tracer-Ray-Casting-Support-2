#ifndef DEPTHTOWORLD_CGINC
#define DEPTHTOWORLD_CGINC

bool depthIsNotSky(float depth)
{
    #if defined(UNITY_REVERSED_Z)
    return (depth > 0.0);
    #else
    return (depth < 1.0);
    #endif
}

float4 getWorldPosFromDepth(float2 screenUV, v2f i)
{
    float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, screenUV);
    float sceneZ = LinearEyeDepth(depth);

    float3 viewPlane = i.camRelativeWorldPos.xyz / dot(i.camRelativeWorldPos.xyz, unity_WorldToCamera._m20_m21_m22);

    float3 worldPos = viewPlane * sceneZ + _WorldSpaceCameraPos;
    worldPos = mul(unity_CameraToWorld, float4(worldPos, 1.0));

    half4 col = 0;

    if (depthIsNotSky(depth))
        col.rgb = saturate(2.0 - abs(frac(worldPos) * 2.0 - 1.0) * 100.0);

    return col;
}


#endif