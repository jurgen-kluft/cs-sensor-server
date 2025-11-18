# C#

NOTE: Work in progress, nothing to see here, move along.

Move the sensor server from Golang to C#/.NET for the following reasons:

- It does UDP, TCP, and Unix Domain Sockets
- It does memory mapped file I/O
- Easy debugging with Visual Studio, VS Code or Rider
- Cross platform (Windows, Linux, MacOS)

# Sensor Server

A udp/tcp server for receiving sensor data in real-time and backing them to disk in a custom structured format.

Some notes:

- Data is very minimal and it should be quite easy to load/use it in a visualization tool
- One or more (local) clients can connect through unix domain sockets to receive real-time data. 
  The data is a simple array of the following format:
  - `{uint16 Sensor Index, uint16 Sensor Value}`

## Status

- Implemention is WIP
  - ipc, udp and tcp server 
  - updating sensor streams with incoming sensor data
  - flushing sensor streams to disk
  Status: Beta version, needs testing.

## Sensor Stream Format

- 64 bytes header
  	- uint16, Year
    - uint8, Month
    - uint8, Day
    - uint64, Full Sensor Identifier
    - uint64, Flags (compressed or not)
    - uint32, Data Length (length of data in bytes)
- Data (variable length, depends on SampleType and SampleFreq)

## Data Size

Sensor data is stored when received, there is no fixed frequency of samples.

Each sample = type (uint16, 2 bytes), value (uint16, 2 bytes), time (uint32, 4 bytes) = 8 bytes total.
Every month or so a 'TimeMarker' is inserted into the stream that 'resets' the time reference to 0.
The 'time' field unit is in milliseconds since the last TimeMarker.

Calculation for a Temperature sensor:

```markdown
Let's say we receive around 32 temperature samples per hour, and we project to run the sensor server for 1 year.
Then the total amount of samples would be: 32 samples/hour * 24 hours/day * 365 days/year = 280320 samples/year.
Each temperature sample is stored as a type (uint16, 2 bytes), a value (uint16, 2 bytes), and a time (uint32, 4 bytes), 
so each sample takes 8 bytes.
So the total data size for 1 year would be: 280320 samples/year * 8 bytes/sample = 2,242,560 bytes/year, or roughly 2 MB/year.
```

Calculation for a presence sensor:

```markdown
Let's say we receive around (average) 16 presence states per hour, and we project to run the sensor server for 1 year.
Then the total amount of samples would be: 16 samples/hour * 24 hours/day * 365 days/year = 140160 samples/year.
Each presence sample is stored as a type (uint16, 2 bytes), a value (uint16, 2 bytes), and a time (uint32, 4 bytes), 
so each sample takes 8 bytes.
So the total data size for 1 year would be: 140160 samples/year * 8 bytes/sample = 1,121,280 bytes/year, or roughly 1 MB/year.
```

