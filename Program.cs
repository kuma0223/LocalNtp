namespace LocalNtp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var port = 123;
            if(args.Length > 0) port = int.Parse(args[0]);

            var server = new NtpServer();
            
            server.OnReceivePacket += (remote, packet) => {
                Console.WriteLine($"receive from {remote}");
            };

            server.OnSendPacket += (remote, packet) => {
                Console.WriteLine($"send to {remote}");
            };

            server.Open(port);
            Console.WriteLine($"NTP server open {port}");

            Console.ReadLine();
            server.Close();
            
            Console.WriteLine("NTP server close");
        }
    }
}
