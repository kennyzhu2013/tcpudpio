using GEM_NET_LIB;
using System.IO;
using UnityEngine;
using GEM_NET_LIB.NetClient;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Timers;
using GEM_NET_LIB.proto;
//using protoSources.GameProto;

namespace GEM_NET_LIB
{
	//support headt beat
    public class CNetWorkGlobal
    {
        //private CNetRecvMsgBuilder m_RecvBuilder;
        private CClientNetworkCtrl m_Ctrl;
        public CClientNetworkCtrl ClientNetworkCtrl
        {
            get { return m_Ctrl; }

            set { m_Ctrl = value; }
        }

		private bool isDebug;
		public bool IsDebug
		{
			get { return isDebug; }
			
			set { isDebug = value; }
		}

        private Dictionary<int, DkRecProto> protoList;
        private CSocketInfo m_SocketInfo;
		private bool        m_isP2P; // for reconnect.

        private static CNetWorkGlobal s_instance = null;
        public static CNetWorkGlobal Instance
        {
            get
            {
                if (null == s_instance)
                    s_instance = new CNetWorkGlobal();
                return s_instance;
            }
        }

		/*
        public uint UIN
        {
            get
            {
                return m_SocketInfo.m_Uin;
            }
        }*/
		

        private CNetWorkGlobal()
        {
			CNetStreamReader reader = new CNetStreamReader();
			reader.OnReceivePacketFinish = this.OnReceiveFullPacket; //设置回调.

            m_Ctrl = new CClientNetworkCtrl();
            m_Ctrl.Reader = reader;            
			m_Ctrl.Writer = new CNetStreamWriter();

			m_SocketInfo = new CSocketInfo();
			//m_SocketInfo.m_Uin = 0;
            protoList = new Dictionary<int, DkRecProto>();
			isDebug = false;
			m_isP2P = false;
        }

		/*
        public void OpenP2P()
        {
		    //need p2p?
            m_Ctrl.InitP2P();
        }*/
		//注册协议.
        public void registProto(DkRecProto proto)
        {
            if (protoList.ContainsKey(proto.m_head.shMsgID) == false)
            {
				if ( CNetWorkGlobal.Instance.IsDebug )
                	Log.info("CNetWorkGlobal",  "Register Message id:" + proto.m_head.shMsgID);
                protoList.Add(proto.m_head.shMsgID, proto);
            }
        }
		//删除协议.
        public void removeProto(int cmd)
        {
            protoList.Remove(cmd);
        }
		
		//网络状态变化监听.
        public void RegisterNetWorkStateLister(dNetWorkStateCallBack lister)
        {
            m_Ctrl.RegisterNetWorkStateLister(lister);
        }
		//删除网络状态变化回调.
        public void UnRegisterNetWorkStateLister(dNetWorkStateCallBack lister)
        {
            m_Ctrl.UnRegisterNetWorkStateLister(lister);
        }

		//udp proxy网络状态变化监听.
		public void RegisterUdpProxyStateLister(dUdpProxyStateCallBack lister)
		{
			m_Ctrl.RegisterUdpProxyStateLister(lister);
		}
		//删除udp proxy网络状态变化回调.
		public void UnRegisterUdpProxyStateLister(dUdpProxyStateCallBack lister)
		{
			m_Ctrl.UnRegisterUdpProxyStateLister(lister);
		}

		//是否连接成功.
        public bool IsConnected()
        {
            return m_Ctrl.IsConnected();
        }

		//连接，在主线程处理.
		public bool Connect(string a_strRomoteIP, ushort a_uPort)
        {
            m_SocketInfo.m_ServerIP = a_strRomoteIP;
            m_SocketInfo.m_Port = a_uPort;

			bool bRet = false;
			bRet = m_Ctrl.Connect (a_strRomoteIP, a_uPort);

			//udp server not reconnect for twice,optional.
			/*
			if ( isP2P 
			    && !m_isP2P ) 
			{
				m_isP2P = isP2P;
				OpenP2P ();
			}*/

			return bRet;
        }

		public bool UdpConnect(string a_strRomoteIP, ushort a_uPort)
		{
			//need p2p?
			if ( m_Ctrl.UdpClient != null )
				m_Ctrl.UdpClient.ConnectServer( m_Ctrl );

			return true;
		}

