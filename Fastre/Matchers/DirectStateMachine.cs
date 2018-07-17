using System;

namespace Fastre
{
    public sealed class DirectStateMachine
    {
        private readonly sbyte _start;
        private readonly Func<byte, sbyte, sbyte> _transition;
        private readonly sbyte[] _accept;

        public DirectStateMachine(sbyte start, Func<byte, sbyte, sbyte> transitionFunc, ReadOnlySpan<sbyte> accept)
        {
            _start = start;
            _transition = transitionFunc;
            _accept = accept.ToArray();
        }

        public bool Run(ReadOnlySpan<byte> input)
        {
            var state = _start;
            for (int i = 0; i < input.Length; ++i)
            {
                state = _transition(input[i], state);
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
