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
        private readonly MemoryMappedFile mMemoryMappedFile;
        private readonly MemoryMappedViewAccessor mAccessor;
        private readonly long sMaxSize;
        private long mWriteCursor;

        public PersistentAppendOnlyLog(string filePath, long maxSize)
        {
            // Create the file on disk if it doesn't exist and fill it with zeros
            sMaxSize = maxSize;

            FileStream fs = new(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            fs.SetLength(maxSize);

            mMemoryMappedFile = MemoryMappedFile.CreateFromFile(fs, null, sMaxSize, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true); 
            mAccessor = mMemoryMappedFile.CreateViewAccessor(0, sMaxSize, MemoryMappedFileAccess.ReadWrite);

            mWriteCursor = mAccessor.ReadInt64(0); // Read write cursor at start of file
            if (mWriteCursor < 8 || mWriteCursor >= sMaxSize)
            {
                mWriteCursor = 8; // Initialize write cursor after header
            }
        }

        private void Flush()
        {
            mAccessor.Flush();
        }

        public bool Append(byte[] data, long dataOffset, long dataLength)
        {
            if (dataLength < 0 || dataOffset < 0 || (mWriteCursor + dataLength) > sMaxSize)
            {
                return false;
            }
            mAccessor.WriteArray(mWriteCursor, data, (int)dataOffset, (int)dataLength);
            mAccessor.Write(0, mWriteCursor + dataLength); // Update write cursor at start of file
            mWriteCursor += dataLength;
            return true;
        }

        public void Dispose()
        {
            mAccessor.Dispose();
            mMemoryMappedFile.Dispose();
        }

    }
}