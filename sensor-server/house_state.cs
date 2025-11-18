using System;
using System.Collections.Generic;

using StreamId = ulong;

namespace sensorserver
{

    public class SensorConfig
    {
        public SensorConfig()
        {
            Name = string.Empty;
            Location = string.Empty;
            Mac = new byte[6];
            Type = SensorType.Unknown;
        }

        public SensorConfig(string name, string location, byte[] mac, SensorType type)
        {
            Name = name;
            Location = location;
            Mac = mac;
            Type = type;
        }

        public string Name { get; set; }
        public string Location { get; set; }
        public byte[] Mac { get; set; }
        public SensorType Type { get; set; }
    }

    public class HouseConfiguration
    {
        public string StoragePath { get; set; }
        public long StorageSize { get; set; }
        public string StatePath { get; set; }
        public long StateSize { get; set; }
        public int TcpPort { get; set; }
        public int UdpPort { get; set; }
        public string UdsPort { get; set; }
        public int FlushPeriodInSeconds { get; set; }
        public List<SensorConfig> Sensors { get; set; }
    }

    // Represents the overall state of the house, including sensors, devices, and their statuses.
    public class HouseState
    {
        public HouseConfiguration Configuration { get; set; }
        private static StreamId InvalidStreamId => 0;
        public static StreamId MakeStreamId(byte[] Mac, ushort SensorId)
        {
            if (Mac.Length != 6)
                return InvalidStreamId;

            StreamId id = 0;
            id |= (StreamId)Mac[0] << 40;
            id |= (StreamId)Mac[1] << 32;
            id |= (StreamId)Mac[2] << 24;
            id |= (StreamId)Mac[3] << 16;
            id |= (StreamId)Mac[4] << 8;
            id |= (StreamId)Mac[5] << 0;
            id <<= 16;
            id |= SensorId;
            return id;
        }

        private PersistentState mPersistentState;

        public List<StreamId> SensorIds { get; set; }
        public List<int> SensorValues { get; set; }
        public List<SensorConfig> SensorInfos { get; set; }
        public Dictionary<StreamId, int> SensorIdToIndex { get; set; }

        private void DecodeSensorConfig(JsonDecoder decoder, SensorConfig sensor)
        {
            var fields = new Dictionary<string, Action<JsonDecoder>>
            {
                ["name"] = d => { sensor.Name = d.DecodeString(); },
                ["location"] = d => { sensor.Location = d.DecodeString(); },
                ["mac"] = d => { sensor.Mac = d.DecodeByteArray(); },
                ["type"] = d => { sensor.Type = (SensorType)d.DecodeInt32(); }
            };
            decoder.Decode(fields);
        }

        public bool ReadConfiguration(string filepath)
        {
            HouseConfiguration cfg = new HouseConfiguration();

            string jsonText = System.IO.File.ReadAllText(filepath);

            JsonDecoder jsonDecoder = JsonDecoder.NewJsonDecoder();
            jsonDecoder.Begin(jsonText);
            {
                var fields = new Dictionary<string, Action<JsonDecoder>>
                {
                    ["storage_path"] = decoder => { cfg.StoragePath          = decoder.DecodeString(); },
                    ["storage_size"] = decoder => { cfg.StorageSize          = decoder.DecodeInt64(); },
                    ["state_path"]   = decoder => { cfg.StatePath            = decoder.DecodeString(); },
                    ["state_size"]   = decoder => { cfg.StateSize            = decoder.DecodeInt64(); },
                    ["tcp_port"]     = decoder => { cfg.TcpPort              = (int)decoder.DecodeInt32(); },
                    ["udp_port"]     = decoder => { cfg.UdpPort              = (int)decoder.DecodeInt32(); },
                    ["uds_port"]     = decoder => { cfg.UdsPort              = decoder.DecodeString(); },
                    ["flush"]        = decoder => { cfg.FlushPeriodInSeconds = (int)decoder.DecodeInt32(); },
                    ["sensors"] = decoder =>
                    {
                        cfg.Sensors = new List<SensorConfig>(4);
                        while (!decoder.ReadUntilArrayEnd())
                        {
                            var sensor = new SensorConfig();
                            cfg.Sensors.Add(sensor);
                            DecodeSensorConfig(decoder, sensor);
                        }
                    }
                };
                jsonDecoder.Decode(fields);
            }
            jsonDecoder.End();

            // Build the sensor state from the configuration
            SensorIds = new List<StreamId>(cfg.Sensors.Count);
            SensorValues = new List<int>(cfg.Sensors.Count);
            SensorInfos = new List<SensorConfig>(cfg.Sensors.Count);
            SensorIdToIndex = new Dictionary<StreamId, int>(cfg.Sensors.Count);

            foreach (var sensor in cfg.Sensors)
            {
                StreamId streamId = MakeStreamId(sensor.Mac, (ushort)sensor.Type);
                SensorIdToIndex[streamId] = SensorIds.Count;
                SensorIds.Add(streamId);
                SensorValues.Add(0);
                SensorInfos.Add(sensor);
            }

            Configuration = cfg;

            // Prepare the sensor data append only log


            // Prepare the persistent state, also when a persistent state was present on disk we
            // read the content and update our house state.
            if (!ReadState())
                return false;

            return true;
        }

        private bool ReadState()
        {
            mPersistentState = new PersistentState(Configuration.StatePath, Configuration.StateSize);
            if (!mPersistentState.OpenReadWrite())
                return false;

            byte[] data = new byte[16];
            mPersistentState.Read(0, data, 0, data.Length);

            // First 4 bytes is number of entries
            int numEntries = BitConverter.ToInt32(data.AsSpan(0, 4));
            if (numEntries is < 0 or > 1024)
                return false;

            // Persistent state is nothing more than an array of
            // [Mac Address(6), Sensor Type(2), Sensor Value(2), Dummy(2), Count(4)]
            const int sizeofEntry = 6 + 2 + 2 + 2 + 4;
            data = new byte[sizeofEntry];
            mPersistentState.Read(4, data, 0, numEntries * sizeofEntry);

            byte[] mac = new byte[6];
            for (int i = 0; i < numEntries; i++)
            {
                var offset = i * sizeofEntry;
                Array.Copy(data, offset + 0, mac, 0, 6);
                ushort sensorType = BitConverter.ToUInt16(data, offset + 6);
                ushort sensorValue = BitConverter.ToUInt16(data, offset + 8);
                StreamId streamId = MakeStreamId(mac, sensorType);
                if (SensorIdToIndex.TryGetValue(streamId, out int sensorIndex))
                {
                    SensorValues[sensorIndex] = sensorValue;
                }
            }

            return false;
        }
    }
}
