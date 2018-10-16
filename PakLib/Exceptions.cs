using System;

namespace PakLib
{
    public sealed class MagicNumberMismatchException : Exception
    {
        public MagicNumberMismatchException(int expectedMagic, int actualMagic)
            : base($"Expected magic number {expectedMagic:X} but got {actualMagic:X}")
        {
            ExpectedMagic = expectedMagic;
            ActualMagic = actualMagic;
        }

        public int ExpectedMagic { get; }
        public int ActualMagic { get; }
    }

    public sealed class UnsupportedVersionException : Exception
    {
        public UnsupportedVersionException(int version, int minimumVersion)
            : base($"Unsupported version {version} (version must be equal to or greater than {minimumVersion})")
        {
            Version = version;
            MinimumVersion = minimumVersion;
        }

        public int Version { get; }
        public int MinimumVersion { get; }
    }

    public sealed class IndexTableEncryptedException : Exception
    {
        public IndexTableEncryptedException()
            : base("The index in this pak is encrypted")
        {
        }
    }

    public sealed class IncorrectEncryptionKeyException : Exception
    {
        public IncorrectEncryptionKeyException()
            : base("The key provided is incorrect.")
        {
        }
    }
}