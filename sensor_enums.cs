using System;
using System.Collections.Generic;

namespace SensorServer
{
    public enum SensorType : byte
    {
        Unknown = 0,      // Unknown sensor type
        Temperature = 1,  // (s8, °C)
        Humidity = 2,     // (s8, %)
        Pressure = 3,     // (s16, hPa)
        Light = 4,        // (s16, lux)
        UV = 5,           // (s16, index)
        CO = 6,           // (s16, ppm)
        CO2 = 7,          // (s16, ppm)
        HCHO = 8,         // (s16, ppm)
        VOC = 9,          // (s16, ppm)
        NOX = 10,         // (s16, ppm)
        PM005 = 11,       // (s16, µg/m3)
        PM010 = 12,       // (s16, µg/m3)
        PM025 = 13,       // (s16, µg/m3)
        PM040 = 14,       // (s16, µg/m3)
        PM100 = 15,       // (s16, µg/m3)
        Noise = 16,       // (s16, dB)
        Vibration = 17,   // (s8, levels)
        State = 18,       // (s32, sensor model/state)
        Battery = 19,     // (u8, battery level)
        Switch = 20,      // (u8, 0=none, 1=open, 2=close)
        Presence1 = 21,   // (u8)
        Presence2 = 22,   // (u8)
        Presence3 = 23,   // (u8)
        Distance1 = 24,   // (u16, cm)
        Distance2 = 25,   // (u16, cm)
        Distance3 = 26,   // (u16, cm)
        X = 27,           // (s16, cm)
        Y = 28,           // (s16, cm)
        Z = 29,           // (s16, cm)
        RSSI = 30,        // (s16, dBm)
        Perf1 = 31,       // Performance Metric 1
        Perf2 = 32,       // Performance Metric 2
        Perf3 = 33,       // Performance Metric 3
        SensorCount = 34  // Max number of sensor types
    }

    public enum SensorState : byte
    {
        Off = 0x10,
        On = 0x20,
        Error = 0x30
    }

    public enum SensorFieldType : byte
    {
        FieldTypeNone = 0x00,
        FieldTypeU1 = 0x01,
        FieldTypeU2 = 0x02,
        FieldTypeS8 = 0x08,
        FieldTypeS16 = 0x10,
        FieldTypeS32 = 0x20,
        FieldTypeS64 = 0x40,
        FieldTypeU8 = 0x88,
        FieldTypeU16 = 0x90,
        FieldTypeU32 = 0xA0,
        FieldTypeU64 = 0xC0
    }

    public static class SensorMappings
    {
        public static readonly Dictionary<string, SensorFieldType> SensorFieldNameToType = new Dictionary<string, SensorFieldType>
        {
            { "none", SensorFieldType.FieldTypeNone },
            { "u1", SensorFieldType.FieldTypeU1 },
            { "u2", SensorFieldType.FieldTypeU2 },
            { "s8", SensorFieldType.FieldTypeS8 },
            { "s16", SensorFieldType.FieldTypeS16 },
            { "s32", SensorFieldType.FieldTypeS32 },
            { "s64", SensorFieldType.FieldTypeS64 },
            { "u8", SensorFieldType.FieldTypeU8 },
            { "u16", SensorFieldType.FieldTypeU16 },
            { "u32", SensorFieldType.FieldTypeU32 },
            { "u64", SensorFieldType.FieldTypeU64 }
        };

        public static readonly Dictionary<string, SensorType> SensorNameToType = new Dictionary<string, SensorType>
        {
            { "temperature", SensorType.Temperature },
            { "humidity", SensorType.Humidity },
            { "pressure", SensorType.Pressure },
            { "light", SensorType.Light },
            { "co2", SensorType.CO2 },
            { "voc", SensorType.VOC },
            { "pm005", SensorType.PM005 },
            { "pm010", SensorType.PM010 },
            { "pm025", SensorType.PM025 },
            { "pm100", SensorType.PM100 },
            { "noise", SensorType.Noise },
            { "uv", SensorType.UV },
            { "co", SensorType.CO },
            { "vibration", SensorType.Vibration },
            { "openclose", SensorType.Switch },
            { "switch", SensorType.Switch },
            { "presence1", SensorType.Presence1 },
            { "presence2", SensorType.Presence2 },
            { "presence3", SensorType.Presence3 },
            { "distance1", SensorType.Distance1 },
            { "distance2", SensorType.Distance2 },
            { "distance3", SensorType.Distance3 },
            { "battery", SensorType.Battery },
            { "state", SensorType.State },
            { "x", SensorType.X },
            { "y", SensorType.Y },
            { "z", SensorType.Z },
            { "rssi", SensorType.RSSI },
            { "perf1", SensorType.Perf1 },
            { "perf2", SensorType.Perf2 },
            { "perf3", SensorType.Perf3 }
        };

