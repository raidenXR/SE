
Texture2D<float4> Texture : register(t0, space2);
SamplerState Sampler : register(s0, space2);


struct VertexIn
{
	float3 Position : TEXCOORD0;
	float3 TexC     : TEXCOORD1;
};

struct VertexOut
{
	float2 TexC     : TEXCOORD0;
	float4 Position : SV_Position; 
};

VertexOut VS(VertexIn input)
{
	VertexOut output;
	output.TexC = input.TexC;
	output.Position = float4(input.Position, 1.0f);

	return output;
}

float4 PS(float2 TexC : TEXCOORD0) : SV_Target0
{
	return Texture.Sample(Sampler, TexC);
}
