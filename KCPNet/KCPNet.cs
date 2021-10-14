  
using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets.Kcp;
using System.Net.Sockets;
using System.Net;
using System.Threading.Tasks;
using System.Threading;

namespace KCPNet
{
    [Serializable]
    public abstract class KCPMsg { }

    public class KCPNet<T>
        where T:KCPSession,new ()
    {
        UdpClient udp;
        IPEndPoint remotePoint;

        private CancellationTokenSource cts;
        private CancellationToken ct;

        public KCPNet()
        {
            cts = new CancellationTokenSource();
            ct = cts.Token;
        }

        #region 
        public T clientSession;
        public void StartAsClient(string ip, int port)
        {
            udp = new UdpClient(0);
            remotePoint = new IPEndPoint(IPAddress.Parse(ip), port);
            KCPTool.ColorLog(KCPLogColor.Green, "Client Start...");

            Task.Run(ClientRecive,ct);
        }

        public void ConnectServer()
        {
            SendUDPMsg(new byte[4], remotePoint);
        }

        async void ClientRecive()
        {
            UdpReceiveResult result;
            while (true)
            {
                try {
                    if (ct.IsCancellationRequested)
                    {
                        //taks终止
                    }

                    result = await udp.ReceiveAsync();
                    if (Equals(remotePoint, result.RemoteEndPoint))
                    {
                        uint sid = BitConverter.ToUInt32(result.Buffer,0);
                        if (sid == 0)
                        {
                            //sid数据
                            if (clientSession != null && clientSession.IsConnected())
                            {
                                //已建立链接，收到多的sid，直接丢弃
                            }
                            else
                            {
                                //未初始化，收到服务器分配的sid数据,初始化客户端session
                                sid = BitConverter.ToUInt32(result.Buffer, 4);
                                KCPTool.ColorLog(KCPLogColor.Green, "UDP Request Conv Sid: {0}", sid);
                                //会话处理
                                clientSession = new T();
                                clientSession.InitSession(sid, SendUDPMsg, remotePoint);
                                clientSession.OnSessionClose = OnClientSessionClose;
                            }
                        }
                        else
                        {
                            //todo 处理业务逻辑
                            if (clientSession != null && clientSession.IsConnected())
                            {
                                clientSession.ReceiveData(result.Buffer);
                            }
                            else
                            {
                                KCPTool.WarnLog("Client is Initing...");
                            }
                        }
                    }
                    else
                    {
                        KCPTool.WarnLog("Client Udp Receive Illegal target Data");
                    }
                }
                catch(Exception e)
                {
                    KCPTool.WarnLog(e.ToString());
                }
            }
        }

        void OnClientSessionClose(uint sid)
        {
            cts.Cancel();
            if(udp!=null)
            {
                udp.Close();
                udp = null;
            }
            KCPTool.WarnLog("Client Session Close,sid{}", sid);
        }
        public void CloseClient()
        {
            if (clientSession != null)
                clientSession.CloseSession();
        }
        #endregion
        void SendUDPMsg(byte[] bytes,IPEndPoint remotePoint)
        {
            if (udp != null)
                udp.SendAsync(bytes, bytes.Length, remotePoint);
        }
    }
}
