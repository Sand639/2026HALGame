Shader "Custom/WorldBorderURP"
{
    Properties
    {
        _BaseColor ("Base Color", Color) = (0.2, 0.7, 1.0, 0.25)
        _EdgeColor ("Edge Color", Color) = (0.5, 0.9, 1.0, 1.0)
        _EmissionStrength ("Emission Strength", Range(0, 10)) = 2.5

        _StripeScale ("Stripe Scale", Float) = 8.0
        _StripeSpeed ("Stripe Speed", Float) = 1.5

        _WaveAmplitude ("Wave Amplitude", Range(0, 0.5)) = 0.05
        _WaveFrequency ("Wave Frequency", Float) = 4.0
        _WaveSpeed ("Wave Speed", Float) = 1.5

        _FresnelPower ("Fresnel Power", Range(0.1, 8.0)) = 3.0
        _AlphaMultiplier ("Alpha Multiplier", Range(0, 1)) = 1.0
    }

    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent"
            "RenderPipeline"="UniversalPipeline"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode"="UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Back

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS  : TEXCOORD0;
                float3 normalWS    : TEXCOORD1;
                float2 uv          : TEXCOORD2;
                float3 viewDirWS   : TEXCOORD3;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _EdgeColor;
                float _EmissionStrength;
                float _StripeScale;
                float _StripeSpeed;
                float _WaveAmplitude;
                float _WaveFrequency;
                float _WaveSpeed;
                float _FresnelPower;
                float _AlphaMultiplier;
            CBUFFER_END

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float3 posOS = IN.positionOS.xyz;

                // ちょっとだけ頂点を横に揺らす
                float wave =
                    sin(posOS.y * _WaveFrequency + _Time.y * _WaveSpeed) *
                    _WaveAmplitude;

                posOS.x += wave;

                VertexPositionInputs posInputs = GetVertexPositionInputs(float4(posOS, 1.0));
                VertexNormalInputs normalInputs = GetVertexNormalInputs(IN.normalOS);

                OUT.positionHCS = posInputs.positionCS;
                OUT.positionWS = posInputs.positionWS;
                OUT.normalWS = normalize(normalInputs.normalWS);
                OUT.uv = IN.uv;
                OUT.viewDirWS = GetWorldSpaceViewDir(posInputs.positionWS);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float3 normalWS = normalize(IN.normalWS);
                float3 viewDirWS = normalize(IN.viewDirWS);

                float fresnel = pow(1.0 - saturate(dot(normalWS, viewDirWS)), _FresnelPower);

                float3 absNormal = abs(normalWS);

                float horizontalCoord;

                // X向きの面ならZを使う
                if (absNormal.x > absNormal.z)
                {
                    horizontalCoord = IN.positionWS.z;

                    // -X面だけ反転
                    if (normalWS.x < 0.0)
                        horizontalCoord = -horizontalCoord;
                }
                else
                {
                    // Z向きの面ならXを使う
                    horizontalCoord = IN.positionWS.x;

                    // +Z面だけ反転
                    if (normalWS.z > 0.0)
                        horizontalCoord = -horizontalCoord;
                }

                float verticalCoord = IN.positionWS.y;
                float stripeCoord = horizontalCoord + verticalCoord * 0.35;

                float stripe =
                    sin((stripeCoord * _StripeScale + _Time.y * _StripeSpeed) * 6.28318) * 0.5 + 0.5;

                float ripple =
                    sin((IN.positionWS.y + horizontalCoord) * 3.0 + _Time.y * 2.0) * 0.5 + 0.5;

                float pattern = saturate(stripe * 0.7 + ripple * 0.3);

                float3 baseCol = _BaseColor.rgb;
                float3 edgeCol = _EdgeColor.rgb;

                float3 finalColor = lerp(baseCol, edgeCol, fresnel);
                finalColor += edgeCol * pattern * _EmissionStrength * 0.35;
                finalColor += edgeCol * fresnel * _EmissionStrength;

                float alpha = _BaseColor.a;
                alpha += fresnel * 0.35;
                alpha += pattern * 0.15;
                alpha *= _AlphaMultiplier;
                alpha = saturate(alpha);

                return half4(finalColor, alpha);
            }
            ENDHLSL
        }
    }
}