using ComputeSharp;
using ComputeSharp.D2D1;

namespace BetterLyrics.WinUI3.Shaders
{
    /// <summary>
    /// Ported and modified from <see href="https://www.shadertoy.com/view/ltffzl"/>.
    /// Credit/Copyright to the original author.
    /// </summary>
    [D2DInputCount(0)]
    [D2DShaderProfile(D2D1ShaderProfile.PixelShader50)]
    [D2DGeneratedPixelShaderDescriptor]
    [D2DRequiresScenePosition]
    public readonly partial struct RaindropEffect(
        float time,
        float2 dispatchSize,
        float speed,
        float size,
        float density,
        float lightAngle,
        float shadowIntensity) : ID2D1PixelShader
    {
        private static readonly float _randomSeed = 4.3315f;

        private static readonly Float3x3 _orthonormalMap = new Float3x3(
            0.788675134594813f, -0.211324865405187f, -0.577350269189626f,
           -0.211324865405187f, 0.788675134594813f, -0.577350269189626f,
            0.577350269189626f, 0.577350269189626f, 0.577350269189626f
        );

        private static Float4 Permute(Float4 t) { return t * ((t * 34.0f) + 133.0f); }

        private static Float3 Grad(float hash)
        {
            Float3 cube = (Hlsl.Floor(hash / new Float3(1.0f, 2.0f, 4.0f)) % 2.0f) * 2.0f - 1.0f;
            Float3 cuboct = cube;
            int index = (int)(hash / 16.0f);
            if (index == 0) cuboct.X = 0.0f;
            else if (index == 1) cuboct.Y = 0.0f;
            else cuboct.Z = 0.0f;
            float type = Hlsl.Floor(hash / 8.0f) % 2.0f;
            Float3 rhomb = (1.0f - type) * cube + type * (cuboct + Hlsl.Cross(cube, cuboct));
            Float3 grad = cuboct * 1.22474487139f + rhomb;
            grad *= (1.0f - 0.042942436724648037f * type) * 3.5946317686139184f;
            return grad;
        }

        private static Float4 Os2NoiseWithDerivativesPart(Float3 x)
        {
            Float3 b = Hlsl.Floor(x);
            Float4 i4 = new Float4(x - b, 2.5f);
            Float3 v1 = b + Hlsl.Floor(Hlsl.Dot(i4, (Float4)0.25f));
            Float3 v2 = b + new Float3(1.0f, 0.0f, 0.0f) + new Float3(-1.0f, 1.0f, 1.0f) * Hlsl.Floor(Hlsl.Dot(i4, new Float4(-0.25f, 0.25f, 0.25f, 0.35f)));
            Float3 v3 = b + new Float3(0.0f, 1.0f, 0.0f) + new Float3(1.0f, -1.0f, 1.0f) * Hlsl.Floor(Hlsl.Dot(i4, new Float4(0.25f, -0.25f, 0.25f, 0.35f)));
            Float3 v4 = b + new Float3(0.0f, 0.0f, 1.0f) + new Float3(1.0f, 1.0f, -1.0f) * Hlsl.Floor(Hlsl.Dot(i4, new Float4(0.25f, 0.25f, -0.25f, 0.35f)));
            Float4 hashes = Permute(new Float4(v1.X, v2.X, v3.X, v4.X) % 289.0f);
            hashes = Permute(hashes + new Float4(v1.Y, v2.Y, v3.Y, v4.Y) % 289.0f);
            hashes = Permute(hashes + new Float4(v1.Z, v2.Z, v3.Z, v4.Z) % 289.0f) % 48.0f;
            Float3 d1 = x - v1; Float3 d2 = x - v2; Float3 d3 = x - v3; Float3 d4 = x - v4;
            Float4 a = Hlsl.Max(0.75f - new Float4(Hlsl.Dot(d1, d1), Hlsl.Dot(d2, d2), Hlsl.Dot(d3, d3), Hlsl.Dot(d4, d4)), 0.0f);
            Float4 aa = a * a; Float4 aaaa = aa * aa;
            Float3 g1 = Grad(hashes.X); Float3 g2 = Grad(hashes.Y);
            Float3 g3 = Grad(hashes.Z); Float3 g4 = Grad(hashes.W);
            Float4 extrapolations = new Float4(Hlsl.Dot(d1, g1), Hlsl.Dot(d2, g2), Hlsl.Dot(d3, g3), Hlsl.Dot(d4, g4));
            Float3x4 m1 = new Float3x4(d1.X, d2.X, d3.X, d4.X, d1.Y, d2.Y, d3.Y, d4.Y, d1.Z, d2.Z, d3.Z, d4.Z);
            Float3x4 m2 = new Float3x4(g1.X, g2.X, g3.X, g4.X, g1.Y, g2.Y, g3.Y, g4.Y, g1.Z, g2.Z, g3.Z, g4.Z);
            Float3 derivative = -8.0f * Hlsl.Mul(m1, (aa * a * extrapolations)) + Hlsl.Mul(m2, aaaa);
            return new Float4(derivative, Hlsl.Dot(aaaa, extrapolations));
        }

        private static Float4 Os2NoiseWithDerivativesImproveXy(Float3 x)
        {
            x = Hlsl.Mul(_orthonormalMap, x);
            Float4 result = Os2NoiseWithDerivativesPart(x) + Os2NoiseWithDerivativesPart(x + 144.5f);
            return new Float4(Hlsl.Mul(result.XYZ, _orthonormalMap), result.W);
        }

        private static float GradientWave(float b, float t) { return Hlsl.SmoothStep(0.0f, b, t) * Hlsl.SmoothStep(1.0f, b, t); }
        private static float Random(Float2 uv, float seed) { return Hlsl.Frac(Hlsl.Sin(Hlsl.Dot(uv * 13.235f, new Float2(12.9898f, 78.233f)) * 0.000001f) * 43758.5453123f * seed); }
        private static Float3 RandomVec3(Float2 uv, float seed) { return new Float3(Random(uv, seed), Random(uv * 2.0f, seed), Random(uv * 3.0f, seed)); }
        private static float MapToRange(float edge0, float edge1, float x) { return Hlsl.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f); }
        private static float ProportionalMapToRange(float edge0, float edge1, float x) { return edge0 + (edge1 - edge0) * x; }

