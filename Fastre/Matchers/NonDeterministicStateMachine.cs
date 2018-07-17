using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;

namespace Fastre
{
    public interface IMatcher<T>
    {
        bool Accepts(ReadOnlySpan<T> input);
    }

    public sealed class NonDeterministicStateMachine<T, TState>
        where TState : IEquatable<TState>
    {
        private readonly HashSet<TState> _initialStates;
        private readonly HashSet<TState> _acceptingStates;
        private readonly Func<T, TState, TState> _transitionFunction;

        public NonDeterministicStateMachine(
            IEnumerable<TState> initialStates,
            Func<T, TState, TState> transitionFunction,
            IEnumerable<TState> acceptingStates)
        {
            _initialStates = new HashSet<TState>(initialStates);
            _transitionFunction = transitionFunction;
            _acceptingStates = new HashSet<TState>(acceptingStates);
        }

        public bool Accepts(ReadOnlySpan<T> input)
        {
            var states = new HashSet<TState>(_initialStates);
            var statesAlt = new HashSet<TState>();

            for (int i = 0; i < input.Length; ++i)
            {
                var x = input[i];
                statesAlt.UnionWith(states.Select(s => _transitionFunction(x, s)));

                (states, statesAlt) = (statesAlt, states);
                statesAlt.Clear();
            }

            states.IntersectWith(_acceptingStates);

            return states.Count > 0;
        }
    }

    internal sealed class StringAsBytesMatcher : IMatcher<char>
    {
        private readonly IMatcher<byte> _inner;

        public StringAsBytesMatcher(IMatcher<byte> inner)
            => _inner = inner;

        public bool Accepts(ReadOnlySpan<char> input)
            => _inner.Accepts(MemoryMarshal.AsBytes(input));
    }
}
