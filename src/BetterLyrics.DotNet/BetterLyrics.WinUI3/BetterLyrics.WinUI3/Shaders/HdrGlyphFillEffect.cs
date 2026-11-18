using ComputeSharp;
using ComputeSharp.D2D1;

namespace BetterLyrics.WinUI3.Shaders;

[D2DInputCount(3)]
[D2DInputSimple(0)]
[D2DInputSimple(1)]
[D2DInputSimple(2)]
[D2DShaderProfile(D2D1ShaderProfile.PixelShader50)]
[D2DGeneratedPixelShaderDescriptor]
internal readonly partial struct HdrGlyphFillEffect(float frontBoost, float glowBoost) : ID2D1PixelShader
{
    private static float BoostChannel(float channel, float gain)
    {
        var linear = channel <= 0.04045f
            ? channel / 12.92f
            : Hlsl.Pow(Hlsl.Abs((channel + 0.055f) / 1.055f), 2.4f);
        linear *= gain;
        return linear <= 0.0031308f ? linear * 12.92f :
            1.055f * Hlsl.Pow(Hlsl.Abs(linear), 1 / 2.4f) - 0.055f;
    }

    public float4 Execute()
    {
        var fill = D2D.GetInput(0);
        var gain = 1 + Hlsl.Max(D2D.GetInput(1).W * frontBoost, D2D.GetInput(2).W * glowBoost);
        if (fill.W <= 0 || gain <= 1) return fill;

        // Keep the antialiased coverage and animated opacity of the original fill.
        return new Float4(
            BoostChannel(fill.X / fill.W, gain) * fill.W,
            BoostChannel(fill.Y / fill.W, gain) * fill.W,
            BoostChannel(fill.Z / fill.W, gain) * fill.W,
            fill.W);
    }
}
