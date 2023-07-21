Shader"Unlit/WaterWaveSurfaces/waterSurfaceGPU"
{
    Properties
    {
        _Skybox("Skybox", Cube) = ""{}
		[PowerSlider(4)] _FresnelExponent ("Fresnel Exponent", Range(0.25, 4)) = 1
        _RefractionIndex ("Refraction Index", Range(0.0, 1.0)) = 1
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog
            #pragma enable_d3d11_debug_symbols 

            #include "UnityCG.cginc"

            struct appdata
            {
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 amplitude1 : TEXCOORD1;
                float4 amplitude2 : TEXCOORD2;
                float4 amplitude3 : TEXCOORD3;
                float4 amplitude4 : TEXCOORD4;
                float4 position : SV_POSITION;
                float3 wavePosition : TEXCOORD5;
            };


            #include "waterSurface.cginc"
            samplerCUBE _Skybox;
            float3 _FresnelColor;
            float _FresnelExponent;
            float _RefractionIndex;

            v2f vert (appdata v)
            {
                v2f o;
                float3 pos = posToGrid(v.uv);
                float2 amplitudePos = gridToAmpl(pos.xz);
                float4 amplitude[NUM] =
                {
                    float4(
                        gridAmplitude(amplitudePos, 0),
                        gridAmplitude(amplitudePos, 1),
                        gridAmplitude(amplitudePos, 2),
                        gridAmplitude(amplitudePos, 3)),
                    
                    float4(
                        gridAmplitude(amplitudePos, 4),
                        gridAmplitude(amplitudePos, 5),
                        gridAmplitude(amplitudePos, 6),
                        gridAmplitude(amplitudePos, 7)),
                    
                    float4(
                        gridAmplitude(amplitudePos, 8),
                        gridAmplitude(amplitudePos, 9),
                        gridAmplitude(amplitudePos, 10),
                        gridAmplitude(amplitudePos, 11)),

                    float4(
                        gridAmplitude(amplitudePos, 12),
                        gridAmplitude(amplitudePos, 13),
                        gridAmplitude(amplitudePos, 14),
                        gridAmplitude(amplitudePos, 15)),

                };
    
                //pos += wavePosition(pos, amplitude);
    
                o.position = UnityObjectToClipPos(pos);
                UNITY_TRANSFER_FOG(o,o.vertex);

                o.amplitude1 = amplitude[0];
                o.amplitude2 = amplitude[1];
                o.amplitude3 = amplitude[2];
                o.amplitude4 = amplitude[3];
                o.wavePosition = pos;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 amplitude[NUM] = { 
                    i.amplitude1,
                    i.amplitude2,
                    i.amplitude3,
                    i.amplitude4};
                float3 normal = UnityObjectToWorldNormal(waveNormal(i.wavePosition, amplitude));
                
                fixed4 fragment = fixed4(0.0f, 0.0f, 0.0f, 1.0f);
                normal= normalize(normal);

                float3 view = normalize(WorldSpaceViewDir(float4(i.wavePosition, 1.0f)));
                float3 reflectionDir = reflect(-view, normal);
                float3 reflection = texCUBE(_Skybox, reflectionDir);
    
                float3 refractionDir = refract(-view, normal, _RefractionIndex);
                float3 refraction = texCUBE(_Skybox, refractionDir);
    
                float fresnel = dot(normal, view);
                
                fresnel = saturate(1 - fresnel);
                fresnel = pow(fresnel, _FresnelExponent);
                fragment.xyz = (1 - fresnel) * refraction + (fresnel) * reflection;
                
                // apply fog
                UNITY_APPLY_FOG(i.fogCoord, fragment);
                return fragment;
            }
            ENDCG
        }
    }
}