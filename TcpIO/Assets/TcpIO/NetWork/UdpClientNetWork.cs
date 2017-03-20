using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;//°üº¬Ìá¹©ÍøÂçÖ§³ÖµÄÀàÐÍ
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Text;
using System.IO;
using GEM_NET_LIB.proto;
using ProtoBuf;
using protoSources.GameProto;
using System.Collections;
using UdpKit;

namespace GEM_NET_LIB
{
	public class P2PUser		
	{
		protected UInt32 uin;		
		protected List<IPEndPoint> netPointList = null;
		protected UdpConnection    peerConnect;
		protected int              reTryPollTimes;

		/// <summary>
		/// The proxy point.当无法直接发送p2p消息时通过proxyserver转接，默认ip和端口和tcp server一致.
		/// </summary>
		protected IPEndPoint       proxyPoint;
		
		public P2PUser(UInt32 id, List<IPEndPoint> pointList)			
		{
			this.uin = id;
			this.netPointList = pointList;
			reTryPollTimes = 5;
			this.proxyPoint = null;
		}

		public P2PUser(UInt32 id)			
		{
			this.uin = id;
			netPointList = new List<IPEndPoint> ();
			reTryPollTimes = 5;
			this.proxyPoint = null;
		}
		
		public UInt32 UIN			
		{			
			get { return uin;}
			
			set { uin = value;}			
		}

		public UdpConnection PeerConnect			
		{			
			get { return peerConnect;}
			
			set { peerConnect = value;}			
		}

		public IPEndPoint ProxyServer			
		{			
			get { return proxyPoint; }
			
			set { proxyPoint = value;}			
		}
		
		public List<IPEndPoint> NetPointList	
		{
			get { return netPointList; }
			
			set { netPointList = value;}			
		}		

		public bool IsUserPoint(string address, int port)
		{
			foreach (IPEndPoint endp in netPointList) {
				if ( ( string.Compare(endp.Address.ToString(), address, true) == 0 )
				    && ( endp.Port == port ) )
				{
					return true;
				}					
			}

			return false;
		}

		public bool IsProxyServer(string address, int port)
		{
			if ( ( string.Compare( proxyPoint.Address.ToString(), address, true ) == 0 )
			    && ( proxyPoint.Port == port ) )
			{
				return true;
			}

			return false;
		}
	}

	/// <summary>
	/// P2P client net work.一个p2p组对应一个udp管理,目前只考虑一个好了.
	/// UIN用来保存udp无法通信情况下改用tcp通信用
	/// </summary>
	public class UdpClientNetWork
	{
		private UInt32    groupid; //p2p组id

		private P2PUser   hostUser;

		private UInt32    maxTrynum;
		private CClientNetworkCtrl owner;
		private bool      isReliable; //TODO: not ack udp pacakge to realize....
		private int       localPort;
		private int       frameRate = 100;
		UdpSocket server; //as client also

		public CClientNetworkCtrl CtrlOwner
		{
			get { return owner; }
			
			set { owner = value;}			
		}

		public UInt32 MaxTryNum
		{
			get { return maxTrynum; }
			
			set { maxTrynum = value;}			
		}

		public UInt32 UIN
		{
			get { return hostUser.UIN; }
			
			set { hostUser.UIN = value;}			
		}

		public bool Reliability
		{
			get { return isReliable; }
			
			set { isReliable = value;}			
		}

		public UInt32 GroupID
		{
			get { return groupid; }
			
			set { groupid = value;}			
		}

		//private Dictionary<UInt32, P2PUser> peerList;
		public UdpClientNetWork ()
		{
			//peerList = new Dictionary<UInt32, P2PUser> ();
			maxTrynum = 1; 
			hostUser = new P2PUser (0);
			//hostUser.UIN = 0;
			frameRate = 100;
			groupid = 0;
			isReliable = true;
		}

		public static void DebugImplement(uint level, string message)
		{
			if (level == UdpLog.ERROR) {
				Debug.LogError (message);
			}
			else
				Log.info("UdpClientNetWork",   message );
		}

