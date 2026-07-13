using ComputeSharp;
using ComputeSharp.D2D1;

namespace BetterLyrics.WinUI3.Shaders;

/// <summary>
///     Ported from <see href="https://www.shadertoy.com/view/Mdt3Df" />.
///     Credit/Copyright to the original author.
/// </summary>
/// <param name="time"></param>
/// <param name="dispatchSize"></param>
[D2DInputCount(0)]
[D2DShaderProfile(D2D1ShaderProfile.PixelShader50)]
[D2DGeneratedPixelShaderDescriptor]
[D2DRequiresScenePosition]
public readonly partial struct SnowEffect(float time, float2 dispatchSize, float density, float speed)
    : ID2D1PixelShader
{
    public float4 Execute()
    {
        var xy = (int2)D2D.GetScenePosition().XY;
        float2 fragCoord = new(xy.X, dispatchSize.Y - xy.Y);

        var snow = 0.0f;

        for (var k = 0; k < 6; k++)
        for (var i = 1; i < 12; i++)
        {
            var cellSize = 2.0f + i * 3.0f;
            var downSpeed = 0.3f + (Hlsl.Sin(time * 0.4f + (k + i * 20)) + 1.0f) * 0.00008f * speed;

            var uv = fragCoord / dispatchSize.X +
                     new float2(
                         0.01f * Hlsl.Sin((time + k * 6185) * 0.6f + i) * (5.0f / i),
                         downSpeed * (time + k * 1352) * (1.0f / i)
                     );

            var uvStep = Hlsl.Ceil(uv * cellSize - new float2(0.5f, 0.5f)) / cellSize;

            var x = Hlsl.Frac(Hlsl.Sin(Hlsl.Dot(uvStep, new float2(12.9898f + k * 12.0f, 78.233f + k * 315.156f))) *
                43758.5453f + k * 12.0f) - 0.5f;
            var y = Hlsl.Frac(Hlsl.Sin(Hlsl.Dot(uvStep, new float2(62.2364f + k * 23.0f, 94.674f + k * 95.0f))) *
                62159.8432f + k * 12.0f) - 0.5f;

            var randomMagnitude1 = Hlsl.Sin(time * 2.5f) * 0.7f / cellSize;
            var randomMagnitude2 = Hlsl.Cos(time * 2.5f) * 0.7f / cellSize;

            var d = 5.0f *
                    Hlsl.Distance(
                        uvStep + new float2(x * Hlsl.Sin(y), y) * randomMagnitude1 +
                        new float2(y, x) * randomMagnitude2, uv);

            var omiVal = Hlsl.Frac(Hlsl.Sin(Hlsl.Dot(uvStep, new float2(32.4691f, 94.615f))) * 31572.1684f);

            if (omiVal < density)
            {
                var newd = (x + 1.0f) * 0.4f *
                           Hlsl.Clamp(1.9f - d * (15.0f + x * 6.3f) * (cellSize / 1.4f), 0.0f, 1.0f);
                snow += newd;
            }
        }

        return snow;
    }
}