		//关闭连接.
        public void Close()
        {
            m_Ctrl.ReleaseSocket();
        }

		public void CloseUdp()
		{
			if (m_Ctrl.UdpClient != null) 
			{
				m_Ctrl.UdpClient.Close();
			}
		}

		//发送接口.
        public void SendProto(DkReqProto proto)
        {
            //MonoBehaviour.print ("SendProto here");
			if ( CNetWorkGlobal.Instance.IsDebug )
            	Log.info("CNetWorkGlobal",  "Send Message id:" + proto.head.shMsgID + " proto.isP2P:" + proto.isP2P);

			//???p2p, some day will add P2P here
			if(proto.isP2P)
			{
				m_Ctrl.UdpSendMessage(proto.head, proto.bytes);
			}
			else
			{
				m_Ctrl.SendMessage(proto.head, proto.bytes);
			}
		
		}	

        //客户端的P2P消息处理
        public bool ReceiveP2P(proto_header head, byte[] block)
        {
            //设置p2p
            if (protoList.ContainsKey(head.shMsgID))
            {
                DkRecProto proto = protoList[head.shMsgID];
                proto.isP2P = true; //设置p2p标记
                proto.respond(head, block);
                return true;
            }
            else
            {
                Log.info("CNetWorkGlobal",  "can not find res msg id " + head.shMsgID);
                return false;
            }
        }

		//主线程调用，当收到一个整包时.
        public bool OnReceiveFullPacket(proto_header head, byte[] block)
        {
            //Initial uin from first message
			/*
            if (m_SocketInfo.m_Uin == 0 && uin != 0)
            {
                m_SocketInfo.m_Uin = uin;

				if ( CNetWorkGlobal.Instance.IsDebug )
                	Log.info("CNetWorkGlobal",  "m_SocketInfo.m_Uin set to: " + uin);
            }*/

			//如果p2p开通
			/*
			if (m_Ctrl.UdpClient != null) {
				if (m_Ctrl.UdpClient.UIN == 0 && uin != 0) {
					m_Ctrl.UdpClient.UIN = (uint)uin;
					Log.info(this, "m_Ctrl.P2PClient.UIN set to: " + uin);
				}
			}*/

            if (protoList.ContainsKey(head.shMsgID))
            {
                DkRecProto proto = protoList[head.shMsgID];
                proto.respond(head, block);
                return true;
            }
            else
            {
                Debug.LogError("[CNetWorkGlobal][Receive] Can not find registered msg id " + head.shMsgID);
                return false;
            }
        }

        public bool SendNetEmptyMessage(proto_header emptyMsg)
        {
            string strOut;
            if (NetOpcodes_C2SString.Instance.GetString(emptyMsg.shMsgID, out strOut))
            {
                //MonoBehaviour.print ("SendEmptyMessage:" + msgID + " " + strOut);
                return m_Ctrl.SendMessage(emptyMsg, null);
            }
            else
            {
                MonoBehaviour.print("Unable to Send Unknown EmptyMessage" + emptyMsg.shMsgID);
            }
            return false;
        }
		
		public bool SendNetStream(MemoryStream data)
		{
			return m_Ctrl.SendMessage(data);
		}
		
		//在主线程每一帧调用.
		public void Update ()
		{
			//MonoBehaviour.print ("CNetWorkGlobal.Update() here");
			//logger.Log("Update  message:", "CNetWorkGlobal");
			//Log.info("CNetWorkGlobal",   "CNetWorkGlobal:Update  message:" );
			m_Ctrl.Update ();

			if (m_Ctrl.UdpClient != null) {
				m_Ctrl.UdpClient.Update();
			}
		}

		/*
		public void CreateP2PGroupProcess( uint groupid )
		{
			if (m_Ctrl.UdpClient == null)
				return;

			m_Ctrl.P2PClient.JoinP2PGroupReq (groupid);
		}

		public void QuitP2PGroupProcess( uint groupid, uint uin )
		{
			if (m_Ctrl.P2PClient == null)
				return;

			m_Ctrl.P2PClient.QuitP2PGroupProcess (groupid, uin);
		}*/

        public static string GetAdapterMAC()
        {
            return CClientNetworkCtrl.GetAdapterMAC();
        }

        public void SetSocketSendNoDeley(bool nodelay)
        {
            m_Ctrl.SetSocketSendNoDeley(nodelay);
        }
    }
}
