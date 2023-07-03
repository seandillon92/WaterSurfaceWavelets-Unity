Shader"Unlit/waterSurface"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        textureData("Texture", 2D) = "white"{}
        _Diffuse("Diffuse", Color) = (1,1,1,1)
        _Ambient("Ambient", Color) = (1,1,1,1)
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
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 amplitude1 : TEXCOORD1;
                float4 amplitude2 : TEXCOORD2;
                float4 amplitude3 : TEXCOORD3;
                float4 amplitude4 : TEXCOORD4;
                float4 vertex : SV_POSITION;
                float3 normal : TEXCOORD5;
                float3 cameraDir : TEXCOORD6;
                float3 lightDir : TEXCOORD7;
                float3 pos : TEXCOORD8;
            };


            #include "waterSurface.cginc"

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Diffuse;
            float4 _Ambient;

            v2f vert (appdata v)
            {
                v2f o;
                float3 pos = v.vertex.xyz;
                pos += wavePosition(v);
    
                o.vertex = UnityObjectToClipPos(pos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
    
                o.amplitude1 = v.amplitude1;
                o.amplitude2 = v.amplitude2;
                o.amplitude3 = v.amplitude3;
                o.amplitude4 = v.amplitude4;
    
                o.lightDir = normalize(light - o.vertex.xyz);
                o.cameraDir = -o.vertex.xyz;
                o.pos = v.vertex.xyz;
                o.normal = UnityObjectToWorldNormal(float3(0, 1, 0));
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = UnityObjectToWorldNormal(waveNormal(i));
                float4 specular = float4(1.0, 1.0, 1.0, 1.0);
                float4 lightColor = float4(1.0, 1.0, 1.0, 1.0);
                float4 diffColor = _Diffuse;
                if (i.pos.x < -50 || i.pos.x > 50 || i.pos.z < -50 || i.pos.z > 50)
                {
                    diffColor.rgb = float3(0.6, 0.6, 0.6);
                }
                
                fixed4 fragment = _Ambient;
                normal= normalize(normal);
                float3 light = normalize(i.lightDir);
                //Diffuse 
                float intensity = max(0.0, dot(normal, light));
                fragment += diffColor * lightColor * intensity;
                
                // apply fog
                //UNITY_APPLY_FOG(i.fogCoord, fragment);
                //fragment.xyz = waveNormal(i);
                //fragment.w = 1.0;
                return fragment;
            }
            ENDCG
        }
    }
}
