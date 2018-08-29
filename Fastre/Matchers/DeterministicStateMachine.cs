using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;

namespace Fastre
{
    public sealed class DeterministicStateMachine<T> : IMatcher<T>
    {
        private readonly int _initialState;
        private readonly int _stateCount;
        private readonly HashSet<int> _acceptingStates;
        private readonly Func<T, int, int> _transitionFunction;

        public DeterministicStateMachine(
            int initialState,
            int stateCount,
            Func<T, int, int> transitionFunction,
            IEnumerable<int> acceptingStates)
        {
            _initialState = initialState;
            _stateCount = stateCount;
            _transitionFunction = transitionFunction;
            _acceptingStates = new HashSet<int>(acceptingStates);
        }

        public IMatcher<T> Optimized(CompilationOptions options)
        {
            if (options.HasFlag(CompilationOptions.NoOptimization))
            {
                return this;
            }

            if (typeof(T) == typeof(byte))
            {
                if (!options.HasFlag(CompilationOptions.NoSse))
                {
                    if (VectoredMatcher<Vector128Impl, Vector128<sbyte>>.CanBeUsed(_stateCount))
                    {
                        return (IMatcher<T>)(object)new VectoredMatcher<Vector128Impl, Vector128<sbyte>>(
                            (sbyte)_initialState,
                            _stateCount,
                            (input, state) => (sbyte)_transitionFunction((T)(object)input, state),
                            _acceptingStates.Select(x => (sbyte)x).ToArray());
                    }
                }

                if (LookupStateMachine.CanBeUsed(_stateCount))
                {
                    return (IMatcher<T>)(object)new LookupStateMachine(
                        _initialState,
                        _stateCount,
                        (input, state) => _transitionFunction((T)(object)input, state),
                        _acceptingStates);
                }
            }

            return this;
        }

        public bool Accepts(ReadOnlySpan<T> input)
        {
            var state = _initialState;

            for (int i = 0; i < input.Length; ++i)
            {
                state = _transitionFunction(input[i], state);
            }

            return _acceptingStates.Contains(state);
        }
    }
}
