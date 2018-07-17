using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Fastre
{
    [Flags]
    public enum CompilationOptions
    {
        None = 0,
        NoOptimization = 1 << 0,
        NoSse = 1 << 1,
    }

    internal enum RegexType
    {
        Single, Block, Any,
        And, Or, Seq,
        Not, Star,
        Fail,
    }

    [StructLayout(LayoutKind.Auto)]
    public abstract class Regex<T>
        : IEquatable<Regex<T>>
        , IComparable<Regex<T>>
        where T : IEquatable<T>
    {
        internal Regex(bool acceptsEmptyString, int hashCode)
        {
            AcceptsEmptyString = acceptsEmptyString;
            CachedHashCode = hashCode;
        }

        public static implicit operator Regex<T>(ReadOnlyMemory<T> s)
            => Block.Create(s);

        public static Regex<T> Just(T c)
            => new Single(c);

        public static Regex<T> Any { get; } = AnyImpl.Instance;
        public static Regex<T> Fail { get; } = FailImpl.Instance;
        public static Regex<T> Success { get; } = !Fail;
        public static Regex<T> Epsilon { get; } = Star.Create(Fail);

        public bool AcceptsEmptyString { get; }
        protected int CachedHashCode { get; }

        public bool Accepts(ReadOnlySpan<T> input)
        {
            if (input.Length == 0)
            {
                return AcceptsEmptyString;
            }

            return Diff(input[0]).Accepts(input.Slice(1));
        }

        public static bool operator ==(Regex<T> left, Regex<T> right)
            => left.Equals(right);

        public static bool operator !=(Regex<T> left, Regex<T> right)
            => !left.Equals(right);

        public sealed override bool Equals(object obj)
            => Equals(obj as Regex<T>);

        public sealed override int GetHashCode()
            => CachedHashCode;

        public static Regex<T> operator *(Regex<T> left, Regex<T> right)
            => Seq.Create(left, right);

        public static Regex<T> operator +(Regex<T> left, Regex<T> right)
            => Or.Create(left, right);

        public static Regex<T> operator !(Regex<T> regex)
            => Not.Create(regex);

        public static Regex<T> operator ~(Regex<T> regex)
            => Star.Create(regex);

        public static Regex<T> operator &(Regex<T> left, Regex<T> right)
            => And.Create(left, right);

        public abstract bool Equals(Regex<T> other);

        public abstract Regex<T> Diff(T input);

        public int CompareTo(Regex<T> other)
            => CachedHashCode.CompareTo(other.CachedHashCode);

        public override string ToString()
        {
            var builder = new StringBuilder();
            ToString(builder, 0);
            return builder.ToString();
        }

        protected abstract void ToString(StringBuilder builder, int parens);

        public abstract Regex<TOut> SelectMany<TOut>(Func<T, IEnumerable<TOut>> projection)
            where TOut : IEquatable<TOut>;

        internal sealed class Block : Regex<T>
        {
            private readonly ReadOnlyMemory<T> _value;
            private Block(ReadOnlyMemory<T> value)
                : base(
                      false,
                      HashCode.Combine(RegexType.Block, value))
            {
                Debug.Assert(value.Length > 0);
                _value = value;
            }

            public static Regex<T> Create(ReadOnlyMemory<T> value)
            {
                if (value.Length == 0)
                {
                    return Epsilon;
                }

                return new Block(value);
            }

            public override Regex<T> Diff(T input)
            {
                if (input.Equals(_value.Span[0]))
                {
                    return Create(_value.Slice(1));
                }
                else
                {
                    return Fail;
                }
            }

            public override bool Equals(Regex<T> other)
            {
                if (CachedHashCode != other.CachedHashCode)
                {
                    return false;
                }

                if (!(other is Block str))
                {
                    return false;
                }

                return _value.Equals(str._value);
            }

            public override Regex<TOut> SelectMany<TOut>(Func<T, IEnumerable<TOut>> projection)
            {
                var list = new List<TOut>(_value.Length);
                var span = _value.Span;
                for (int i = 0; i < _value.Length; ++i)
                {
                    list.AddRange(projection(span[i]));
                }

                return Regex<TOut>.Block.Create(list.ToArray());
            }

            protected override void ToString(StringBuilder builder, int parens)
            {
                if (parens > 2) builder.Append('(');

                var span = _value.Span;
                for (int i = 0; i < span.Length; ++i)
                {
                    builder.Append(span[i]);
                }

                if (parens > 2) builder.Append(')');
            }
        }

        internal sealed class FailImpl : Regex<T>
        {
            private FailImpl()
                : base(false, HashCode.Combine(RegexType.Fail))
            { }

            public static FailImpl Instance { get; } = new FailImpl();

            public override Regex<T> Diff(T input)
                => Instance;

            public override bool Equals(Regex<T> other)
                => ReferenceEquals(this, other);

            public override Regex<TOut> SelectMany<TOut>(Func<T, IEnumerable<TOut>> projection)
                => Regex<TOut>.Fail;

            protected override void ToString(StringBuilder builder, int parens)
                => builder.Append("(?!)");
        }

        internal sealed class AnyImpl : Regex<T>
        {
            private AnyImpl()
                : base(false, HashCode.Combine(RegexType.Any))
            { }

            public static Regex<T> Instance { get; } = new AnyImpl();

            public override Regex<T> Diff(T input)
                => Epsilon;

            public override bool Equals(Regex<T> other)
                => ReferenceEquals(this, other);

            // TODO: this is wrong, it needs to handle Unicode...
            public override Regex<TOut> SelectMany<TOut>(Func<T, IEnumerable<TOut>> projection)
                => Regex<TOut>.Any;

            protected override void ToString(StringBuilder builder, int parens)
                => builder.Append('.');
        }

        internal sealed class Star : Regex<T>
        {
            private readonly Regex<T> _regex;

            private Star(Regex<T> regex)
                : base(true, HashCode.Combine(RegexType.Star, regex))
                => _regex = regex;

            public static Regex<T> Create(Regex<T> regex)
            {
                // start (star x) == star x
                if (regex is Star)
                {
                    return regex;
                }

                return new Star(regex);
            }

            public override Regex<T> Diff(T input)
                => _regex.Diff(input).Then(this);

            public override bool Equals(Regex<T> other)
            {
                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                if (CachedHashCode != other.CachedHashCode)
                {
                    return false;
                }

                return other is Star s && _regex.Equals(s._regex);
            }

            public override Regex<TOut> SelectMany<TOut>(Func<T, IEnumerable<TOut>> projection)
                => ~_regex.SelectMany(projection);

            protected override void ToString(StringBuilder builder, int parens)
            {
                _regex.ToString(builder, 4);
                builder.Append("*");
            }
        }

        internal sealed class Seq : Regex<T>
        {
            private readonly Regex<T> _left;
            private readonly Regex<T> _right;

            private Seq(Regex<T> left, Regex<T> right)
                : base(
                      left.AcceptsEmptyString && right.AcceptsEmptyString,
                      HashCode.Combine(RegexType.Seq, left, right))
            {
                _left = left;
                _right = right;
            }

            public static Regex<T> Create(Regex<T> left, Regex<T> right)
            {
                // right-associate all seqs
                if (left is Seq seq)
                {
                    return Create(seq._left, Create(seq._right, right));
                }

                if (left == Fail || right == Fail)
                {
                    return Fail;
                }

                if (left == Epsilon)
                {
                    return right;
                }

                if (right == Epsilon)
                {
                    return left;
                }

                return new Seq(left, right);
            }

            public override Regex<T> Diff(T input)
            {
                var leftDiffed = _left.Diff(input).Then(_right);
                if (_left.AcceptsEmptyString)
                {
                    return leftDiffed.Or(_right.Diff(input));
                }
                else
                {
                    return leftDiffed;
                }
            }

            public override bool Equals(Regex<T> other)
            {
                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                if (CachedHashCode != other.CachedHashCode)
                {
                    return false;
                }

                return other is Seq seq
                    && _left.Equals(seq._left)
                    && _right.Equals(seq._right);
            }

            public override Regex<TOut> SelectMany<TOut>(Func<T, IEnumerable<TOut>> projection)
                => _left.SelectMany(projection) * _right.SelectMany(projection);

            protected override void ToString(StringBuilder builder, int parens)
            {
                if (parens > 2) builder.Append('(');
                _left.ToString(builder, 2);
                _right.ToString(builder, 2);
                if (parens > 2) builder.Append(')');
            }
        }

        internal sealed class And : Regex<T>
        {
            private readonly SortedSet<Regex<T>> _regexes;

            private And(SortedSet<Regex<T>> regexes)
                : base(
                      regexes.All(r => r.AcceptsEmptyString),
                      regexes.Aggregate(RegexType.And.GetHashCode(), (hash, rex) => hash * 7 + rex.GetHashCode()))
            {
                _regexes = regexes;
            }

            public static Regex<T> Create(params Regex<T>[] regexes)
                => Create((IEnumerable<Regex<T>>)regexes);

            public static Regex<T> Create(IEnumerable<Regex<T>> regexes)
            {
                var set = new SortedSet<Regex<T>>();

                foreach (var regex in regexes)
                {
                    if (regex == Success)
                    {
                        continue;
                    }

                    if (regex == Fail)
                    {
                        return regex;
                    }

                    if (regex is And and)
                    {
                        foreach (var inner in and._regexes)
                        {
                            set.Add(inner);
                        }
                    }
                    else
                    {
                        set.Add(regex);
                    }
                }

                if (set.Count == 1)
                {
                    return set.Min;
                }

                if (set.Count == 0)
                {
                    return Fail;
                }

                return new And(set);
            }

            public override Regex<T> Diff(T input)
                => Create(_regexes.Select(r => r.Diff(input)));

            public override bool Equals(Regex<T> other)
            {
                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                if (CachedHashCode != other.CachedHashCode)
                {
                    return false;
                }

                return other is And and && _regexes.SequenceEqual(and._regexes);
            }

            public override Regex<TOut> SelectMany<TOut>(Func<T, IEnumerable<TOut>> projection)
                => Regex<TOut>.And.Create(_regexes.Select(r => r.SelectMany(projection)));

            protected override void ToString(StringBuilder builder, int parens)
            {
                bool first = true;
                if (parens > 1) builder.Append('(');

                foreach (var regex in _regexes)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        builder.Append('&');
                    }

                    regex.ToString(builder, 1);
                }

                if (parens > 1) builder.Append(')');
            }
        }

        internal sealed class Or : Regex<T>
        {
            private readonly SortedSet<Regex<T>> _regexes;

            private Or(SortedSet<Regex<T>> regexes)
                : base(
                      regexes.Any(r => r.AcceptsEmptyString),
                      regexes.Aggregate(RegexType.Or.GetHashCode(), (hash, rex) => hash * 7 + rex.GetHashCode()))
            {
                _regexes = regexes;
            }

            public static Regex<T> Create(params Regex<T>[] regexes)
                => Create((IEnumerable<Regex<T>>)regexes);

            public static Regex<T> Create(IEnumerable<Regex<T>> regexen)
            {
                var set = new SortedSet<Regex<T>>();

                foreach (var regex in regexen)
                {
                    if (regex == Fail)
                    {
                        continue;
                    }

                    if (regex == Success)
                    {
                        return regex;
                    }

                    if (regex is Or or)
                    {
                        foreach (var inner in or._regexes)
                        {
                            set.Add(inner);
                        }
                    }
                    else
                    {
                        set.Add(regex);
                    }
                }

                if (set.Count == 1)
                {
                    return set.Min;
                }

                if (set.Count == 0)
                {
                    return Success;
                }

                return new Or(set);
            }

            public override Regex<T> Diff(T input)
                => Create(_regexes.Select(r => r.Diff(input)));

            public override bool Equals(Regex<T> other)
            {
                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                if (CachedHashCode != other.CachedHashCode)
                {
                    return false;
                }

                return other is Or or && _regexes.SequenceEqual(or._regexes);
            }

            public override Regex<TOut> SelectMany<TOut>(Func<T, IEnumerable<TOut>> projection)
                => Regex<TOut>.Or.Create(_regexes.Select(r => r.SelectMany(projection)));

            protected override void ToString(StringBuilder builder, int parens)
            {
                bool first = true;

                if (parens > 0) builder.Append('(');

                foreach (var regex in _regexes)
                {
                    if (first)
                    {
                        first = false;
                    }
                    else
                    {
                        builder.Append('|');
                    }

                    regex.ToString(builder, 0);
                }

                if (parens > 0) builder.Append(')');
            }
        }

        internal sealed class Not : Regex<T>
        {
            private readonly Regex<T> _regex;

            private Not(Regex<T> regex)
                : base(
                      !regex.AcceptsEmptyString,
                      HashCode.Combine(RegexType.Not, regex))
            {
                _regex = regex;
            }

            public static Regex<T> Create(Regex<T> regex)
            {
                // not . not == id
                if (regex is Not inner)
                {
                    return inner._regex;
                }

                return new Not(regex);
            }

            public override Regex<T> Diff(T input)
                => _regex.Diff(input).Not();

            public override bool Equals(Regex<T> other)
            {
                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                if (CachedHashCode != other.CachedHashCode)
                {
                    return false;
                }

                return other is Not not && _regex.Equals(not._regex);
            }

            public override Regex<TOut> SelectMany<TOut>(Func<T, IEnumerable<TOut>> projection)
                => !_regex.SelectMany(projection);

            protected override void ToString(StringBuilder builder, int parens)
            {
                builder.Append("!");
                if (parens > 3) builder.Append('(');
                _regex.ToString(builder, 3);
                if (parens > 3) builder.Append(')');
            }
        }

        internal sealed class Single : Regex<T>
        {
            private T _value;

            public Single(T value)
                : base(false, HashCode.Combine(RegexType.Single, value))
                => _value = value;

            public override Regex<T> Diff(T input)
            {
                return _value.Equals(input) ? Epsilon : Fail;
            }

            public override bool Equals(Regex<T> other)
            {
                if (ReferenceEquals(this, other))
                {
                    return true;
                }

                if (CachedHashCode != other.CachedHashCode)
                {
                    return false;
                }

                return other is Single s && _value.Equals(s._value);
            }

            public override Regex<TOut> SelectMany<TOut>(Func<T, IEnumerable<TOut>> projection)
                => Regex<TOut>.Block.Create(projection(_value).ToArray());

            protected override void ToString(StringBuilder builder, int parens)
                => builder.Append(_value);
        }
    }

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