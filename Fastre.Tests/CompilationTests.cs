using System;
using System.Collections.Generic;
using System.Text;
using Xunit;

namespace Fastre.Tests
{
    public sealed class CompilationTests
    {
        public static object[][] Examples =
        {
            new object []{ "a".Literal(), new[]{"a", "b", "" } },
            new object []{ "a".Literal() + "b".Literal(), new[]{"a", "b", "c", "" } },
            new object []{ "a".Literal() & "b".Literal(), new[]{"a", "b", "c", "" } },
            new object []{ "a".Literal() * "b".Literal(), new[]{"a", "ab", "b", "c", "" } },
            new object []{ ~("a".Literal()) * "b".Literal(), new[]{"a", "ab", "aab", "aaaaaaaaaaaaaaaaaaaaaaab", "b", "c", "" } },
            new object []{ !("a".Literal()), new[]{"a", "b", "" } },
        };

        private static IEnumerable<char> AllChars()
        {
            for (int i = 0; i <= char.MaxValue; ++i)
            {
                yield return (char)i;
            }
        }

        [Theory]
        [MemberData(nameof(Examples))]
        public void Compilation_Preserves_Accepts_Result(Regex<char> regex, string[] examples)
            => Assert.All(
                examples,
                example =>
                    Assert.Equal(
                        regex.Accepts(example),
                        regex.Compile(AllChars()).Accepts(example)));

        [Theory]
        [MemberData(nameof(Examples))]
        public void Compilation_Preserves_Accepts_Result_UTF8(Regex<char> regex, string[] examples)
            => Assert.All(
                examples,
                example =>
                    Assert.Equal(
                        regex.Accepts(example),
                        regex.Compile(Encoding.UTF8).Accepts(Encoding.UTF8.GetBytes(example))));

        [Theory]
        [MemberData(nameof(Examples))]
        public void Compilation_Preserves_Accepts_Result_UTF16(Regex<char> regex, string[] examples)
            => Assert.All(
                examples,
                example =>
                    Assert.Equal(
                        regex.Accepts(example),
                        regex.Compile(Encoding.Unicode).Accepts(Encoding.Unicode.GetBytes(example))));

        [Theory]
        [MemberData(nameof(Examples))]
        public void Compilation_Preserves_Accepts_Result_UTF16_Bytes(Regex<char> regex, string[] examples)
            => Assert.All(
                examples,
                example =>
                    Assert.Equal(
                        regex.Accepts(example),
                        regex.Compile().Accepts(example)));
    }
}
