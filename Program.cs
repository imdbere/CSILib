using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Linq;
using System.Text;
using System.Threading;

namespace CSIUserTool
{
    class Program
    {
        static List<CSIPacket> Packets = new List<CSIPacket>();
        async static Task Main(string[] args)
        {
            var reader = new CSIReader();

            var ouputFileName = "./test.txt";
            if (File.Exists(ouputFileName))
            {
                File.Delete(ouputFileName);
            }

            var stream = File.OpenWrite(ouputFileName);
            var writer = new StreamWriter(stream);

            reader.OnNewPacket += (sender, packet) => 
            {
                HandleNewPacket(packet);

                foreach (var nr in packet.CSIMatrix)
                {
                    writer.Write(nr.ToMathString());
                    writer.Write(" ");
                }
                writer.WriteLine();
            };
            reader.StartReading();
   
            Console.CancelKeyPress += (sender, e) => 
            {
                Console.WriteLine("Saving CSI data");
                reader.Stop();
                writer.Close();
                Console.WriteLine("Saved!");   
            };
        }

        static void HandleNewPacket(CSIPacket packet)
        {
            lock (Packets)
            {
                if (!Packets.Any())
                {
                    var statusPacket = packet.StatusPacket;

                    var dbgString = "Got First CSI packet! ";
                    dbgString += $"RX_Ant: {statusPacket.NumReceivingAntennas}, TX_Ant: {statusPacket.NumTransmittingAntennas} ";
                    dbgString += $"Num_Subc: {statusPacket.NumSubcarriers}, Channel {statusPacket.Channel} with BW {statusPacket.ChannelBandwidth} ";
                    dbgString += $"RSSI: {statusPacket.RxRSSIAll}, CSI_Payload_Len: {statusPacket.CSIDataLength}";

                    Console.WriteLine(dbgString);
                }
                Packets.Add(packet);
            }

            var matrix = packet.CSIMatrix;
            var displaySubcarrier = 1;

            var csiValues = new List<Complex>();
            for (int r = 0; r < packet.StatusPacket.NumReceivingAntennas; r++)
            {
                for (int t = 0; t < packet.StatusPacket.NumTransmittingAntennas; t++)
                {
                    csiValues.Add(matrix[r, t, displaySubcarrier]);
                }
            }

            var csiDisplay = csiValues.Select(nr =>
                $"({nr.Magnitude.ToString("0.00").PadLeft(7)}, {ToDeg(nr.Phase).ToString("0.00").PadLeft(7)})"
            );
            Console.WriteLine(string.Join(',', csiDisplay));
        }

        static double ToDeg(double radiants) 
        {
            return radiants * 180 / Math.PI;
        }
     
    }
}
