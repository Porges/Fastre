using Hedgehog;
using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fastre.Tests
{
    static class Generators
    {
        public static Gen<Regex<char>> Regex { get; } = DefineGen();

        private sealed class GetRegex : FSharpFunc<Unit, Gen<Regex<char>>>
        {
            // gross
            public override Gen<Regex<char>> Invoke(Unit func) => Regex;
        }

        private static Gen<Regex<char>> DefineGen()
        {
            var delayedRegex = Gen.Delay(new GetRegex());
            return Gen.ChoiceRecursive(
                new[]
                {
                    from c in Gen.Unicode
                    select Regex<char>.Just(c),

                    Gen.Constant(Regex<char>.Fail),

                    Gen.Constant(Regex<char>.Epsilon),
                },
                new[]
                {
                    from c in delayedRegex
                    select ~c,

                    from c in delayedRegex
                    select !c,

                    from l in delayedRegex
                    from r in delayedRegex
                    select l * r,

                    from l in delayedRegex
                    from r in delayedRegex
                    select l + r,

                    from l in delayedRegex
                    from r in delayedRegex
                    select l & r,
                });
        }
    }
}
