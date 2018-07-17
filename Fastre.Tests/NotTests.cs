using Hedgehog;
using Xunit;

namespace Fastre.Tests
{
    public sealed class NotTests
    {
        [Fact]
        public void Not_Accepts_Opposite()
        {
            Property.Check(
                from rex in Property.ForAll(Generators.Regex)
                from str in Property.ForAll(Gen.String(Range.Constant(0, 20), Gen.Unicode))
                select rex.Accepts(str) != (!rex).Accepts(str));
        }
    }
}
