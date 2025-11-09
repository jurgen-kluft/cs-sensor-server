using System;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace sensorserver
{
    // Notes:
    // - Receiving the sensor data packets from TCP or UDP is also written
    //   directly into a sensor data packet log. This is mainly done to 
    //   facilitate debugging and analysis of incoming data. We are also
    //   later able to reconstruct the sensor data streams from the packet log.
    //
    // Requirements for the sensor stream database:
    // - Efficiently store large amounts of sensor data streams
    // - Allow for fast read and write access to individual streams
    // - Support for multiple concurrent streams
    // - Recoverable in case of application crash or OS reboot/shutdown
    // - Recoverable in case of hard reset or power loss
    // - Easy to construct a database to easily obtain the blocks for a specific stream

    // Design:
    // - We provide an API to allocate 64KB blocks
    // - A block has a 256-byte header with metadata
    // - The rest of the block is used for storing sensor data
    // - The blocks are stored in a single large memory-mapped file on disk
    // - We periodically flush the memory-mapped file to disk


    // The sensor stream database is responsible for storing incoming sensor data streams into
    // a single large memory-mapped file for efficient access and storage.
    public class StreamBlocksLog
    {
        private readonly MemoryMappedFile mMemoryMappedFile;
        private readonly MemoryMappedViewAccessor mAccessor;
        private readonly long sBlockSize = 64 * 1024; // 64KB blocks
        private readonly long sMaxSize;
        private readonly long sHeaderSize = 64;
        private long mBlockIndex;

        public StreamBlocksLog(string filePath, long maxSize)
        {
            // Create the file on disk if it doesn't exist and fill it with zeros
            sMaxSize = maxSize;

            FileStream fs = new(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            fs.SetLength(maxSize);
            fs.Close();

            mMemoryMappedFile = MemoryMappedFile.CreateFromFile(filePath, FileMode.OpenOrCreate, null, sMaxSize);
            mAccessor = mMemoryMappedFile.CreateViewAccessor(0, sMaxSize, MemoryMappedFileAccess.ReadWrite);
            blockIndex = 0;
        }

        private void Flush()
        {
            mAccessor.Flush();
        }

        public bool NewBlock(out long outBlockIndex)
        {
            long blockOffset = headerSize + (mBlockIndex * sBlockSize);
            if (blockOffset + sBlockSize > maxSize)
            {
                outBlockIndex = -1;
                return false;
            }
            outBlockIndex = mBlockIndex;
            mBlockIndex++;
            return true;
        }

        public bool BlockWriteAt(long blockIndex, long blockOffset, byte[] data, long dataOffset, long dataLength)
        {
            if (blockIndex < 0 || blockOffset < 0 || dataLength < 0 || dataOffset < 0 || (blockOffset + dataLength)>sBlockSize)
            {
                return false;
            }
            long writeOffset = sHeaderSize + (blockIndex * sBlockSize) + blockOffset;
            mAccessor.WriteArray(writeOffset, data, (int)dataOffset, (int)dataLength);
            return true;
        }

        public void Dispose()
        {
            mAccessor.Dispose();
            mmf.Dispose();
        }

    }
}