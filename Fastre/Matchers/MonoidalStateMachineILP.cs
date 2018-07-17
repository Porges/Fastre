using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Fastre
{
    public sealed class MonoidalStateMachineILP : IMatcher<byte>
    {
        private readonly sbyte _start;
        private readonly Vector128<sbyte>[] _transitions = new Vector128<sbyte>[byte.MaxValue + 1];
        private readonly sbyte[] _accept;

        public unsafe MonoidalStateMachineILP(sbyte start, Func<byte, sbyte, sbyte> transitionFunc, ReadOnlySpan<sbyte> accept)
        {
            _start = start;
            _accept = accept.ToArray();

            Span<sbyte> transition = stackalloc sbyte[16];
            fixed (sbyte* bytes = transition)
            for (int i = 0; i <= byte.MaxValue; ++i)
            {
                for (int j = 0; j < 16; ++j)
                {
                    transition[j] = transitionFunc((byte)i, (sbyte)j);
                }

                _transitions[i] = Sse2.LoadVector128(bytes);
            }
        }

        public bool Accepts(ReadOnlySpan<byte> input)
        {
            var transition = default(Vector128Impl).Id;

            int i = 0;
            for (; i + 6 < input.Length; i += 7)
            {
                var t1 = _transitions[input[i]];
                var t2 = _transitions[input[i+1]];
                var t3 = _transitions[input[i+2]];
                var t4 = _transitions[input[i+3]];
                var t5 = _transitions[input[i+4]];
                var t6 = _transitions[input[i+5]];
                var t7 = _transitions[input[i+6]];

                var t01 = Ssse3.Shuffle(t1, transition);
                var t23 = Ssse3.Shuffle(t3, t2);
                var t45 = Ssse3.Shuffle(t5, t4);
                var t67 = Ssse3.Shuffle(t7, t6);

                var t0123 = Ssse3.Shuffle(t23, t01);
                var t4567 = Ssse3.Shuffle(t67, t45);

                transition = Ssse3.Shuffle(t4567, t0123);
            }

            for (; i < input.Length; ++i)
            {
                transition = Ssse3.Shuffle(_transitions[input[i]], transition);
            }

            var state = Sse41.Extract(transition, (byte)_start);

            bool found = false;
            for (int j = 0; j < _accept.Length; ++j)
            {
                found = found | (_accept[j] == state);
            }

            return found;
        }
    }
}
