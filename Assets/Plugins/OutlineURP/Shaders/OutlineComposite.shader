Shader "Hidden/OutlineURP/Composite"
{
    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }

        Pass
        {
            Name "OutlineComposite"

            Cull Off
            ZWrite Off
            ZTest Always
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D_X(_BlitTexture);

            float _OutlineThickness;
            float _OutlineDebugShowMask;
            float _OutlineDebugForceFullscreen;
            float4 _OutlineDebugForceColor;
            float4 _BlitTexture_TexelSize;

            struct Attributes
            {
                uint vertexID : SV_VertexID;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = GetFullScreenTriangleVertexPosition(input.vertexID);
                output.uv = GetFullScreenTriangleTexCoord(input.vertexID);
                return output;
            }

            half4 SampleMask(float2 uv)
            {
                return SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_PointClamp, uv);
            }

            half MaskPresence(half4 sample)
            {
                return sample.a;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                if (_OutlineDebugShowMask > 0.5f)
                {
                    half4 sample = SampleMask(input.uv);
                    return half4(sample.aaa, 1.0h);
                }

                if (_OutlineDebugForceFullscreen > 0.5f)
                {
                    return _OutlineDebugForceColor;
                }

                half centerPresence = MaskPresence(SampleMask(input.uv));
                if (centerPresence > 0.5h)
                {
                    return half4(0, 0, 0, 0);
                }

                float2 baseTexel = abs(_BlitTexture_TexelSize.xy);
                if (baseTexel.x <= 0.0 || baseTexel.y <= 0.0)
                {
                    baseTexel = 1.0 / max(_ScreenParams.xy, float2(1.0, 1.0));
                }
                const float2 directions[8] =
                {
                    float2(1, 0),
                    float2(-1, 0),
                    float2(0, 1),
                    float2(0, -1),
                    float2(1, 1),
                    float2(-1, 1),
                    float2(1, -1),
                    float2(-1, -1)
                };

                int radius = clamp((int)ceil(_OutlineThickness), 1, 8);
                half4 neighbor = half4(0, 0, 0, 0);
                half neighborPresence = 0.0h;
                [loop]
                for (int r = 1; r <= radius; r++)
                {
                    float2 stepTexel = baseTexel * r;
                    [unroll]
                    for (int i = 0; i < 8; i++)
                    {
                        float2 uv = saturate(input.uv + directions[i] * stepTexel);
                        half4 sample = SampleMask(uv);
                        half presence = MaskPresence(sample);
                        if (presence > neighborPresence)
                        {
                            neighbor = sample;
                            neighborPresence = presence;
                        }
                    }
                }

                if (neighborPresence <= 0.5h)
                {
                    return half4(0, 0, 0, 0);
                }

                return half4(neighbor.rgb, 1.0h);
            }
            ENDHLSL
        }
    }
}
