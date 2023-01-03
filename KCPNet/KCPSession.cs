using System;
using System.Net;
using System.Net.Sockets.Kcp;
using System.Threading;
using System.Threading.Tasks;

namespace KCPNet
{
    public enum SessionState
    {
        None,
        Connetced,
        DisConnected
    }

    public abstract class KCPSession
    {
        private IPEndPoint m_remotePoint;
        private Action<byte[], IPEndPoint> m_udpSender;

        protected uint m_sid;
        protected SessionState m_sessionState = SessionState.None;

        public KCPHandle m_handle;
        public Kcp kcp;



        public void InitSession(uint conv,Action<byte[],IPEndPoint> udpSender,IPEndPoint remotePoint) 
        {
            
            m_sid = conv;

            m_udpSender = udpSender;
            m_sessionState = SessionState.Connetced;
            m_remotePoint = remotePoint;
            
            m_handle = new KCPHandle();
            kcp = new Kcp(conv, m_handle);

            kcp.NoDelay(1, 10, 2, 1);
            kcp.WndSize(64, 64);
            kcp.SetMtu(512);

            m_handle.Out = (Memory<byte> buffer) =>
              {
                  byte[] bytes = buffer.ToArray();
                  udpSender(bytes, m_remotePoint);
              };
        }

        public bool IsConnected()
        {
            return m_sessionState == SessionState.Connetced;
        }
    }
}
