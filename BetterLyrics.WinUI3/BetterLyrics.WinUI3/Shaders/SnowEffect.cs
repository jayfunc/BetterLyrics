using ComputeSharp;
using ComputeSharp.D2D1;
using System;
using System.Collections.Generic;
using System.Text;

namespace BetterLyrics.WinUI3.Shaders
{
    [D2DInputCount(0)]
    [D2DShaderProfile(D2D1ShaderProfile.PixelShader50)]
    [D2DGeneratedPixelShaderDescriptor]
    [D2DRequiresScenePosition]
    public readonly partial struct SnowEffect(float time, Float2 resolution) : ID2D1PixelShader
    {
        public Float4 Execute()
        {
            Float2 fragCoord = D2D.GetScenePosition().XY;

            float snow = 0.0f;

            for (int k = 0; k < 6; k++)
            {
                for (int i = 0; i < 12; i++)
                {
                    float cellSize = 2.0f + ((float)i * 3.0f);
                    float downSpeed = 0.3f + (Hlsl.Sin(time * 0.4f + (float)(k + i * 20)) + 1.0f) * 0.00008f;
                    downSpeed /= 10;
                    downSpeed *= -1;

                    Float2 uv = (fragCoord / resolution.X) +
                                new Float2(
                                    0.01f * Hlsl.Sin((time + (float)(k * 6185)) * 0.6f + (float)i) * (5.0f * i),
                                    downSpeed * (time + (float)(k * 1352)) * i
                                );

                    Float2 uvStep = (Hlsl.Ceil(uv * cellSize - new Float2(0.5f, 0.5f)) / cellSize);

                    float x = Hlsl.Frac(Hlsl.Sin(Hlsl.Dot(uvStep, new Float2(12.9898f + (float)k * 12.0f, 78.233f + (float)k * 315.156f))) * 43758.5453f + (float)k * 12.0f) - 0.5f;
                    float y = Hlsl.Frac(Hlsl.Sin(Hlsl.Dot(uvStep, new Float2(62.2364f + (float)k * 23.0f, 94.674f + (float)k * 95.0f))) * 62159.8432f + (float)k * 12.0f) - 0.5f;

                    float randomMagnitude1 = Hlsl.Sin(time * 2.5f) * 0.7f / cellSize;
                    float randomMagnitude2 = Hlsl.Cos(time * 2.5f) * 0.7f / cellSize;

                    float d = 5.0f * Hlsl.Distance((uvStep + new Float2(x * Hlsl.Sin(y), y) * randomMagnitude1 + new Float2(y, x) * randomMagnitude2), uv);

                    float omiVal = Hlsl.Frac(Hlsl.Sin(Hlsl.Dot(uvStep, new Float2(32.4691f, 94.615f))) * 31572.1684f);

                    if (omiVal < 0.08f)
                    {
                        float newd = (x + 1.0f) * 0.4f * Hlsl.Clamp(1.9f - d * (15.0f + (x * 6.3f)) * (cellSize / 1.4f), 0.0f, 1.0f);
                        snow += newd;
                    }
                }
            }

            return (Float4)snow;
        }
    }
}
