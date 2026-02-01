using System;
using System.Threading;
using System.Threading.Tasks;

namespace BetterLyrics.WinUI3.Helper
{
    public static class STATaskHelper
    {
        /// <summary>
        /// 在一个专用的后台 STA 线程上运行一个函数。
        /// </summary>
        /// <typeparam name="TResult">返回类型</typeparam>
        /// <param name="func">要执行的函数</param>
        /// <returns>一个 Task，其结果是函数的返回值</returns>
        public static Task<TResult> RunAsSTATask<TResult>(Func<TResult> func)
        {
            var tcs = new TaskCompletionSource<TResult>();
            var thread = new Thread(() =>
            {
                try
                {
                    var result = func();
                    tcs.SetResult(result);
                }
                catch (Exception e)
                {
                    tcs.SetException(e);
                }
            });

            // 设置单元状态为 STA
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();

            return tcs.Task;
        }

        /// <summary>
        /// 在一个专用的后台 STA 线程上运行一个 Action。
        /// </summary>
        /// <param name="action">要执行的 Action</param>
        /// <returns>一个 Task</returns>
        public static Task RunAsSTATask(Action action)
        {
            return RunAsSTATask(() =>
            {
                action();
                return true; // 返回一个虚拟结果
            });
        }
    }
}
