using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Fastre
{
    /// <summary>
    /// See description on <see cref="IVectorImpl{TVectorType}"/>.
    /// </summary>
    public struct Vector128Impl : IVectorImpl<Vector128<sbyte>>
    {
        public bool IsSupported =>
            Sse2.IsSupported && Sse41.IsSupported && Ssse3.IsSupported;

        public Vector128<sbyte> Id =>
            // this is 0, 1, 2, …, 15, but this version loads faster
            // find these constants via:
            //     Sse.StaticCast<sbyte, long>(Sse2.SetVector128(15, 14, 13, …))
            // then Sse41.Extract
            Sse.StaticCast<long, sbyte>(Sse2.SetVector128(1084818905618843912, 506097522914230528));

        public Vector128<sbyte> Zero => Sse2.SetZeroVector128<sbyte>();

        public bool IsZero(Vector128<sbyte> vector) => Sse41.TestAllZeros(vector, vector);

        public int VectorByteWidth => 16;

        public sbyte Extract(Vector128<sbyte> vec, byte index)
            => Sse41.Extract(vec, index);

        public unsafe Vector128<sbyte> Load(sbyte* ptr)
            => Sse2.LoadVector128(ptr);

        public Vector128<sbyte> Shuffle(Vector128<sbyte> vec, Vector128<sbyte> mask)
            => Ssse3.Shuffle(vec, mask);

        public unsafe byte* MemChr(byte byteToLookFor, byte* at, byte* end)
        {
            var vecToLookFor = Sse2.SetAllVector128(byteToLookFor);

            for (; at + 31 < end; at += 32)
            {
                var vec1p = at;
                var vec2p = at + 16;

                var vec1 = Sse2.LoadVector128(vec1p);
                var vec2 = Sse2.LoadVector128(vec2p);

                var cmp1 = Sse2.CompareEqual(vecToLookFor, vec1);
                var cmp2 = Sse2.CompareEqual(vecToLookFor, vec2);

                var cmpResult1 = (uint)Sse2.MoveMask(cmp1);
                var cmpResult2 = (uint)Sse2.MoveMask(cmp2);

                if ((cmpResult1 | cmpResult2) != 0)
                {
                    // masks are the reverse of what you'd expect, so we want trailing zeroes
                    var combined = (cmpResult2 << 16) | cmpResult1;

                    // TODO: replace with Bmi1.TzCnt when .NET Core 3.0 is out
                    var tzCnt = TrailingZeros.TrailingZeroes(combined);
                    return at + tzCnt;
                }
            }

            // find in leftovers
            for (; at < end; ++at)
            {
                if (*at == byteToLookFor)
                {
                    break;
                }
            }

            return at;
        }
    }
}
