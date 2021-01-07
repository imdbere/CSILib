using System;
using System.Numerics;
using System.Runtime.InteropServices;

namespace CSIUserTool
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CSIStatusPacket
    {
        public UInt64 TimeStamp;

        public UInt16 CSIDataLength;    
        public UInt16 Channel;

        // Should be 0
        public PHYErrorCode PhyError;

        // Currently always 0
        private byte NoiseFloor;
        public byte TransmissionRate;
        public Bandwidth ChannelBandwidth;
        public byte NumSubcarriers;

        public byte NumReceivingAntennas;
        public byte NumTransmittingAntennas;

        public byte RxRSSIAll;
        public byte RxRSSIChain0;
        public byte RxRSSIChain1;
        public byte RxRSSIChain2;

        public UInt16 PayloadLength;

        //public UInt16 DataLength;

    }

    // public struct ComplexNumber 
    // {
    //     public Int32 Real;
    //     public Int32 Imaginary;
    // }

    public class CSIPacket
    {
        public Complex[,,] CSIMatrix;

        public byte[] PayloadData;
        
        public CSIStatusPacket StatusPacket;
    }

    public enum PHYErrorCode : byte
    {
        OK = 0,
        TimingError = 1,
        IllegalParity = 2,
        IllegalRate = 3,
        IllegalLength = 4,
        RadarDetect = 5,
        IllegalService = 6,
        TransmitOverrideReceive = 7
    }

    public enum Bandwidth : byte
    {
        MHz_20 = 0,
        MHz_40 = 1
    }
}