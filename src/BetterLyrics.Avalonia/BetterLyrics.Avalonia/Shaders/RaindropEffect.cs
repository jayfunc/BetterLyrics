namespace BetterLyrics.Avalonia.Shaders;

public class RaindropEffect
{
    /// <summary>
    ///     Ported and modified from <see href="https://www.shadertoy.com/view/ltffzl" />.
    ///     Credit/Copyright to the original author.
    /// </summary>
    public const string SkSlShaderCode = @"
        uniform float u_time;
        uniform vec2 u_resolution;
        uniform float u_speed;
        uniform float u_size;
        uniform float u_density;
        uniform float u_lightAngle;
        uniform float u_shadowIntensity;

        const float randomSeed = 4.3315;

        // In SkSL, mat3 constructor takes arguments column-by-column. 
        // We transpose this from HLSL's row-major constructor to guarantee math parity during multiplication.
        const mat3 orthonormalMap = mat3(
            0.788675134594813, -0.211324865405187, 0.577350269189626,
            -0.211324865405187, 0.788675134594813, 0.577350269189626,
            -0.577350269189626, -0.577350269189626, 0.577350269189626
        );

        vec4 Permute(vec4 t) {
            return t * (t * 34.0 + 133.0);
        }

        vec3 Grad(float hash) {
            vec3 cube = mod(floor(hash / vec3(1.0, 2.0, 4.0)), 2.0) * 2.0 - 1.0;
            vec3 cuboct = cube;
            int index = int(hash / 16.0);
            
            if (index == 0) cuboct.x = 0.0;
            else if (index == 1) cuboct.y = 0.0;
            else cuboct.z = 0.0;
            
            float type = mod(floor(hash / 8.0), 2.0);
            vec3 rhomb = (1.0 - type) * cube + type * (cuboct + cross(cube, cuboct));
            vec3 grad = cuboct * 1.22474487139 + rhomb;
            grad *= (1.0 - 0.042942436724648037 * type) * 3.5946317686139184;
            
            return grad;
        }

        vec4 Os2NoiseWithDerivativesPart(vec3 x) {
            vec3 b = floor(x);
            vec4 i4 = vec4(x - b, 2.5);
            vec3 v1 = b + floor(dot(i4, vec4(0.25)));
            vec3 v2 = b + vec3(1.0, 0.0, 0.0) + vec3(-1.0, 1.0, 1.0) * floor(dot(i4, vec4(-0.25, 0.25, 0.25, 0.35)));
            vec3 v3 = b + vec3(0.0, 1.0, 0.0) + vec3(1.0, -1.0, 1.0) * floor(dot(i4, vec4(0.25, -0.25, 0.25, 0.35)));
            vec3 v4 = b + vec3(0.0, 0.0, 1.0) + vec3(1.0, 1.0, -1.0) * floor(dot(i4, vec4(0.25, 0.25, -0.25, 0.35)));
            
            vec4 hashes = Permute(mod(vec4(v1.x, v2.x, v3.x, v4.x), 289.0));
            hashes = Permute(hashes + mod(vec4(v1.y, v2.y, v3.y, v4.y), 289.0));
            hashes = mod(Permute(hashes + mod(vec4(v1.z, v2.z, v3.z, v4.z), 289.0)), 48.0);
            
            vec3 d1 = x - v1;
            vec3 d2 = x - v2;
            vec3 d3 = x - v3;
            vec3 d4 = x - v4;
            
            vec4 a = max(0.75 - vec4(dot(d1, d1), dot(d2, d2), dot(d3, d3), dot(d4, d4)), 0.0);
            vec4 aa = a * a;
            vec4 aaaa = aa * aa;
            
            vec3 g1 = Grad(hashes.x);
            vec3 g2 = Grad(hashes.y);
            vec3 g3 = Grad(hashes.z);
            vec3 g4 = Grad(hashes.w);
            
            vec4 extrapolations = vec4(dot(d1, g1), dot(d2, g2), dot(d3, g3), dot(d4, g4));
            
            // --- FIX: Unrolled Matrix Multiplication ---
            // Instead of using mat4x3, we calculate the dot products as vector combinations directly.
            vec4 mul1 = aa * a * extrapolations;
            vec4 mul2 = aaaa;
            
            vec3 term1 = d1 * mul1.x + d2 * mul1.y + d3 * mul1.z + d4 * mul1.w;
            vec3 term2 = g1 * mul2.x + g2 * mul2.y + g3 * mul2.z + g4 * mul2.w;
            
            vec3 derivative = -8.0 * term1 + term2;
            // -------------------------------------------
            
            return vec4(derivative, dot(aaaa, extrapolations));
        }

