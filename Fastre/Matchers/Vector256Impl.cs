using System;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Fastre
{
    /// <summary>
    /// See description on <see cref="IVectorImpl{TVectorType}"/>.
    /// </summary>
    public struct Vector256Impl : IVectorImpl<Vector256<sbyte>>
    {
        private static readonly Vector256<sbyte> _id =
            Avx.SetVector256(
                31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16,
                15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);

        public Vector256<sbyte> Id => _id;

        public bool IsSupported => false; // see TODO below

        public int VectorByteWidth => 32;

        public sbyte Extract(Vector256<sbyte> vec, byte index)
            => Avx.Extract(vec, index);

        public unsafe Vector256<sbyte> Load(sbyte* ptr)
            => Avx.LoadVector256(ptr);

        // TODO: not sure if there's actually a shuffle we can use on AVX...
        public Vector256<sbyte> Shuffle(Vector256<sbyte> vec, Vector256<sbyte> mask)
            => throw new NotImplementedException();
    }
}
