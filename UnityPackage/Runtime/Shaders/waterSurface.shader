Shader"Unlit/WaterWaveSurfaces/waterSurface"
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
                float4 position : SV_POSITION;
                float3 wavePosition : POSITION1;
                float2 amplitudePosition : POSITION2;
            };


            #include "waterSurface.cginc"
            samplerCUBE _Skybox;
            float _FresnelExponent;
            float _RefractionIndex;

            v2f vert (appdata v)
            {
                v2f o;
                float3 pos = posToGrid(v.uv);
                float2 amplitudePos = gridToAmpl(pos.xz);
                //pos += wavePosition(pos, amplitude);
    
                o.position = UnityObjectToClipPos(pos);
                UNITY_TRANSFER_FOG(o,o.vertex);

                o.wavePosition = pos;
                o.amplitudePosition = amplitudePos;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = UnityObjectToWorldNormal(waveNormal(i.wavePosition, i.amplitudePosition));
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