        vec4 Os2NoiseWithDerivativesImproveXy(vec3 x) {
            x = orthonormalMap * x; // Matches mul(_orthonormalMap, x)
            vec4 result = Os2NoiseWithDerivativesPart(x) + Os2NoiseWithDerivativesPart(x + 144.5);
            return vec4(result.xyz * orthonormalMap, result.w); // Matches mul(result.XYZ, _orthonormalMap)
        }

        float GradientWave(float b, float t) {
            return smoothstep(0.0, b, t) * smoothstep(1.0, b, t);
        }

        float Random(vec2 uv, float seed) {
            return fract(sin(dot(uv * 13.235, vec2(12.9898, 78.233)) * 0.000001) * 43758.5453123 * seed);
        }

        vec3 RandomVec3(vec2 uv, float seed) {
            return vec3(Random(uv, seed), Random(uv * 2.0, seed), Random(uv * 3.0, seed));
        }

        float MapToRange(float edge0, float edge1, float x) {
            return clamp((x - edge0) / (edge1 - edge0), 0.0, 1.0);
        }

        float ProportionalMapToRange(float edge0, float edge1, float x) {
            return edge0 + (edge1 - edge0) * x;
        }

        vec3 RaindropSurface(vec2 xy, float distanceScale, float zScale) {
            float a = distanceScale;
            float x = xy.x;
            float y = xy.y;
            float n = 1.5;
            float m = 0.5;
            float s = zScale;
            
            float tempZ = 1.0 - pow(x / a, 2.0) - pow(y / a, 2.0);
            float z = pow(max(0.0, tempZ), a / 2.0);
            float zInMAndN = (z - m) / (n - m);
            float t = min(max(zInMAndN, 0.0), 1.0);
            float height = s * t * t * (3.0 - 2.0 * t);
            float part01 = s * (6.0 * t - 8.0 * t * t);
            float part02 = 1.0 / (n - m);
            float part03 = -1.0 / a * pow(max(0.0, tempZ), a / 2.0 - 1.0);
            
            float tempValue = (zInMAndN > 0.0 && zInMAndN < 1.0) ? (part01 * part02) : 0.0;
            vec2 partialDerivative = height > 0.0 ? vec2(tempValue * (x * part03), tempValue * (y * part03)) : vec2(0.0);
            
            return vec3(height, partialDerivative);
        }

        vec3 StaticRaindrops(vec2 uv, float time, float uvScale, float density) {
            vec2 tempUv = uv * uvScale;
            vec2 id = floor(tempUv);
            vec3 randomValue = RandomVec3(vec2(id.x * 470.15, id.y * 653.58), randomSeed);
            tempUv = fract(tempUv) - 0.5;
            vec2 randomPoint = (randomValue.xy - 0.5) * 0.25;
            vec2 xy = randomPoint - tempUv;
            float distance = length(tempUv - randomPoint);

            vec3 noiseInput = vec3(tempUv.x * 305.0 * 0.02, tempUv.y * 305.0 * 0.02, 1.8660254037844386);
            vec4 noiseResult = Os2NoiseWithDerivativesImproveXy(noiseInput);
            float edgeRandomCurveAdjust = noiseResult.w * mix(0.02, 0.175, fract(randomValue.x));

            distance = edgeRandomCurveAdjust * 0.5 + distance;
            distance = distance * clamp(mix(1.0, 55.0, randomPoint.x), 1.0, 3.0);
            float gradientFade = GradientWave(0.0005, fract(time * 0.02 + randomValue.z));
            float distanceMaxRange = 1.45 * gradientFade;
            vec2 direction = tempUv - randomPoint;

            float theta = 3.141592653 - acos(dot(normalize(direction), vec2(0.0, 1.0)));
            theta = theta * randomValue.z;
            float distanceScale = 0.2 / (1.0 - 0.8 * cos(theta - 3.141593 / 2.0 - 1.6));
            float yDistance = length(vec2(0.0, tempUv.y) - vec2(0.0, randomPoint.y));

            float scale = 1.65 * (0.2 + distanceScale * 1.0) * distanceMaxRange * mix(1.5, 0.5, randomValue.x);
            vec2 tempXy = vec2(xy.x * 1.0, xy.y) * 4.0;
            float randomScale = ProportionalMapToRange(0.85, 1.35, randomValue.z);
            
            tempXy.x = randomScale * mix(tempXy.x, tempXy.x / smoothstep(1.0, 0.4, yDistance * randomValue.z), smoothstep(1.0, 0.0, randomValue.x));
            tempXy = tempXy + edgeRandomCurveAdjust * 1.0;
            
            vec3 heightAndNormal = RaindropSurface(tempXy, scale, 1.0);
            heightAndNormal.yz = -heightAndNormal.yz;

            float randomVisible = fract(randomValue.z * 10.0 * randomSeed) < density ? 1.0 : 0.0;
            heightAndNormal.yz = heightAndNormal.yz * randomVisible;
            heightAndNormal.x = smoothstep(0.0, 1.0, heightAndNormal.x) * randomVisible;

            return heightAndNormal;
        }