        private static Float3 RaindropSurface(Float2 xy, float distanceScale, float zScale)
        {
            float a = distanceScale; float x = xy.X; float y = xy.Y;
            float n = 1.5f; float m = 0.5f; float s = zScale;
            float tempZ = 1.0f - Hlsl.Pow(x / a, 2.0f) - Hlsl.Pow(y / a, 2.0f);
            float z = Hlsl.Pow(Hlsl.Max(0.0f, tempZ), a / 2.0f);
            float zInMAndN = (z - m) / (n - m);
            float t = Hlsl.Min(Hlsl.Max(zInMAndN, 0.0f), 1.0f);
            float height = s * t * t * (3.0f - 2.0f * t);
            float part01 = s * (6.0f * t - 8.0f * t * t);
            float part02 = 1.0f / (n - m);
            float part03 = -1.0f / a * Hlsl.Pow(Hlsl.Max(0.0f, tempZ), a / 2.0f - 1.0f);
            float tempValue = (zInMAndN > 0.0f && zInMAndN < 1.0f) ? (part01 * part02) : 0.0f;
            Float2 partialDerivative = height > 0.0f ? new Float2(tempValue * (x * part03), tempValue * (y * part03)) : Float2.Zero;
            return new Float3(height, partialDerivative);
        }

