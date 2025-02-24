using LocalNtp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace LocalNtp
{
    internal class NtpServer : IDisposable
    {
        public event Action<IPEndPoint, NtpPacket>? OnReceivePacket;
        public event Action<IPEndPoint, NtpPacket>? OnSendPacket;

        public byte Stratum = 3;
        public byte[] ReferenceId = new byte[]{ 0, 0, 0, 0 };

        private UdpClient? _udp;
        private bool _opened;

        public void Dispose() {
            Close();
            GC.SuppressFinalize(this);
        }

        public void Open(int port) {
            if(_opened) return;

            _udp = new UdpClient(port);
            _opened = true;
            Task.Run(Receive);
        }

        public void Close() {
            _opened = false;
            _udp?.Close();
        }

        private async Task Receive() {
            while (_opened) {
                try {
                    var rec = await _udp!.ReceiveAsync();
                    var rectime = DateTime.Now;
                    var recclock = Environment.TickCount64;

                    if(rec.Buffer.Length < NtpPacket.Size) continue;

                    var packet = new NtpPacket(rec.Buffer);
                    OnReceivePacket?.Invoke(rec.RemoteEndPoint, packet);

                    if(packet.VersionNumber < 2) continue;
                    if(packet.Mode != (int)NtpMode.Client) continue;

                    var res = new NtpPacket();
                    res.LeapIndicator = (int)NtpLi.NoWarning;
                    res.VersionNumber = 3;
                    res.Mode = (int)NtpMode.Server;
                    res.Stratum = Stratum;
                    res.PollInterval = packet.PollInterval;
                    res.Precision = 0xF6;
                    res.RootDelay = 0;
                    res.RootDispersion = 0;
                    res.ReferenceId = ReferenceId;

                    res.ReferenceTimestamp = NtpPacket.Date2Num(rectime.AddSeconds(-3));
                    res.OriginateTimestamp = packet.TransmitTimestamp;
                    res.ReceiveTimestamp = NtpPacket.Date2Num(rectime);
                    res.TransmitTimestamp = NtpPacket.Date2Num(rectime.AddMilliseconds(Environment.TickCount64 - recclock));

                    await _udp!.SendAsync(res.GetBytes(), NtpPacket.Size, rec.RemoteEndPoint);
                    OnSendPacket?.Invoke(rec.RemoteEndPoint, res);

                } catch(Exception ex) {
                    if(!_opened) break;
                    Debug.WriteLine(ex);
                }
            }
        }
    }
}
