## CSI Lib

This .NET library can be used to parse message coming from the [Atheros CSI Tool](https://wands.sg/AtherosCSI/).
Example code:
```cs
// Default Buffer size
var bufferSize = 4096;
var reader = new CSIReader("/dev/CSI_dev", bufferSize);
reader.OnNewPacket += (sender, packet) =>
{
    // Packet contains CSI data as well as some metadata
    HandleNewPacket(packet);
};

reader.StartReading();
```