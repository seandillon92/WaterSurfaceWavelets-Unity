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

            //shader model 6
            //#pragma require WaveBasic	
            //#pragma require WaveVote
            //#pragma require WaveBallot
            //#pragma require WaveMath
            //#pragma require WaveMultiPrefix 

            #include "UnityCG.cginc"
            #include "waterSurface.cginc"

            struct appdata
            {
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                UNITY_FOG_COORDS(1)
                float4 position : SV_POSITION;
                float3 wavePosition : POSITION1;
                float4 amplitude[DIR_NUM/4] : POSITION2;
            };



            samplerCUBE _Skybox;
            float _FresnelExponent;
            float _RefractionIndex;

            v2f vert (appdata v)
            {
                v2f o;
                float3 pos = posToGrid(v.uv);
                float2 amplitudePos = gridToAmpl(pos.xz);

                o.amplitude[0].x = gridAmplitude(amplitudePos, 0);
                o.amplitude[0].y = gridAmplitude(amplitudePos, 1);
                o.amplitude[0].z = gridAmplitude(amplitudePos, 2);
                o.amplitude[0].w = gridAmplitude(amplitudePos, 3);
                o.amplitude[1].x = gridAmplitude(amplitudePos, 4);
                o.amplitude[1].y = gridAmplitude(amplitudePos, 5);
                o.amplitude[1].z = gridAmplitude(amplitudePos, 6);
                o.amplitude[1].w = gridAmplitude(amplitudePos, 7);
                o.amplitude[2].x = gridAmplitude(amplitudePos, 8);
                o.amplitude[2].y = gridAmplitude(amplitudePos, 9);
                o.amplitude[2].z = gridAmplitude(amplitudePos, 10);
                o.amplitude[2].w = gridAmplitude(amplitudePos, 11);
                o.amplitude[3].x = gridAmplitude(amplitudePos, 12);
                o.amplitude[3].y = gridAmplitude(amplitudePos, 13);
                o.amplitude[3].z = gridAmplitude(amplitudePos, 14);
                o.amplitude[3].w = gridAmplitude(amplitudePos, 15);
                
                pos += wavePosition(pos, o.amplitude);
                o.position = UnityObjectToClipPos(pos);
                UNITY_TRANSFER_FOG(o, pos);
                o.wavePosition = pos;
                
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {

                float3 normal = UnityObjectToWorldNormal(waveNormal(i.wavePosition, i.amplitude));
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
