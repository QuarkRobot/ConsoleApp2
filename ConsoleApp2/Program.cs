using kcp2k;
using System.Diagnostics;

namespace ConsoleApp2
{
    internal class Program
    {
        private static EdgegapKcpClient client;
        private static EdgegapKcpServer server;

        static async Task Main(string[] args)
        {
            Console.WriteLine("Hello, World!");

            KcpConfig config = new KcpConfig(
           // force NoDelay and minimum interval.
           // this way UpdateSeveralTimes() doesn't need to wait very long and
           // tests run a lot faster.
           NoDelay: true,
           // not all platforms support DualMode.
           // run tests without it so they work on all platforms.
           DualMode: false,
           Interval: 1, // 1ms so at interval code at least runs.
           Timeout: 2000,

           // large window sizes so large messages are flushed with very few
           // update calls. otherwise tests take too long.
           SendWindowSize: Kcp.WND_SND * 1000,
           ReceiveWindowSize: Kcp.WND_RCV * 1000,

           // congestion window _heavily_ restricts send/recv window sizes
           // sending a max sized message would require thousands of updates.
           CongestionWindow: false,

           FastResend: 2, // 0 normal, 2 turbo

           // maximum retransmit attempts until dead_link detected
           // default * 2 to check if configuration works
           MaxRetransmits: Kcp.DEADLINK * 2,

          Mtu: Kcp.MTU_DEF - Protocol.Overhead
        );

            // create config from serialized settings.
            // with MaxPayload as max size to respect relay overhead.
            // config = new KcpConfig(false, RecvBufferSize, SendBufferSize, MaxPayload, NoDelay, Interval, FastResend, false, SendWindowSize, ReceiveWindowSize, Timeout, MaxRetransmit);

            client = new EdgegapKcpClient(
              () => { },
              (message, channel) => Debug.Print($"[KCP] OnClietDataReceived({BitConverter.ToString(message.Array, message.Offset, message.Count)} @ {channel})"),
              () => { },
              (error, reason) => Debug.Print($"[KCP] OnClientError({error}, {reason}"),
              config
          );

            // server
            server = new EdgegapKcpServer(
               (connectionId) => { },
               (connectionId, message, channel) => Debug.Print($"[KCP] OnServerDataReceived({connectionId}, {BitConverter.ToString(message.Array, message.Offset, message.Count)} @ {channel})"),
               (connectionId) => { },
               (connectionId, error, reason) => Debug.Print($"[KCP] OnServerError({connectionId}, {error}, {reason}"),
               config
           );

            server.Start("172.235.205.43", 30783, 2638712282, 2142294991);

            client.userId = 1862302252;
            client.sessionId = 2142294991;
            client.connectionState = ConnectionState.Checking; // reset from last time
            client.Connect("172.235.205.43", 32577);

            // https://github.com/MirrorNetworking/kcp2k/blob/master/kcp2k/kcp2k.Example/Program.cs

            do
            {
                if (client != null)
                {
                    client.TickIncoming();
                    client.TickOutgoing();
                }

                if (server != null)
                {
                    server.TickIncoming();
                    server.TickOutgoing();
                }

                await Task.Delay(16);
            } while (!Console.KeyAvailable);
        }
    }
}
