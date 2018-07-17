using Hedgehog;
using Xunit;

namespace Fastre.Tests
{
    /// <summary>
    /// This class checks that the expected normalizations are performed.
    /// They are all taken from the paper: https://www.cs.kent.ac.uk/people/staff/sao/documents/jfp09.pdf
    /// </summary>
    public class EqualityTests
    {
        static readonly Regex<char> _fail = Regex<char>.Fail;
        static readonly Regex<char> _eps = Regex<char>.Epsilon;

        private static readonly Property<Regex<char>> SomeRegex = Property.ForAll(Generators.Regex);

        [Fact]
        public void Star_Idempotent()
            => Property.Check(
                from rex in SomeRegex
                select Assert.Equal(~rex, ~~rex));

        [Fact]
        public void Star_Eps_NoChange()
            => Assert.Equal(_eps, ~_eps);

        [Fact]
        public void Star_Fail_Is_Eps()
            => Assert.Equal(_eps, ~_fail);

        [Fact]
        public void Not_Involution()
            => Property.Check(from rex in SomeRegex select Assert.Equal(rex, !!rex));

        [Fact]
        public void And_Idempotent()
            => Property.Check(from rex in SomeRegex select Assert.Equal(rex, rex & rex));

        [Fact]
        public void And_Commutative()
            => Property.Check(
                from rex1 in SomeRegex
                from rex2 in SomeRegex
                select Assert.Equal(rex1 & rex2, rex2 & rex1));

        [Fact]
        public void And_Associative()
            => Property.Check(
                from rex1 in SomeRegex
                from rex2 in SomeRegex
                from rex3 in SomeRegex
                select Assert.Equal(rex1 & (rex2 & rex3), (rex1 & rex2) & rex3));

        [Fact]
        public void And_Fail_Absorbing_Left()
            => Property.Check(from rex in SomeRegex select Assert.Equal(_fail, _fail & rex));

        [Fact]
        public void And_Fail_Absorbing_Right()
            => Property.Check(from rex in SomeRegex select Assert.Equal(_fail, rex & _fail));

        [Fact]
        public void And_NotFail_Identity_Left()
            => Property.Check(from rex in SomeRegex select Assert.Equal(rex, !_fail & rex));

        [Fact]
        public void And_NotFail_Identity_Right()
            => Property.Check(from rex in SomeRegex select Assert.Equal(rex, rex & !_fail));

        [Fact]
        public void Or_Idempotent()
            => Property.Check(from rex in SomeRegex select Assert.Equal(rex, rex + rex));

        [Fact]
        public void Or_Commutative()
            => Property.Check(
                from rex1 in SomeRegex
                from rex2 in SomeRegex
                select Assert.Equal(rex1 + rex2, rex2 + rex1));

        [Fact]
        public void Or_Associative()
            => Property.Check(
                from rex1 in SomeRegex
                from rex2 in SomeRegex
                from rex3 in SomeRegex
                select Assert.Equal(rex1 + (rex2 + rex3), (rex1 + rex2) + rex3));

        [Fact]
        public void Or_Fail_Identity_Left()
            => Property.Check(from rex in SomeRegex select Assert.Equal(rex, _fail + rex));

        [Fact]
        public void Or_Fail_Identity_Right()
            => Property.Check(from rex in SomeRegex select Assert.Equal(rex, rex + _fail));

        [Fact]
        public void Or_NotFail_Absorbing_Left()
            => Property.Check(from rex in SomeRegex select Assert.Equal(!_fail, !_fail + rex));

        [Fact]
        public void Or_NotFail_Absorbing_Right()
            => Property.Check(from rex in SomeRegex select Assert.Equal(!_fail, rex + !_fail));

        [Fact]
        public void Seq_Associative()
            => Property.Check(
                from rex1 in SomeRegex
                from rex2 in SomeRegex
                from rex3 in SomeRegex
                select Assert.Equal(rex1 * (rex2 * rex3), (rex1 * rex2) * rex3));

        [Fact]
        public void Seq_Fail_Absorbing_Left()
            => Property.Check(from rex in SomeRegex select Assert.Equal(_fail, _fail * rex));

        [Fact]
        public void Seq_Fail_Absorbing_Right()
            => Property.Check(from rex in SomeRegex select Assert.Equal(_fail, rex * _fail));

        [Fact]
        public void Seq_Eps_Identity_Left()
            => Property.Check(from rex in SomeRegex select Assert.Equal(rex, _eps * rex));

        [Fact]
        public void Seq_Eps_Identity_Right()
            => Property.Check(from rex in SomeRegex select Assert.Equal(rex, rex * _eps));
    }
}