        private static Float3 StaticRaindrops(Float2 uv, float time, float uvScale, float density)
        {
            Float2 tempUv = uv * uvScale;
            Float2 id = Hlsl.Floor(tempUv);
            Float3 randomValue = RandomVec3(new Float2(id.X * 470.15f, id.Y * 653.58f), _randomSeed);
            tempUv = Hlsl.Frac(tempUv) - 0.5f;
            Float2 randomPoint = (randomValue.XY - 0.5f) * 0.25f;
            Float2 xy = randomPoint - tempUv;
            float distance = Hlsl.Length(tempUv - randomPoint);

            Float3 noiseInput = new Float3(new Float2(tempUv.X * 305.0f * 0.02f, tempUv.Y * 305.0f * 0.02f), 1.8660254037844386f);
            Float4 noiseResult = Os2NoiseWithDerivativesImproveXy(noiseInput);
            float edgeRandomCurveAdjust = noiseResult.W * Hlsl.Lerp(0.02f, 0.175f, Hlsl.Frac(randomValue.X));

            distance = edgeRandomCurveAdjust * 0.5f + distance;
            distance = distance * Hlsl.Clamp(Hlsl.Lerp(1.0f, 55.0f, randomPoint.X), 1.0f, 3.0f);
            float gradientFade = GradientWave(0.0005f, Hlsl.Frac(time * 0.02f + randomValue.Z));
            float distanceMaxRange = 1.45f * gradientFade;
            Float2 direction = tempUv - randomPoint;

            float theta = 3.141592653f - Hlsl.Acos(Hlsl.Dot(Hlsl.Normalize(direction), new Float2(0.0f, 1.0f)));
            theta = theta * randomValue.Z;
            float distanceScale = 0.2f / (1.0f - 0.8f * Hlsl.Cos(theta - 3.141593f / 2.0f - 1.6f));
            float yDistance = Hlsl.Length(new Float2(0.0f, tempUv.Y) - new Float2(0.0f, randomPoint.Y));

            float scale = 1.65f * (0.2f + distanceScale * 1.0f) * distanceMaxRange * Hlsl.Lerp(1.5f, 0.5f, randomValue.X);
            Float2 tempXy = new Float2(xy.X * 1.0f, xy.Y) * 4.0f;
            float randomScale = ProportionalMapToRange(0.85f, 1.35f, randomValue.Z);
            tempXy.X = randomScale * Hlsl.Lerp(tempXy.X, tempXy.X / Hlsl.SmoothStep(1.0f, 0.4f, yDistance * randomValue.Z), Hlsl.SmoothStep(1.0f, 0.0f, randomValue.X));
            tempXy = tempXy + edgeRandomCurveAdjust * 1.0f;
            Float3 heightAndNormal = RaindropSurface(tempXy, scale, 1.0f);
            heightAndNormal.YZ = -heightAndNormal.YZ;

            float randomVisible = (Hlsl.Frac(randomValue.Z * 10.0f * _randomSeed) < density ? 1.0f : 0.0f);
            heightAndNormal.YZ = heightAndNormal.YZ * randomVisible;
            heightAndNormal.X = Hlsl.SmoothStep(0.0f, 1.0f, heightAndNormal.X) * randomVisible;

            return heightAndNormal;
        }

