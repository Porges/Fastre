using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Fastre
{
    public static class RegexExtensions
    {
        public static Regex<char> Literal(this string s)
            => Literal(s.AsMemory());

        public static Regex<T> Literal<T>(this ReadOnlyMemory<T> s)
            where T : IEquatable<T>
            => Regex<T>.Block.Create(s);

        public static Regex<T> Or<T>(this Regex<T> left, Regex<T> right)
            where T : IEquatable<T>
            => Regex<T>.Or.Create(left, right);

        public static Regex<T> And<T>(this Regex<T> left, Regex<T> right)
            where T : IEquatable<T>
            => Regex<T>.And.Create(left, right);

        public static Regex<T> Not<T>(this Regex<T> regex)
            where T : IEquatable<T>
            => Regex<T>.Not.Create(regex);

        public static Regex<T> Then<T>(this Regex<T> left, Regex<T> right)
            where T : IEquatable<T>
            => Regex<T>.Seq.Create(left, right);

        public static Regex<T> Star<T>(this Regex<T> regex)
            where T : IEquatable<T>
            => Regex<T>.Star.Create(regex);

        private static readonly byte[] AllBytes = Enumerable.Range(0, 256).Select(x => (byte)x).ToArray();
        public static IMatcher<byte> Compile(this Regex<char> regex, Encoding encoding, CompilationOptions options = default)
        {
            var mapped = regex.SelectMany(c => encoding.GetBytes(new[] { c }));
            return mapped.Compile(AllBytes, options);
        }

        public static IMatcher<char> Compile(this Regex<char> regex, CompilationOptions options = default)
        {
            var mapped = regex.SelectMany(c => Encoding.Unicode.GetBytes(new[] { c }));
            var matcher = mapped.Compile(AllBytes, options);
            return new StringAsBytesMatcher(matcher);
        }

        public static IMatcher<T> Compile<T>(this Regex<T> regex, IEnumerable<T> alphabet, CompilationOptions options = default)
            where T : IEquatable<T>
        {
            var explored = new Dictionary<Regex<T>, int> { { regex, 0 } };
            var transitions = new Dictionary<(T, int), int>();
            var toExplore = new Queue<(Regex<T> regex, int state)>();

            toExplore.Enqueue((regex, 0));

            while (toExplore.TryDequeue(out var exploring))
            {
                foreach (var item in alphabet)
                {
                    var diffed = exploring.regex.Diff(item);

                    if (explored.TryGetValue(diffed, out var state))
                    {
                        transitions[(item, exploring.state)] = state;
                    }
                    else
                    {
                        var newState = explored.Count;
                        explored[diffed] = newState;
                        transitions[(item, exploring.state)] = newState;
                        toExplore.Enqueue((diffed, newState));
                    }
                }
            }

            var result = new DeterministicStateMachine<T>(
                0,
                explored.Count,
                (item, state) => transitions[(item, state)],
                explored.Where(kvp => kvp.Key.AcceptsEmptyString).Select(kvp => kvp.Value));

            return result.Optimized(options);
        }
    }
}