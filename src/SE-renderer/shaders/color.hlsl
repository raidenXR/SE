
cbuffer cbPerObject : register(b0)
{
    float4x4 world;
    float4x4 viewProj;
    float4x4 texTransform;
}

struct VertexIn
{
    float3 PosL   : POSITION;
    float3 Normal : NORMAL;
    float2 TexC   : TEXCOORD;         
};

struct VertexOut
{
    float4 PosW    : SV_POSITION;
    float3 PosH    : POSITION;
    float3 Normal  : NORMAL;
    float2 TexC    : TEXCOORD;
};

VertexOut VS(VertexIn vin)
{
    VertexOut vout;

    // transform to homogenous clip space
    float4 posW = mul(float4(vin.PosL, 1.0f), world);
    vout.PosH = mul(posW, viewProj);

    return vout;
}

float4 PS(VertexOut pin) : SV_Target
{
    return float4(0.5f, 0.5f, 0.5f, 1.0f);
}
