using System;

namespace PakLib
{
    public enum PakVersion
    {
        Initial = 1,
        NoTimestamps = 2,
        CompressionEncryption = 3,
        IndexEncryption = 4,
        RelativeChunkOffsets = 5,
        DeleteRecords = 6,
        EncryptionKeyGuid = 7
    }

    public enum PakCompressionMethod
    {
        None = 0,
        Zip = 1,
        ActiveMask = 2,
        Blosc = 3
    }

    [Flags]
    public enum PakEntryFlags
    {
        None = 0,
        Encrypted = 1,
        Deleted = 2
    }
}