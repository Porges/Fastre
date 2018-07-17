using Hedgehog;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Fastre.Tests
{
    public sealed class NotTests
    {
        [Fact]
        public void Opposite()
        {
            //Property.Check(
            //    from rex in Property.ForAll(Generators.Regex)
            //    from str in Property.ForAll(Gen.String(Range.Linear(0, 100), Gen.Unicode))
            //    select rex.Accepts(str) != (!rex).Accepts(str));
        }
    }
}
