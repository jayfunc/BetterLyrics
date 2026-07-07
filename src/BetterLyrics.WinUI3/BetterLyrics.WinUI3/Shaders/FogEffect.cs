using ComputeSharp;
using ComputeSharp.D2D1;

namespace BetterLyrics.WinUI3.Shaders;

/// <summary>
///     Ported from <see href="https://www.shadertoy.com/view/lllSR2" />.
///     Credit/Copyright to the original author.
/// </summary>
/// <param name="time"></param>
/// <param name="dispatchSize"></param>
[D2DInputCount(0)]
[D2DShaderProfile(D2D1ShaderProfile.PixelShader50)]
[D2DGeneratedPixelShaderDescriptor]
[D2DRequiresScenePosition]
public readonly partial struct FogEffect(float time, float2 dispatchSize) : ID2D1PixelShader
{
    private static readonly float2x2 _noiseMatrix = new(1.6f, -1.2f, 1.2f, 1.6f);

    private static float Hash(float2 p)
    {
        var h = Hlsl.Dot(p, new Float2(127.1f, 311.7f));
        return Hlsl.Frac(Hlsl.Sin(h) * 458.325421f) * 2.0f - 1.0f;
    }

    private static float Noise(float2 p)
    {
        var i = Hlsl.Floor(p);
        var f = Hlsl.Frac(p);

        f = f * f * (3.0f - 2.0f * f);

        return Hlsl.Lerp(
            Hlsl.Lerp(Hash(i + new Float2(0.0f, 0.0f)), Hash(i + new Float2(1.0f, 0.0f)), f.X),
            Hlsl.Lerp(Hash(i + new Float2(0.0f, 1.0f)), Hash(i + new Float2(1.0f, 1.0f)), f.X),
            f.Y
        );
    }

    private static float2 Rot(float2 p, float a)
    {
        return new Float2(
            p.X * Hlsl.Cos(a) - p.Y * Hlsl.Sin(a),
            p.X * Hlsl.Sin(a) + p.Y * Hlsl.Cos(a)
        );
    }

    private static float GenNoise(float2 p, float time)
    {
        var d = 0.5f;

        var color = 0.0f;
        for (var i = 0; i < 2; i++)
        {
            color += d * Noise(p * 5.0f + time);

            p = Hlsl.Mul(p, _noiseMatrix);
            d /= 2.0f;
        }

        return color;
    }

    public float4 Execute()
    {
        var totalColor = Float4.Zero;

        for (var count = 0; count < 2; count++)
        {
            var scenePos = D2D.GetScenePosition().XY;

            var fragCoord = new Float2(scenePos.X, dispatchSize.Y - scenePos.Y);

            var uv = -1.0f + 2.0f * (fragCoord / dispatchSize);
            uv *= 1.4f;

            uv.X += Hash(uv + time + count) / 512.0f;
            uv.Y += Hash(new Float2(uv.Y, uv.X) + time + count) / 512.0f;

            var aspectRatio = dispatchSize.X / dispatchSize.Y;

            var dir = Hlsl.Normalize(new Float3(
                uv * new Float2(aspectRatio, 1.0f),
                1.0f + Hlsl.Sin(time) * 0.01f
            ));

            dir.XZ = Rot(dir.XZ, Hlsl.Radians(70.0f));
            dir.XY = Rot(dir.XY, Hlsl.Radians(90.0f));

            var noiseVal = GenNoise(dir.XZ, time) * 0.5f;
            noiseVal *= 1.0f - uv.Y * 0.5f;
            noiseVal *= 0.05f;

            var fogAlpha = Hlsl.Pow(Hlsl.Max(0.0f, noiseVal), 0.717f);

            // 雾的颜色
            var fogColor = new Float3(1.0f, 1.0f, 1.0f);

            var premultipliedColor = fogColor * fogAlpha;

            totalColor += new Float4(
                premultipliedColor.X,
                premultipliedColor.Y,
                premultipliedColor.Z,
                Hlsl.Saturate(fogAlpha)
            );
        }

        // 对应 GLSL 'fragColor /= vec4(2.0);'
        return totalColor / 2.0f;
    }
}