#pragma enable_d3d11_debug_symbols 
#define DIR_NUM 16

static const int NUM = DIR_NUM / 4;
static const int NUM_INTEGRATION_NODES = 8 * DIR_NUM;

uniform sampler2D textureData;
uniform float profilePeriod;
uniform float3 light;

// Define getAmpl for vertex shader
float Ampl(int i, float4 amplitude[NUM]) {
    i = i % DIR_NUM;
    
    return amplitude[i/4][i % 4];
}

static const float tau = 6.28318530718;

float iAmpl(float angle/*in [0,2pi]*/, float4 amplitude[NUM]) {
    float a = DIR_NUM * angle / tau + DIR_NUM - 0.5;
    int ia = int(floor(a));
    float w = a - ia;
    return (1 - w) * Ampl(ia % DIR_NUM, amplitude) + w * Ampl((ia + 1) % DIR_NUM, amplitude);
}

static const int seed = 40234324;

float3 wavePosition(appdata data) {

    float3 p = data.vertex.xyz;
    float3 result = float3(0.0, 0.0, 0.0);
    float4 amplitude[NUM] = { 
        data.amplitude1, 
        data.amplitude2, 
        data.amplitude3, 
        data.amplitude4
    };

    const int N = NUM_INTEGRATION_NODES;
    float da = 1.0 / N;
    float dx = DIR_NUM * tau / N;
    for (float a = 0; a < 1; a += da) {

        float angle = a * tau;
        float2 kdir = float2(cos(angle), sin(angle));
        float kdir_x = dot(p.xz, kdir) + tau * sin(seed * a);
        float w = kdir_x / profilePeriod;

        float4 tt = dx * iAmpl(angle, amplitude) * tex2Dlod(textureData, float4(w, 0, 0, 0));

        result.xz += kdir * tt.x;
        result.y += tt.y;
    }
    return result;
}

float3 waveNormal(v2f data) {
    
    float3 p = data.pos;

    float3 tx = float3(1.0, 0.0, 0.0);
    float3 ty = float3(0.0, 1.0, 0.0);
    
    float4 amplitude[NUM] =
    {
        data.amplitude1,
        data.amplitude2,
        data.amplitude3,
        data.amplitude4
    };

    const int N = NUM_INTEGRATION_NODES;
    float da = 1.0 / N;
    float dx = DIR_NUM * tau / N;
    for (float a = 0; a < 1; a += da) {

        float angle = a * tau;
        float2 kdir = float2(cos(angle), sin(angle));
        float kdir_x = dot(p.xz, kdir) + tau * sin(seed * a);
        float w = kdir_x / profilePeriod;

        float4 tt = dx * iAmpl(angle, amplitude) * tex2Dlod(textureData, float4(w, 0, 0, 0));

        tx.xz += kdir.x * tt.zw;
        ty.yz += kdir.y * tt.zw;
    }

    return normalize(cross(tx, ty)).xzy;
}
