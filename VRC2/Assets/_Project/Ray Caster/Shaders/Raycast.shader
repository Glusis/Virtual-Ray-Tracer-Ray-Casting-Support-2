Shader "Unlit/Raycast"
{
    Properties
    {
        _MainTex ("Texture", 3D) = "white" {}//the 3d texture with density values
        _MinDensity ("MinDensity", float) = 0.02//minimum density under which samples are ignored
        _StepSize ("Step Size", float) = 0.01//distance between samples taken
        _AlphaCutoff ("Alpha cutoff", float) = 0.8//maximum opacity for accumulate
        _CompositingFunction ("Compositing Function", int) = 0//enum for which compositing method is to be used
        _TargetDens ("Target Density(first)",float) = 0.42//target density for first compositing method
        _Transfer1 ("Transfer1",float) = 0.0//color lookup table
        _Transfer2 ("Transfer2",float) = 0.0
        _Transfer3 ("Transfer3",float) = 0.0
        _Transfer4 ("Transfer4",float) = 0.0
        _Transfer5 ("Transfer5",float) = 0.0
        _Transfer1c ("Transfer1c",Color) = (0,0,0,0)
        _Transfer2c ("Transfer2c",Color) = (0,0,0,0)
        _Transfer3c ("Transfer3c",Color) = (0,0,0,0)
        _Transfer4c ("Transfer4c",Color) = (0,0,0,0)
        _Transfer5c ("Transfer5c",Color) = (0,0,0,0)
    }
    SubShader
    {
        Tags { "Queue" = "AlphaTest" "RenderType" = "Transparent" }
        ZWrite Off
        ZTest LEqual
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Back

        Pass
        {
            CGPROGRAM

//WARNING: if you edit the shader and the following two lines appear, delete them or it will not run
// Upgrade NOTE: excluded shader from DX11; has structs without semantics (struct v2f members pos)
// #pragma exclude_renderers d3d11

            
            #include "Compositing.cginc"
            #include "Lighting.cginc"

            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            // Maximum amount of raymarching samples
            //TODO: set dynamically
            #define MAX_STEP_COUNT 800

            // Allowed floating point inaccuracy
            #define EPSILON 0.00001f

            //vertex shader input
            struct appdata
            {
                float4 vertex : POSITION;
            };

            //vertex shader output/fragment shader input
            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 objectVertex : TEXCOORD0;
                float3 vectorToSurface : TEXCOORD1;

                float4 pos: TEXCOORD4;
                // float4 screenuv: TEXCOORD5;
            };
            
            //properties, see above
            sampler3D _MainTex;
            sampler3D _NormalTex;
            float4 _MainTex_ST;
            float _MinDensity;
            float _StepSize;
            float _AlphaCutoff;
            int _CompositingFunction;
            float _TargetDens;
            float _Transfer1;
            float _Transfer2;
            float _Transfer3;
            float _Transfer4;
            float _Transfer5;
            float4 _Transfer1c;
            float4 _Transfer2c;
            float4 _Transfer3c;
            float4 _Transfer4c;
            float4 _Transfer5c;

            sampler2D _CameraDepthTexture;

            ///<summary>
            /// vertex shader. calculates ray origin and direction.
            ///</summary>
            v2f vert (appdata v)
            {
                v2f o;
                // Vertex in object space this will be the starting point of raymarching
                o.objectVertex = v.vertex;

                // Calculate vector from camera to vertex in world space
                float3 worldVertex = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.vectorToSurface = worldVertex - _WorldSpaceCameraPos;

                //convert to clip position
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 Transfer(float dens)//overloaded transfer function for legibility
            {
                return Transfer(dens,_Transfer1,_Transfer2,_Transfer3,_Transfer4,_Transfer5,_Transfer1c,_Transfer2c,_Transfer3c,_Transfer4c,_Transfer5c);
            }

            //central differences for calculating surface normals for lighting
            float3 centralDiff(float x, float y, float z, float delta)
            {
                float xdif = tex3D(_MainTex,float3(x + delta, y, z)     + float3(0.5f, 0.5f, 0.5f)).a- tex3D(_MainTex,float3(x - delta, y, z) + float3(0.5f, 0.5f, 0.5f)).a;
                float ydif = tex3D(_MainTex,float3(x , y + delta, z)    + float3(0.5f, 0.5f, 0.5f)).a- tex3D(_MainTex,float3(x , y - delta, z) + float3(0.5f, 0.5f, 0.5f)).a;
                float zdif = tex3D(_MainTex,float3(x , y, z + delta)    + float3(0.5f, 0.5f, 0.5f)).a- tex3D(_MainTex,float3(x , y, z - delta) + float3(0.5f, 0.5f, 0.5f)).a;
                return float3(xdif, ydif, zdif);
            }

            
            /**
             *fragment shader containing the main raycasting loop for each rayAW 
             */
            fixed4 frag(v2f i) : SV_Target
            {
                // Start raymarching at the front surface of the object
                float3 rayOrigin = i.objectVertex;

                // Use vector from camera to object surface to get ray direction
                float3 rayDirection = mul(unity_WorldToObject, float4(normalize(i.vectorToSurface), 1));

                float4 color = float4(0, 0, 0, 0);
                
                float dens = 0.0f;
                float3 samplePosition = rayOrigin;

                int sampleCount = 0; //sample count for average compositing

                //calculate distance between surface of voxel grid and objects in depth buffer
                float4 screenuv = ComputeScreenPos(i.pos);
                float2 uv = screenuv.xy / screenuv.w;
                float depth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture,uv);
                float surfaceDepth = i.pos.z / i.pos.w;
                float depthLength = max(LinearEyeDepth(depth) - LinearEyeDepth(surfaceDepth),0);
                int stepsToDepth = depthLength / _StepSize;
            
                int stepCount;
                float diag = (sqrt(3.0)*3);//(sqrt(3)*3) is diagonal of cube, therefore longest distance
                if(_StepSize > 0)//safeguard against division by 0
                {
                        stepCount = (diag / _StepSize); 
                }else
                {
                    stepCount = 500;
                }
                // Raymarch through object space
                [loop]//prevent unrolling of loop by compiler
                for (int i = 0; i < stepCount; i++)
                {
                    if(i>=stepsToDepth)//if we've hit another object
                    {
                        continue;
                    }
                    
                    // Accumulate color only within unit cube bounds
                    if(max(abs(samplePosition.x), max(abs(samplePosition.y), abs(samplePosition.z))) < 1.5f + EPSILON)
                    {
                        float sampledDens = tex3D(_MainTex, samplePosition + float3(0.5f, 0.5f, 0.5f)).a;
                        if(sampledDens >= _MinDensity)//skip samples under minAlpha
                        {
                            sampleCount++;
                            if(_CompositingFunction == 0)
                            {
                                color = Accumulate(color, _AlphaCutoff,_StepSize,sampledDens,_Transfer1,_Transfer2,_Transfer3,_Transfer4,_Transfer5,_Transfer1c,_Transfer2c,_Transfer3c,_Transfer4c,_Transfer5c); 
                            }
                            if(_CompositingFunction == 1)
                            {
                                dens = Maximum(dens, sampledDens);
                            }
                            if(_CompositingFunction == 2)
                            {
                                dens = Average(dens, sampledDens, sampleCount); 
                            }
                            if(_CompositingFunction == 3)
                            {
                                // dens = First(dens, sampledDens, _TargetDens);
                                if(color.r == 0.0 && color.g == 0.0 && color.b == 0.0)//early ray termination
                                {
                                    color = First_lit(color ,sampledDens ,_TargetDens, samplePosition);
                                }
                            }
                        }
                        samplePosition += rayDirection * _StepSize;
                    }
                }
                if(_CompositingFunction == 0)
                {
                    return color;
                }
                if(_CompositingFunction == 3)
                {
                    //sample normal map at color.rgb
                    // float3 normal = tex3D(_NormalTex, color.rgb + float3(0.5f, 0.5f, 0.5f)).rgb;
                    float3 normal = centralDiff(color.r,color.g,color.b, 1.0f/256);
                    //apply transfer function
                    color = Transfer(color.a);
                    if(color.a <= 0)//skip for background 
                        {
                            return color;
                        }
                    //phong shade
                    float3 lightPos = float3(0,3,7);
                    float4 diffusion = float4(color.rgb * diffuse(lightPos,normal,1,1),color.a);
                    float4 specularity = float4(color.rgb * specular(lightPos,normal,1,1,3),color.a);
                    float4 ambient = color * 0.1;
                    
                    return ambient + diffusion + specularity;
                    // return float4(normal,1);
                }
                return Transfer(dens,_Transfer1,_Transfer2,_Transfer3,_Transfer4,_Transfer5,_Transfer1c,_Transfer2c,_Transfer3c,_Transfer4c,_Transfer5c);
            }
            ENDCG
        }
    }
}
