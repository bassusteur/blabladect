using System;
using System.Data.Common;

namespace Busmail
{
    public static class FrameBuilder
    {
        private static uint _txSeq;
        public static uint TxSeq
        {
            get => _txSeq;
            set
            {
                if (value >=7 ) _txSeq = 0;
                else _txSeq = value;
            }
        }
        public static uint RxSeq = 0;

        public static BusMailFrame BuildFrame(FrameType type, byte[] data = null, bool pollFinal = false, SupervisorId Id = SupervisorId.ReceiveNotReady)
        {
            BusMailFrame frame = new BusMailFrame
            {
                FrameChar = 0x10
            };

            switch (type)
            {
                case FrameType.Supervisory:
                    switch (Id)
                    {
                        case SupervisorId.ReceiveNotReady:
                            frame.Header = (byte)((pollFinal ? 0x8 : 0x0) | (byte)FrameType.Supervisory | (byte)SupervisorId.ReceiveNotReady | (byte)RxSeq);
                            break;
                        case SupervisorId.ReceiveReady:
                            frame.Header = (byte)((pollFinal ? 0x8 : 0x0) | (byte)FrameType.Supervisory | (byte)SupervisorId.ReceiveReady | (byte)RxSeq);
                            break;
                        case SupervisorId.Reject:
                            frame.Header = (byte)((pollFinal ? 0x8 : 0x0) | (byte)FrameType.Supervisory | (byte)SupervisorId.Reject | (byte)RxSeq);
                            break;
                    }
                    frame.Length = 0x0001;
                    frame.Checksum = frame.Header;
                    break;
                case FrameType.Information:
                    BuildInformationHeader(ref frame.Header, pollFinal);
                    frame.Mail = new byte[data.Length + 2];
                    Array.Copy(data, 0, frame.Mail, 2, data.Length);
                    frame.Mail[0] = 0x00; // Program ID
                    frame.Mail[1] = 0x01; // Task ID
                    frame.Length = (ushort)(frame.Mail.Length + 1);
                    frame.Checksum = CalculateChecksum(frame);
                    break;
                case FrameType.Unnumbered:
                    frame.Header = (byte)((byte)FrameType.Unnumbered | (pollFinal ? 0x8 : 0x0));
                    frame.Length = 0x0001;
                    frame.Checksum = frame.Header;
                    break;
            }

            return frame;
        }

        private static byte CalculateChecksum(BusMailFrame frame) => frame.Mail.Aggregate(frame.Header, (acc, b) => (byte)(acc + b));

        private static void BuildInformationHeader(ref byte header, bool pollFinal)
        {
            Console.WriteLine($"TxSeq: {TxSeq}, PollFinal: {pollFinal}");
            byte txSeqBits = (byte)(TxSeq << 4);
            byte rxSeqBits = (byte)RxSeq;

            header = (byte)((pollFinal ? 0x8 : 0x0) | txSeqBits | rxSeqBits);

            TxSeq++;
        }

        public static byte[] FrameToData(BusMailFrame frame)
        {
            byte[] data = new byte[frame.Length + 4];
            data[0] = frame.FrameChar;
            data[1] = (byte)(frame.Length >> 8);
            data[2] = (byte)(frame.Length & 0xFF);
            data[3] = frame.Header;
            if(frame.Mail == null)
            {}
            else{
                Array.Copy(frame.Mail, 0, data, 4, frame.Mail.Length);
            }
            data[data.Length - 1] = frame.Checksum;

            return data;
        }
    }
}