        vec4 RollingRaindrops(vec2 uv, float time, float uvScale, float density) {
            vec2 localUv = uv * uvScale;
            vec2 tempUv = localUv;
            vec2 constantA = vec2(6.0, 1.0);
            vec2 gridNum = constantA * 2.0;
            vec2 gridId = floor(localUv * gridNum);

            float randomFloat = Random(vec2(gridId.x * 131.26, gridId.x * 101.81), randomSeed);
            float timeMovingY = time * 0.85 * ProportionalMapToRange(0.1, 0.25, randomFloat);
            localUv.y += timeMovingY + randomFloat;

            vec2 scaledUv = localUv * gridNum;
            gridId = floor(scaledUv);
            vec3 randomVec3 = RandomVec3(vec2(gridId.x * 17.32, gridId.y * 2217.54), randomSeed);
            vec2 gridUv = fract(scaledUv) - vec2(0.5, 0.0);

            float swingX = randomVec3.x - 0.5;
            float swingY = tempUv.y * 20.0;
            float swingPosition = sin(swingY + sin(gridId.y * randomVec3.z + swingY) + gridId.y * randomVec3.z);
            swingX += swingPosition * (0.5 - abs(swingX)) * (randomVec3.z - 0.5);
            swingX *= 0.65;
            
            float randomNormalizedTime = fract(timeMovingY + randomVec3.z) * 1.0;
            swingY = clamp((GradientWave(0.87, randomNormalizedTime) - 0.5) * 0.9 + 0.5, 0.15, 0.85);
            vec2 position = vec2(swingX, swingY);

            vec2 xy = position - gridUv;
            vec2 direction = (gridUv - position) * constantA.yx;
            float distance = length(direction);

            vec3 noiseInput = vec3(tempUv.x * 513.20 * 0.02, tempUv.y * 779.40 * 0.02, 2.1660251037743386);
            vec4 noiseResult = Os2NoiseWithDerivativesImproveXy(noiseInput);
            float edgeRandomCurveAdjust = noiseResult.w * mix(0.02, 0.175, fract(randomVec3.y));

            distance = edgeRandomCurveAdjust + distance;
            float theta = 3.141592653 - acos(dot(normalize(direction), vec2(0.0, 1.0)));
            theta = theta * randomVec3.z;
            
            float distanceScale = 0.2 / (1.0 - 0.8 * cos(theta - 3.141593 / 2.0 - 1.6));
            float scale = 1.65 * (0.2 + distanceScale * 1.0) * 1.45 * mix(1.0, 0.25, randomVec3.x * 1.0);
            
            vec2 tempXy = vec2(xy.x * 1.0, xy.y) * 4.0;
            tempXy = tempXy * vec2(1.0, 4.2) + edgeRandomCurveAdjust * 0.85;
            vec3 heightAndNormal = RaindropSurface(tempXy, scale, 1.0);

            float trailY = pow(smoothstep(1.0, swingY, gridUv.y), 0.5);
            float trailX = abs(gridUv.x - swingX) * mix(0.8, 4.0, smoothstep(0.0, 1.0, randomVec3.x));
            float trail = smoothstep(0.25 * trailY, 0.15 * trailY * trailY, trailX);
            float trailClamp = smoothstep(-0.02, 0.02, gridUv.y - swingY);
            trail *= trailClamp * trailY;

            float signOfTrailX = sign(gridUv.x - swingX);
            vec3 trailNoiseInput = vec3(tempUv.x * 513.20 * 0.02 * signOfTrailX, tempUv.y * 779.40 * 0.02, 2.1660251037743386);
            vec4 trailNoiseResult = Os2NoiseWithDerivativesImproveXy(trailNoiseInput);
            float trailEdgeRandomCurveAdjust = trailNoiseResult.w * mix(0.002, 0.175, fract(randomVec3.y));
            float trailXDistance = MapToRange(0.0, 0.1, trailEdgeRandomCurveAdjust * 0.5 + trailX);
            vec2 trailDirection = signOfTrailX * vec2(1.0, 0.0) + vec2(0.0, 1.0) * smoothstep(1.0, 0.0, trail) * 0.5;
            vec2 trailXy = trailDirection * 1.0 * trailXDistance;

            vec3 trailHeightAndNormal = RaindropSurface(trailXy, 1.0, 1.0);
            trailHeightAndNormal = trailHeightAndNormal * pow(trail * randomVec3.y, 2.0);
            trailHeightAndNormal.x = smoothstep(0.0, 1.0, trailHeightAndNormal.x);

            swingY = tempUv.y;
            float remainTrail = smoothstep(0.2 * trailY, 0.0, trailX);
            float remainDroplet = max(0.0, sin(swingY * (1.0 - swingY) * 120.0) - gridUv.y) * remainTrail * trailClamp * randomVec3.z;
            swingY = fract(swingY * 10.0) + (gridUv.y - 0.5);
            
            vec2 remainDropletXy = gridUv - vec2(swingX, swingY);
            remainDropletXy = remainDropletXy * vec2(1.2, 0.8) + edgeRandomCurveAdjust * 0.85;
            vec3 remainDropletHeightAndNormal = RaindropSurface(remainDropletXy, 2.0 * remainDroplet, 1.0);
            remainDropletHeightAndNormal.x = smoothstep(0.0, 1.0, remainDropletHeightAndNormal.x);
            remainDropletHeightAndNormal = trailHeightAndNormal.x > 0.0 ? vec3(0.0) : remainDropletHeightAndNormal;

            vec4 returnValue = vec4(0.0);
            returnValue.x = heightAndNormal.x + trailHeightAndNormal.x * trailY * trailClamp + remainDropletHeightAndNormal.x * trailY * trailClamp;
            returnValue.yz = heightAndNormal.yz + trailHeightAndNormal.yz + remainDropletHeightAndNormal.yz;
            returnValue.w = trail;

            float randomVisible = fract(randomVec3.z * 20.0 * randomSeed) < density ? 1.0 : 0.0;
            returnValue *= randomVisible;
            return returnValue;
        }

