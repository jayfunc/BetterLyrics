namespace BetterLyrics.Avalonia.Shaders;

public class SnowEffect
{
    /// <summary>
    ///     Ported from <see href="https://www.shadertoy.com/view/Mdt3Df" />.
    ///     Credit/Copyright to the original author.
    /// </summary>
    /// <param name="time"></param>
    /// <param name="dispatchSize"></param>
    public const string SkSlShaderCode = @"
        uniform float u_time;
        uniform vec2 u_resolution;
        uniform float u_density;
        uniform float u_speed;

        vec4 main(vec2 fragCoord) {
            // Recreating the HLSL scene position logic matching Win2D's Y-axis inversion.
            vec2 flippedCoord = vec2(fragCoord.x, u_resolution.y - fragCoord.y);

            float snow = 0.0;

            for (int k = 0; k < 6; k++) {
                for (int i = 1; i < 12; i++) {
                    float fk = float(k);
                    float fi = float(i);

                    float cellSize = 2.0 + fi * 3.0;
                    float downSpeed = 0.3 + (sin(u_time * 0.4 + (fk + fi * 20.0)) + 1.0) * 0.00008 * u_speed;

                    vec2 uv = flippedCoord / u_resolution.x +
                              vec2(
                                  0.01 * sin((u_time + fk * 6185.0) * 0.6 + fi) * (5.0 / fi),
                                  downSpeed * (u_time + fk * 1352.0) * (1.0 / fi)
                              );

                    vec2 uvStep = ceil(uv * cellSize - vec2(0.5, 0.5)) / cellSize;

                    float x = fract(sin(dot(uvStep, vec2(12.9898 + fk * 12.0, 78.233 + fk * 315.156))) * 43758.5453 + fk * 12.0) - 0.5;
                    float y = fract(sin(dot(uvStep, vec2(62.2364 + fk * 23.0, 94.674 + fk * 95.0))) * 62159.8432 + fk * 12.0) - 0.5;

                    float randomMagnitude1 = sin(u_time * 2.5) * 0.7 / cellSize;
                    float randomMagnitude2 = cos(u_time * 2.5) * 0.7 / cellSize;

                    float d = 5.0 * distance(
                        uvStep + vec2(x * sin(y), y) * randomMagnitude1 + vec2(y, x) * randomMagnitude2, 
                        uv
                    );

                    float omiVal = fract(sin(dot(uvStep, vec2(32.4691, 94.615))) * 31572.1684);

                    if (omiVal < u_density) {
                        float newd = (x + 1.0) * 0.4 * clamp(1.9 - d * (15.0 + x * 6.3) * (cellSize / 1.4), 0.0, 1.0);
                        snow += newd;
                    }
                }
            }

            // In HLSL, implicitly casting a float to a float4 copies the value to RGBA.
            // This natively behaves as a pre-multiplied white pixel.
            return vec4(snow);
        }
    ";
}