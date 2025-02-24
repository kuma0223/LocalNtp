using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace LocalNtp
{
    public class NtpPacket
    {
        public const int Size = 48;
        
        public static readonly DateTime UtcZero
            = new DateTime(1900, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>うるう秒挿入の情報</summary>
        public int LeapIndicator;
        
        /// <summary>プロトコルのバージョン</summary>
        public int VersionNumber;
        
        /// <summary>モード</summary>
        public int Mode;
        
        /// <summary>階層</summary>
        public byte Stratum;
        
        /// <summary>ポーリング間隔(log2)</summary>
        public byte PollInterval;
        
        /// <summary>時刻精度</summary>
        public byte Precision;
        
        /// <summary>遅延</summary>
        public long RootDelay;

        /// <summary>揺らぎ</summary>
        public long RootDispersion;
        
        /// <summary>参照先</summary>
        public byte[] ReferenceId = new byte[4];
        
        /// <summary>最終同期時刻</summary>
        public long ReferenceTimestamp;
        
        /// <summary>T1/リクエスト送信時刻</summary>
        public long OriginateTimestamp;
        
        /// <summary>T2/リクエスト受信時刻</summary>
        public long ReceiveTimestamp;
        
        /// <summary>T3/送信時刻</summary>
        public long TransmitTimestamp;

        public NtpPacket() {
        }

        public NtpPacket(byte[] bytes) {
            LeapIndicator = (bytes[0] >> 6) & 0x03;
            VersionNumber = (bytes[0] >> 3) & 0x07;
            Mode = (bytes[0] >> 0) & 0x07;
            Stratum = bytes[1];
            PollInterval = bytes[2];
            Precision = bytes[3];
            RootDelay = GetNum(bytes, 4, 4);
            RootDispersion = GetNum(bytes, 8, 4);
            ReferenceId[0] = bytes[12];
            ReferenceId[1] = bytes[13];
            ReferenceId[2] = bytes[14];
            ReferenceId[3] = bytes[15];
            ReferenceTimestamp = GetNum(bytes, 16, 8);
            OriginateTimestamp = GetNum(bytes, 24, 8);
            ReceiveTimestamp = GetNum(bytes, 32, 8);
            TransmitTimestamp = GetNum(bytes, 40, 8);
        }

        public byte[] GetBytes() {
            var ret = new byte[Size];

            ret[0] |= (byte)(LeapIndicator << 6);
            ret[0] |= (byte)(VersionNumber << 3);
            ret[0] |= (byte)Mode;
            ret[1] = Stratum;
            ret[2] = PollInterval;
            ret[3] = Precision;
            SetNum(ret, 4, 4, RootDelay);
            SetNum(ret, 8, 4, RootDispersion);
            ret[12] = ReferenceId[0];
            ret[13] = ReferenceId[1];
            ret[14] = ReferenceId[2];
            ret[15] = ReferenceId[3];
            SetNum(ret, 16, 8, ReferenceTimestamp);
            SetNum(ret, 24, 8, OriginateTimestamp);
            SetNum(ret, 32, 8, ReceiveTimestamp);
            SetNum(ret, 40, 8, TransmitTimestamp);

            return ret;
        }

        public void SetReferenceId(string value) {
            byte[] buf;

            if(value.Length <= 4) {
                buf = value.ToCharArray().Select(x=>(byte)x).ToArray();
            } else {
                buf = value.Split('.').Select(byte.Parse).ToArray();
            }
            
            ReferenceId[0] = buf.Length > 0 ? buf[0] : (byte)0;
            ReferenceId[1] = buf.Length > 1 ? buf[1] : (byte)0;
            ReferenceId[2] = buf.Length > 2 ? buf[2] : (byte)0;
            ReferenceId[3] = buf.Length > 3 ? buf[3] : (byte)0;
        }

        public static long Date2Num(DateTime x) {
            if(x.Kind != DateTimeKind.Utc) x = x.ToUniversalTime();
            var dif = (x - UtcZero).TotalSeconds;
            var sec = (long)dif;
            var dec = 0xFFFFFFFFL & (long)((dif - sec) * 0x100000000L);

            if(sec > 0xFFFFFFFFL) {
                sec -= 0xFFFFFFFFL;
            }

            return (sec << 32) | dec;
        }

        private static long GetNum(byte[] bytes, int index, int count) {
            long ret = 0;
            for(int i=0; i<count; i++) {
                ret <<= 8;
                ret |= bytes[index + i];
            }
            return ret;
        }

        private static void SetNum(byte[] dest, int index, int count, long value) {
            for(int i=0; i<count; i++) {
                dest[index + i] = (byte)(value >> (count - i - 1) * 8);
            }
        }
    }

    public enum NtpMode {
        Reserved = 0,
        SymmetricActive = 1,
        SymmetricPassive = 2,
        Client = 3,
        Server = 4,
        Broadcast = 5,
        NtpControlMessage = 6,
        Private = 7,
    }

    public enum NtpLi {
        NoWarning = 0,
        AddSecond = 1,
        SubSecond = 2,
        Unknown = 3,
    }
}
