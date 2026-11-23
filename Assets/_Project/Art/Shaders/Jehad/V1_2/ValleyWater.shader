Shader "Echoheart/World/Valley Water V1.2"
{
    Properties
    {
        _DeepColor ("Water", Color) = (0.025, 0.38, 0.54, 1)
        _ShallowColor ("Shallows", Color) = (0.08, 0.7, 0.73, 1)
        _FoamColor ("Foam", Color) = (0.82, 0.98, 0.95, 1)
        _Speed ("Flow Speed", Float) = 0.65
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "RenderType"="Opaque" "Queue"="Geometry+10" }
        Pass
        {
            Name "WaterForward"
            Tags { "LightMode"="UniversalForward" }
            Cull Back
            ZWrite On
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _DeepColor;
                half4 _ShallowColor;
                half4 _FoamColor;
                float _Speed;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; half3 normalWS : TEXCOORD1; float2 uv : TEXCOORD2; half fog : TEXCOORD3; };
            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs p = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = p.positionCS;
                output.positionWS = p.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                output.fog = ComputeFogFactor(p.positionCS.z);
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                float2 p = input.positionWS.xz;
                float t = _Time.y * _Speed;
                half waves = sin(p.x * 0.63 + t) * sin(p.y * 0.82 - t * 0.7);
                half shore = saturate(input.uv.x);
                half3 normal = normalize(input.normalWS + half3(sin(p.x + t) * 0.07, 0, cos(p.y - t) * 0.07));
                Light sun = GetMainLight();
                half3 view = GetWorldSpaceNormalizeViewDir(input.positionWS);
                half specular = pow(saturate(dot(normal, normalize(sun.direction + view))), 80) * 0.5;
                half3 color = lerp(_DeepColor.rgb, _ShallowColor.rgb, saturate(shore * 0.8 + waves * 0.08));
                half foam = smoothstep(0.82, 0.99, shore) * 0.85 + step(0.965, waves) * 0.22;
                color = lerp(color, _FoamColor.rgb, saturate(foam));
                color *= 0.8 + 0.2 * saturate(dot(normal, sun.direction));
                color += sun.color * specular;
                return half4(MixFog(color, input.fog), 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
