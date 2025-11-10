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
            PersistentReadOnlyLog log = new PersistentReadOnlyLog("E:\\sensor_packet_log.dat");

            // Just read sensor readings
            // Ticks (6 bytes), Mac-Address (6 bytes), Sensor ID (2 bytes), Sensor Value (2 bytes)
            byte[] sensorPacket = new byte[6 + 6 + 2 + 2];
            while (log.Read(sensorPacket, 0, sensorPacket.Length))
            {
                long ticks = BitConverter.ToInt64(sensorPacket, 0);
                byte[] macAddress = new byte[6];
                Array.Copy(sensorPacket, 6, macAddress, 0, 6);
                ushort sensorId = BitConverter.ToUInt16(sensorPacket, 12);
                ushort sensorValue = BitConverter.ToUInt16(sensorPacket, 14);

                Console.WriteLine($"Ticks: {ticks}, MAC: {BitConverter.ToString(macAddress)}, Sensor ID: {sensorId}, Sensor Value: {sensorValue}");
            }
            

            Console.WriteLine("Press Enter to close the log...");
            for (; ; )
            {
                string line = Console.ReadLine();
                if (string.IsNullOrEmpty(line))
                    break;
				if (log.Read(sensorPacket, 0, sensorPacket.Length))
			    {
					long ticks = BitConverter.ToInt64(sensorPacket, 0);
					byte[] macAddress = new byte[6];
					Array.Copy(sensorPacket, 6, macAddress, 0, 6);
					ushort sensorId = BitConverter.ToUInt16(sensorPacket, 12);
					ushort sensorValue = BitConverter.ToUInt16(sensorPacket, 14);

					Console.WriteLine($"Ticks: {ticks}, MAC: {BitConverter.ToString(macAddress)}, Sensor ID: {sensorId}, Sensor Value: {sensorValue}");
				}
			}

            log.Dispose();
        }
    }
}
