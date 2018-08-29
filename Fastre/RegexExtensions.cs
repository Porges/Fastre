using System;
using System.Collections.Generic;
using System.Diagnostics;
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

        public static Regex<char> Literal(this char s)
            => Regex<char>.Just(s);

        public static Regex<T> Or<T>(this Regex<T> left, Regex<T> right)
            where T : IEquatable<T>
            => Regex<T>.Or.Create(left, right);

        public static Regex<T> Or<T>(this IEnumerable<Regex<T>> regexes)
            where T : IEquatable<T>
            => Regex<T>.Or.Create(regexes);

        public static Regex<T> And<T>(this Regex<T> left, Regex<T> right)
            where T : IEquatable<T>
            => Regex<T>.And.Create(left, right);

        public static Regex<T> And<T>(this IEnumerable<Regex<T>> regexes)
            where T : IEquatable<T>
            => Regex<T>.And.Create(regexes);

        public static Regex<T> Not<T>(this Regex<T> regex)
            where T : IEquatable<T>
            => Regex<T>.Not.Create(regex);

        public static Regex<T> Then<T>(this Regex<T> left, Regex<T> right)
            where T : IEquatable<T>
            => Regex<T>.Seq.Create(left, right);

        public static Regex<T> Then<T>(this IEnumerable<Regex<T>> regexes)
            where T : IEquatable<T>
            => regexes.Aggregate((l, r) => l * r);

        public static Regex<T> Star<T>(this Regex<T> regex)
            where T : IEquatable<T>
            => Regex<T>.Star.Create(regex);

        public static Regex<T> Plus<T>(this Regex<T> regex)
            where T : IEquatable<T>
            => regex * ~regex;

        public static Regex<T> Question<T>(this Regex<T> regex)
            where T : IEquatable<T>
            => regex + Regex<T>.Epsilon;

        public static Regex<char> Parse(string regex)
            => Parse(regex.AsMemory());

        public static Regex<char> Parse(ReadOnlyMemory<char> regex)
        {
            var stack = new Stack<Regex<char>>();

            foreach (var c in ParseTokens(regex))
            {
                switch (c)
                {
                case '.':
                    {
                        var r = stack.Pop();
                        var l = stack.Pop();
                        stack.Push(l * r);
                    }
                    break;

                case '|':
                    stack.Push(stack.Pop() + stack.Pop());
                    break;

                case '?':
                    stack.Push(stack.Pop().Question());
                    break;

                case '+':
                    stack.Push(stack.Pop().Plus());
                    break;

                case '*':
                    stack.Push(stack.Pop().Star());
                    break;

                default:
                    stack.Push(Regex<char>.Just(c));
                    break;
                }
            }

            Debug.Assert(stack.Count == 1);

            return stack.Pop();
        }

        public static IEnumerable<char> ParseTokens(ReadOnlyMemory<char> regex)
        {
            // sorry about this, it's a copy of Russ Cox's C implementation

            var parens = new Stack<(int natoms, int nalts)>();
            var natom = 0;
            var nalt = 0;

            for (int i = 0; i < regex.Length; ++i)
            {
                var c = regex.Span[i];
                switch (c)
                {
                case '(':
                    if (natom > 1)
                    {
                        --natom;
                        yield return '.';
                    }

                    parens.Push((natom, nalt));
                    natom = nalt = 0;
                    break;

                case '|':
                    if (natom == 0)
                    {
                        throw new ArgumentException(nameof(regex));
                    }

                    while (--natom > 0)
                    {
                        yield return '.';
                    }

                    ++nalt;
                    break;


                case ')':
                    if (parens.Count == 0)
                    {
                        throw new ArgumentException(nameof(regex));
                    }

                    if (natom == 0)
                    {
                        throw new ArgumentException(nameof(regex));
                    }

                    while (--natom > 0)
                    {
                        yield return '.';
                    }

                    for (; nalt > 0; --nalt)
                    {
                        yield return '|';
                    }

                    (natom, nalt) = parens.Pop();
                    ++natom;
                    break;

                case '*':
                case '?':
                case '+':
                    if (natom == 0)
                    {
                        throw new ArgumentException(nameof(regex));
                    }

                    yield return c;
                    break;

                default:
                    if (natom > 1)
                    {
                        --natom;
                        yield return '.';
                    }

                    yield return c;
                    ++natom;
                    break;
                }
            }

            if (parens.Count > 0)
            {
                throw new ArgumentException(nameof(regex));
            }

            while (--natom > 0)
            {
                yield return '.';
            }

            for (; nalt > 0; --nalt)
            {
                yield return '|';
            }
        }

        private static object reg()
        {
            throw new NotImplementedException();
        }

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

        public static readonly char[] AllChars = Enumerable.Range(0, char.MaxValue + 1).Select(c => (char)c).ToArray();
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

            var result =
                new DeterministicStateMachine<T>(
                    0,
                    explored.Count,
                    (item, state) => transitions[(item, state)],
                    explored.Where(kvp => kvp.Key.AcceptsEmptyString).Select(kvp => kvp.Value));

            return result.Optimized(options);
        }
    }
}