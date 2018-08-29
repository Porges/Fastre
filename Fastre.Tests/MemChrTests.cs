using Hedgehog;
using System;
using Xunit;

namespace Fastre.Tests
{
    public sealed class MemChrTests
    {
        private static byte[] MakeArray(int size, int index)
        {
            var result = new byte[size];
            result[index] = 1;
            return result;
        }

        private static unsafe int FindVector128(byte[] array)
        {
            fixed (byte* pArray = array)
            {
                var offset = default(Vector128Impl).MemChr(1, pArray, pArray + array.Length);
                return checked((int)(offset - pArray));
            }
        }
        private static unsafe int FindVector256(byte[] array)
        {
            fixed (byte* pArray = array)
            {
                var offset = default(Vector256Impl).MemChr(1, pArray, pArray + array.Length);
                return checked((int)(offset - pArray));
            }
        }

        [Fact]
        public void Vector128Impl_IsCorrect()
        {
            Property.Check(
                from arraySize in Property.ForAll(Gen.Int32(Range.Constant(1, 500)))
                from index in Property.ForAll(Gen.Int32(Range.Constant(0, arraySize - 1)))
                let array = MakeArray(arraySize, index)
                select index == FindVector128(array));
        }

        [Fact]
        public void Vector256Impl_IsCorrect()
        {
            Property.Check(
                from arraySize in Property.ForAll(Gen.Int32(Range.Constant(1, 500)))
                from index in Property.ForAll(Gen.Int32(Range.Constant(0, arraySize - 1)))
                let array = MakeArray(arraySize, index)
                select index == FindVector256(array));
        }
    }
}
