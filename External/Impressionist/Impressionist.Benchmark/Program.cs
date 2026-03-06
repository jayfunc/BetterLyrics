using BenchmarkDotNet.Running;

namespace Impressionist.Benchmark
{
    public class Program
    {
        static void Main(string[] args)
        {
            BenchmarkRunner.Run<BenchMark>();
        }
    }
}
