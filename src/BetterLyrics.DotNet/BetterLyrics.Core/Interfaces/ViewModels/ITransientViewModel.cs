namespace BetterLyrics.Core.Interfaces.ViewModels;

/// <summary>
/// 标记接口：生命周期跟随视图实例的 ViewModel。
/// 只有实现该接口的 ViewModel 会在视图关闭时被 MemoryLeakDetector 跟踪，
/// 应用级单例 ViewModel 不会被误报为泄漏。
/// </summary>
public interface ITransientViewModel
{
}
