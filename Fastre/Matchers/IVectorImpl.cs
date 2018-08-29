namespace Fastre
{
    /// <summary>
    /// Abstracts over the details of a particular vector type,
    /// so that <see cref="VectoredMatcher{TVectorImpl, TVectorType}"/> can reuse the same code.
    ///
    /// This interface is designed to be implemented by a <c>struct</c> type, which is
    /// instantiated by <c>default</c>. This will allow the JITter to inline the methods
    /// into the callsite.
    /// </summary>
    public interface IVectorImpl<TVectorType>
    {
        bool IsSupported { get; }

        int VectorByteWidth { get; }

        TVectorType Id { get; }

        TVectorType Zero { get; }

        bool IsZero(TVectorType vector);

        unsafe TVectorType Load(sbyte* ptr);

        TVectorType Shuffle(TVectorType vec, TVectorType mask);

        sbyte Extract(TVectorType vec, byte index);

        unsafe byte* MemChr(byte lookFor, byte* start, byte* end);
    }
}
