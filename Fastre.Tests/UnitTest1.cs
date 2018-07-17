using Hedgehog;
using System.Collections.Generic;
using Xunit;

namespace Fastre.Tests
{
    public class LiteralTests
    {
        [Fact]
        public void LiteralAcceptsItself()
        {
            Property.Check(
                from s in Property.ForAll(Gen.String(Range.Constant(0, 100), Gen.UnicodeAll))
                select s.Literal().Accepts(s));
        }

        [Fact]
        public void LiteralAcceptsItselfOnLeftOfOr()
        {
            Property.Check(
                from s in Property.ForAll(Gen.String(Range.Constant(0, 100), Gen.UnicodeAll))
                from s2 in Property.ForAll(Gen.String(Range.Constant(0, 100), Gen.UnicodeAll))
                select s.Literal().Or(s2.Literal()).Accepts(s));
        }

        [Fact]
        public void LiteralAcceptsItselfOnRightOfOr()
        {
            Property.Check(
                from s in Property.ForAll(Gen.String(Range.Constant(0, 100), Gen.UnicodeAll))
                from s2 in Property.ForAll(Gen.String(Range.Constant(0, 100), Gen.UnicodeAll))
                select s2.Literal().Or(s.Literal()).Accepts(s));
        }

        [Fact]
        public void LiteralAcceptsItselfOnBothSidesOfAnd()
        {
            Property.Check(
                from s in Property.ForAll(Gen.String(Range.Constant(0, 100), Gen.UnicodeAll))
                select s.Literal().And(s.Literal()).Accepts(s));
        }
    }
}
