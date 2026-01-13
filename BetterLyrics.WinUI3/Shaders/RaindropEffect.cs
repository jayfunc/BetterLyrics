using ComputeSharp;
using ComputeSharp.D2D1;

namespace BetterLyrics.WinUI3.Shaders
{
    [D2DInputCount(1)]
    [D2DShaderProfile(D2D1ShaderProfile.PixelShader50)]
    [D2DGeneratedPixelShaderDescriptor]
    [D2DRequiresScenePosition]
    public readonly partial struct RaindropEffect(float time, float2 dispatchSize) : ID2D1PixelShader
    {
        // === 来自 GLSL #define 的常量 ===
        private static readonly float RandomSeed = 4.3315f;
        private static readonly float NumberScaleOfStaticRaindrops = 0.35f;
        private static readonly float NumberScaleOfRollingRaindrops = 0.35f;
        // private static readonly float RaindropBlur = 0.0f; // 已移除 (无背景)
        // private static readonly float BackgroundBlur = 2.0f; // 已移除 (无背景)
        private static readonly float StaticRaindropUVScale = 20.0f;
        private static readonly float RollingRaindropUVScaleLayer01 = 2.25f;
        private static readonly float RollingRaindropUVScaleLayer02 = 2.25f;

        // === 3D OpenSimplex2S 噪声 (HLSL 移植版) ===

        // GLSL mat3(col0, col1, col2) - 列优先
        // C# Float3x3(r0c0, r0c1, r0c2, r1c0, ...) - 行优先
        // 所以我们需要转置 GLSL 构造函数中的值
        private static readonly Float3x3 OrthonormalMap = new Float3x3(
            0.788675134594813f, -0.211324865405187f, -0.577350269189626f,
           -0.211324865405187f, 0.788675134594813f, -0.577350269189626f,
            0.577350269189626f, 0.577350269189626f, 0.577350269189626f
        );

        private static Float4 Permute(Float4 t)
        {
            return t * ((t * 34.0f) + 133.0f);
        }

        private static Float3 Grad(float hash)
        {
            // Random vertex of a cube, +/- 1 each
            Float3 cube = (Hlsl.Floor(hash / new Float3(1.0f, 2.0f, 4.0f)) % 2.0f) * 2.0f - 1.0f;

            // Random edge
            Float3 cuboct = cube;

            // HLSL/C# 中向量不支持动态索引 (cuboct[index] = 0.0)
            // 我们必须使用 if/else 链
            int index = (int)(hash / 16.0f);
            if (index == 0) cuboct.X = 0.0f;
            else if (index == 1) cuboct.Y = 0.0f;
            else cuboct.Z = 0.0f;

            // In a funky way, pick one of the four points on the rhombic face
            float type = Hlsl.Floor(hash / 8.0f) % 2.0f;
            Float3 rhomb = (1.0f - type) * cube + type * (cuboct + Hlsl.Cross(cube, cuboct));

            Float3 grad = cuboct * 1.22474487139f + rhomb;

            grad *= (1.0f - 0.042942436724648037f * type) * 3.5946317686139184f;

            return grad;
        }

        private static Float4 Os2NoiseWithDerivativesPart(Float3 X)
        {
            Float3 b = Hlsl.Floor(X);
            Float4 i4 = new Float4(X - b, 2.5f);

            // Pick between each pair of oppposite corners in the cube.
            Float3 v1 = b + Hlsl.Floor(Hlsl.Dot(i4, (Float4)0.25f));
            Float3 v2 = b + new Float3(1.0f, 0.0f, 0.0f) + new Float3(-1.0f, 1.0f, 1.0f) * Hlsl.Floor(Hlsl.Dot(i4, new Float4(-0.25f, 0.25f, 0.25f, 0.35f)));
            Float3 v3 = b + new Float3(0.0f, 1.0f, 0.0f) + new Float3(1.0f, -1.0f, 1.0f) * Hlsl.Floor(Hlsl.Dot(i4, new Float4(0.25f, -0.25f, 0.25f, 0.35f)));
            Float3 v4 = b + new Float3(0.0f, 0.0f, 1.0f) + new Float3(1.0f, 1.0f, -1.0f) * Hlsl.Floor(Hlsl.Dot(i4, new Float4(0.25f, 0.25f, -0.25f, 0.35f)));

            // Gradient hashes for the four vertices in this half-lattice.
            Float4 hashes = Permute(new Float4(v1.X, v2.X, v3.X, v4.X) % 289.0f);
            hashes = Permute(hashes + new Float4(v1.Y, v2.Y, v3.Y, v4.Y) % 289.0f);
            hashes = Permute(hashes + new Float4(v1.Z, v2.Z, v3.Z, v4.Z) % 289.0f) % 48.0f;

            // Gradient extrapolations & kernel function
            Float3 d1 = X - v1; Float3 d2 = X - v2; Float3 d3 = X - v3; Float3 d4 = X - v4;
            Float4 a = Hlsl.Max(0.75f - new Float4(Hlsl.Dot(d1, d1), Hlsl.Dot(d2, d2), Hlsl.Dot(d3, d3), Hlsl.Dot(d4, d4)), 0.0f);
            Float4 aa = a * a; Float4 aaaa = aa * aa;
            Float3 g1 = Grad(hashes.X); Float3 g2 = Grad(hashes.Y);
            Float3 g3 = Grad(hashes.Z); Float3 g4 = Grad(hashes.W);
            Float4 extrapolations = new Float4(Hlsl.Dot(d1, g1), Hlsl.Dot(d2, g2), Hlsl.Dot(d3, g3), Hlsl.Dot(d4, g4));

            // Derivatives of the noise
            // GLSL: mat4x3 * vec4 -> HLSL: mul(Float3x4, Float4)
            // GLSL 构造函数是列优先, HLSL Float3x4 构造函数也是列优先
            Float3x4 m1 = new Float3x4(
                d1.X, d2.X, d3.X, d4.X,
                d1.Y, d2.Y, d3.Y, d4.Y,
                d1.Z, d2.Z, d3.Z, d4.Z
            );

            Float3x4 m2 = new Float3x4(
                g1.X, g2.X, g3.X, g4.X,
                g1.Y, g2.Y, g3.Y, g4.Y,
                g1.Z, g2.Z, g3.Z, g4.Z
            );
            Float3 derivative = -8.0f * Hlsl.Mul(m1, (aa * a * extrapolations))
                                + Hlsl.Mul(m2, aaaa);

            // Return it all as a vec4
            return new Float4(derivative, Hlsl.Dot(aaaa, extrapolations));
        }

        private static Float4 Os2NoiseWithDerivatives_ImproveXY(Float3 X)
        {
            // GLSL: X = orthonormalMap * X -> HLSL: X = mul(M, v)
            X = Hlsl.Mul(OrthonormalMap, X);
            Float4 result = Os2NoiseWithDerivativesPart(X) + Os2NoiseWithDerivativesPart(X + 144.5f);

            // GLSL: result.xyz * orthonormalMap -> HLSL: mul(v, M)
            return new Float4(Hlsl.Mul(result.XYZ, OrthonormalMap), result.W);
        }

        // === GLSL 辅助函数 (HLSL 移植版) ===

        private static float GradientWave(float b, float t)
        {
            return Hlsl.SmoothStep(0.0f, b, t) * Hlsl.SmoothStep(1.0f, b, t);
        }

        private static float Random(Float2 UV, float Seed)
        {
            return Hlsl.Frac(Hlsl.Sin(Hlsl.Dot(UV * 13.235f, new Float2(12.9898f, 78.233f)) * 0.000001f) * 43758.5453123f * Seed);
        }

        private static Float3 RandomVec3(Float2 UV, float Seed)
        {
            return new Float3(Random(UV, Seed), Random(UV * 2.0f, Seed), Random(UV * 3.0f, Seed));
        }

        private static Float3 RaindropSurface(Float2 XY, float DistanceScale, float ZScale)
        {
            float A = DistanceScale;
            float x = XY.X;
            float y = XY.Y;
            float N = 1.5f;
            float M = 0.5f;
            float S = ZScale;

            float TempZ = 1.0f - Hlsl.Pow(x / A, 2.0f) - Hlsl.Pow(y / A, 2.0f);
            float Z = Hlsl.Pow(Hlsl.Max(0.0f, TempZ), A / 2.0f);
            float ZInMAndN = (Z - M) / (N - M);
            float t = Hlsl.Min(Hlsl.Max(ZInMAndN, 0.0f), 1.0f);

            float Height = S * t * t * (3.0f - 2.0f * t);

            float Part01 = S * (6.0f * t - 8.0f * t * t);
            float Part02 = 1.0f / (N - M);
            float Part03 = -1.0f / A * Hlsl.Pow(Hlsl.Max(0.0f, TempZ), A / 2.0f - 1.0f);

            float Part03OfX = x * Part03;
            float Part03OfY = y * Part03;

            float TempValue = (ZInMAndN > 0.0f && ZInMAndN < 1.0f) ? (Part01 * Part02) : 0.0f;

            float PartialDerivativeX = TempValue * Part03OfX;
            float PartialDerivativeY = TempValue * Part03OfY;
            Float2 PartialDerivative = Height > 0.0f ? new Float2(PartialDerivativeX, PartialDerivativeY) : Float2.Zero;

            // C# return vec3(Height, PartialDerivative);
            return new Float3(Height, PartialDerivative);
        }

        private static float MapToRange(float edge0, float edge1, float x)
        {
            float t = Hlsl.Clamp((x - edge0) / (edge1 - edge0), 0.0f, 1.0f);
            return t;
        }

        private static float ProportionalMapToRange(float edge0, float edge1, float x)
        {
            float t = edge0 + (edge1 - edge0) * x;
            return t;
        }

        // === 雨滴主逻辑 (HLSL 移植版) ===

        // 返回: x 是高度; yz 是法线
        private static Float3 StaticRaindrops(Float2 UV, float Time, float UVScale)
        {
            Float2 TempUV = UV;
            TempUV *= UVScale; //15.0

            Float2 ID = Hlsl.Floor(TempUV);
            Float3 RandomValue = RandomVec3(new Float2(ID.X * 470.15f, ID.Y * 653.58f), RandomSeed);
            TempUV = Hlsl.Frac(TempUV) - 0.5f;
            Float2 RandomPoint = (RandomValue.XY - 0.5f) * 0.25f;
            Float2 XY = RandomPoint - TempUV;
            float Distance = Hlsl.Length(TempUV - RandomPoint);

            Float3 X = new Float3(new Float2(TempUV.X * 305.0f * 0.02f, TempUV.Y * 305.0f * 0.02f), 1.8660254037844386f);
            Float4 noiseResult = Os2NoiseWithDerivatives_ImproveXY(X);
            float EdgeRandomCurveAdjust = noiseResult.W * Hlsl.Lerp(0.02f, 0.175f, Hlsl.Frac(RandomValue.X));

            Distance = EdgeRandomCurveAdjust * 0.5f + Distance;
            Distance = Distance * Hlsl.Clamp(Hlsl.Lerp(1.0f, 55.0f, RandomPoint.X), 1.0f, 3.0f);
            // float Height = Hlsl.SmoothStep(0.2f, 0.0f, Distance); // 未在 GLSL 中使用

            float GradientFade = GradientWave(0.0005f, Hlsl.Frac(Time * 0.02f + RandomValue.Z));

            float DistanceMaxRange = 1.45f * GradientFade;
            Float2 Direction = (TempUV - RandomPoint);

            float Theta = 3.141592653f - Hlsl.Acos(Hlsl.Dot(Hlsl.Normalize(Direction), new Float2(0.0f, 1.0f)));
            Theta = Theta * RandomValue.Z;
            float DistanceScale = 0.2f / (1.0f - 0.8f * Hlsl.Cos(Theta - 3.141593f / 2.0f - 1.6f));
            float YDistance = Hlsl.Length(new Float2(0.0f, TempUV.Y) - new Float2(0.0f, RandomPoint.Y));

            // float NewDistance = MapToRange(0.0f, DistanceMaxRange * Hlsl.Pow(DistanceScale, 1.0f), Distance); // 未在 GLSL 中使用

            float Scale = 1.65f * (0.2f + DistanceScale * 1.0f) * DistanceMaxRange * Hlsl.Lerp(1.5f, 0.5f, RandomValue.X);
            Float2 TempXY = new Float2(XY.X * 1.0f, XY.Y) * 4.0f;
            float RandomScale = ProportionalMapToRange(0.85f, 1.35f, RandomValue.Z);
            TempXY.X = RandomScale * Hlsl.Lerp(TempXY.X, TempXY.X / Hlsl.SmoothStep(1.0f, 0.4f, YDistance * RandomValue.Z), Hlsl.SmoothStep(1.0f, 0.0f, RandomValue.X));
            TempXY = TempXY + EdgeRandomCurveAdjust * 1.0f;
            Float3 HeightAndNormal = RaindropSurface(TempXY, Scale, 1.0f);
            HeightAndNormal.YZ = -HeightAndNormal.YZ;

            float RandomVisible = (Hlsl.Frac(RandomValue.Z * 10.0f * RandomSeed) < NumberScaleOfStaticRaindrops ? 1.0f : 0.0f);
            HeightAndNormal.YZ = HeightAndNormal.YZ * RandomVisible;
            HeightAndNormal.X = Hlsl.SmoothStep(0.0f, 1.0f, HeightAndNormal.X) * RandomVisible;

            return HeightAndNormal;
        }

        // 返回: x 是高度; yz 是法线; w 是轨迹.
        private static Float4 RollingRaindrops(Float2 UV, float Time, float UVScale)
        {
            Float2 LocalUV = UV * UVScale;
            Float2 TempUV = LocalUV;

            Float2 ConstantA = new Float2(6.0f, 1.0f);
            Float2 GridNum = ConstantA * 2.0f;
            Float2 GridID = Hlsl.Floor(LocalUV * GridNum);

            float RandomFloat = Random(new Float2(GridID.X * 131.26f, GridID.X * 101.81f), RandomSeed);

            float TimeMovingY = Time * 0.85f * ProportionalMapToRange(0.1f, 0.25f, RandomFloat); //Time
            LocalUV.Y += TimeMovingY;
            float YShift = RandomFloat;
            LocalUV.Y += YShift;


            Float2 ScaledUV = LocalUV * GridNum;
            GridID = Hlsl.Floor(ScaledUV);
            Float3 randomVec3 = RandomVec3(new Float2(GridID.X * 17.32f, GridID.Y * 2217.54f), RandomSeed);

            Float2 GridUV = Hlsl.Frac(ScaledUV) - new Float2(0.5f, 0.0f);


            float SwingX = randomVec3.X - 0.5f;

            float SwingY = TempUV.Y * 20.0f;
            float SwingPosition = Hlsl.Sin(SwingY + Hlsl.Sin(GridID.Y * randomVec3.Z + SwingY) + GridID.Y * randomVec3.Z);
            SwingX += SwingPosition * (0.5f - Hlsl.Abs(SwingX)) * (randomVec3.Z - 0.5f);
            SwingX *= 0.65f;
            float RandomNormalizedTime = Hlsl.Frac(TimeMovingY + randomVec3.Z) * 1.0f; // Time
            SwingY = (GradientWave(0.87f, RandomNormalizedTime) - 0.5f) * 0.9f + 0.5f;
            SwingY = Hlsl.Clamp(SwingY, 0.15f, 0.85f);
            Float2 Position = new Float2(SwingX, SwingY);


            Float2 XY = Position - GridUV;
            Float2 Direction = (GridUV - Position) * ConstantA.YX;
            float Distance = Hlsl.Length(Direction);

            //---------
            Float3 X = new Float3(new Float2(TempUV.X * 513.20f * 0.02f, TempUV.Y * 779.40f * 0.02f), 2.1660251037743386f);
            Float4 NoiseResult = Os2NoiseWithDerivatives_ImproveXY(X);
            float EdgeRandomCurveAdjust = NoiseResult.W * Hlsl.Lerp(0.02f, 0.175f, Hlsl.Frac(randomVec3.Y));

            Distance = EdgeRandomCurveAdjust + Distance;
            // float Height = Hlsl.SmoothStep(0.2f, 0.0f, Distance); // 未在 GLSL 中使用
            // float NewDistance = MapToRange(0.0f, 0.2f, Distance); // 未在 GLSL 中使用


            float DistanceMaxRange = 1.45f;

            float Theta = 3.141592653f - Hlsl.Acos(Hlsl.Dot(Hlsl.Normalize(Direction), new Float2(0.0f, 1.0f)));
            Theta = Theta * randomVec3.Z;
            float DistanceScale = 0.2f / (1.0f - 0.8f * Hlsl.Cos(Theta - 3.141593f / 2.0f - 1.6f));
            float Scale = 1.65f * (0.2f + DistanceScale * 1.0f) * DistanceMaxRange * Hlsl.Lerp(1.0f, 0.25f, randomVec3.X * 1.0f);
            Float2 TempXY = new Float2(XY.X * 1.0f, XY.Y) * 4.0f;
            // float RandomScale = ProportionalMapToRange(0.85f, 1.35f, RandomVec3.Z); // 未在 GLSL 中使用
            TempXY = TempXY * new Float2(1.0f, 4.2f) + EdgeRandomCurveAdjust * 0.85f;
            Float3 HeightAndNormal = RaindropSurface(TempXY, Scale, 1.0f);

            //----------

            // Trail
            float TrailY = Hlsl.Pow(Hlsl.SmoothStep(1.0f, SwingY, GridUV.Y), 0.5f);
            float TrailX = Hlsl.Abs(GridUV.X - SwingX) * Hlsl.Lerp(0.8f, 4.0f, Hlsl.SmoothStep(0.0f, 1.0f, randomVec3.X));
            float Trail = Hlsl.SmoothStep(0.25f * TrailY, 0.15f * TrailY * TrailY, TrailX);
            float TrailClamp = Hlsl.SmoothStep(-0.02f, 0.02f, GridUV.Y - SwingY);
            Trail *= TrailClamp * TrailY;

            float SignOfTrailX = Hlsl.Sign(GridUV.X - SwingX);
            Float3 NoiseInput = new Float3(new Float2(TempUV.X * 513.20f * 0.02f * SignOfTrailX, TempUV.Y * 779.40f * 0.02f), 2.1660251037743386f);
            Float4 TrailNoiseResult = Os2NoiseWithDerivatives_ImproveXY(NoiseInput);
            float TrailEdgeRandomCurveAdjust = TrailNoiseResult.W * Hlsl.Lerp(0.002f, 0.175f, Hlsl.Frac(randomVec3.Y));
            float TrailXDistance = MapToRange(0.0f, 0.1f, TrailEdgeRandomCurveAdjust * 0.5f + TrailX);
            Float2 TrailDirection = SignOfTrailX * new Float2(1.0f, 0.0f) + new Float2(0.0f, 1.0f) * Hlsl.SmoothStep(1.0f, 0.0f, Trail) * 0.5f;
            Float2 TrailXY = TrailDirection * 1.0f * TrailXDistance;

            Float3 TrailHeightAndNormal = RaindropSurface(TrailXY, 1.0f, 1.0f);

            TrailHeightAndNormal = TrailHeightAndNormal * Hlsl.Pow(Trail * randomVec3.Y, 2.0f);
            TrailHeightAndNormal.X = Hlsl.SmoothStep(0.0f, 1.0f, TrailHeightAndNormal.X);

            // Remain Trail Droplets
            SwingY = TempUV.Y;
            float RemainTrail = Hlsl.SmoothStep(0.2f * TrailY, 0.0f, TrailX);
            float RemainDroplet = Hlsl.Max(0.0f, (Hlsl.Sin(SwingY * (1.0f - SwingY) * 120.0f) - GridUV.Y)) * RemainTrail * TrailClamp * randomVec3.Z;
            SwingY = Hlsl.Frac(SwingY * 10.0f) + (GridUV.Y - 0.5f);
            Float2 RemainDropletXY = GridUV - new Float2(SwingX, SwingY);
            RemainDropletXY = RemainDropletXY * new Float2(1.2f, 0.8f);

            RemainDropletXY = RemainDropletXY + EdgeRandomCurveAdjust * 0.85f;
            Float3 RemainDropletHeightAndNormal = RaindropSurface(RemainDropletXY, 2.0f * RemainDroplet, 1.0f);

            RemainDropletHeightAndNormal.X = Hlsl.SmoothStep(0.0f, 1.0f, RemainDropletHeightAndNormal.X);
            RemainDropletHeightAndNormal = TrailHeightAndNormal.X > 0.0f ? Float3.Zero : RemainDropletHeightAndNormal;


            Float4 ReturnValue = new();

            ReturnValue.X = HeightAndNormal.X + TrailHeightAndNormal.X * TrailY * TrailClamp + RemainDropletHeightAndNormal.X * TrailY * TrailClamp;
            ReturnValue.YZ = HeightAndNormal.YZ + TrailHeightAndNormal.YZ + RemainDropletHeightAndNormal.YZ;
            ReturnValue.W = Trail;


            float RandomVisible = (Hlsl.Frac(randomVec3.Z * 20.0f * RandomSeed) < NumberScaleOfRollingRaindrops ? 1.0f : 0.0f);
            ReturnValue *= RandomVisible;
            return ReturnValue;
        }

        private static Float4 Raindrops(Float2 UV, float Time, float UVScale00, float UVScale01, float UVScale02)
        {
            Float3 StaticRaindrop = StaticRaindrops(UV, Time, UVScale00);
            Float4 RollingRaindrop01 = RollingRaindrops(UV, Time, UVScale01);
            //Float4 RollingRaindrop02 = RollingRaindrops(UV * 1.7f, Time, UVScale02); // 在 GLSL 中被注释掉了

            float Height = StaticRaindrop.X + RollingRaindrop01.X; // + RollingRaindrop02.x;
            Float2 Normal = StaticRaindrop.YZ + RollingRaindrop01.YZ; // + RollingRaindrop02.yz;
            float Trail = RollingRaindrop01.W; //(RollingRaindrop01.w + RollingRaindrop02.w)*0.5;

            // C# return vec4(Height, Normal, Trail);
            return new Float4(Height, Normal, Trail);
        }

        /// <summary>
        /// 着色器主入口点 (替换 GLSL 'mainImage')
        /// </summary>
        public Float4 Execute()
        {
            float Time = time; // 来自构造函数
            Float2 scenePos = D2D.GetScenePosition().XY;

            // 采样背景纹理
            Float4 backgroundColor = D2D.GetInput(0); // 假设输入0是背景

            // 暂时先返回背景颜色，确保背景能正确显示
            return backgroundColor;

            // 对应 GLSL 'vec2 LocalUV = (fragCoord.xy-.5*iResolution.xy) / iResolution.y;'
            // GLSL fragCoord Y 轴在底部
            Float2 fragCoord = new Float2(scenePos.X, dispatchSize.Y - scenePos.Y);
            Float2 LocalUV = (fragCoord - (0.5f * dispatchSize)) / dispatchSize.Y;

            // --- 移除了所有与背景模糊相关的 GLSL 逻辑 ---

            // 计算雨滴数据
            Float4 Raindrop = Raindrops(LocalUV, Time,
                StaticRaindropUVScale,
                RollingRaindropUVScaleLayer01,
                RollingRaindropUVScaleLayer02);

            // --- 移除了法线和背景采样逻辑 ---

            // **新的输出逻辑**
            // 我们只关心雨滴的高度 (Raindrop.x)，它包含了静态雨滴、滚动雨滴和轨迹。
            // 我们将这个高度图作为 Alpha 通道。
            float alpha = Hlsl.Saturate(Raindrop.X); // Hlsl.Saturate 确保值在 [0, 1] 范围

            // Win2D/D2D 使用预乘 Alpha。
            // 我们输出白色的雨滴 (1, 1, 1)。
            // (R, G, B) = (1.0 * alpha, 1.0 * alpha, 1.0 * alpha)
            // (A) = alpha
            // 最终结果是 (alpha, alpha, alpha, alpha)
            return new Float4(alpha, alpha, alpha, alpha);
        }
    }
}
