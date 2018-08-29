using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Text;
using System.Text.RegularExpressions;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Attributes.Jobs;
using BenchmarkDotNet.Running;

namespace Fastre
{
    [CoreJob]
    [DisassemblyDiagnoser]
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
        private static readonly VectoredMatcher<Vector128Impl, Vector128<sbyte>> _vectoredMatcher128 = new VectoredMatcher<Vector128Impl, Vector128<sbyte>>(Start, 2, TransitionFunc, Accept);
        private static readonly VectoredMatcher<Vector256Impl, Vector128<sbyte>> _vectoredMatcher256 = new VectoredMatcher<Vector256Impl, Vector128<sbyte>>(Start, 2, TransitionFunc, Accept);


        private static readonly string Aaa = new string('a', 30);
        private static readonly Regex AaaRegex = new Regex("(?:a?){30}a{30}", RegexOptions.ExplicitCapture);
        private static readonly Regex AaaRegexCompiled = new Regex("(?:a?){30}a{30}", RegexOptions.ExplicitCapture|RegexOptions.Compiled);
        private static readonly Regex<char> AaaRegexMineSlow =
            (Enumerable.Repeat("a".Literal().Question(), 30).Then() * Aaa.Literal());

        //private static readonly IMatcher<char> AaaRegexMineCompiled = AaaRegexMineSlow.Compile(RegexExtensions.AllChars);
        private static readonly IMatcher<char> AaaRegexMineCompiledBytes = AaaRegexMineSlow.Compile();

        private byte[] Data;

        [GlobalSetup]
        public void Setup()
        {
            Data = Encoding.UTF8.GetBytes("/*" + new string(' ', N-4) + "*/");
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

        [Params(100_000)]
        public int N;

        //[Benchmark]
        //public bool Direct() => _direct.Run(Data);

        //[Benchmark]
        //public bool Lookup() => _lookup.Accepts(Data);

        //[Benchmark]
        //public bool Monoidal() => _monoidal.Accepts(Data);

        //[Benchmark]
        //public bool MonoidalUnsafe() => _monoidalUnsafe.Accepts(Data);

        //[Benchmark]
        //public bool MonoidalILP() => _monoidalILP.Accepts(Data);

        //[Benchmark]
        //public bool MonoidalILPUnsafe() => _monoidalILPUnsafe.Accepts(Data);

        [Benchmark]
        public bool VectoredMatcher128() => _vectoredMatcher128.Accepts(Data);

        [Benchmark]
        public bool VectoredMatcher256() => _vectoredMatcher256.Accepts(Data);

        //[Benchmark]
        //public bool PlainRegex() => AaaRegex.IsMatch(Aaa);

        //[Benchmark]
        //public bool RegexCompiled() => AaaRegexCompiled.IsMatch(Aaa);

        //[Benchmark]
        //public bool MyRegexSlow() => AaaRegexMineSlow.Accepts(Aaa);

        //[Benchmark]
        //public bool MyRegexCompiled() => AaaRegexMineCompiled.Accepts(Aaa);

        //[Benchmark]
        //public bool MyRegexCompiledBytes() => AaaRegexMineCompiledBytes.Accepts(Aaa);

        public static void Main(string[] args)
        {
            //var p = new Program();
            //p.N = 1000;
            //p.Setup();
            //Console.WriteLine(p.VectoredMatcher128());
            //Console.WriteLine(p.VectoredMatcher256());
            var summary = BenchmarkRunner.Run<Program>();
        }
    }
}
