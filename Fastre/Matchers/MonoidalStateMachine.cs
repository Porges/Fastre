using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Fastre
{
    public sealed class MonoidalStateMachine : IMatcher<byte>
    {
        private readonly sbyte _start;
        private readonly Vector128<sbyte>[] _transitions = new Vector128<sbyte>[byte.MaxValue + 1];
        private readonly sbyte[] _accept;

        public unsafe MonoidalStateMachine(sbyte start, int stateCount, Func<byte, sbyte, sbyte> transitionFunc, ReadOnlySpan<sbyte> accept)
        {
            _start = start;
            _accept = accept.ToArray();

            Span<sbyte> transition = stackalloc sbyte[16];
            fixed (sbyte* bytes = transition)
            for (int i = 0; i <= byte.MaxValue; ++i)
            {
                for (int j = 0; j < stateCount; ++j)
                {
                    transition[j] = transitionFunc((byte)i, (sbyte)j);
                }

                _transitions[i] = Sse2.LoadVector128(bytes);
            }
        }

        public bool Accepts(ReadOnlySpan<byte> input)
        {
            var transition = default(Vector128Impl).Id;
            for (int i = 0; i < input.Length; ++i)
            {
                transition = Ssse3.Shuffle(_transitions[input[i]], transition);
            }

            var state = Sse41.Extract(transition, (byte)_start);

            bool found = false;
            for (int i = 0; i < _accept.Length; ++i)
            {
                found = found | (_accept[i] == state);
            }

            return found;
        }
    }
}