		public void Start(CClientNetworkCtrl clientNetworkCtrl)
		{
			if (server == null) 
			{
				UdpLog.Writer writer = new UdpLog.Writer( DebugImplement );
				//(lvl, s) => Log.info("P2PClientNetWork",  s)
				UdpLog.SetWriter(writer);

				try
				{
					server = UdpSocket.Create<UdpPlatformManaged, P2PSerializer>();
				}
				catch(Exception e){
					Log.info(e, "P2PClientNetWork Start#Exception happened");
					//MonoBehaviour.print(e);
				}

				//sever and client.
				ConnectServer( clientNetworkCtrl );

				/*
				localPort = clientNetworkCtrl.LocalPort;
				UdpEndPoint serverPoint = new UdpEndPoint(UdpIPv4Address.Any, (ushort)localPort);
				server.Start(serverPoint);
				IPAddress ipaddr = IPAddress.Parse (clientNetworkCtrl.ServerIP);
				hostUser.ProxyServer = new IPEndPoint( ipaddr, clientNetworkCtrl.ServerPort );

				UdpIPv4Address address = UdpIPv4Address.Parse(clientNetworkCtrl.ServerIP);
				UdpEndPoint endp = new UdpEndPoint(address, (ushort)clientNetworkCtrl.ServerPort);
				server.Connect( endp );

				Log.info("P2PClientNetWork",  "P2PClientWork Start, UdpPort:" + localPort + " ServerIP:" + clientNetworkCtrl.ServerIP.ToString() + " ProxyServer address:" + endp.ToString());
				CtrlOwner = clientNetworkCtrl;*/
			}
		}

		public void ConnectServer (CClientNetworkCtrl clientNetworkCtrl)
		{
			//sever and client.
			localPort = clientNetworkCtrl.LocalPort;
			UdpEndPoint serverPoint = new UdpEndPoint(UdpIPv4Address.Any, (ushort)localPort);
			server.Start(serverPoint);
			IPAddress ipaddr = IPAddress.Parse (clientNetworkCtrl.ServerIP);
			hostUser.ProxyServer = new IPEndPoint( ipaddr, clientNetworkCtrl.ServerPort );

			UdpIPv4Address address = UdpIPv4Address.Parse(clientNetworkCtrl.ServerIP);
			UdpEndPoint endp = new UdpEndPoint(address, (ushort)clientNetworkCtrl.ServerPort);
			server.Connect( endp );

			Log.info("UdpClientNetWork",  "UdpClientNetWork Start, UdpPort:" + localPort + " ServerIP:" + clientNetworkCtrl.ServerIP.ToString() + " ProxyServer address:" + endp.ToString());
			CtrlOwner = clientNetworkCtrl;

		}

		public void Close ()
		{
			//peerList.Clear ();

			resetTcpAlias ();
			//hostUser.UIN = 0;
			groupid = 0;
			hostUser.PeerConnect = null;

			if (server != null)
				server.Close ();

			Log.info(this, "P2PClientNetWork Close--------------------------!");
			/*if (listenThread != null)
				listenThread.Abort ();*/
		}

		//重置tcp关联部分.
		private void resetTcpAlias()
		{
			hostUser.UIN = 0;

			//else to do?
		}
			

		/// <summary>
		/// Sends the message. proto_header is serialized in stream.
		/// </summary>
		/// <returns><c>true</c>, if message was sent, <c>false</c> otherwise.</returns>
		/// <param name="uin">Uin.</param>
		/// <param name="stream">Stream.</param>
		public bool SendMessage (byte[] stream)
		{
			/*
			P2PUser peerUser = null;

			//没有p2p连接直接默认中级形式.
			if (peerList.TryGetValue ((uint)uin, out peerUser) == false) 
			{
				//host user default.
				if ( hostUser != null && hostUser.PeerConnect != null )
				{
					hostUser.PeerConnect.Send(stream);
					Log.info("P2PClientNetWork",  "Server Send Message to uin:" + uin);
					return true;
				}
				else
				{
					Debug.LogError("hostUser.PeerConnect = null and User not found in the list: id:" + uin);
					return false;
				}
			}

			if (peerList == null){
				Debug.LogError("Found NULL in the list: id:" + uin);
				return false;
			}

			for (int i = 0; i < maxTrynum; ++i) 
			{
				Log.info("P2PClientNetWork",  "P2P Send Message Begin, uin:" + uin);
				if ( peerUser.PeerConnect != null)
				{
					//client.SendTo(stream, peerUser.PeerPoint); 
					peerUser.PeerConnect.Send(stream);
					Log.info("P2PClientNetWork",  "P2P Send Message End, PeerPoint:" + peerUser.PeerConnect.RemoteEndPoint.ToString());
					return true;
				}

				peerUser.PeerConnect = null;
				for ( int j = 0; j < peerUser.NetPointList.Count; ++j )
				{
					//client.SendTo(p2pBytes, peerUser.NetPointList[j]);
					UdpIPv4Address ipv4 = UdpIPv4Address.Parse(peerUser.NetPointList[j].Address.ToString());
					UdpEndPoint endPoint = new UdpEndPoint(ipv4, (ushort)peerUser.NetPointList[j].Port);

					server.Connect(endPoint);
					Log.info("P2PClientNetWork",  "P2PConnect Send to :" + endPoint.Address.ToString() + " Port:" + endPoint.Port
					          + " head.iUin" + uin);

				}
			}*/

			if (isReliable) 
			{
				if ( hostUser.PeerConnect != null )
				{
					hostUser.PeerConnect.Send(stream);
					Log.info("P2PClientNetWork",  "Server Send Message to Point:" + hostUser.PeerConnect.RemoteEndPoint.ToString() );
				}
				else
				{
					Log.info("P2PClientNetWork",  "Ignore message : because hostUser.PeerConnect = null.");
				}

				//not allow tcp for present
				//CtrlOwner.SendMessage(stream);
			}

			return false;
		}

