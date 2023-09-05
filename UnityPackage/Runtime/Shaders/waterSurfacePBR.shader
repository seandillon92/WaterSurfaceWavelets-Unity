Shader "WaterWaveSurfaces/waterSurfacePBR"
{
    Properties
    {
        
		[PowerSlider(4)] _FresnelExponent ("Fresnel Exponent", Range(0.25, 4)) = 1
        _RefractionIndex ("Refraction Index", Range(0.0, 1.0)) = 1
        _Color("Color", Color) = (.25, .5, .5, 1)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows vertex:vert
        // make fog work
        #pragma multi_compile_fog
        #pragma enable_d3d11_debug_symbols
        #pragma target 4.0

        //shader model 6
        //#pragma require WaveBasic	
        //#pragma require WaveVote
        //#pragma require WaveBallot
        //#pragma require WaveMath
        //#pragma require WaveMultiPrefix 

        #include "UnityCG.cginc"
        #include "waterSurface.cginc"

        struct Input
        {
            float3 wavePosition;
            float amplitude0;
            float amplitude1;
            float amplitude2;
            float amplitude3;
            float amplitude4;
            float amplitude5;
            float amplitude6;
            float amplitude7;
            float amplitude8;
            float amplitude9;
            float amplitude10;
            float amplitude11;
            float amplitude12;
            float amplitude13;
            float amplitude14;
            float amplitude15;
        };



        uniform samplerCUBE _Skybox;
        uniform float _FresnelExponent;
        uniform float _RefractionIndex;
        uniform float4 _Color;
        uniform float _Scale;

        uniform half _Glossiness;
        uniform half _Metallic;

        void vert(inout appdata_full v, out Input o)
        {
    
            float3 pos = posToGrid(v.vertex.xy);
            float2 amplitudePos = gridToAmpl(pos);
                
            o.amplitude0 = gridAmplitude(amplitudePos, getItheta(0));
            o.amplitude1 = gridAmplitude(amplitudePos, getItheta(1));
            o.amplitude2 = gridAmplitude(amplitudePos, getItheta(2));
            o.amplitude3 = gridAmplitude(amplitudePos, getItheta(3));
            o.amplitude4 = gridAmplitude(amplitudePos, getItheta(4));
            o.amplitude5 = gridAmplitude(amplitudePos, getItheta(5));
            o.amplitude6 = gridAmplitude(amplitudePos, getItheta(6));
            o.amplitude7 = gridAmplitude(amplitudePos, getItheta(7));
            o.amplitude8 = gridAmplitude(amplitudePos, getItheta(8));
            o.amplitude9 = gridAmplitude(amplitudePos, getItheta(9));
            o.amplitude10 = gridAmplitude(amplitudePos, getItheta(10));
            o.amplitude11 = gridAmplitude(amplitudePos, getItheta(11));
            o.amplitude12 = gridAmplitude(amplitudePos, getItheta(12));
            o.amplitude13 = gridAmplitude(amplitudePos, getItheta(13));
            o.amplitude14 = gridAmplitude(amplitudePos, getItheta(14));
            o.amplitude15 = gridAmplitude(amplitudePos, getItheta(15));
                
            o.wavePosition = pos;
    
            float amp[16] =
            {
                o.amplitude0,
                o.amplitude1,
                o.amplitude2,
                o.amplitude3,
                o.amplitude4,
                o.amplitude5,
                o.amplitude6,
                o.amplitude7,
                o.amplitude8,
                o.amplitude9,
                o.amplitude10,
                o.amplitude11,
                o.amplitude12,
                o.amplitude13,
                o.amplitude14,
                o.amplitude15
            };
            pos += wavePosition(pos, amp);
            v.vertex.xyz = pos;
        }

        void surf(in Input i, inout SurfaceOutputStandard o)
        {
    
            float amp[16] =
            {
                i.amplitude0,
                i.amplitude1,
                i.amplitude2,
                i.amplitude3,
                i.amplitude4,
                i.amplitude5,
                i.amplitude6,
                i.amplitude7,
                i.amplitude8,
                i.amplitude9,
                i.amplitude10,
                i.amplitude11,
                i.amplitude12,
                i.amplitude13,
                i.amplitude14,
                i.amplitude15
            };
            float3 normal = UnityObjectToWorldNormal(waveNormal(i.wavePosition, amp));
            normal = normalize(normal);

            float3 view = normalize(WorldSpaceViewDir(float4(i.wavePosition, 1.0f)));
            
            float fresnel = dot(normal, view);

            fresnel = saturate(1 - fresnel);
            fresnel = pow(fresnel, _FresnelExponent);

            o.Normal = normal;
            o.Albedo = _Color;
            o.Metallic = 0;
            o.Smoothness = fresnel;
}
        ENDCG
        
    }
    Fallback "Diffuse"
}
