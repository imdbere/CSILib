using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading;

namespace CSIUserTool
{
    public class CSIReader
    {
        const int CSIBufferSize = 4096;
        Thread ReadThread;
        CancellationTokenSource ThreadCancellation = new CancellationTokenSource();
        public event EventHandler<CSIPacket> OnNewPacket;

        object Locker = new object();
        public CSIReader()
        {

        }

        public void StartReading()
        {
            lock (Locker)
            {
                if (ReadThread != null)
                {
                    Console.WriteLine("Already Started");
                    return;
                }

                ReadThread = new Thread(Read);
                ReadThread.Start();
            }

        }
        void Read()
        {
            var fileStream = File.Open("/dev/CSI_dev", FileMode.Open, FileAccess.Read);

            while(!ThreadCancellation.IsCancellationRequested)
            {
                var packet = ReadCSIPacket(fileStream);
                if (packet != null)
                {
                    OnNewPacket?.Invoke(this, packet);
                }
            }

            fileStream.Dispose();  
        }

        public void Stop()
        {
            lock (Locker) 
            {
                if (ReadThread == null)
                {
                    Console.WriteLine("Already Stopped");
                    return;
                }

                ThreadCancellation.Cancel();
                ReadThread.Join();
                ReadThread = null;
            }
        }
        

        CSIPacket ReadCSIPacket(FileStream file)
        {
            var buffer = new byte[CSIBufferSize];
            var bytesRead = file.Read(buffer, 0, CSIBufferSize);
            if (bytesRead <= 0) //(bytesRead < CSIBufferSize)
            {
                //Console.WriteLine("No bytes read");
                return null;
            }

            var span = new Span<byte>(buffer);
            var csiPacket = new CSIPacket();

            int csiPacketLength = Marshal.SizeOf(typeof(CSIStatusPacket));
            //var statusBuffer = new Span<byte>(buffer, csiPacketLength);

            var handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
            var statusPacket = (CSIStatusPacket) Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(CSIStatusPacket));
            csiPacket.StatusPacket = statusPacket;

            if (statusPacket.NumReceivingAntennas == 0 || statusPacket.NumTransmittingAntennas == 0) {
                Console.WriteLine("Num transmitting or receiving antennas is 0, skipping packet");
                return null;
            }

            if (statusPacket.PhyError != PHYErrorCode.OK) {
                Console.WriteLine("Error in packet: " + statusPacket.PhyError.ToString());
            }
            
            var matrixSlice = span.Slice(csiPacketLength);
            var csiDataLength = statusPacket.CSIDataLength;
            if (csiDataLength == 0) 
            {
                //Console.WriteLine("CSI Data length is 0, skipping packet");
                return null;
            }
            csiPacket.CSIMatrix = ReadCSIMatrix(matrixSlice, statusPacket.NumReceivingAntennas, statusPacket.NumTransmittingAntennas, statusPacket.NumSubcarriers);

            //var copyStartIndex = csiPacketLength + csiDataLength;
            var payloadSlice = matrixSlice.Slice(statusPacket.CSIDataLength, statusPacket.PayloadLength);
            csiPacket.PayloadData = new byte[statusPacket.PayloadLength];
            payloadSlice.CopyTo(csiPacket.PayloadData);
            //Array.Copy(buffer, copyStartIndex, csiPayload.Data, 0, payloadLength); 

            return csiPacket;
        }

        Complex[,,] ReadCSIMatrix (Span<byte> buffer, int numRx, int numTx, int numSubcarriers)
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

        int convertNegative(int data, int maxbit)
        {
            if ((data & (1 << (maxbit - 1))) != 0)
            {
                /* negative */
                data -= (1 << maxbit);    
            }
            return data;
        }
    }
}
