using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace sensorserver
{

    class Program
    {
        static void Main(string[] args)
        {
            PersistentAppendOnlyLog log = new PersistentAppendOnlyLog("E:\\sensor_packet_log.dat", 1024 * 1024 * 1024);

            // Just write 65536 sensor reading
            // Ticks (6 bytes), Mac-Address (6 bytes), Sensor ID (2 bytes), Sensor Value (2 bytes)
            byte[] sensorPacket = new byte[6 + 6 + 2 + 2];
            int i = 0;

			for (; i < 65536; i++)
            {
                long ticks = DateTime.UtcNow.Ticks;
                Array.Copy(BitConverter.GetBytes(ticks), 0, sensorPacket, 0, 6);
                Array.Copy(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x01 }, 0, sensorPacket, 6, 6);
                Array.Copy(BitConverter.GetBytes((ushort)1), 0, sensorPacket, 12, 2);
                Array.Copy(BitConverter.GetBytes((ushort)(i % 100)), 0, sensorPacket, 14, 2);
                log.Append(sensorPacket, 0, sensorPacket.Length);
            }

            Console.WriteLine("Press Enter to close the log...");
            for (; ; )
            {
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    break;

				long ticks = DateTime.UtcNow.Ticks;
				Array.Copy(BitConverter.GetBytes(ticks), 0, sensorPacket, 0, 6);
				Array.Copy(new byte[] { 0xDE, 0xAD, 0xBE, 0xEF, 0x00, 0x01 }, 0, sensorPacket, 6, 6);
				Array.Copy(BitConverter.GetBytes((ushort)1), 0, sensorPacket, 12, 2);
				Array.Copy(BitConverter.GetBytes((ushort)(i % 100)), 0, sensorPacket, 14, 2);
				log.Append(sensorPacket, 0, sensorPacket.Length);
                i++;
			}

			log.Dispose();
        }
    }
}
