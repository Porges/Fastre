﻿using System;
using System.Diagnostics;

namespace Fastre
{
    public sealed class VectoredMatcher<TVectorImpl, TVectorType> : IMatcher<byte>
        where TVectorImpl : struct, IVectorImpl<TVectorType>
    {
        private readonly sbyte _start;
        private readonly sbyte[] _transitions = new sbyte[default(TVectorImpl).VectorByteWidth * (byte.MaxValue + 1)];
        private readonly sbyte[] _accept;

        public static bool CanBeUsed(int stateCount)
        {
            return stateCount <= default(TVectorImpl).VectorByteWidth && default(TVectorImpl).IsSupported;
        }

        public unsafe VectoredMatcher(sbyte start, int stateCount, Func<byte, sbyte, sbyte> transitionFunc, ReadOnlySpan<sbyte> accept)
        {
            Debug.Assert(stateCount <= default(TVectorImpl).VectorByteWidth);

            _start = start;
            _accept = accept.ToArray();

            for (int i = 0; i <= byte.MaxValue; ++i)
            {
                var offset = i * default(TVectorImpl).VectorByteWidth;
                for (int j = 0; j < stateCount; ++j)
                {
                    _transitions[offset + j] = transitionFunc((byte)i, (sbyte)j);
                }
            }
        }

        public unsafe bool Accepts(ReadOnlySpan<byte> inputt)
        {
            var vecImpl = default(TVectorImpl);

            var transition = vecImpl.Id;

            // we fix these arrays here to avoid bounds-checks
            // the .NET JITter cannot yet elide on ReadOnlySpan
            fixed (sbyte* transitions = _transitions)
            fixed (byte* input = inputt)
            {
                byte* at = input;
                byte* end = input + inputt.Length;

                for (; at + 6 < end; at += 7)
                {
                    var i0 = at[0];
                    var i1 = at[1];
                    var i2 = at[2];
                    var i3 = at[3];
                    var i4 = at[4];
                    var i5 = at[5];
                    var i6 = at[6];

                    var t1 = vecImpl.Load(transitions + (i0 * vecImpl.VectorByteWidth));
                    var t2 = vecImpl.Load(transitions + (i1 * vecImpl.VectorByteWidth));
                    var t3 = vecImpl.Load(transitions + (i2 * vecImpl.VectorByteWidth));
                    var t4 = vecImpl.Load(transitions + (i3 * vecImpl.VectorByteWidth));
                    var t5 = vecImpl.Load(transitions + (i4 * vecImpl.VectorByteWidth));
                    var t6 = vecImpl.Load(transitions + (i5 * vecImpl.VectorByteWidth));
                    var t7 = vecImpl.Load(transitions + (i6 * vecImpl.VectorByteWidth));

                    var t01 = vecImpl.Shuffle(t1, transition);
                    var t23 = vecImpl.Shuffle(t3, t2);
                    var t45 = vecImpl.Shuffle(t5, t4);
                    var t67 = vecImpl.Shuffle(t7, t6);

                    var t0123 = vecImpl.Shuffle(t23, t01);
                    var t4567 = vecImpl.Shuffle(t67, t45);

                    transition = vecImpl.Shuffle(t4567, t0123);
                }

                // handle any leftovers
                for (; at < end; ++at)
                {
                    var next = vecImpl.Load(transitions + (at[0] * vecImpl.VectorByteWidth));
                    transition = vecImpl.Shuffle(next, transition);
                }
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