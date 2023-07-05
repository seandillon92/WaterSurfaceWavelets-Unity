Shader"Unlit/waterSurface"
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
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                
                float4 amplitude1 : TEXCOORD1;
                float4 amplitude2 : TEXCOORD2;
                float4 amplitude3 : TEXCOORD3;
                float4 amplitude4 : TEXCOORD4;
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
                float2 depth : TEXCOORD6;};


            #include "waterSurface.cginc"
            samplerCUBE _Skybox;
            float4 _FoamColor;
            float3 _FresnelColor;
            float _FresnelExponent;
            float _RefractionIndex;

            v2f vert (appdata v)
            {
                v2f o;
                float3 pos = v.vertex.xyz;
                float4 amplitude[NUM] =
                {
                    v.amplitude1,
                    v.amplitude2,
                    v.amplitude3,
                    v.amplitude4
                };
    
                pos += wavePosition(pos, amplitude);
    
                o.position = UnityObjectToClipPos(pos);
                UNITY_TRANSFER_FOG(o,o.vertex);
    
                o.amplitude1 = v.amplitude1;
                o.amplitude2 = v.amplitude2;
                o.amplitude3 = v.amplitude3;
                o.amplitude4 = v.amplitude4;
                o.wavePosition = pos;
                COMPUTE_EYEDEPTH(o.depth);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 amplitude[NUM] = { 
                    i.amplitude1,
                    i.amplitude2,
                    i.amplitude3,
                    i.amplitude4};
    
                float depth = LinearEyeDepth(i.depth);
                float3 normal = UnityObjectToWorldNormal(waveNormal(i.wavePosition, amplitude, depth));
                
                fixed4 fragment;
                normal= normalize(normal);

                float3 view = normalize(WorldSpaceViewDir(float4(i.wavePosition, 1.0f)));
                float3 reflectionDir = reflect(-view, normal);
                float3 reflection = texCUBE(_Skybox, reflectionDir);
    
                float3 refractionDir = refract(-view, normal, _RefractionIndex);
                float3 refraction = texCUBE(_Skybox, refractionDir);
    
                float fresnel = dot(normal, view);
                
                fresnel = saturate(1 - fresnel);
			    //raise the fresnel value to the exponents power to be able to adjust it
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
