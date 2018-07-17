using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Fastre.Tests
{
    /// <summary>
    /// This class checks that the expected normalizations are performed.
    /// </summary>
    public class EqualityTests
    {
        static readonly Regex<char> _rA = Regex<char>.Just('a');
        static readonly Regex<char> _rB = Regex<char>.Just('b');
        static readonly Regex<char> _rC = Regex<char>.Just('c');
        static readonly Regex<char> _fail = Regex<char>.Fail;
        static readonly Regex<char> _eps = Regex<char>.Epsilon;

        [Fact]
        public void SingleChar()
            => Assert.Equal(_rA, _rA);

        [Fact]
        public void SingleCharNot()
            => Assert.NotEqual(_rA, _rB);

        // TODO: parametrize the below tests

        [Fact]
        public void Star_Idempotent()
            => Assert.Equal(~_rA, ~~_rA);

        [Fact]
        public void Star_Eps_NoChange()
            => Assert.Equal(_eps, ~_eps);

        [Fact]
        public void Star_Fail_Is_Eps()
            => Assert.Equal(_eps, ~_fail);

        [Fact]
        public void Not_Involution()
            => Assert.Equal(_rA, !!_rA);

        [Fact]
        public void And_Idempotent()
            => Assert.Equal(_rA, _rA & _rA);

        [Fact]
        public void And_Commutative()
            => Assert.Equal(_rA & _rB, _rB & _rA);

        [Fact]
        public void And_Associative()
            => Assert.Equal(_rA & (_rB & _rC), (_rA & _rB) & _rC);

        [Fact]
        public void And_Fail_Absorbing_Left()
            => Assert.Equal(_fail, _fail & _rA);

        [Fact]
        public void And_Fail_Absorbing_Right()
            => Assert.Equal(_fail, _rA & _fail);

        [Fact]
        public void And_NotFail_Identity_Left()
            => Assert.Equal(_rA, !_fail & _rA);

        [Fact]
        public void And_NotFail_Identity_Right()
            => Assert.Equal(_rA, _rA & !_fail);

        [Fact]
        public void Or_Idempotent()
            => Assert.Equal(_rA, _rA + _rA);

        [Fact]
        public void Or_Commutative()
            => Assert.Equal(_rA + _rB, _rB + _rA);

        [Fact]
        public void Or_Associative()
            => Assert.Equal(_rA + (_rB + _rC), (_rA + _rB) + _rC);

        [Fact]
        public void Or_Fail_Identity_Left()
            => Assert.Equal(_rA, _fail + _rA);

        [Fact]
        public void Or_Fail_Identity_Right()
            => Assert.Equal(_rA, _rA + _fail);

        [Fact]
        public void Or_NotFail_Absorbing_Left()
            => Assert.Equal(!_fail, !_fail + _rA);

        [Fact]
        public void Or_NotFail_Absorbing_Right()
            => Assert.Equal(!_fail, _rA + !_fail);

        [Fact]
        public void Seq_Associative()
            => Assert.Equal(_rA * (_rB * _rC), (_rA * _rB) * _rC);

        [Fact]
        public void Seq_Fail_Absorbing_Left()
            => Assert.Equal(_fail, _fail * _rA);

        [Fact]
        public void Seq_Fail_Absorbing_Right()
            => Assert.Equal(_fail, _rA * _fail);

        [Fact]
        public void Seq_Eps_Identity_Left()
            => Assert.Equal(_rA, _eps * _rA);

        [Fact]
        public void Seq_Eps_Identity_Right()
            => Assert.Equal(_rA, _rA * _eps);
    }
}
