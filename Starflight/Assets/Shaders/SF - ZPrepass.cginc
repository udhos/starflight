
#ifndef SF_SHADER_ZPREPASS
#define SF_SHADER_ZPREPASS

#include "SF - Core.cginc"

SF_VertexShaderOutput vertZPrepass_SF( SF_VertexShaderInput v )
{
	return ComputeVertexShaderOutput( v );
}

float4 fragZPrepass_SF( SF_VertexShaderOutput i ) : SV_Target
{
	return 1;
}

#endif
