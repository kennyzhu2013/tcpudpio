using UnityEngine;
using System.Collections;
using protoSources.GameProto;
using GEM_NET_LIB;
using System.Collections.Generic;
using System;
using System.Reflection;

public delegate void NetworkMsgCallFunc(DkRspMsg msg); 

/// <summary>
/// 网络消息泵，主要用来根据具体消息ID，回调之前注册的相关接口.
/// </summar.>

public class OnlyMsg : MonoBehaviour {

	private static Dictionary<int,NetworkMsgCallFunc> msgBacks = new Dictionary<int, NetworkMsgCallFunc>();

	private static OnlyMsg _instance;
	public static OnlyMsg instance
	{
		get{
			if(_instance==null)
			{
				GameObject obj = new GameObject("msg");
				_instance = obj.AddComponent<OnlyMsg>();
			}
			return _instance;
		}
	}

	void Awake(){
		DontDestroyOnLoad(this.gameObject);
		_instance = this;
	}
	
	// Update is called once per frame
	void LateUpdate () {
		DkRspMsg drm =  DkProtoQueue.Instance.pop();
		//收到服务器消息..
		if(drm != null && msgBacks.ContainsKey(drm.head.shMsgID))
		{
			//not considering retransmitting and heart beat...
			//simplize processing
			NetworkMsgCallFunc ed = msgBacks[drm.head.shMsgID];
			if(ed!=null)
				ed(drm);
		}
		else if(drm!=null)
		{
			Debug.LogError("接收到消息号为:"+ (protoSources.GameProto.GameCmdCode)drm.head.shMsgID + "   ，但是您的协议未注册");
		}
	}
	public void addCallBack(int msgID,NetworkMsgCallFunc backFuck)
	{
		msgBacks.Add(msgID,backFuck);
	}
}
