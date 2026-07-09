namespace BetterLyrics.Avalonia.Shaders;

public class FogEffect
{
    /// <summary>
    ///     Ported from <see href="https://www.shadertoy.com/view/lllSR2" />.
    ///     Credit/Copyright to the original author.
    /// </summary>
    public const string SkSlShaderCode = @"
        uniform float u_time;
        uniform vec2 u_resolution;

        // In SkSL, mat2 takes arguments column-by-column. 
        // This is transposed from HLSL's row-major constructor to ensure identical math to mul(p, matrix)
        const mat2 noiseMatrix = mat2(1.6, 1.2, -1.2, 1.6); 

        float Hash(vec2 p) {
            float h = dot(p, vec2(127.1, 311.7));
            return fract(sin(h) * 458.325421) * 2.0 - 1.0;
        }

        float Noise(vec2 p) {
            vec2 i = floor(p);
            vec2 f = fract(p);

            f = f * f * (3.0 - 2.0 * f);

            return mix(
                mix(Hash(i + vec2(0.0, 0.0)), Hash(i + vec2(1.0, 0.0)), f.x),
                mix(Hash(i + vec2(0.0, 1.0)), Hash(i + vec2(1.0, 1.0)), f.x),
                f.y
            );
        }

        vec2 Rot(vec2 p, float a) {
            return vec2(
                p.x * cos(a) - p.y * sin(a),
                p.x * sin(a) + p.y * cos(a)
            );
        }

        float GenNoise(vec2 p, float time) {
            float d = 0.5;
            float color = 0.0;
            
            for (int i = 0; i < 2; i++) {
                color += d * Noise(p * 5.0 + time);
                p = noiseMatrix * p;
                d /= 2.0;
            }

            return color;
        }

        vec4 main(vec2 fragCoord) {
            vec4 totalColor = vec4(0.0);

            for (int count = 0; count < 2; count++) {
                // Skia natively places origin at top-left, matching D2D. 
                // We recreate the Y-flip from the original HLSL shader here.
                vec2 flippedFrag = vec2(fragCoord.x, u_resolution.y - fragCoord.y);

                vec2 uv = -1.0 + 2.0 * (flippedFrag / u_resolution);
                uv *= 1.4;

                float floatCount = float(count);
                uv.x += Hash(uv + u_time + floatCount) / 512.0;
                uv.y += Hash(vec2(uv.y, uv.x) + u_time + floatCount) / 512.0;

                float aspectRatio = u_resolution.x / u_resolution.y;

                vec3 dir = normalize(vec3(
                    uv * vec2(aspectRatio, 1.0),
                    1.0 + sin(u_time) * 0.01
                ));

                dir.xz = Rot(dir.xz, radians(70.0));
                dir.xy = Rot(dir.xy, radians(90.0));

                float noiseVal = GenNoise(dir.xz, u_time) * 0.5;
                noiseVal *= 1.0 - uv.y * 0.5;
                noiseVal *= 0.05;

                float fogAlpha = pow(max(0.0, noiseVal), 0.717);

                // Fog Color
                vec3 fogColor = vec3(1.0, 1.0, 1.0);

                vec3 premultipliedColor = fogColor * fogAlpha;

                totalColor += vec4(
                    premultipliedColor.x,
                    premultipliedColor.y,
                    premultipliedColor.z,
                    clamp(fogAlpha, 0.0, 1.0)
                );
            }

            return totalColor / 2.0;
        }
    ";
}