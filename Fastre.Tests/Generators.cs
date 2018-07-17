using Hedgehog;
using Microsoft.FSharp.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Fastre.Tests
{
    static class Generators
    {
        //public static Gen<Regex<char>> Regex { get; } = Gen.Sized(FSharpFunc<int, Gen<Regex<char>>>.FromConverter(Regex));

        //public static Gen<Regex<char>> Regex(int n)
        //{
        //    Gen.ChoiceRecursive(
        //        new[]
        //        {
        //            from c in Gen.Unicode
        //            select Regex<char>.Just(c),

        //            Gen.Int16()

        //            Gen.Constant(Regex<char>.Fail),

        //            Gen.Constant(Regex<char>.Epsilon),
        //        },
        //        new[]
        //        {
        //            from c in Regex
        //            select ~c,

        //            from c in Regex
        //            select !c,

        //            from l in Regex
        //            from r in Regex
        //            select l * r,

        //            from l in Regex
        //            from r in Regex
        //            select l + r,

        //            from l in Regex
        //            from r in Regex
        //            select l & r,
        //        });
        //}
    }
}
