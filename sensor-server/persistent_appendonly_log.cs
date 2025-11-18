using System.IO;
using System.IO.MemoryMappedFiles;

namespace sensorserver
{
    // Notes:
    //   - Receiving the sensor data packets from TCP or UDP is written
    //     directly into a sensor data packet log. This is mainly done to
    //     facilitate debugging and analysis of incoming data. We are also
    //     later able to reconstruct the sensor blocks from the packet log.
    //
    public class PersistentAppendOnlyLog
    {
        private MemoryMappedFile mMemoryMappedFile;
        private MemoryMappedViewAccessor mAccessor;
        private readonly string sFilePath;
        private readonly long sMaxSize;
        private long mWriteCursor;

        public PersistentAppendOnlyLog(string filePath, long maxSize)
        {
            sFilePath = filePath;
            sMaxSize = maxSize;
        }

        public bool OpenReadWrite()
        {
            const FileOptions fileOptions = FileOptions.WriteThrough | FileOptions.SequentialScan;
            FileStream fileStream = new(sFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite, 4096, fileOptions);

            mMemoryMappedFile = MemoryMappedFile.CreateFromFile(fileStream, null, sMaxSize, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
            mAccessor = mMemoryMappedFile.CreateViewAccessor(0, sMaxSize, MemoryMappedFileAccess.ReadWrite);

            mWriteCursor = mAccessor.ReadInt64(0); // Read write cursor at start of file
            if (mWriteCursor < 8 || mWriteCursor >= sMaxSize)
            {
                mWriteCursor = 8; // Initialize write cursor after header
            }

            return true;
        }

        public bool OpenReadOnly()
        {
            FileStream fileStream = new(sFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            mMemoryMappedFile = MemoryMappedFile.CreateFromFile(fileStream, null, sMaxSize, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
            mAccessor = mMemoryMappedFile.CreateViewAccessor(0, sMaxSize, MemoryMappedFileAccess.Read);
            mWriteCursor = -1; // Not used in read-only mode
            return true;
        }

        private void Flush()
        {
            mAccessor.Flush();
        }

        public bool Append(byte[] data, long dataOffset, long dataLength)
        {
            if (mWriteCursor<0 || dataLength < 0 || dataOffset < 0 || (mWriteCursor + dataLength) > sMaxSize)
            {
                return false;
            }
            mAccessor.WriteArray(mWriteCursor, data, (int)dataOffset, (int)dataLength);
            mAccessor.Write(0, mWriteCursor + dataLength); // Update write cursor at start of file
            mWriteCursor += dataLength;
            return true;
        }

        public bool Read(long readOffset, byte[] data, long dataOffset, long dataLength)
        {
            if (dataLength < 0 || dataOffset < 0 || readOffset < 8 || (readOffset + dataLength) > sMaxSize)
            {
                return false;
            }
            mAccessor.ReadArray(readOffset, data, (int)dataOffset, (int)dataLength);
            return true;
        }

        public void Dispose()
        {
            mAccessor.Dispose();
            mMemoryMappedFile.Dispose();
        }

    }
}
