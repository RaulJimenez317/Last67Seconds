using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Codice.Multiplayer
{
    // Bounded, length-prefixed UTF-8 frames. Socket tasks never call Unity APIs.
    public sealed class LanWire : IDisposable
    {
        public const int Port = 26767, MaxFrame = 2 * 1024 * 1024;
        readonly TcpClient socket;
        readonly ConcurrentQueue<string> incoming = new ConcurrentQueue<string>();
        readonly ConcurrentQueue<byte[]> outgoing = new ConcurrentQueue<byte[]>();
        readonly SemaphoreSlim wake = new SemaphoreSlim(0);
        readonly CancellationTokenSource stop = new CancellationTokenSource();
        string latestWorld;
        int disposed, queuedIn, queuedOut;
        public bool Closed => Volatile.Read(ref disposed) != 0;
        public LanWire(TcpClient client)
        { socket=client;socket.NoDelay=true;_ = Read();_ = Write(); }
        public static async Task<LanWire> Connect(string address)
        {
            if(!IPAddress.TryParse(address,out var ip)||ip.AddressFamily!=AddressFamily.InterNetwork)throw new ArgumentException("Introduce una dirección IPv4 válida.");
            var client=new TcpClient();
            try { var connect=client.ConnectAsync(ip,Port);if(await Task.WhenAny(connect,Task.Delay(5000))!=connect)throw new TimeoutException("El anfitrión no responde.");await connect;return new LanWire(client); }
            catch { client.Close();throw; }
        }
        public bool TryRead(out string value){if(incoming.TryDequeue(out value)){Interlocked.Decrement(ref queuedIn);return true;}value=Interlocked.Exchange(ref latestWorld,null);return value!=null;}
        public void Send(string value)
        {
            if(Closed)return;var bytes=Encoding.UTF8.GetBytes(value);
            if(bytes.Length==0||bytes.Length>MaxFrame||Interlocked.Increment(ref queuedOut)>32){Dispose();return;}
            outgoing.Enqueue(bytes);wake.Release();
        }
        async Task ReadExact(NetworkStream stream,byte[] data)
        {int offset=0;while(offset<data.Length){int count=await stream.ReadAsync(data,offset,data.Length-offset,stop.Token);if(count==0)throw new EndOfStreamException();offset+=count;}}
        async Task Read()
        {
            try {var stream=socket.GetStream();var header=new byte[4];while(!Closed){await ReadExact(stream,header);int length=IPAddress.NetworkToHostOrder(BitConverter.ToInt32(header,0));if(length<1||length>MaxFrame)throw new InvalidDataException();var bytes=new byte[length];await ReadExact(stream,bytes);var text=Encoding.UTF8.GetString(bytes);if(text.StartsWith("{\"type\":\"world\"",StringComparison.Ordinal)){Interlocked.Exchange(ref latestWorld,text);continue;}if(Volatile.Read(ref queuedIn)>=60&&(text.StartsWith("{\"type\":\"input\"",StringComparison.Ordinal)||text.StartsWith("{\"type\":\"effect\"",StringComparison.Ordinal))){if(incoming.TryDequeue(out _))Interlocked.Decrement(ref queuedIn);}if(Interlocked.Increment(ref queuedIn)>64)throw new InvalidDataException();incoming.Enqueue(text);}}
            catch { Dispose(); }
        }
        async Task Write()
        {
            try{var stream=socket.GetStream();while(!Closed){await wake.WaitAsync(stop.Token);while(outgoing.TryDequeue(out var bytes)){Interlocked.Decrement(ref queuedOut);var header=BitConverter.GetBytes(IPAddress.HostToNetworkOrder(bytes.Length));await stream.WriteAsync(header,0,4,stop.Token);await stream.WriteAsync(bytes,0,bytes.Length,stop.Token);}}}
            catch{Dispose();}
        }
        public void Dispose(){if(Interlocked.Exchange(ref disposed,1)!=0)return;stop.Cancel();socket.Close();}
    }
}
