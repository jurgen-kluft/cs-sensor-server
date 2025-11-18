using System.IO;
using System.IO.MemoryMappedFiles;

namespace sensorserver
{
    public class PersistentState
    {
        private MemoryMappedFile mMemoryMappedFile;
        private MemoryMappedViewAccessor mAccessor;
        private readonly long sMaxSize;
        private readonly string mFilePath;

        public PersistentState(string filePath, long maxSize)
        {
            // Create the file on disk if it doesn't exist and fill it with zeros
            mFilePath  = filePath;
            sMaxSize = maxSize;
        }

        public bool OpenReadWrite()
        {
            FileStream fileStream = new(mFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.ReadWrite);
            fileStream.SetLength(sMaxSize);
            mMemoryMappedFile = MemoryMappedFile.CreateFromFile(fileStream, null, sMaxSize, MemoryMappedFileAccess.ReadWrite, HandleInheritability.None, true);
            mAccessor = mMemoryMappedFile.CreateViewAccessor(0, sMaxSize, MemoryMappedFileAccess.ReadWrite);
            return true;
        }

        public bool OpenReadOnly()
        {
            FileStream fileStream = new(mFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            mMemoryMappedFile = MemoryMappedFile.CreateFromFile(fileStream, null, sMaxSize, MemoryMappedFileAccess.Read, HandleInheritability.None, true);
            mAccessor = mMemoryMappedFile.CreateViewAccessor(0, sMaxSize, MemoryMappedFileAccess.Read);
            return true;
        }

        private void Flush()
        {
            mAccessor.Flush();
        }

        public bool Write(byte[] data, long dataOffset, long dataLength)
        {
            if (dataLength < 0 || dataOffset < 0)
            {
                return false;
            }
            mAccessor.WriteArray(0, data, (int)dataOffset, (int)dataLength);
            return true;
        }

        public bool Read(byte[] data, long dataOffset, long dataLength)
        {
            if (dataLength < 0 || dataOffset < 0)
            {
                return false;
            }
            mAccessor.ReadArray(0, data, (int)dataOffset, (int)dataLength);
            return true;
        }

        public void Dispose()
        {
            mAccessor.Dispose();
            mMemoryMappedFile.Dispose();
        }

    }
}
