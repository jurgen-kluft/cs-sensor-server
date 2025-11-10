using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Collections.Generic;

namespace sensorserver
{
    // Requirements for the stream block log:
    //   - Efficiently store large amounts of blocks of data
    //   - Allow for fast read and write access to individual streams
    //   - Support for multiple concurrent streams
    //   - Recoverable in case of application crash or OS reboot/shutdown
    //   - Recoverable in case of hard reset or power loss
    //   - Easy to construct a database to easily obtain the blocks for a specific stream

    // Example block header (for sensor data):
    //   - Block Id: 8 bytes (Mac + Sensor Type?)
    //   - Time Ticks: 8 bytes
    //   - Zeros: 12 bytes
    //   - CRC32: 4 bytes

    // Design:
    //   - We provide an API to allocate 64KB blocks
    //   - A block has a 256-byte header with metadata
    //   - The rest of the block is used for storing sensor data
    //   - The blocks are stored in a single large memory-mapped file on disk
    //   - We periodically flush the memory-mapped file to disk

    // The stream block log is responsible for storing sensor data blocks into
    // a single large memory-mapped file for efficient access and storage.
    public class StreamBlockLog
    {
		private static readonly long sLogHeaderSize = 32;
		private static readonly long sMaxBlockCount = 32 * 1024;
		private static readonly long sBlockHeaderSize = 64;
		private static readonly long sSizeOfTocEntry = 32;
		private static readonly long sSizeOfToc = sMaxBlockCount * sSizeOfTocEntry;
		
        private readonly MemoryMappedFile mMemoryMappedFile;
        private readonly MemoryMappedViewAccessor mAccessor;
        private readonly long sMaxSize;
        private long mBlockCount;
        private long mAppendCursor;

        public readonly struct BlockId
        {
            public readonly byte[] Data = new byte[16];
			public BlockId()
			{
			}
		}
        private struct BlockInfo
        {
            public Int64 Offset {  get; set; }
            public Int64 Size { get; set; }
        }
		private readonly Dictionary<BlockId, BlockInfo> mBlockOffsetMap;

		public StreamBlockLog(string filePath, long maxSize)
        {
            // Create the file on disk if it doesn't exist and fill it with zeros
            sMaxSize = maxSize;

            FileStream fs = new(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            fs.SetLength(maxSize);
            fs.Close();

            mMemoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.OpenOrCreate, null, sMaxSize);
            mAccessor = mMemoryMappedFile.CreateViewAccessor(0, sMaxSize, MemoryMappedFileAccess.ReadWrite);

            mBlockCount = mAccessor.ReadInt64(0); // Read block count at start of file
            mAppendCursor = mAccessor.ReadInt64(8); // Read append cursor after block count
            if (mBlockCount < 0 || mBlockCount >= sMaxBlockCount)
            {
                mBlockCount = 0; // Initialize block count 
                mAppendCursor = sLogHeaderSize + sSizeOfToc; // Initialize append cursor after header and TOC
            }

            // Read the TOC entries
            mBlockOffsetMap = [];
            for (long i = 0; i < mBlockCount; i++)
            {
                long tocOffset = sLogHeaderSize + (i * sSizeOfTocEntry);
                mAccessor.Read(tocOffset, out BlockId tocEntry);
                var blockInfo = new BlockInfo
                {
                    Offset = mAccessor.ReadInt64(tocOffset + 16),
                    Size = mAccessor.ReadInt64(tocOffset + 24)
                };
                mBlockOffsetMap[tocEntry] = blockInfo;
            }
        }

        private void Flush()
        {
            mAccessor.Flush();
        }

        public bool NewBlock(BlockId blockId, long blockSize)
        {
            if (!mBlockOffsetMap.TryGetValue(blockId, out var blockInfo))
            {
                if (mBlockCount >= sMaxBlockCount)
                {
                    return false; // Max block count reached
                }
                long tocOffset = sLogHeaderSize + (mBlockCount * sSizeOfTocEntry);
                mAccessor.WriteArray(tocOffset, blockId.Data, 0, blockId.Data.Length);
                long blockOffset = mAppendCursor;
                mAccessor.Write(tocOffset + 16, blockOffset);
                mAccessor.Write(tocOffset + 24, blockSize);
                mBlockOffsetMap[blockId] = new BlockInfo { Offset = blockOffset, Size = blockSize };
                mBlockCount++;
                mAppendCursor += IntegerExtensions.AlignUp(blockSize + sBlockHeaderSize, 32); // Align to 32 bytes
                mAccessor.Write(0, mBlockCount); // Update block count at start of file
                mAccessor.Write(8, mAppendCursor); // Update append cursor after block count
            }
            return true;
        }

        public bool BlockWriteAt(BlockId blockId, long blockOffset, byte[] data, long dataOffset, long dataLength)
        {
			if (mBlockOffsetMap.TryGetValue(blockId, out var blockInfo))
			{
				if (blockOffset < 0 || dataLength < 0 || dataOffset < 0 || (blockOffset + dataLength) > blockInfo.Size)
                {
                    return false;
                }
                long writeOffset = blockInfo.Offset + blockOffset;
                mAccessor.WriteArray(writeOffset, data, (int)dataOffset, (int)dataLength);
                return true;
            }
            return false;
        }

        public void Dispose()
        {
            mAccessor.Dispose();
            mMemoryMappedFile.Dispose();
        }
    }
}