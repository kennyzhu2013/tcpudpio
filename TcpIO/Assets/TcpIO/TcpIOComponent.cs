using UnityEngine;
using System.Collections;

//net call back...
using GEM_NET_LIB;
using System;


public delegate void VoidNotParamHandle();

//tcp i/o, work with leaf network..
public class TcpIOComponent : MonoBehaviour {

	//public Dictionary<int, IReceiver> receList = new Dictionary<int, IReceiver>(); 
	public NetworkDefine.NetworkStatus status = NetworkDefine.NetworkStatus.None;
	public event VoidNotParamHandle OnConnectScuessCallBack ;
	public event VoidNotParamHandle OnConnectFalseCallBack ;

	public event VoidNotParamHandle OnUdpProxyConnectCallBack ;
	public event VoidNotParamHandle OnUdpProxyDisConnectCallBack ;

	public bool bShowHeartBeat = false;

	public bool IsRestConnect = true;
	public bool IsInited = false;
	public string severIP = "183.131.145.158";
	public ushort port = 3563;

	//Udp proxy 网络层状态.
	public NetworkDefine.NetworkStatus proxyStatus = NetworkDefine.NetworkStatus.None;
	public bool IsProxyServerConnected()
	{
		return proxyStatus == NetworkDefine.NetworkStatus.Connected;
	}

	public void Awake()
	{
		//if(s_instance != null)
		//s_instance = this;
		Initalize();
		ReconnectServer();

		//beat app self add...
		//HeartBeatController.Instance.Init();
		//S2C_GameReconnectRsp.register();
		IsInited = true;
	}

	/// <summary>
	/// Connects the server.if bUdpOpen = false, just tcp connect
	/// </summary>
	void ConnectServer( )
	{
		if(status == NetworkDefine.NetworkStatus.Connected)
			return;

		ConnectTcp(severIP, port);
		Debug.Log ("Connect to Server IP:" + severIP + " Port:" + port);
	}

	/// <summary>
	/// Connects the server.
	/// </summary>
	void ConnectUdpServer()
	{
		if(proxyStatus == NetworkDefine.NetworkStatus.Connected)
			return;

		ConnectUdp(severIP, port);
	}

	/// <summary>
	/// Reconnects the server.
	/// </summary>
	public void ReconnectServer()
	{
		StartCoroutine(TryConnectRepeatedly());
	}

	/// <summary>
	/// Reconnects the udpserver.
	/// </summary>
	public void UdpReconnectServer()
	{
		StartCoroutine(TryUdpConnectRepeatedly());
	}


	void OnDestroy()
	{
		CNetWorkGlobal.Instance.Close ();
	}
	/// <summary>
	/// 初始化工作.
	/// </summary>
	void Initalize()
	{
		Log.addLevel (Log.ALL);
		CNetWorkGlobal.Instance.IsDebug = true;
		CNetWorkGlobal.Instance.RegisterNetWorkStateLister ( new GEM_NET_LIB.dNetWorkStateCallBack(OnConnectStateChange) );

		//add udp state
		CNetWorkGlobal.Instance.RegisterUdpProxyStateLister( new GEM_NET_LIB.dUdpProxyStateCallBack(OnUdpStateStateChange) );

		//register callback...
		if (OnUdpProxyDisConnectCallBack == null)
		OnUdpProxyDisConnectCallBack = new VoidNotParamHandle (OnUdpProxyDisConnect);
		else
		OnUdpProxyDisConnectCallBack += OnUdpProxyDisConnect;

		//Protocol.Init();
		NetworkDefine.Init ();
	}

	public void ConnectTcp(string url, ushort port)
	{
		CNetWorkGlobal.Instance.Connect(url, port);
		status = NetworkDefine.NetworkStatus.Connecting;

	}

	public void ConnectUdp(string url, ushort port)
	{
		CNetWorkGlobal.Instance.UdpConnect(url, port);//open udp.
		proxyStatus = NetworkDefine.NetworkStatus.Connecting;
	}

	int _connectCount = 0;
	public void OnConnectStateChange(GEM_NET_LIB.EClientNetWorkState a_eState, string ip, ushort port, Exception e)
	{
		if (GEM_NET_LIB.EClientNetWorkState.Connected == a_eState)
		{
			Log.info(this, "[NetworkManager][OnConnectStateChange] Succeed to connect server");
			status = NetworkDefine.NetworkStatus.Connected;

			if (null != OnConnectScuessCallBack)
			OnConnectScuessCallBack();

			//TODO...Start by config...
			//HeartBeatController.Instance.StartHeartBeat();

			_connectCount ++;
			/*
			if(bInReconnectingProcesss == true && _connectCount > 1)
			{
				SendReConnectRequest();
			}*/

			//TODO:只有第一次连接需要发送first bytes.....
			/*
			if (_connectCount == 1) {
			C2S_SendFirstBytes.SendFirstByte ();
			}*/
		} 
		else if(GEM_NET_LIB.EClientNetWorkState.Connecting == a_eState)
		{
			Log.info(this, "[NetworkManager][OnConnectStateChange] Begin connecting");
		}
		else
		{
			Log.error(this, "[NetworkManager][OnConnectStateChange] Failed to connect server, inner state :  " + a_eState + " Exception: " + e);


			if(bInReconnectingProcesss == true)
			{
				//PopMessage.Create("服务器连接失败，尝试重连...");
				Log.error(this, "服务器连接失败，尝试重连...");
				return ;
			}
			else
			{
				if(status == NetworkDefine.NetworkStatus.Failed)
				{
					Log.error(this, "您已断开连接=================!");
					/*
					MsgBox mbx = DialogBoxManager.GetInstance().MsgBox("您已断开连接", "提示") as MsgBox;
					if(OnReconnectFailCallback != null)
					OnReconnectFailCallback();
					mbx.Btn_YesOnClick = ()=>{
					Application.Quit();
					};*/
					Stop();
					return ;
				}
				else
				{
					//PopMessage.Create("网络连接不稳定");
					Log.error(this, "网络连接不稳定......");
				}

			}
			/*status = NetworkDefine.NetworkStatus.Failed;*/

			if (null != OnConnectFalseCallBack)
			OnConnectFalseCallBack();
		}

		/*
		Engine.Core.EventDispatcher.Instance.DispatchEvent(
		new Engine.Core.NetworkEvent()
		{
		errorType = a_eState,		
		connectCount = _connectCount,
		});*/

		Log.info(this, "[NetworkManager][OnConnectStateChange] EClientNetWorkState " + a_eState.ToString());

	}

