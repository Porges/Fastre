using System;
using System.Collections.Generic;
using System.Linq;

namespace Fastre
{
    public sealed class LookupStateMachine : IMatcher<byte>
    {
        private readonly byte _start;
        private readonly byte[,] _transitions;
        private readonly byte[] _accept;

        public LookupStateMachine(int start, int stateCount, Func<byte, int, int> transitionFunc, HashSet<int> accept)
        {
            _start = checked((byte)start);
            _accept = accept.Select(x => checked((byte)x)).ToArray();
            _transitions = new byte[byte.MaxValue + 1, stateCount];

            for (int i = 0; i <= byte.MaxValue; ++i)
            {
                for (int j = 0; j < stateCount; ++j)
                {
                    _transitions[i, j] = checked((byte)transitionFunc((byte)i, j));
                }
            }
        }

        public static bool CanBeUsed(int stateCount)
            => stateCount <= byte.MaxValue + 1;

        public bool Accepts(ReadOnlySpan<byte> input)
        {
            var state = _start;
            for (int i = 0; i < input.Length; ++i)
            {
                state = _transitions[input[i], state];
            }

            bool found = false;
            for (int i = 0; i < _accept.Length; ++i)
            {
                found = found | (_accept[i] == state);
            }

            return found;
        }
    }
}