        private static Float4 RollingRaindrops(Float2 uv, float time, float uvScale, float density)
        {
            Float2 localUv = uv * uvScale;
            Float2 tempUv = localUv;
            Float2 constantA = new Float2(6.0f, 1.0f);
            Float2 gridNum = constantA * 2.0f;
            Float2 gridId = Hlsl.Floor(localUv * gridNum);

            float randomFloat = Random(new Float2(gridId.X * 131.26f, gridId.X * 101.81f), _randomSeed);
            float timeMovingY = time * 0.85f * ProportionalMapToRange(0.1f, 0.25f, randomFloat);
            localUv.Y += timeMovingY + randomFloat;

            Float2 scaledUv = localUv * gridNum;
            gridId = Hlsl.Floor(scaledUv);
            Float3 randomVec3 = RandomVec3(new Float2(gridId.X * 17.32f, gridId.Y * 2217.54f), _randomSeed);
            Float2 gridUv = Hlsl.Frac(scaledUv) - new Float2(0.5f, 0.0f);

            float swingX = randomVec3.X - 0.5f;
            float swingY = tempUv.Y * 20.0f;
            float swingPosition = Hlsl.Sin(swingY + Hlsl.Sin(gridId.Y * randomVec3.Z + swingY) + gridId.Y * randomVec3.Z);
            swingX += swingPosition * (0.5f - Hlsl.Abs(swingX)) * (randomVec3.Z - 0.5f);
            swingX *= 0.65f;
            float randomNormalizedTime = Hlsl.Frac(timeMovingY + randomVec3.Z) * 1.0f;
            swingY = Hlsl.Clamp((GradientWave(0.87f, randomNormalizedTime) - 0.5f) * 0.9f + 0.5f, 0.15f, 0.85f);
            Float2 position = new Float2(swingX, swingY);

            Float2 xy = position - gridUv;
            Float2 direction = (gridUv - position) * constantA.YX;
            float distance = Hlsl.Length(direction);

            Float3 noiseInput = new Float3(new Float2(tempUv.X * 513.20f * 0.02f, tempUv.Y * 779.40f * 0.02f), 2.1660251037743386f);
            Float4 noiseResult = Os2NoiseWithDerivativesImproveXy(noiseInput);
            float edgeRandomCurveAdjust = noiseResult.W * Hlsl.Lerp(0.02f, 0.175f, Hlsl.Frac(randomVec3.Y));

            distance = edgeRandomCurveAdjust + distance;
            float theta = 3.141592653f - Hlsl.Acos(Hlsl.Dot(Hlsl.Normalize(direction), new Float2(0.0f, 1.0f)));
            theta = theta * randomVec3.Z;
            float distanceScale = 0.2f / (1.0f - 0.8f * Hlsl.Cos(theta - 3.141593f / 2.0f - 1.6f));
            float scale = 1.65f * (0.2f + distanceScale * 1.0f) * 1.45f * Hlsl.Lerp(1.0f, 0.25f, randomVec3.X * 1.0f);
            Float2 tempXy = new Float2(xy.X * 1.0f, xy.Y) * 4.0f;
            tempXy = tempXy * new Float2(1.0f, 4.2f) + edgeRandomCurveAdjust * 0.85f;
            Float3 heightAndNormal = RaindropSurface(tempXy, scale, 1.0f);

            float trailY = Hlsl.Pow(Hlsl.SmoothStep(1.0f, swingY, gridUv.Y), 0.5f);
            float trailX = Hlsl.Abs(gridUv.X - swingX) * Hlsl.Lerp(0.8f, 4.0f, Hlsl.SmoothStep(0.0f, 1.0f, randomVec3.X));
            float trail = Hlsl.SmoothStep(0.25f * trailY, 0.15f * trailY * trailY, trailX);
            float trailClamp = Hlsl.SmoothStep(-0.02f, 0.02f, gridUv.Y - swingY);
            trail *= trailClamp * trailY;

            float signOfTrailX = Hlsl.Sign(gridUv.X - swingX);
            Float3 trailNoiseInput = new Float3(new Float2(tempUv.X * 513.20f * 0.02f * signOfTrailX, tempUv.Y * 779.40f * 0.02f), 2.1660251037743386f);
            Float4 trailNoiseResult = Os2NoiseWithDerivativesImproveXy(trailNoiseInput);
            float trailEdgeRandomCurveAdjust = trailNoiseResult.W * Hlsl.Lerp(0.002f, 0.175f, Hlsl.Frac(randomVec3.Y));
            float trailXDistance = MapToRange(0.0f, 0.1f, trailEdgeRandomCurveAdjust * 0.5f + trailX);
            Float2 trailDirection = signOfTrailX * new Float2(1.0f, 0.0f) + new Float2(0.0f, 1.0f) * Hlsl.SmoothStep(1.0f, 0.0f, trail) * 0.5f;
            Float2 trailXy = trailDirection * 1.0f * trailXDistance;

            Float3 trailHeightAndNormal = RaindropSurface(trailXy, 1.0f, 1.0f);
            trailHeightAndNormal = trailHeightAndNormal * Hlsl.Pow(trail * randomVec3.Y, 2.0f);
            trailHeightAndNormal.X = Hlsl.SmoothStep(0.0f, 1.0f, trailHeightAndNormal.X);

            swingY = tempUv.Y;
            float remainTrail = Hlsl.SmoothStep(0.2f * trailY, 0.0f, trailX);
            float remainDroplet = Hlsl.Max(0.0f, (Hlsl.Sin(swingY * (1.0f - swingY) * 120.0f) - gridUv.Y)) * remainTrail * trailClamp * randomVec3.Z;
            swingY = Hlsl.Frac(swingY * 10.0f) + (gridUv.Y - 0.5f);
            Float2 remainDropletXy = gridUv - new Float2(swingX, swingY);
            remainDropletXy = remainDropletXy * new Float2(1.2f, 0.8f) + edgeRandomCurveAdjust * 0.85f;
            Float3 remainDropletHeightAndNormal = RaindropSurface(remainDropletXy, 2.0f * remainDroplet, 1.0f);
            remainDropletHeightAndNormal.X = Hlsl.SmoothStep(0.0f, 1.0f, remainDropletHeightAndNormal.X);
            remainDropletHeightAndNormal = trailHeightAndNormal.X > 0.0f ? Float3.Zero : remainDropletHeightAndNormal;

            Float4 returnValue = new Float4();
            returnValue.X = heightAndNormal.X + trailHeightAndNormal.X * trailY * trailClamp + remainDropletHeightAndNormal.X * trailY * trailClamp;
            returnValue.YZ = heightAndNormal.YZ + trailHeightAndNormal.YZ + remainDropletHeightAndNormal.YZ;
            returnValue.W = trail;

            float randomVisible = (Hlsl.Frac(randomVec3.Z * 20.0f * _randomSeed) < density ? 1.0f : 0.0f);
            returnValue *= randomVisible;
            return returnValue;
        }