        public static readonly Dictionary<SensorType, string> SensorTypeToName = new Dictionary<SensorType, string>
        {
            { SensorType.Temperature, "temperature" },
            { SensorType.Humidity, "humidity" },
            { SensorType.Pressure, "pressure" },
            { SensorType.Light, "light" },
            { SensorType.CO2, "co2" },
            { SensorType.VOC, "voc" },
            { SensorType.PM005, "pm005" },
            { SensorType.PM010, "pm010" },
            { SensorType.PM025, "pm025" },
            { SensorType.PM100, "pm100" },
            { SensorType.Noise, "noise" },
            { SensorType.UV, "uv" },
            { SensorType.CO, "co" },
            { SensorType.Vibration, "vibration" },
            { SensorType.Switch, "switch" },
            { SensorType.Presence1, "presence1" },
            { SensorType.Presence2, "presence2" },
            { SensorType.Presence3, "presence3" },
            { SensorType.Distance1, "distance1" },
            { SensorType.Distance2, "distance2" },
            { SensorType.Distance3, "distance3" },
            { SensorType.Battery, "battery" },
            { SensorType.State, "state" },
            { SensorType.X, "x" },
            { SensorType.Y, "y" },
            { SensorType.Z, "z" },
            { SensorType.RSSI, "rssi" },
            { SensorType.Perf1, "perf1" },
            { SensorType.Perf2, "perf2" },
            { SensorType.Perf3, "perf3" }
        };

        public static readonly Dictionary<SensorType, SensorFieldType> SensorTypeToFieldType = new Dictionary<SensorType, SensorFieldType>
        {
            { SensorType.Unknown, SensorFieldType.FieldTypeS16 },
            { SensorType.Temperature, SensorFieldType.FieldTypeS8 },
            { SensorType.Humidity, SensorFieldType.FieldTypeS8 },
            { SensorType.Pressure, SensorFieldType.FieldTypeS16 },
            { SensorType.Light, SensorFieldType.FieldTypeS16 },
            { SensorType.CO2, SensorFieldType.FieldTypeS16 },
            { SensorType.VOC, SensorFieldType.FieldTypeS16 },
            { SensorType.PM005, SensorFieldType.FieldTypeS16 },
            { SensorType.PM010, SensorFieldType.FieldTypeS16 },
            { SensorType.PM025, SensorFieldType.FieldTypeS16 },
            { SensorType.PM100, SensorFieldType.FieldTypeS16 },
            { SensorType.Noise, SensorFieldType.FieldTypeS16 },
            { SensorType.UV, SensorFieldType.FieldTypeS16 },
            { SensorType.CO, SensorFieldType.FieldTypeS16 },
            { SensorType.Vibration, SensorFieldType.FieldTypeS8 },
            { SensorType.Switch, SensorFieldType.FieldTypeU8 },
            { SensorType.Presence1, SensorFieldType.FieldTypeU8 },
            { SensorType.Presence2, SensorFieldType.FieldTypeU8 },
            { SensorType.Presence3, SensorFieldType.FieldTypeU8 },
            { SensorType.Distance1, SensorFieldType.FieldTypeU16 },
            { SensorType.Distance2, SensorFieldType.FieldTypeU16 },
            { SensorType.Distance3, SensorFieldType.FieldTypeU16 },
            { SensorType.X, SensorFieldType.FieldTypeS16 },
            { SensorType.Y, SensorFieldType.FieldTypeS16 },
            { SensorType.Z, SensorFieldType.FieldTypeS16 },
            { SensorType.RSSI, SensorFieldType.FieldTypeS16 },
            { SensorType.Perf1, SensorFieldType.FieldTypeS16 },
            { SensorType.Perf2, SensorFieldType.FieldTypeS16 },
            { SensorType.Perf3, SensorFieldType.FieldTypeS16 }
        };

        // Frequency constants in milliseconds
        public const int OneEveryHalfSecond = 500;
        public const int OneEverySecond = 1000;
        public const int OneEveryHalfMinute = 30 * OneEverySecond;
        public const int OneEveryMinute = 60 * OneEverySecond;
        public const int OnePerTwoMinutes = 2 * OneEveryMinute;
        public const int OneEveryTenSeconds = 10 * OneEverySecond;

        public static readonly Dictionary<SensorType, int> SensorTypeToFrequency = new Dictionary<SensorType, int>
        {
            { SensorType.Unknown, OneEveryMinute },
            { SensorType.Temperature, OneEveryMinute },
            { SensorType.Humidity, OneEveryMinute },
            { SensorType.Pressure, OneEveryMinute },
            { SensorType.Light, OneEveryHalfMinute },
            { SensorType.CO2, OnePerTwoMinutes },
            { SensorType.VOC, OnePerTwoMinutes },
            { SensorType.PM005, OnePerTwoMinutes },
            { SensorType.PM010, OnePerTwoMinutes },
            { SensorType.PM025, OnePerTwoMinutes },
            { SensorType.PM100, OnePerTwoMinutes },
            { SensorType.Noise, OneEveryTenSeconds },
            { SensorType.UV, OneEveryMinute },
            { SensorType.CO, OneEveryTenSeconds },
            { SensorType.Vibration, OneEveryTenSeconds },
            { SensorType.Switch, OneEveryHalfSecond },
            { SensorType.Presence1, OneEveryHalfSecond },
            { SensorType.Presence2, OneEveryHalfSecond },
            { SensorType.Presence3, OneEveryHalfSecond },
            { SensorType.Distance1, OneEveryHalfSecond },
            { SensorType.Distance2, OneEveryHalfSecond },
            { SensorType.Distance3, OneEveryHalfSecond },
            { SensorType.X, OneEveryHalfSecond },
            { SensorType.Y, OneEveryHalfSecond },
            { SensorType.Z, OneEveryHalfSecond },
            { SensorType.RSSI, OneEveryTenSeconds },
            { SensorType.Perf1, OneEveryMinute },
            { SensorType.Perf2, OneEveryMinute },
            { SensorType.Perf3, OneEveryMinute }
        };
    }
}