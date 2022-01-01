using System.IO;

namespace AkkoCore.Extensions;

public static class StreamExt
{
    /// <summary>
    /// Gets a copy of this <see cref="Stream"/>.
    /// </summary>
    /// <param name="stream">This stream.</param>
    /// <param name="rewind">Defines whether this stream should have its position set to 0 after the copy is created.</param>
    /// <returns>A seekable, readable and writable copy of this <see cref="Stream"/>.</returns>
    public static Stream GetCopy(this Stream stream, bool rewind = true)
    {
        var result = new MemoryStream();

        if (stream.Position == stream.Length)
            stream.Position = 0;

        stream.CopyTo(result);
        result.Position = 0;

        if (rewind)
            stream.Position = 0;

        return result;
    }
}
