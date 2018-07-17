using System;
using System.Diagnostics;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace Fastre
{
    public interface IVectorImpl<TVectorType>
    {
        bool IsSupported { get; }

        int VectorByteWidth { get; }

        TVectorType Id { get; }

        unsafe TVectorType Load(sbyte* ptr);

        TVectorType Shuffle(TVectorType vec, TVectorType mask);

        sbyte Extract(TVectorType vec, byte index);
    }

    public struct Vector128Impl : IVectorImpl<Vector128<sbyte>>
    {
        public bool IsSupported =>
            Sse2.IsSupported && Sse41.IsSupported && Ssse3.IsSupported;

        public Vector128<sbyte> Id =>
            Sse2.SetVector128(15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);

        public int VectorByteWidth => 16;

        public sbyte Extract(Vector128<sbyte> vec, byte index)
            => Sse41.Extract(vec, index);

        public unsafe Vector128<sbyte> Load(sbyte* ptr)
            => Sse2.LoadVector128(ptr);

        public Vector128<sbyte> Shuffle(Vector128<sbyte> vec, Vector128<sbyte> mask)
            => Ssse3.Shuffle(vec, mask);
    }

    public struct Vector256Impl : IVectorImpl<Vector256<sbyte>>
    {
        public Vector256<sbyte> Id =>
            Avx.SetVector256(
                31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20, 19, 18, 17, 16,
                15, 14, 13, 12, 11, 10, 9, 8, 7, 6, 5, 4, 3, 2, 1, 0);

        public bool IsSupported => false;

        public int VectorByteWidth => 32;

        public sbyte Extract(Vector256<sbyte> vec, byte index)
            => Avx.Extract(vec, index);

        public unsafe Vector256<sbyte> Load(sbyte* ptr)
            => Avx.LoadVector256(ptr);

        public Vector256<sbyte> Shuffle(Vector256<sbyte> vec, Vector256<sbyte> mask)
            => throw new NotImplementedException();
    }

    public sealed class MonoidalStateMachineILPUnsafe2<TVectorImpl, TVectorType> : IMatcher<byte>
        where TVectorImpl : struct, IVectorImpl<TVectorType>
    {
        private readonly sbyte _start;
        private readonly sbyte[] _transitions = new sbyte[16 * (byte.MaxValue + 1)];
        private readonly sbyte[] _accept;

        public static bool CanBeUsed(int stateCount)
        {
            return stateCount <= default(TVectorImpl).VectorByteWidth && default(TVectorImpl).IsSupported;
        }

        public unsafe MonoidalStateMachineILPUnsafe2(sbyte start, int stateCount, Func<byte, sbyte, sbyte> transitionFunc, in ReadOnlySpan<sbyte> accept)
        {
            Debug.Assert(stateCount <= default(TVectorImpl).VectorByteWidth);

            _start = start;
            _accept = accept.ToArray();

            for (int i = 0; i <= byte.MaxValue; ++i)
            for (int j = 0; j < stateCount; ++j)
            {
                _transitions[i * default(TVectorImpl).VectorByteWidth + j] = transitionFunc((byte)i, (sbyte)j);
            }
        }

        public unsafe bool Accepts(ReadOnlySpan<byte> inputt)
        {
            var vecImpl = default(TVectorImpl);
            var length = inputt.Length;

            // we fix these here to avoid bounds-checks
            // the .NET JITter cannot yet elide
            fixed (sbyte* transitions = _transitions)
            fixed (byte* input = inputt)
            {
                var transition = vecImpl.Id;

                int i = 0;
                for (; i + 6 < length; i += 7)
                {
                    var t1 = vecImpl.Load(transitions + (input[i + 0] * vecImpl.VectorByteWidth));
                    var t2 = vecImpl.Load(transitions + (input[i + 1] * vecImpl.VectorByteWidth));
                    var t3 = vecImpl.Load(transitions + (input[i + 2] * vecImpl.VectorByteWidth));
                    var t4 = vecImpl.Load(transitions + (input[i + 3] * vecImpl.VectorByteWidth));
                    var t5 = vecImpl.Load(transitions + (input[i + 4] * vecImpl.VectorByteWidth));
                    var t6 = vecImpl.Load(transitions + (input[i + 5] * vecImpl.VectorByteWidth));
                    var t7 = vecImpl.Load(transitions + (input[i + 6] * vecImpl.VectorByteWidth));

                    var t01 = vecImpl.Shuffle(t1, transition);
                    var t23 = vecImpl.Shuffle(t3, t2);
                    var t45 = vecImpl.Shuffle(t5, t4);
                    var t67 = vecImpl.Shuffle(t7, t6);

                    var t0123 = vecImpl.Shuffle(t23, t01);
                    var t4567 = vecImpl.Shuffle(t67, t45);

                    transition = vecImpl.Shuffle(t4567, t0123);
                }

                // handle any leftovers
                for (; i < length; ++i)
                {
                    transition = vecImpl.Shuffle(vecImpl.Load(transitions + (input[i] * vecImpl.VectorByteWidth)), transition);
                }

                var state = vecImpl.Extract(transition, (byte)_start);

                bool found = false;
                for (int j = 0; j < _accept.Length; ++j)
                {
                    found = found | (_accept[j] == state);
                }

                return found;
            }
        }
    }
}
