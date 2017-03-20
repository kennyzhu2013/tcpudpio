using UnityEngine;
using System;
using System.Collections.Generic;
using System.Net;//包含提供网络支持的类型
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;
using System.Text;
using System.IO;

using GEM_NET_LIB.proto;

namespace GEM_NET_LIB
{
// State object for receiving data from remote device.
	internal class StateObjectForRecvData
	{
		// Client socket.
		public Socket workSocket = null;
		// Size of receive buffer.
		public const int BufferSize = 4096;
		//Max Once Read
		// Receive buffer.
		public byte[] buffer = new byte[BufferSize];
	}

	public interface INetMessageReader
	{
		void DidReadData (byte[] data, int size);
		void ReadP2PData (byte[] data, int size);
		void Reset ();
	}

	public interface INetMessageWriter
	{
		byte[] MakeStream (proto_header header, MemoryStream data);

		void Reset ();
	}

	public class MemoryStreamEx : MemoryStream
	{
		public void Clear ()
		{
			SetLength (0);
		}
	}

	public enum EClientNetWorkState
	{
		None,
		Connecting, //连接中.
		Connected, //连接成功.
		ConnectFailed, //连接失败.
		DisConnected,	//未连接.
		SocketReleased, //连接已经释放.
		Max,		
	}

	public delegate void dNetWorkStateCallBack (EClientNetWorkState state ,string ip,ushort port, Exception e);
	public delegate void dUdpProxyStateCallBack( EClientNetWorkState state, string ip, ushort port );

	//网络状态信息.
	public class NetWorkStateInfo
	{
		public	EClientNetWorkState state = EClientNetWorkState.None; //当前网络状态.
		public Exception exception = null; //是否有异常引发.
	}

	public class CClientNetworkCtrl
	{
		private IAsyncResult m_ar_Recv = null;
		private IAsyncResult m_ar_Send = null;
		private IAsyncResult m_ar_Connect = null; 
		private Socket m_ClientSocket = null; //SOCKET.
		private string m_strRomoteIP = "127.0.0.1"; //服务器IP, tcp and udp.
		private ushort m_uRemotePort = 0; //服务器端口,tcp and udp.
		private ushort m_uLocalPort = 0; //本地端口.

		//服务器IP地址.
		public string ServerIP
		{
			get { return m_strRomoteIP; }
			
			set { m_strRomoteIP = value;}			
		}

		//远程服务器端口.
		public ushort ServerPort
		{
			get { return m_uRemotePort; }
			
			set { m_uRemotePort = value;}			
		}

		//socket本端端口.
		public ushort LocalPort
		{
			get { return m_uLocalPort; }			
			set { m_uLocalPort = value;}			
		}

		//接收数据回调.
		private INetMessageReader m_Reader = null;
		//发送数据代理.
		private INetMessageWriter m_Writer = null;
		//网络状态变化回调.
		private dNetWorkStateCallBack m_StateCallBack = null;
		private dUdpProxyStateCallBack m_ProxyStateCallback = null;

		//接收数据缓存.
		private MemoryStreamEx m_ComunicationMem = new MemoryStreamEx ();
		//网络状态信息.
		public NetWorkStateInfo m_networkState = new NetWorkStateInfo();
		//发送缓冲队列.
		private Queue<byte[]> m_SendQueue = new Queue<byte[]> ();
		//-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-==-=-=-=-=-=-==-=-=-=-=-=
		private UdpClientNetWork m_p2p = null;


		public void InitP2P()
		{
			m_p2p = new UdpClientNetWork ();
			if (m_p2p != null)
				m_p2p.Start (this);
		}

		public bool IsConnected ()
		{
			bool isConnected = IsSocketConnected(m_ClientSocket);
			if (isConnected) {
				m_uLocalPort = (ushort)(m_ClientSocket.LocalEndPoint as IPEndPoint).Port;
			}
			return isConnected;
		}

		public INetMessageReader Reader {
			get { return m_Reader; }
			set { m_Reader = value; }
		}

		public INetMessageWriter Writer {
			get { return m_Writer; }
			set { m_Writer = value; }
		}


		public UdpClientNetWork UdpClient {
			get { return m_p2p; }
			set { m_p2p = value; }
		}

		public void RegisterNetWorkStateLister (dNetWorkStateCallBack callback)
		{
			if (m_StateCallBack == null)
				m_StateCallBack = new dNetWorkStateCallBack (callback);
			else
				m_StateCallBack += callback;
		}