        private static Float4 Raindrops(Float2 uv, float time, float staticScale, float rollingScale, float density)
        {
            Float3 staticRaindrop = StaticRaindrops(uv, time, staticScale, density);
            Float4 rollingRaindrop01 = RollingRaindrops(uv, time, rollingScale, density);

            float height = staticRaindrop.X + rollingRaindrop01.X;
            Float2 normal = staticRaindrop.YZ + rollingRaindrop01.YZ;
            float trail = rollingRaindrop01.W;

            return new Float4(height, normal, trail);
        }

        public Float4 Execute()
        {
            float scaledTime = time * speed;
            Float2 scenePos = D2D.GetScenePosition().XY;
            Float2 fragCoord = new Float2(scenePos.X, dispatchSize.Y - scenePos.Y);
            Float2 localUv = (fragCoord - (0.5f * dispatchSize)) / dispatchSize.Y;

            float staticUvScale = 20.0f / Hlsl.Max(0.1f, size);
            float rollingUvScale = 2.25f / Hlsl.Max(0.1f, size);

            Float4 raindrop = Raindrops(localUv, scaledTime, staticUvScale, rollingUvScale, density);

            float height = raindrop.X;
            Float2 normal = raindrop.YZ;

            float dropMask = Hlsl.SmoothStep(0.02f, 0.15f, height);

            float lightX = Hlsl.Cos(lightAngle);
            float lightY = Hlsl.Sin(lightAngle);
            Float3 lightDir = Hlsl.Normalize(new Float3(lightX, lightY, 1.5f));

            Float3 surfaceNormal = Hlsl.Normalize(new Float3(normal.X, normal.Y, 1.0f - height));

            float specular = Hlsl.Max(0.0f, Hlsl.Dot(surfaceNormal, lightDir));
            specular = Hlsl.Pow(specular, 24.0f);

            float edgeShadow = Hlsl.SmoothStep(0.2f, 1.0f, Hlsl.Length(normal)) * shadowIntensity;

            float baseBrightness = Hlsl.Saturate((0.15f * height) + specular - edgeShadow);

            float alpha = dropMask * Hlsl.Saturate(0.25f + specular);

            return new Float4(
                baseBrightness,
                baseBrightness,
                baseBrightness,
                alpha
            );
        }
    }
}