namespace PakLib
{
    /// <summary>
    /// Contains the metadata for a file embedded in a pak.
    /// </summary>
    public sealed class PakEntryMetadata
    {
        public string FileName { get; internal set; }
        public long Size { get; internal set; }
        public long UncompressedSize { get; internal set; }
        public PakCompressionMethod CompressionMethod { get; internal set; }
        public PakEntryFlags Flags { get; internal set; }

        public bool IsEncrypted { get => Flags.HasFlag(PakEntryFlags.Encrypted); }
        public bool IsDeleted { get => Flags.HasFlag(PakEntryFlags.Deleted); }
        public bool IsCompressed { get => CompressionMethod == PakCompressionMethod.None; }

        internal byte[] Hash { get; set; }
        internal long Offset { get; set; }
        internal int CompressionBlockSize { get; set; }
    }
}