		//取消监听网络状态变化.
		public void UnRegisterNetWorkStateLister (dNetWorkStateCallBack callback)
		{
			if (m_StateCallBack != null)
				m_StateCallBack -= callback;
		}

		public void RegisterUdpProxyStateLister (dUdpProxyStateCallBack callback)
		{
			if (m_ProxyStateCallback == null)
				m_ProxyStateCallback = new dUdpProxyStateCallBack (callback);
			else
				m_ProxyStateCallback += callback;
		}

		//取消监听网络状态变化.
		public void UnRegisterUdpProxyStateLister (dUdpProxyStateCallBack callback)
		{
			if (m_ProxyStateCallback != null)
				m_ProxyStateCallback -= callback;
		}

		//连接服务器.
		public bool Connect (string a_strRomoteIP, ushort a_uPort)
		{
			if (m_ClientSocket == null) {
				try
				{
					m_ClientSocket = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
				}
				catch(Exception e)
				{
					MonoBehaviour.print(e);
				}

				IPAddress ip = IPAddress.Parse (a_strRomoteIP);
				m_strRomoteIP = a_strRomoteIP;
				m_uRemotePort = a_uPort;
				m_networkState.state = EClientNetWorkState.Connecting;
				m_networkState.exception = null;
				CallBackNetStateMarker (m_networkState);
				m_ar_Connect = m_ClientSocket.BeginConnect (ip, a_uPort, new AsyncCallback (ConnectCallback), m_ClientSocket);
				return true;
			}
			return false;
		}
		//释放之前的socket，重新连接.
		public bool ReConnect ()
		{
			if (m_strRomoteIP != null) 
			{
				ReleaseSocket ();
				//First Release Socket Resource
				return Connect (m_strRomoteIP, m_uRemotePort);
			}
			return false;
		}

		//发送消息主接口.
		public bool SendMessage (proto_header header, MemoryStream data)
		{
			if (m_Writer != null) {
				byte[] stream = m_Writer.MakeStream (header, data);
				lock (m_SendQueue) 
				{
					if (m_SendQueue.Count == 0) 
					{
						return Send (stream);
					} 
					else 
					{
						m_SendQueue.Enqueue (stream);
						return true;
					}
				}
			}
			return false;
		}

		//发送MemorySream里面的内容.
		public bool SendMessage(MemoryStream stream)
		{
			byte[] data = stream.ToArray();
            lock (m_SendQueue)
            {
                if (m_SendQueue.Count == 0)
                {
					return Send(data);
				}
				else
                {
					m_SendQueue.Enqueue(data);
					return true;
                }
            }
        }

		//直接发送原始数据.
		public bool SendMessage(byte[] data)
		{
			lock (m_SendQueue)
			{
				if (m_SendQueue.Count == 0)
				{
					return Send(data);
				}
				else
				{
					m_SendQueue.Enqueue(data);
					return true;
				}
			}
		}

		//for p2p send, to protobuf and message header
		public bool UdpSendMessage (proto_header header, MemoryStream data)
		{
			if (m_Writer != null) 
			{
				byte[] stream = m_Writer.MakeStream (header, data);
				return m_p2p.SendMessage (/*header.iUin,*/ stream); //header.iUin = dest uin
			}

			return false;
		}

		//清理所有数据》。.
		/// <summary>
		/// Releases the socket.but udp must hold.
		/// </summary>
		public void ReleaseSocket ()
		{
			if (m_ar_Recv != null)
				m_ar_Recv.AsyncWaitHandle.Close ();
			if (m_ar_Send != null)
				m_ar_Send.AsyncWaitHandle.Close ();
			if (m_ar_Connect != null)
				m_ar_Connect.AsyncWaitHandle.Close ();
			if (m_ClientSocket != null) 
			{
				try 
				{
					m_ClientSocket.Shutdown (SocketShutdown.Both);
				} 
				catch (Exception e) 
				{
					MonoBehaviour.print (e);
				} 
				finally 
				{
					m_ClientSocket.Close ();
					m_ClientSocket = null;
				}
			}
			m_networkState.state = EClientNetWorkState.SocketReleased;
			m_networkState.exception = null;
			if (m_Reader != null)
				m_Reader.Reset ();
			if (m_Writer != null)
				m_Writer.Reset ();
			if(m_SendQueue != null && m_SendQueue.Count > 0)
				m_SendQueue.Clear();
		}

