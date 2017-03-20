using UnityEngine;
using System.Collections;

public class NetworkDefine 
{
	public enum	 NetworkStatus
	{
		None,
		Connecting,
		ReConnecting,
		Connected,
		Failed,
		Disconnected,
		TimeOut,
		Max,

	}

	//all protocol init here...
	public static void Init()
	{
		Log.info( "--------------------协议注册-------------------------");
	}
}