        vec4 Raindrops(vec2 uv, float time, float staticScale, float rollingScale, float density) {
            vec3 staticRaindrop = StaticRaindrops(uv, time, staticScale, density);
            vec4 rollingRaindrop01 = RollingRaindrops(uv, time, rollingScale, density);

            float height = staticRaindrop.x + rollingRaindrop01.x;
            vec2 normal = staticRaindrop.yz + rollingRaindrop01.yz;
            float trail = rollingRaindrop01.w;

            return vec4(height, normal, trail);
        }

        vec4 main(vec2 fragCoord) {
            float scaledTime = u_time * u_speed;
            
            // Recreating the HLSL scene position logic matching Win2D's Y-axis inversion.
            vec2 scenePos = fragCoord;
            vec2 flippedFrag = vec2(scenePos.x, u_resolution.y - scenePos.y);
            
            vec2 localUv = (flippedFrag - 0.5 * u_resolution) / u_resolution.y;

            float staticUvScale = 20.0 / max(0.1, u_size);
            float rollingUvScale = 2.25 / max(0.1, u_size);

            vec4 raindrop = Raindrops(localUv, scaledTime, staticUvScale, rollingUvScale, u_density);

            float height = raindrop.x;
            vec2 normal = raindrop.yz;

            float dropMask = smoothstep(0.02, 0.15, height);

            float lightX = cos(u_lightAngle);
            float lightY = sin(u_lightAngle);
            vec3 lightDir = normalize(vec3(lightX, lightY, 1.5));

            vec3 surfaceNormal = normalize(vec3(normal.x, normal.y, 1.0 - height));

            float specular = max(0.0, dot(surfaceNormal, lightDir));
            specular = pow(specular, 24.0);

            float edgeShadow = smoothstep(0.2, 1.0, length(normal)) * u_shadowIntensity;

            float baseBrightness = clamp(0.15 * height + specular - edgeShadow, 0.0, 1.0);
            float alpha = dropMask * clamp(0.25 + specular, 0.0, 1.0);

            return vec4(
                baseBrightness,
                baseBrightness,
                baseBrightness,
                alpha
            );
        }
    ";
}