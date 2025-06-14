
cbuffer UniformBlock : register(b0, space1)
{
    float4x4 gWorldViewProj : packoffset(c0);
};

struct VertexIn
{
    float3 PosL   : POSITION;
    float3 NormalL : NORMAL;
    float3 TangentU : TANGENT;
    float2 TexC   : TEXCOORD;         
};

struct VertexOut
{
    float4 PosH    : SV_POSITION;
    float4 Color   : COLOR;
};

VertexOut VS(VertexIn vin)
{
    VertexOut vout;
    vout.PosH = mul(float4(vin.PosL, 1.0f), gWorldViewProj);
    vout.Color = float4(0.9f, 0.1f, 0.9f, 1.0f);

    return vout;
}

float4 PS(VertexOut pin) : SV_Target
{
    return pin.Color;
}

