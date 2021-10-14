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

    public abstract class KCPSession<T>
        where T:KCPMsg
    {
        protected uint m_sid;
        Action<byte[], IPEndPoint> m_udpSender;
        protected SessionState m_sessionState = SessionState.None;
        private IPEndPoint m_remotePoint;
        public Action<uint> OnSessionClose;

        public KCPHandle m_handle;
        public Kcp kcp;

        private CancellationTokenSource cts;
        private CancellationToken ct;


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
            m_handle.Recv = (byte[] buffer) =>
              {
                  buffer = KCPTool.DeCompress(buffer);
                  T msg = KCPTool.DeSerialize<T>(buffer);
                  if (msg != null)
                  {
                      OnReciveMsg(msg);
                  }
              };

            OnConnected();

            cts = new CancellationTokenSource();
            ct = cts.Token;
            Task.Run(Update,ct);
        }
        public void ReceiveData(byte[] buffer)
        {
            kcp.Input(buffer.AsSpan());
        }

        async void Update()
        {
            try
            {
                while (true)
                {
                    DateTime now = DateTime.UtcNow;
                    OnUpdate(now);
                    if (ct.IsCancellationRequested)
                    {
                        KCPTool.ColorLog(KCPLogColor.Cyan, "SessionUpdate Task isCancelled");
                        break;
                    }
                    else
                    {
                        kcp.Update(now);
                        int len;
                        while ((len = kcp.PeekSize()) > 0)
                        {
                            var buffer = new byte[0];
                            if (kcp.Recv(buffer) > 0)
                            {
                                m_handle.Recive(buffer);
                            }
                            await Task.Delay(10);
                        }
                    }
                }
            }
            catch(Exception e)
            {
                KCPTool.WarnLog(e.ToString());
            }
        }

        public void SendMsg(T msg)
        {
            if (IsConnected())
            {
                byte[] bytes = KCPTool.Serialize(msg);
                if (bytes != null)
                    SendMsg(bytes);
            }else
                KCPTool.WarnLog("Session Disconnected,Can not Sendmsg");
        }
        public void SendMsg(byte[] msg_bytes)
        {
            if (IsConnected())
            {
                msg_bytes = KCPTool.Compress(msg_bytes);
                kcp.Send(msg_bytes.AsSpan());
            }
            else
            {
                KCPTool.WarnLog("Session Disconnected,Can not Sendmsg");
            }
        }

        public void CloseSession()
        {
            cts.Cancel();
            OnDisConnected();
            OnSessionClose?.Invoke(m_sid);
            OnSessionClose = null;

            m_sessionState = SessionState.DisConnected;
            m_remotePoint = null;
            m_udpSender = null;
            m_handle = null;
            kcp = null;
            m_sid = 0;
            cts = null;
        }

        protected abstract void OnConnected();
        protected abstract void OnDisConnected();
        protected abstract void OnUpdate(DateTime now);

        protected abstract void OnReciveMsg(T msg);

        public bool IsConnected()
        {
            return m_sessionState == SessionState.Connetced;
        }
    }
}
