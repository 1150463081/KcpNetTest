using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets.Kcp;
using System.Buffers;

namespace KCPNet
{
    /// <summary>
    /// kcp数据处理器
    /// </summary>
    public class KCPHandle : IKcpCallback
    {
        public Action<Memory<byte>> Out;
        public void Output(IMemoryOwner<byte> buffer, int avalidLength)
        {
            using (buffer)
                Out(buffer.Memory.Slice(0, avalidLength));
        }

        public Action<byte[]> Recv;
        public void Recive(byte[] buffer)
        {
            Recv(buffer);
        }
    }
}
