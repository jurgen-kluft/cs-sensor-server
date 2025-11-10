using System;
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
    public class PersistentReadOnlyLog
    {
        private readonly MemoryMappedFile mMemoryMappedFile;
        private readonly MemoryMappedViewAccessor mAccessor;
        private readonly long sMaxSize;
        private long mReadCursor;
        
        public PersistentReadOnlyLog(string filePath)
        {
            // Create the file on disk if it doesn't exist and fill it with zeros
            FileInfo fileInfo = new FileInfo(filePath);
            if (fileInfo.Exists)
            {
                sMaxSize = fileInfo.Length;
				FileStream fs = new(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
				mMemoryMappedFile = MemoryMappedFile.CreateFromFile(fs, null, sMaxSize, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
                mAccessor = mMemoryMappedFile.CreateViewAccessor(0, sMaxSize, MemoryMappedFileAccess.Read);

                mReadCursor = 8;
            }
        }

        private void Flush()
        {
            mAccessor.Flush();
        }

        public bool Read(byte[] data, long dataOffset, long dataLength)
        {
            if (dataLength < 0 || dataOffset < 0 || (mReadCursor + dataLength) > sMaxSize)
            {
                return false;
            }
            Int64 writeCursor = mAccessor.ReadInt64(0); // Read write cursor at start of file
            if ((mReadCursor + dataLength) > writeCursor)
            {
                return false;
            }
            mAccessor.ReadArray(mReadCursor, data, (int)dataOffset, (int)dataLength);
            mReadCursor += dataLength;
            return true;
        }

        public void Dispose()
        {
            mAccessor.Dispose();
            mMemoryMappedFile.Dispose();
        }

    }
}