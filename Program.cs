using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CSITool
{
    class Program
    {
        const int CSIBufferSize = 4096;
        async static Task Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            var fileStream = File.Open("/dev/CSI_dev", FileMode.Open, FileAccess.Read);
            while (true)
            {
                var packet = ReadCSIPacket(fileStream);
                
                if (packet != null)
                {
                    Console.WriteLine($"Got CSI packet, {packet.StatusPacket.NumReceivingAntennas} RX, {packet.StatusPacket.NumTransmittingAntennas} TX, {packet.StatusPacket.NumSubcarriers} Subcarriers");
                }
            }
                        /*var numLines = 0;
            while (!fileStream.EndOfStream)
            {
                var nextLine = await fileStream.ReadLineAsync();
                Console.WriteLine("Line from DEV: " + nextLine);
                numLines++;
                if (numLines > 50)
                {
                    Console.WriteLine("Read 50 lines, closing now");
                    break;
                }
            }*/

            Console.WriteLine("Stream closed");
        }

        static CSIPacket ReadCSIPacket(FileStream file)
        {
            var buffer = new byte[CSIBufferSize];
            var bytesRead = file.Read(buffer, 0, CSIBufferSize);
            if (bytesRead <= 0) //(bytesRead < CSIBufferSize)
            {
                Console.WriteLine("Not enough bytes read: " + bytesRead);
                return null;
            }

            var span = new Span<byte>(buffer);
            var csiPacket = new CSIPacket();

            int csiPacketLength = Marshal.SizeOf(typeof(CSIStatusPacket));
            //var statusBuffer = new Span<byte>(buffer, csiPacketLength);

            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var statusPacket = (CSIStatusPacket) Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(CSIStatusPacket));
            csiPacket.StatusPacket = statusPacket;

            var matrixSlice = span.Slice(csiPacketLength);
            csiPacket.CSIMatrix = ReadCSIMatrix(matrixSlice, statusPacket.NumReceivingAntennas, statusPacket.NumTransmittingAntennas, statusPacket.NumSubcarriers);

            //var copyStartIndex = csiPacketLength + csiDataLength;
            var payloadSlice = matrixSlice.Slice(statusPacket.CSIDataLength, statusPacket.PayloadLength);
            payloadSlice.CopyTo(csiPacket.PayloadData);
            //Array.Copy(buffer, copyStartIndex, csiPayload.Data, 0, payloadLength); 

            return csiPacket;
        }

        static int convertNegative(int data, int maxbit)
        {
            if ((data & (1 << (maxbit - 1))) != 0)
            {
                /* negative */
                data -= (1 << maxbit);    
            }
            return data;
        }
        static Complex[,,] ReadCSIMatrix (Span<byte> buffer, int numRx, int numTx, int numSubcarriers)
        {
            var matrix = new Complex[numRx, numTx, numSubcarriers];
            int currentByte = 0;
            uint currentData = 0;
            byte bitsLeft = 0;

            for (int sub = 0; sub < numSubcarriers; sub++)
            {
                for (int tx = 0; tx < numTx; tx++)
                {
                    for (int rx = 0; rx < numRx; rx++)
                    {
                        var number = new int[2];

                        for (int i = 0; i < 2; i++)
                        {
                            if(bitsLeft < 10) {
                                ushort nextData = buffer[currentByte++];
                                nextData += (ushort) (buffer[currentByte++] << 8);

                                currentData += (uint)(nextData << bitsLeft);
                                bitsLeft += 16;
                            }

                            ushort current10BitNr = (ushort)(currentData & ((1 << 10) - 1));
                            number[i] = convertNegative(current10BitNr, 10);

                            bitsLeft -= 10;
                            currentData = currentData >> 10;
                        }

                        matrix[rx, tx, sub] = new Complex(number[1], number[0]);
                    }
                }
            }

            return matrix;
        }
        
        
    }
}
