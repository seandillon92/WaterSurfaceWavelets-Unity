#pragma enable_d3d11_debug_symbols 
#define DIR_NUM 16

static const int NUM = DIR_NUM / 4;
static const int NUM_INTEGRATION_NODES = 8 * DIR_NUM;

uniform Texture2D textureData;
uniform float profilePeriod;

uniform float waterLevel;
uniform float3 cameraPos;
uniform float3 cameraProjectionForward;
uniform float4x4 cameraInverseProjection;

uniform Texture3D amplitude;

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

static const float tau = 6.28318530718;
static const float d_theta = tau / DIR_NUM;

float posModuloItheta(float itheta)
{
    return (itheta % DIR_NUM + DIR_NUM) % DIR_NUM;
}

float getItheta(uint index)
{
    float itheta = index + (env_rotation) / d_theta;
    return posModuloItheta(itheta);
}

float gridAmplitude(float2 pos, float itheta)
{
    float3 samplingPos = float3((pos + float2(1.5, 1.5)) / (nx + uint2(2, 2)), (itheta + 0.5) / 16.0f);
    return amplitude.SampleLevel(linear_clamp_sampler, samplingPos, 0).x * amp_mult;
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

float iAmpl(float angle/*in [0,2pi]*/, float amplitude[DIR_NUM]) {
    float a = DIR_NUM * angle / tau + DIR_NUM - 0.5;
    uint ia = uint(floor(a));
    float w = a - ia;
    return (1 - w) * amplitude[ia % DIR_NUM]+ w * amplitude[(ia + 1) % DIR_NUM];
}

static const int seed = 40234324;

float3 wavePosition(float3 pos, float amplitude[DIR_NUM]) {
    float3 result = float3(0.0, 0.0, 0.0);

    const int N = NUM_INTEGRATION_NODES;
    float da = 1.0 / N;
    float dx = DIR_NUM * tau / N;
    for (float a = 0; a < 1; a += da) {

        float angle = a * tau;
        float2 kdir = float2(cos(angle), sin(angle));
        float kdir_x = dot(pos.xz, kdir) + tau * sin(seed * a);
        float w = kdir_x / profilePeriod;

        float4 tt = dx * iAmpl(angle, amplitude) * textureData.SampleLevel(point_repeat_sampler, float2(w, 0), 0);

        result.xz += kdir * tt.x;
        result.y += tt.y;
    }
    return result;
}

float3 waveNormal(float3 pos, float amplitude[DIR_NUM]) {

    float3 tx = float3(1.0, 0.0, 0.0);
    float3 ty = float3(0.0, 1.0, 0.0);

    const float N = NUM_INTEGRATION_NODES;
    float da = 1.0 / N;
    float dx = DIR_NUM * tau / N;
    for (float a = 0; a < 1; a += da) {

        float angle = a * tau;
        float2 kdir = float2(cos(angle), sin(angle));
        float kdir_x = dot(pos.xz, kdir) + tau * sin(a * seed);
        float w = kdir_x / profilePeriod;
        float4 tt = dx * iAmpl(angle, amplitude) * textureData.Sample(linear_repeat_sampler, float2(w, 0));

        tx.xz += kdir.x * tt.zw;
        ty.yz += kdir.y * tt.zw;
    }

    return normalize(cross(tx, ty)).xzy;
}