		//每一秒轮训socket状态.
		float _lastCheckTime = 0;
		bool _lastDisconnected = false;
		void CheckSocketState()
		{
			if(Time.realtimeSinceStartup - _lastCheckTime > 1f)
			{
				_lastCheckTime = Time.realtimeSinceStartup;
//				if(m_ClientSocket == null)
//					return ;
				bool isDisconnected = ( (m_ClientSocket == null) || (m_ClientSocket.Poll(1000, SelectMode.SelectRead) && (m_ClientSocket.Available == 0)));

				if(isDisconnected == true)
				{
					m_networkState.state = EClientNetWorkState.DisConnected;
					CallBackNetStateMarker(m_networkState);
				}
				else
				{
					if(_lastDisconnected == true)
					{
						m_networkState.state = EClientNetWorkState.Connected;
						CallBackNetStateMarker(m_networkState);
					}
				}
				_lastDisconnected = isDisconnected;
			}
		}

		//这个在主线程调用.
		public void Update ()
		{
			//TODO @zjm 每一帧都lock，可能存在性能问题.
			lock (m_ComunicationMem) 
			{
				if (m_ComunicationMem.Length > 0) 
				{
					if ( CNetWorkGlobal.Instance.IsDebug )
						Log.info(this,  "ClientNetWork : Update m_ComunicationMem.Length: " + m_ComunicationMem.Length);
					if (m_Reader != null) 
					{
						m_Reader.DidReadData (m_ComunicationMem.GetBuffer (), (int)(m_ComunicationMem.Length));
					}
					m_ComunicationMem.Clear ();
				}
			}

			lock(m_networkState)
			{
				CheckSocketState();
				if(this.isStateChanged == true)
				{
					CallBackNetState (m_networkState);
					this.isStateChanged = false; 
				}
			}
		}

		//用于通知外部事件，外部轮训该标记，检查异常事件，这样可以防止线程竞争，这标志应该在主线程中置成false.
		public bool isStateChanged = false;
		private void CallBackNetStateMarker (NetWorkStateInfo info)
		{
			lock(m_networkState)
			{
				isStateChanged = true;	
			}
		}


		//在主线程的update里面回调.
		public void  CallBackNetState (NetWorkStateInfo info)
		{
			if (m_StateCallBack != null)
				m_StateCallBack (info.state, m_strRomoteIP, m_uRemotePort, info.exception);
		}

		/// <summary>
		/// Calls the state of the UDP proxy server.IP and port keep the same present.
		/// </summary>
		/// <param name="info">Info.</param>
		public void CallUdpProxyServerState ( NetWorkStateInfo info )
		{
			if (m_ProxyStateCallback != null)
				m_ProxyStateCallback ( info.state, m_strRomoteIP, m_uRemotePort );
		}

		//设定Socket的Nodelay.
		public void SetSocketSendNoDeley (bool nodelay)
		{
			if (m_ClientSocket != null) {
				m_ClientSocket.SetSocketOption (SocketOptionLevel.Tcp, SocketOptionName.NoDelay, nodelay ? 1 : 0);					
			}
		}
		
		//连接成功回调, 开始异步接收.
		private void ConnectCallback (IAsyncResult ar)
		{
			try 
			{
				ar.AsyncWaitHandle.Close ();
				m_ar_Connect = null;
				Socket client = (Socket)ar.AsyncState;
				client.EndConnect (ar);
				client.Blocking = false;

				m_uLocalPort = (ushort)(m_ClientSocket.LocalEndPoint as IPEndPoint).Port;
				lock (m_networkState) 
				{
					m_networkState.state = EClientNetWorkState.Connected;
					m_networkState.exception = null;				
					CallBackNetStateMarker (m_networkState);
				}
				Receive ();
			} 
			catch (Exception e) 
			{
				DidConnectError (e);
			}
		}

		private void DidConnectError (Exception e)
		{
			lock (m_networkState) 
			{
				m_networkState.state = EClientNetWorkState.ConnectFailed;
				m_networkState.exception = e;
				CallBackNetStateMarker (m_networkState);
			}
		}

		private void DidDisconnect (Exception e)
		{
			lock (m_networkState) 
			{
				m_networkState.state = EClientNetWorkState.DisConnected;
				m_networkState.exception = e;
				CallBackNetStateMarker ( m_networkState );
			}
		}

