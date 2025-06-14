
Texture2D<float4> Texture : register(t0, space2);
SamplerState Sampler : register(s0, space2);


struct VertexIn
{
	float3 Position : POSITION;
	float3 Normal   : NORMAL;
	float3 Tangent  : TEXCOORD0;
	float2 TexC     : TEXCOORD1;
};

struct VertexOut
{
	float2 Color    : TEXCOORD0;
	float4 Position : SV_Position; 
};

VertexOut VS(VertexIn input)
{
	VertexOut output;
	output.Color = float4(0.5f, 0.4f, 0.3f, 1.0f);;
	output.Position = float4(input.Position, 1.0f);

	return output;
}

float4 PS(float4 Color : TEXCOORD0) : SV_Target0
{
	return Color;
}
