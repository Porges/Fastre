using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Running;
using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Text;

namespace Fastre
{
    [CoreJob]
    public class Program
    {
        private static readonly sbyte Start = 0;
        private static readonly sbyte[] Accept = { 0, 1 };

        private static readonly DirectStateMachine _direct = new DirectStateMachine(Start, TransitionFunc, Accept);
        private static readonly LookupStateMachine _lookup = new LookupStateMachine(Start, 2, (input, state) => TransitionFunc(input, (sbyte)state), Accept.Select(x => (int)x).ToHashSet());
        private static readonly MonoidalStateMachine _monoidal = new MonoidalStateMachine(Start, 2, TransitionFunc, Accept);
        private static readonly MonoidalStateMachineUnsafe _monoidalUnsafe = new MonoidalStateMachineUnsafe(Start, TransitionFunc, Accept);
        private static readonly MonoidalStateMachineILP _monoidalILP = new MonoidalStateMachineILP(Start, TransitionFunc, Accept);
        private static readonly MonoidalStateMachineILPUnsafe _monoidalILPUnsafe = new MonoidalStateMachineILPUnsafe(Start, TransitionFunc, Accept);
        private static readonly MonoidalStateMachineILPUnsafe2<Vector128Impl, Vector128<sbyte>> _monoidalILPUnsafe2 = new MonoidalStateMachineILPUnsafe2<Vector128Impl, Vector128<sbyte>>(Start, 2, TransitionFunc, Accept);

        private byte[] Data;

        [GlobalSetup]
        public void Setup()
        {
            Data = Encoding.UTF8.GetBytes("/*" + new string(' ', N) + "*/");
        }

        private static sbyte TransitionFunc(byte input, sbyte state)
        {
            switch (state)
            {
                case 0:
                    return (sbyte)(input == '/' ? 1 : 0);

                case 1:
                    return (sbyte)(input == '*' ? 2 :
                                   input == '/' ? 1 : 0);

                case 2:
                    return (sbyte)(input == '*' ? 3 : 2);

                case 3:
                    return (sbyte)(input == '/' ? 0 :
                                   input == '*' ? 3 : 2);

                default: return 0;
            }
        }

        [Params(/*1_000, 10_000, */100_000)]
        public int N;

        [Benchmark]
        public bool Direct() => _direct.Run(Data);

        [Benchmark]
        public bool Lookup() => _lookup.Accepts(Data);

        [Benchmark]
        public bool Monoidal() => _monoidal.Accepts(Data);

        [Benchmark]
        public bool MonoidalUnsafe() => _monoidalUnsafe.Accepts(Data);

        [Benchmark]
        public bool MonoidalILP() => _monoidalILP.Accepts(Data);

        [Benchmark]
        public bool MonoidalILPUnsafe() => _monoidalILPUnsafe.Accepts(Data);

        [Benchmark]
        public bool MonoidalILPUnsafe2() => _monoidalILPUnsafe2.Accepts(Data);

        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<Program>();
        }
    }
}