		//开始接受数据》.
		private void Receive ()
		{
			try 
			{
				StateObjectForRecvData state = new StateObjectForRecvData ();
				state.workSocket = m_ClientSocket;
				m_ar_Recv = m_ClientSocket.BeginReceive (state.buffer, 0, StateObjectForRecvData.BufferSize, 0, new AsyncCallback (ReceiveCallback), state);
			} 
			catch (Exception e) 
			{
				DidDisconnect (e);
			}
		}

		//接受数据回调.
		private void ReceiveCallback (IAsyncResult ar)
		{
			try 
			{
				ar.AsyncWaitHandle.Close ();
				m_ar_Recv = null;
				StateObjectForRecvData state = (StateObjectForRecvData)ar.AsyncState;
				Socket client = state.workSocket;
				int bytesRead = client.EndReceive (ar);
				if (bytesRead > 0) 
				{
					lock (m_ComunicationMem) 
					{
						m_ComunicationMem.Write (state.buffer, 0, bytesRead);
					}
				}
				m_ar_Recv = client.BeginReceive (state.buffer, 0, StateObjectForRecvData.BufferSize, 0, new AsyncCallback (ReceiveCallback), state);
			} 
			catch (Exception e) 
			{
				DidDisconnect (e);
			}
		}
		
		//开始发送.
		private bool Send (byte[] byteData)
		{
			// Begin sending the data to the remote device.
			try 
			{
				m_ar_Send = m_ClientSocket.BeginSend (byteData, 0, byteData.Length, 0, new AsyncCallback (SendCallback), m_ClientSocket);
				return true;
			}
			catch (Exception e) 
			{
				DidDisconnect (e);
			}
			return false;
		}
		
		//发送回调.
		private void SendCallback (IAsyncResult ar)
		{
			try 
			{
				ar.AsyncWaitHandle.Close ();
				m_ar_Send = null;
				Socket client = (Socket)ar.AsyncState;
				client.EndSend (ar);								
				OnSendSuccess ();
			} 
			catch (Exception e) 
			{
				DidDisconnect (e);
			}
		}

		//发送成功回调.
		private void OnSendSuccess ()
		{
			lock (m_SendQueue) 
			{
				if (m_SendQueue.Count > 0) 
				{
					Send (m_SendQueue.Dequeue ());
				}
			}
		}
	
		public static IPAddress GetLocalIP ()
		{
			string hostName = Dns.GetHostName ();
			IPAddress[] ips = Dns.GetHostAddresses (hostName);
			return ips.Length > 0 ? ips [0] : null;
		}

		public static string GetLocalIPString ()
		{
			IPAddress ip = GetLocalIP ();
			return ip != null ? ip.ToString () : "127.0.0.1";
		}

		public static string GetAdapterMAC ()
		{
			IPAddress local = GetLocalIP ();
			if (local != null) 
			{
				NetworkInterface[] nics = NetworkInterface.GetAllNetworkInterfaces ();
				foreach (NetworkInterface adaper in nics) 
				{
					IPInterfaceProperties ipPro = adaper.GetIPProperties ();
					UnicastIPAddressInformationCollection unicastColl = ipPro.UnicastAddresses;
					for (int i = 0; i < unicastColl.Count; i++) 
					{
						if (unicastColl [i].Address.Equals (local)) 
						{
							return adaper.GetPhysicalAddress ().ToString ();
						}
					}
				}
			}
			return "000000000000";
		}


		public static bool IsSocketConnected(Socket s)
		{
			#region remarks
			/* As zendar wrote, it is nice to use the Socket.Poll and Socket.Available, but you need to take into consideration 
             * that the socket might not have been initialized in the first place. 
             * This is the last (I believe) piece of information and it is supplied by the Socket.Connected property. 
             * The revised version of the method would looks something like this: 
             * from：http://stackoverflow.com/questions/2661764/how-to-check-if-a-socket-is-connected-disconnected-in-c */
			#endregion
			
			#region 过程
			
			if (s == null)
				return false;
			return !((s.Poll(1000, SelectMode.SelectRead) && (s.Available == 0)) || !s.Connected);
			
			/* The long, but simpler-to-understand version:

                    bool part1 = s.Poll(1000, SelectMode.SelectRead);
                    bool part2 = (s.Available == 0);
                    if ((part1 && part2 ) || !s.Connected)
                        return false;
                    else
                        return true;

            */
			#endregion
		}
	}
}
