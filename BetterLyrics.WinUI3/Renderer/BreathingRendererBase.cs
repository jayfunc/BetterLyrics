using Microsoft.Graphics.Canvas;
using System.Numerics;

namespace BetterLyrics.WinUI3.Renderer
{
    public abstract class BreathingRendererBase
    {
        // 缩放状态
        protected float _currentScale = 1.0f;
        private float _targetScale = 1.0f;

        /// <summary>
        /// 根据低音能量更新呼吸缩放值
        /// </summary>
        /// <param name="bassEnergy">低音能量 (0.0 - 1.0)</param>
        /// <param name="intensity">呼吸强度 (0 - 100)</param>
        public virtual void UpdateBreathing(float bassEnergy, int intensity)
        {
            if (intensity <= 0)
            {
                _currentScale = 1.0f;
                return;
            }

            float maxScaleOffset = intensity / 100.0f;
            _targetScale = 1.0f + (bassEnergy * maxScaleOffset);

            if (_targetScale > _currentScale)
            {
                // 鼓点出现，快速放大 (Attack)
                _currentScale += (_targetScale - _currentScale) * 0.2f;
            }
            else
            {
                // 鼓点消失，缓慢回落 (Decay)
                _currentScale += (_targetScale - _currentScale) * 0.05f;
            }
        }

        /// <summary>
        /// 应用呼吸缩放变换到画布会话
        /// </summary>
        /// <param name="ds">绘图会话</param>
        /// <param name="center">缩放中心点</param>
        /// <param name="isEnabled">是否启用效果</param>
        protected void ApplyBreathingTransform(CanvasDrawingSession ds, Vector2 center, bool isEnabled)
        {
            if (isEnabled && _currentScale > 1.0f)
            {
                ds.Transform = Matrix3x2.CreateScale(_currentScale, center);
            }
        }

        /// <summary>
        /// 重置画布变换（绘制结束后调用）
        /// </summary>
        protected void ResetTransform(CanvasDrawingSession ds, bool isEnabled)
        {
            if (isEnabled)
            {
                ds.Transform = Matrix3x2.Identity;
            }
        }
    }
}