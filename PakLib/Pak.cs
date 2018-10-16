using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace PakLib
{
    public sealed class PakEntryTable : List<PakEntryMetadata> { }

    /// <summary>
    /// Represents an Unreal Engine 4 pak file.
    /// </summary>
    public sealed class Pak : IDisposable
    {
        public const int Magic = 0x5A6F12E1;
        private const int HashSize = 20;

        private long _indexOffset;
        private long _indexSize;
        private byte[] _indexHash;
        private long _encryptedIndexOffset;
        private PakEntryTable _entryTable;

        private bool _isDisposed;

        private readonly Stream _stream;

        private Pak(Stream stream)
        {
            _stream = stream;
        }

        /// <summary>
        /// The version used by this pak.
        /// </summary>
        public PakVersion Version { get; private set; }

        /// <summary>
        /// <c>true</c> if the file index table is encrypted; Otherwise, <c>false</c>.
        /// </summary>
        public bool IsIndexEncrypted { get; private set; }

        /// <summary>
        /// Identifies the GUID of the encryption key that will be used to decrypt this pak.
        /// </summary>
        public Guid EncryptionKeyGuid { get; private set; }

        /// <summary>
        /// Gets the mount point for all files in this pak.
        /// </summary>
        public string MountPoint { get; private set; }

        /// <summary>
        /// Opens an Unreal Engine 4 pak file.
        /// </summary>
        /// <param name="path">The path to a pak file.</param>
        /// <returns></returns>
        public static async Task<Pak> OpenAsync(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException(nameof(stream));

            var pak = new Pak(stream);
            await Task.Run(() => pak.ReadHeader());
            return pak;
        }

        /// <summary>
        /// Opens an Unreal Engine 4 pak file.
        /// </summary>
        /// <param name="path">The path to a pak file.</param>
        /// <returns></returns>
        public static Task<Pak> OpenAsync(string path)
        {
            return OpenAsync(File.OpenRead(path));
        }

        /// <summary>
        /// Gets a <see cref="PakEntryTable"/> that lists the files embedded in this pak.
        /// </summary>
        /// <returns></returns>
        public Task<PakEntryTable> GetEntriesAsync()
        {
            return Task.Run(() =>
            {
                if (IsIndexEncrypted)
                {
                    throw new IndexTableEncryptedException();
                }

                if (_entryTable == null)
                {
                    ReadEntries();
                }

                return _entryTable;
            });
        }

        /// <summary>
        /// Gets a <see cref="PakEntryTable"/> that lists the files embedded in this pak.
        /// </summary>
        /// <param name="encryptionKey">The encryption key used to decrypt the index.</param>
        /// <returns></returns>
        public Task<PakEntryTable> GetEntriesAsync(string encryptionKey)
        {
            return Task.Run(() =>
            {
                if (_entryTable == null)
                {
                    ReadEntries(encryptionKey);
                }

                return _entryTable;
            });
        }

        /// <summary>
        /// Gets a <see cref="Stream"/> that contains the data for an entry.
        /// </summary>
        /// <param name="metadata">The metadata for the entry.</param>
        /// <returns></returns>
        public Task<Stream> GetEntryStreamAsync(PakEntryMetadata metadata)
        {
            return Task.Run(() => GetEntryStream(metadata));
        }

        /// <summary>
        /// Gets a <see cref="Stream"/> that contains the decrypted data for an entry.
        /// </summary>
        /// <param name="metadata">The metadata for the entry.</param>
        /// <param name="encryptionKey">The encryption key used to decrypt the data.</param>
        /// <returns></returns>
        public Task<Stream> GetEntryStreamAsync(PakEntryMetadata metadata, string encryptionKey)
        {
            return Task.Run(() =>
            {
                var stream = GetEntryStream(metadata);
                DecryptEntryStream(stream, metadata.Offset, encryptionKey);
                return stream;
            });
        }

        /// <summary>
        /// Disposes this instance which will close the file stream.
        /// </summary>
        public void Dispose()
        {
            if (!_isDisposed)
            {
                _stream.Dispose();
                _isDisposed = true;
            }
        }

        private void ReadHeader()
        {
            using (var reader = new BinaryReader(_stream, Encoding.UTF8, true))
            {
                const int baseHeaderSize = 44;
                _stream.Seek(-baseHeaderSize, SeekOrigin.End);

                int magic = reader.ReadInt32();
                if (Magic != magic)
                {
                    throw new MagicNumberMismatchException(Magic, magic);
                }

                Version = (PakVersion)reader.ReadInt32();

                _indexOffset = reader.ReadInt64();
                _indexSize = reader.ReadInt64();
                _indexHash = reader.ReadBytes(HashSize);

                if (Version >= PakVersion.IndexEncryption)
                {
                    _stream.Seek(-(baseHeaderSize + 1), SeekOrigin.End);
                    _encryptedIndexOffset = _stream.Position;
                    IsIndexEncrypted = reader.ReadBoolean();
                }

                EncryptionKeyGuid = Guid.Empty;
                if (Version >= PakVersion.EncryptionKeyGuid)
                {
                    _stream.Seek(-(baseHeaderSize + 1 + 16), SeekOrigin.End);
                    EncryptionKeyGuid = new Guid(reader.ReadBytes(16));
                }
            }
        }

        private void ReadEntries()
        {
            _entryTable = new PakEntryTable();

            using (var reader = new BinaryReader(_stream, Encoding.UTF8, true))
            {
                _stream.Seek(_indexOffset, SeekOrigin.Begin);
                MountPoint = reader.ReadFString();
                int fileCount = reader.ReadInt32();
                for (int i = 0; i < fileCount; i++)
                {
                    string fileName = reader.ReadFString();
                    PakEntryMetadata metadata = ReadEntryMetadata(reader);
                    metadata.FileName = fileName;

                    _entryTable.Add(metadata);
                }
            }
        }

        private void ReadEntries(string encryptionKey)
        {
            if (!IsIndexEncrypted)
            {
                ReadEntries();
                return;
            }

            _entryTable = new PakEntryTable();

            byte[] indexBlock;

            using (var reader = new BinaryReader(_stream, Encoding.UTF8, true))
            {
                _stream.Seek(_indexOffset, SeekOrigin.Begin);
                indexBlock = reader.ReadBytes((int)_indexSize);

                try
                {
                    var aes = new AesManaged
                    {
                        Mode = CipherMode.ECB,
                        Padding = PaddingMode.Zeros,
                        Key = encryptionKey.ConvertHexStringToBytes()
                    };
                    aes.CreateDecryptor().TransformBlock(indexBlock, 0, indexBlock.Length, indexBlock, 0);
                }
                catch (Exception ex) when (ex is ArgumentException || ex is FormatException || ex is CryptographicException)
                {
                    _entryTable = null;
                    throw new IncorrectEncryptionKeyException();
                }
            }

            using (var reader = new BinaryReader(new MemoryStream(indexBlock)))
            {
                try
                {
                    MountPoint = reader.ReadFString();
                    int fileCount = reader.ReadInt32();
                    for (int i = 0; i < fileCount; i++)
                    {
                        string fileName = reader.ReadFString();
                        PakEntryMetadata metadata = ReadEntryMetadata(reader);
                        metadata.FileName = fileName;

                        _entryTable.Add(metadata);
                    }
                }
                catch (ArgumentOutOfRangeException) // reader.ReadFString will throw this if MountPoint cannot be read
                {
                    _entryTable = null;
                    throw new IncorrectEncryptionKeyException();
                }
            }
        }

        private PakEntryMetadata ReadEntryMetadata(BinaryReader reader)
        {
            var metadata = new PakEntryMetadata
            {
                Offset = reader.ReadInt64(),
                Size = reader.ReadInt64(),
                UncompressedSize = reader.ReadInt64(),
                CompressionMethod = (PakCompressionMethod)reader.ReadInt32(),
                Hash = reader.ReadBytes(20)
            };

            if (Version >= PakVersion.CompressionEncryption)
            {
                if (metadata.CompressionMethod != PakCompressionMethod.None)
                {
                    // TODO: Compression blocks
                    throw new NotImplementedException();
                }

                metadata.Flags = (PakEntryFlags)reader.ReadByte();
                metadata.CompressionBlockSize = reader.ReadInt32();
            }

            return metadata;
        }

        private long Align(long value, long alignment = 16)
        {
            return (value + alignment - 1) & ~(alignment - 1);
        }

        private Stream GetEntryStream(PakEntryMetadata metadata)
        {
            var entryStream = new MemoryStream();

            using (var reader = new BinaryReader(_stream, Encoding.UTF8, true))
            using (var writer = new BinaryWriter(entryStream, Encoding.UTF8, true))
            {
                _stream.Seek(metadata.Offset, SeekOrigin.Begin);
                ReadEntryMetadata(reader);

                byte[] buffer = reader.ReadBytes((int)metadata.Size);
                writer.Write(buffer, 0, buffer.Length);
            }

            entryStream.Seek(0, SeekOrigin.Begin);
            return entryStream;
        }

        private void DecryptEntryStream(Stream entryStream, long offset, string encryptionKey)
        {
            var aes = new AesManaged
            {
                Mode = CipherMode.ECB,
                Padding = PaddingMode.Zeros,
                Key = encryptionKey.ConvertHexStringToBytes()
            };

            using (var reader = new BinaryReader(_stream, Encoding.UTF8, true))
            using (var writer = new BinaryWriter(entryStream, Encoding.UTF8, true))
            {
                entryStream.Seek(0, SeekOrigin.Begin);
                _stream.Seek(offset, SeekOrigin.Begin);
                ReadEntryMetadata(reader);

                long bytesRemaining = entryStream.Length;
                while (bytesRemaining > 0)
                {
                    long bytesToRead = bytesRemaining;
                    if (bytesToRead > 256) bytesToRead = 256;
                    long alignedBytesToRead = Align(bytesToRead);
                    byte[] buffer = reader.ReadBytes((int)alignedBytesToRead);
                    aes.CreateDecryptor().TransformBlock(buffer, 0, buffer.Length, buffer, 0);
                    writer.Write(buffer, 0, (int)bytesToRead);
                    bytesRemaining -= bytesToRead;
                }
            }

            entryStream.Seek(0, SeekOrigin.Begin);
        }
    }
}
