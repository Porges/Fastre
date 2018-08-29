using Hedgehog;
using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Fastre.Tests
{
    public sealed class CompilationTests
    {
        private static IEnumerable<char> AllChars()
        {
            for (int i = 0; i <= char.MaxValue; ++i)
            {
                yield return (char)i;
            }
        }

        private static readonly Property<Regex<char>> SomeRegex =
            Property.ForAll(Generators.Regex);

        private static readonly Property<string> SomeString =
            Property.ForAll(Gen.String(Range.Constant(0, 100), Gen.Unicode));

        [Fact]
        public void Compilation_Preserves_Accepts_Result()
            => Property.Check(
                from rex in SomeRegex
                from str in SomeString
                select Assert.Equal(rex.Accepts(str), rex.Compile(AllChars()).Accepts(str)));

        [Fact]
        public void Compilation_Preserves_Accepts_Result_UTF8()
            => Property.Check(
                from rex in SomeRegex
                from str in SomeString
                select Assert.Equal(rex.Accepts(str), rex.Compile(Encoding.UTF8).Accepts(Encoding.UTF8.GetBytes(str))));

        [Fact]
        public void Compilation_Preserves_Accepts_Result_UTF16()
            => Property.Check(
                from rex in SomeRegex
                from str in SomeString
                select Assert.Equal(rex.Accepts(str), rex.Compile(Encoding.Unicode).Accepts(Encoding.Unicode.GetBytes(str))));

        [Fact]
        public void Compilation_Preserves_Accepts_Result_UTF16_Bytes()
            => Property.Check(
                from rex in SomeRegex
                from str in SomeString
                select Assert.Equal(rex.Accepts(str), rex.Compile().Accepts(str)));
    }
}
