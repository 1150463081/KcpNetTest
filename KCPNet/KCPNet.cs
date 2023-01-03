using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Net;
using System.Text;

namespace KCPNet
{
    public class KCPNet<T>
        where T:KCPSession,new()
    {
        UdpClient udp;
        IPEndPoint remoteIP;

        #region Server
        #endregion

        #region Client
        T clientSession;
        public void StartAsClient(string ip, int port)
        {
            udp = new UdpClient(0);
            remoteIP = new IPEndPoint(IPAddress.Parse(ip), port);
            KCPTool.ColorLog(KCPLogColor.Green, "Client start...");
        }
        public void ConnectServer()
        {
            SendUdpMsg(new byte[4], remoteIP);
        }
        async void ClientReceive()
        {
            UdpReceiveResult receiveResult;
            while (true)
            {
                try
                {
                    //异步接受消息
                    receiveResult = await udp.ReceiveAsync();
                    //如果来自一个IP，进行解析
                    if (Equals(remoteIP, receiveResult.RemoteEndPoint))
                    {
                        uint sid= BitConverter.ToUInt32(receiveResult.Buffer, 0);
                        if (sid == 0)
                        {
                            //第一次连接，获得sid
                            if (clientSession != null && clientSession.IsConnected())
                                KCPTool.Warn("Client Has Init...");
                            else
                            {
                                sid= BitConverter.ToUInt32(receiveResult.Buffer, 4);
                                KCPTool.ColorLog(KCPLogColor.Green, "UDP Request Conv Sid:{0}", sid);

                                clientSession = new T();
                                clientSession.InitSession(sid, SendUdpMsg, remoteIP);
                            }
                        }
                        else
                        {
                            //处理业务逻辑
                        }
                    }
                    else
                    {
                        KCPTool.Warn("Client Receive illegal target data");
                    }
                }
                catch (Exception e)
                {
                    KCPTool.Warn("Client Udp Receive Data Exception:{0}", e.ToString());
                }
            }
        }
        #endregion
        void SendUdpMsg(byte[] bytes, IPEndPoint remotePoint)
        {
            if (udp != null)
            {
                udp.SendAsync(bytes, bytes.Length, remotePoint);
            }
        }
    }
}
