using Xunit;

namespace Fastre.Tests
{
    public class ParseTests
    {
        [Fact]
        public void ParseConcat()
        {
            var rex = 'a'.Literal() * 'b'.Literal();
            var parsed = RegexExtensions.Parse("ab");
            Assert.Equal(rex, parsed);
        }

        [Fact]
        public void ParseStar()
        {
            var rex = 'a'.Literal().Star();
            var parsed = RegexExtensions.Parse("a*");
            Assert.Equal(rex, parsed);
        }

        [Fact]
        public void ParseQuestion()
        {
            var rex = 'a'.Literal().Question();
            var parsed = RegexExtensions.Parse("a?");
            Assert.Equal(rex, parsed);
        }

        [Fact]
        public void ParsePlus()
        {
            var rex = 'a'.Literal().Plus();
            var parsed = RegexExtensions.Parse("a+");
            Assert.Equal(rex, parsed);
        }

        [Fact]
        public void ParseNested()
        {
            var rex = 'a'.Literal() * ('b'.Literal() * 'c'.Literal()).Star();
            var parsed = RegexExtensions.Parse("a(bc)*");
            Assert.Equal(rex, parsed);
        }
    }
}