		/*
		void AddP2PUser(uint uin)
		{
			if ( peerList.ContainsKey(uin) ) 
			{
				return;
			}
			peerList.Add (uin, new P2PUser (uin));
		}*/

		void ProcessUdpConnection(UdpConnection udpCon)
		{
			string address = udpCon.RemoteEndPoint.Address.ToString ();
			int port = udpCon.RemoteEndPoint.Port;

			//host <--> server connection
			if (hostUser.IsProxyServer (address, port) == true) {
				Log.info("P2PClientNetWork",  "ProcessUdpConnection#ProxyServer ipv4:" + udpCon.RemoteEndPoint.ToString());
				hostUser.PeerConnect = udpCon;

				//通知外部事件更改.
				NetWorkStateInfo info = new NetWorkStateInfo();
				info.state = EClientNetWorkState.Connected;
				owner.CallUdpProxyServerState(info);
				return;
			}

			//current p2p connection
			/*
			foreach (KeyValuePair<UInt32, P2PUser> peerUser in peerList) {
				//之前有记录的就不再记录了.
				if (peerUser.Value.PeerConnect != null)
				{
					Log.info("P2PClientNetWork",  "ProcessUdpConnection#ignore uin:" + peerUser.Key);
					continue;
				}

				if ( peerUser.Value.IsUserPoint(address, port) == true )
				{
					Log.info("P2PClientNetWork",  "ProcessUdpConnection#Find user:" + peerUser.Key + " ipv4:" + udpCon.RemoteEndPoint.ToString());
					peerUser.Value.PeerConnect = udpCon;
					return;
				}
			}*/
		}

		void ProcessUdpDisConnection(UdpConnection udpCon)
		{
			string address = udpCon.RemoteEndPoint.Address.ToString ();
			int port = udpCon.RemoteEndPoint.Port;

			//host <--> server connection
			if (hostUser.IsProxyServer (address, port) == true) {
				Debug.LogError("ProcessUdpDisConnection#ProxyServer ipv4:" + udpCon.RemoteEndPoint.ToString());
				hostUser.PeerConnect = null;

				//通知外部事件更改.
				NetWorkStateInfo info = new NetWorkStateInfo();
				info.state = EClientNetWorkState.DisConnected;
				owner.CallUdpProxyServerState(info);
				return;
			}

			/*
			foreach (KeyValuePair<UInt32, P2PUser> peerUser in peerList) 
			{
				if (peerUser.Value.PeerConnect == null)
				{
					Log.info("P2PClientNetWork",  "ProcessUdpDisConnection#ignore uin:" + peerUser.Key);
					continue;
				}
				
				if ( peerUser.Value.IsUserPoint(address, port) == true )
				{
					Log.info("P2PClientNetWork",  "ProcessUdpDisConnection#Find user:" + peerUser.Key + " ipv4:" + udpCon.RemoteEndPoint.ToString());
					peerUser.Value.PeerConnect = null;
					return;
				}
			}*/
		}

		//for out event update..
		public void Update () {
			UdpEvent ev;
			byte[] buffer;
			while (server.Poll(out ev)) {
				switch (ev.EventType) {
				case UdpEventType.Connected:
				{
					Log.info("P2PClientNetWork",  "Client connect from :" + ev.Connection.RemoteEndPoint);
					ProcessUdpConnection(ev.Connection);
					break;
				}
				case UdpEventType.ObjectReceived:
				{
					buffer = ev.Object as byte[];
					Log.info("P2PClientNetWork",  "Client received from " + ev.Connection.RemoteEndPoint + " DataLength:" + buffer.Length);
					CtrlOwner.Reader.ReadP2PData(buffer, buffer.Length);
					break;
				}
				case UdpEventType.Disconnected:
				{
					Log.info("P2PClientNetWork",  "Client disconnect from :" + ev.Connection.RemoteEndPoint);
					ProcessUdpDisConnection(ev.Connection);
					break;
				}
				case UdpEventType.ObjectLost:
				{
					Log.info("P2PClientNetWork",  "Client lost, reconection :" + ev.Connection.RemoteEndPoint);
					ev.Connection.Send(ev.Object);
					break;
				}
				default:
					Log.info("P2PClientNetWork",  "Receive event, not process:" + ev.EventType.ToString());
					break;
				}
			}
		}

		public static IPAddress[] GetAllLocalIPs ()
		{
			string hostName = Dns.GetHostName ();
			IPAddress[] ips = Dns.GetHostAddresses (hostName);
			return ips;
		}
	}
}
