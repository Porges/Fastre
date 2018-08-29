using System;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Fastre
{
    /// <summary>
    /// <remarks>
    /// Unfortunately there's no 256-bit vector shuffle (it treats the input
    /// as two 128-bit wide vectors), so the only operation that uses 256-bit vectors
    /// here is memchr.
    /// </remarks>
    /// </summary>
    public struct Vector256Impl : IVectorImpl<Vector128<sbyte>>
    {
        public bool IsSupported =>
            default(Vector128Impl).IsSupported;
            //&& Avx.IsSupported && Avx2.IsSupported && Lzcnt.IsSupported;

        public Vector128<sbyte> Id => default(Vector128Impl).Id;

        public Vector128<sbyte> Zero => default(Vector128Impl).Zero;

        public bool IsZero(Vector128<sbyte> vector) => default(Vector128Impl).IsZero(vector);

        public int VectorByteWidth => default(Vector128Impl).VectorByteWidth;

        public sbyte Extract(Vector128<sbyte> vec, byte index) => default(Vector128Impl).Extract(vec, index);

        public unsafe Vector128<sbyte> Load(sbyte* ptr) => default(Vector128Impl).Load(ptr);

        public Vector128<sbyte> Shuffle(Vector128<sbyte> vec, Vector128<sbyte> mask) => default(Vector128Impl).Shuffle(vec, mask);

        public unsafe byte* MemChr(byte byteToLookFor, byte* at, byte* end) => default(Vector128Impl).MemChr(byteToLookFor, at, end);

        // removed for now to remove any potential difference between implemntations
        //{
        //    var vecToLookFor = Avx.SetAllVector256(byteToLookFor);

        //    for (; at + 63 < end; at += 64)
        //    {
        //        var vec1p = at;
        //        var vec2p = at + 32;

        //        var vec1 = Avx.LoadVector256(vec1p);
        //        var vec2 = Avx.LoadVector256(vec2p);

        //        var cmp1 = Avx2.CompareEqual(vecToLookFor, vec1);
        //        var cmp2 = Avx2.CompareEqual(vecToLookFor, vec2);

        //        var cmpResult1 = (uint)Avx2.MoveMask(cmp1);
        //        var cmpResult2 = (uint)Avx2.MoveMask(cmp2);

        //        if ((cmpResult1 | cmpResult2) != 0)
        //        {
        //            // masks are the reverse of what you'd expect, so we need to
        //            // look for trailing zeroes

        //            // TODO: replace with intrinsic in .NET Core 3.0
        //            var tzCnt = TrailingZeros.TrailingZeroes(cmpResult1);
        //            return at + (tzCnt < 32 ? tzCnt : (tzCnt + TrailingZeros.TrailingZeroes(cmpResult2)));
        //        }
        //    }

        //    // find in leftovers
        //    for (; at < end; ++at)
        //    {
        //        if (*at == byteToLookFor)
        //        {
        //            break;
        //        }
        //    }

        //    return at;
        //}
    }
}
