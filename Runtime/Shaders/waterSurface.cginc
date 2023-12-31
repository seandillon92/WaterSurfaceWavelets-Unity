#pragma enable_d3d11_debug_symbols 
#define DIR_NUM 16

#define NUM DIR_NUM / 4
#define NUM_INTEGRATION_NODES 8 * DIR_NUM

#define TAU 6.28318530718
#define D_THETA TAU / (DIR_NUM)
#define DA 1.0 / (NUM_INTEGRATION_NODES)
#define DX (DIR_NUM) * TAU / (NUM_INTEGRATION_NODES)

#define SEED 40234324

uniform sampler2D textureData;
uniform float profilePeriod;
uniform float scale;

uniform float waterLevel;
uniform float3 cameraPos;
uniform float3 cameraProjectionForward;
uniform float4x4 cameraInverseProjection;

uniform sampler3D amplitude;

uniform SamplerState linear_clamp_sampler;
uniform SamplerState linear_repeat_sampler;
uniform SamplerState point_repeat_sampler;

uniform float2 xmin;
uniform float2 dx;
uniform float4x4 env_trans;
uniform float4x4 env_trans_inv;
uniform float2 env_size;
uniform uint2 nx;
uniform uint direction;
uniform float amp_mult;
uniform bool renderOutsideBorders;
uniform float env_rotation;

uniform float4x4 boat_trans;
uniform float3 boat_size;
uniform sampler2D boat;


float3 mulPoint(float4x4 m, float3 p)
{
    float4 p4 = mul(m, float4(p, 1));
    return p4.xyz / p4.w;
}

float2 gridToAmpl(float3 pos)
{
    float2 newPos = mul(env_trans_inv, float4(pos, 1)).xz; // account for terrain transform
    newPos = (newPos - xmin) * dx - float2(0.5, 0.5); // transfer to simulation space
    return newPos;
}



float posModuloItheta(float itheta)
{
    return (itheta % DIR_NUM + DIR_NUM) % DIR_NUM;
}

float getItheta(uint index)
{
    float itheta = index + (env_rotation) / D_THETA;
    return posModuloItheta(itheta);
}

bool isBoat(float3 pos)
{
    float3 local = mul(boat_trans, float4(pos, 1));
    float2 coords = float2(0.5 - local.z / boat_size.z, 0.5 -  local.x / boat_size.x);
    
    float4 pixel = tex2Dlod(boat, float4(coords, 0, 0));
    return pixel.x < 0.0f;
}

float gridAmplitude(float2 pos, float itheta)
{
    float3 samplingPos = float3((pos + float2(1.5, 1.5)) / (nx + uint2(2, 2)), (itheta + 0.5) / 16.0f);
    
    return tex3Dlod(amplitude, float4(samplingPos, 0)).x * amp_mult;    
    return 0.0f;
}

float3 posToGrid(float2 pos)
{
    float3 p = float3(pos.xy, 0) + cameraProjectionForward;
    p  = mulPoint(cameraInverseProjection, p);
    float3 dir = normalize(p - cameraPos);
    float camY = cameraPos.y - waterLevel;
    float t = -camY / dir.y;
    
    t = t < 0 || isnan(t) ? 1000 : t;
    p = cameraPos + t * dir;
    p.y = waterLevel;
    
    if (!renderOutsideBorders)
    {
        float2 orthoP = mul(env_trans_inv, float4(p, 1)).xz;
        orthoP = clamp(orthoP,-env_size, env_size);
        p.xz = mul(env_trans, float4(orthoP.x, 0, orthoP.y, 1)).xz;
    }

    return p;
}

float iAmpl(float angle, float amplitude[DIR_NUM]) {
    float a = DIR_NUM * angle / TAU + DIR_NUM - 0.5;
    uint ia = uint(floor(a));
    float w = a - ia;
    return (1 - w) * amplitude[ia % DIR_NUM] + w * amplitude[(ia + uint(1)) % DIR_NUM];
}

float3 wavePosition(float3 pos, float amplitude[DIR_NUM]) {
    
    float3 result = float3(0.0, 0.0, 0.0);
    for (int i = 0; i < NUM_INTEGRATION_NODES; i++)
    {
        float a = i * DA;
        float angle = a * TAU;
        float2 kdir = float2(cos(angle), sin(angle));
        float kdir_x = dot(pos.xz, kdir) + TAU * sin(SEED * a);
        float w = scale * kdir_x / profilePeriod;

        float4 tt = DX * iAmpl(angle, amplitude) * tex2Dlod(textureData, float4(w, 0, 0, 0));

        result.xz += kdir * tt.x;
        result.y += tt.y;
    }
    
    return result;
}

float3 waveNormal(float3 pos, float amplitude[DIR_NUM]) {
    
    float3 tx = float3(1.0, 0.0, 0.0);
    float3 ty = float3(0.0, 1.0, 0.0);

    for (int i = 0; i < NUM_INTEGRATION_NODES; i++)
    {
        float a = i * DA;
        float angle = a * TAU;
        float2 kdir = float2(cos(angle), sin(angle));
        float kdir_x = dot(pos.xz, kdir) + TAU * sin(a * SEED);
        float w = scale * kdir_x / profilePeriod;
        float4 tt = DX * iAmpl(angle, amplitude) * tex2Dlod(textureData, float4(w, 0, 0, 0));

        tx.xz += kdir.x * tt.zw;
        ty.yz += kdir.y * tt.zw;
    }

    return normalize(cross(tx, ty)).xzy;
    return float3(0, 0, 0);
}