	//udp proxy state func, register here
	public void OnUdpStateStateChange(GEM_NET_LIB.EClientNetWorkState a_eState, string ip, ushort port)
	{
		if (GEM_NET_LIB.EClientNetWorkState.Connected == a_eState)
		{
			Log.info(this, "[NetworkManager][OnUdpStateStateChange] Succeed to connect server");

			if (null != OnUdpProxyConnectCallBack)
			OnUdpProxyConnectCallBack();

			proxyStatus = NetworkDefine.NetworkStatus.Connected;
			//SendUdpRegiterRequest();
		} 
		else if(GEM_NET_LIB.EClientNetWorkState.DisConnected == a_eState)
		{
			Log.info(this, "[NetworkManager][OnUdpStateStateChange] Udp proxy-server loses connection");
			if (null != OnUdpProxyDisConnectCallBack)
			OnUdpProxyDisConnectCallBack();

			proxyStatus = NetworkDefine.NetworkStatus.Disconnected;
		}
		else
		{
			//nothing to do
		}

		Log.info(this, "[NetworkManager][OnUdpStateStateChange] EClientNetWorkState " + a_eState.ToString());
	}


	public void OnUdpProxyDisConnect()
	{
		//udpkit support heart beat...
		if (IsRestConnect) {
			UdpReconnectServer ();
		}
	}

	//重连.
	bool bInReconnectingProcesss = false;
	IEnumerator TryConnectRepeatedly(float waitTime = 10)
	{
		if(status == NetworkDefine.NetworkStatus.ReConnecting)
			yield break;
		if(bInReconnectingProcesss == true)
			yield break;
		int i = 0;
		bInReconnectingProcesss = true;
		Log.info(this, "[NetworkManager][TryConnectRepeatedly] Begin connecting .... ");
		while(i < 10)
		{
			i++;
			status = NetworkDefine.NetworkStatus.ReConnecting;
			CNetWorkGlobal.Instance.Close();
			ConnectServer();

			float waitDestTime = Time.realtimeSinceStartup + waitTime;
			//yield return new  WaitForSeconds(waitTime);
			while (waitDestTime > Time.realtimeSinceStartup)
			{
				yield return null;
			}
			//yield return new WaitForSeconds (waitTime);
			if(status == NetworkDefine.NetworkStatus.Connected)
				break;
		}
		if(status != NetworkDefine.NetworkStatus.Connected)
		{
			status = NetworkDefine.NetworkStatus.Failed;
			Log.error(this, "[NetworkManager][TryConnectRepeatedly] Cannot connect server!" + severIP + ":" + port);
		}

		bInReconnectingProcesss = false;
	}

	//Udp重连, Only need udp connections..
	IEnumerator TryUdpConnectRepeatedly(float waitTime = 10)
	{
		if(proxyStatus == NetworkDefine.NetworkStatus.ReConnecting)
			yield break;

		Log.info(this, "[NetworkManager][TryUdpConnectRepeatedly] Begin  Reconnecting .... ");
		proxyStatus = NetworkDefine.NetworkStatus.ReConnecting;
		CNetWorkGlobal.Instance.CloseUdp();
		ConnectUdpServer();
	}

	//TODO:reconnect and retransmit......

	public void Update()
	{
		if(IsRestConnect && IsInited)
		{
		//HeartBeatController.Instance.Update();
			CNetWorkGlobal.Instance.Update ();
		}
	}

	public void Stop()
	{
		IsRestConnect = false;
	}


	#if false
	void OnGUI()
	{
	if(GUILayout.Button("DumpRetrans", GUILayout.Width(100),GUILayout.Height(100)))
	{
	RetransmissionManager.Instance.Dump();
	}

	if(GUILayout.Button("Disconnect", GUILayout.Width(100),GUILayout.Height(100)))
	{
	CNetWorkGlobal.Instance.Close();
	}
	if(GUILayout.Button("Reconnect", GUILayout.Width(100),GUILayout.Height(100)))
	{
	ConnectServer();
	}
	}

	#endif
}
