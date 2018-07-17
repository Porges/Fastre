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

        private static readonly Vector128<sbyte> _id =
            Sse2.SetVector128(15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);

        public Vector128<sbyte> Id => _id;

        public int VectorByteWidth => 16;

        public sbyte Extract(Vector128<sbyte> vec, byte index)
            => Sse41.Extract(vec, index);

        public unsafe Vector128<sbyte> Load(sbyte* ptr)
            => Sse2.LoadVector128(ptr);

        public Vector128<sbyte> Shuffle(Vector128<sbyte> vec, Vector128<sbyte> mask)
            => Ssse3.Shuffle(vec, mask);
    }
}
