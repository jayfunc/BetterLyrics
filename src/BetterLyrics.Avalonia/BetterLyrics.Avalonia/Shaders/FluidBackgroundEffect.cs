namespace BetterLyrics.Avalonia.Shaders;

public class FluidBackgroundEffect
{
    /// <summary>
    ///     Ported from
    ///     <see href="https://github.com/Storyteller-Studios/Isolation/blob/main/ShaderTest.UWP/Shaders/effect.hlsl" />.
    /// </summary>
    public const string SkSlShaderCode = @"
        uniform vec2 u_resolution;
        uniform float u_time;
        uniform vec3 u_c1;
        uniform vec3 u_c2;
        uniform vec3 u_c3;
        uniform vec3 u_c4;
        uniform vec3 u_rnd;
        uniform vec3 u_flags; // x: useHsvBlending, y: enableLightWave, z: enableDithering

        vec2 Rotate(vec2 p, float a) {
            float c = cos(a);
            float s = sin(a);
            return vec2(p.x * c - p.y * s, p.x * s + p.y * c);
        }

        vec2 F_Hash(vec2 p) {
            p = vec2(
                dot(p, vec2(2127.1, 81.17)),
                dot(p, vec2(1269.5, 283.37))
            );
            return fract(sin(p) * 43758.5453);
        }

        float F_Noise(vec2 p) {
            vec2 i = floor(p);
            vec2 f = fract(p);
            vec2 u = f * f * (3.0 - 2.0 * f);

            float n = mix(
                mix(
                    dot(-1.0 + 2.0 * F_Hash(i + vec2(0.0, 0.0)), f - vec2(0.0, 0.0)),
                    dot(-1.0 + 2.0 * F_Hash(i + vec2(1.0, 0.0)), f - vec2(1.0, 0.0)),
                    u.x),
                mix(
                    dot(-1.0 + 2.0 * F_Hash(i + vec2(0.0, 1.0)), f - vec2(0.0, 1.0)),
                    dot(-1.0 + 2.0 * F_Hash(i + vec2(1.0, 1.0)), f - vec2(1.0, 1.0)),
                    u.x),
                u.y);
            return 0.5 + 0.5 * n;
        }

        float Range(float val, float mi, float ma) {
            return val * (ma - mi) + mi;
        }

        vec3 Hsv2Rgb(vec3 c) {
            vec4 K = vec4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
            vec3 p = abs(fract(c.xxx + K.xyz) * 6.0 - K.www);
            return c.z * mix(K.xxx, clamp(p - K.xxx, 0.0, 1.0), c.y);
        }

        vec3 Rgb2Hsv(vec3 c) {
            vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
            vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
            vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));

            float d = q.x - min(q.w, q.y);
            float e = 1.0e-10;
            return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
        }

        vec3 LightWave(vec3 inputColor, bool isHSV, vec2 uv) {
            vec3 hsv = isHSV ? inputColor : Rgb2Hsv(inputColor);
            vec2 p = -1.0 + 1.5 * uv.xy;
            float t = u_time / 5.0;
            float x = p.x;
            float y = p.y;

            float mov0 = x + y + cos(sin(t) * 2.0) * 100.0 + sin(x / 100.0) * 1000.0;
            float mov1 = y / 0.3 + t;
            float mov2 = x / 0.2;

            float c1 = sin(mov1 + t + u_rnd.x) / 2.0 + mov2 / 2.0 - mov1 - mov2 + t;
            float c2 = cos(c1 + sin(mov0 / 1000.0 + t - u_rnd.y) + sin(y / 40.0 + t + u_rnd.z) + sin((x + y) / 100.0) * 3.0);
            float c3 = abs(sin(c2 + cos(mov1 + mov2 + c2) + cos(mov2) + sin(x / 1000.0)));

            vec3 col = Hsv2Rgb(vec3(
                Range(abs(c2), hsv.x * 0.95, hsv.x),
                Range(c3, hsv.y, hsv.y * 0.85),
                Range(c3, hsv.z, hsv.z * 0.85)
            ));
            return col;
        }

        float RemapTri(float v) {
            float orig = v * 2.0 - 1.0;
            v = orig / sqrt(abs(orig));
            v = max(-1.0, v);
            v = v - sign(orig) + 0.5;
            return v;
        }

        vec3 RemapTriVec3(vec3 c) {
            return vec3(RemapTri(c.r), RemapTri(c.g), RemapTri(c.b));
        }

        vec3 ScreenSpaceDither(vec2 vScreenPos, float time) {
            float colorDepth = 32.0;
            float dotValue = dot(vec2(131.0, 312.0), vScreenPos.xy + time);
            vec3 vDither = vec3(dotValue);
            vDither = fract(vDither / vec3(103.0, 71.0, 97.0));
            return RemapTriVec3(vDither) / colorDepth;
        }

        vec4 main(vec2 fragCoord) {
            vec2 scene = fragCoord;
            vec2 uv = scene / u_resolution;

            vec2 tuv = uv;
            tuv -= 0.5;

            float degree = F_Noise(vec2(u_time * 0.1, tuv.x * tuv.y));
            tuv = Rotate(tuv, radians((degree - 0.5) * 720.0 + 180.0));

            float frequency = 5.0;
            float amplitude = 25.0;
            float speed = u_time * 0.75;

            bool enableDithering = u_flags.z > 0.5;
            vec3 diter = enableDithering ? ScreenSpaceDither(scene, u_time) : vec3(0.0);

            tuv.x += sin(tuv.y * frequency + speed) / amplitude;
            tuv.y += sin(tuv.x * frequency * 1.5 + speed) / (amplitude * 0.5);

            bool useHsvBlending = u_flags.x > 0.5;
            
            vec3 c1 = useHsvBlending ? Rgb2Hsv(u_c1) : u_c1;
            vec3 c2 = useHsvBlending ? Rgb2Hsv(u_c2) : u_c2;
            vec3 c3 = useHsvBlending ? Rgb2Hsv(u_c3) : u_c3;
            vec3 c4 = useHsvBlending ? Rgb2Hsv(u_c4) : u_c4;

            float rotatedX = Rotate(tuv, radians(-5.0)).x;

            vec3 layer1 = mix(c1, c2, smoothstep(-0.3, 0.2, rotatedX));
            vec3 layer2 = mix(c3, c4, smoothstep(-0.3, 0.2, rotatedX));

            vec3 finalComp = mix(layer1, layer2, smoothstep(0.5, -0.3, tuv.y));

            bool enableLightWave = u_flags.y > 0.5;
            vec4 result;

            if (enableLightWave) {
                result = vec4(clamp(LightWave(finalComp, useHsvBlending, uv) + diter, 0.0, 1.0), 1.0);
            } else if (useHsvBlending) {
                result = vec4(clamp(Hsv2Rgb(finalComp) + diter, 0.0, 1.0), 1.0);
            } else {
                result = vec4(clamp(finalComp + diter, 0.0, 1.0), 1.0);
            }

            // SkSL expects colors pre-multiplied by alpha, but since alpha is 1.0, this is safe.
            return result;
        }
    ";
}