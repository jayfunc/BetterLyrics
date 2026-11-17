using ComputeSharp;
using ComputeSharp.D2D1;

namespace BetterLyrics.WinUI3.Shaders;

[D2DInputCount(1)]
[D2DInputSimple(0)]
[D2DShaderProfile(D2D1ShaderProfile.PixelShader50)]
[D2DGeneratedPixelShaderDescriptor]
internal readonly partial struct ScRgbOutputEffect(float sdrWhiteScale) : ID2D1PixelShader
{
    private static float ToLinear(float channel)
    {
        return channel <= 0.04045f
            ? channel / 12.92f
            : Hlsl.Pow(Hlsl.Abs((channel + 0.055f) / 1.055f), 2.4f);
    }

    public float4 Execute()
    {
        var pixel = D2D.GetInput(0);
        var alpha = pixel.W;
        if (alpha <= 0) return Float4.Zero;

        // Decode the unassociated sRGB color, then premultiply in linear light for composition.
        var scale = sdrWhiteScale * alpha;
        return new Float4(
            ToLinear(pixel.X / alpha) * scale,
            ToLinear(pixel.Y / alpha) * scale,
            ToLinear(pixel.Z / alpha) * scale,
            alpha);
    }
}
