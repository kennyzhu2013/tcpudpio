using UnityEngine;
using System.Collections;
using GEM_NET_LIB;
using GEM_NET_LIB.proto;
using protoSources.GameProto;

/// <summary>
/// 协议回调注册接口，即Stub..
/// </summary>
public class SingleDKProto  {
	protected static void baseRegister(int shMsgID , System.Type type,NetworkMsgCallFunc backFuc)
	{
		DkRecProto dkrp = new DkRecProto();
		dkrp.m_head = new proto_header();
		dkrp.m_head.shMsgID = shMsgID;
		dkrp.type = type;
		OnlyMsg.instance.addCallBack(shMsgID,backFuc);
		dkrp.regist ();
	}
}

/// <summary>
/// 协议回调注册接口，即Stub..
/// </summary>
/*
public class SingleDKProto<T> : SingletonT<T> where T : new() 
{	
	protected void baseRegister(int shMsgID , System.Type type,NetworkMsgCallFunc backFuc)
	{
		DkRecProto dkrp = new DkRecProto();
		dkrp.m_head = new proto_header();
		dkrp.m_head.shMsgID = shMsgID;
		dkrp.type = type;
		OnlyMsg.instance.addCallBack(shMsgID,backFuc);
		dkrp.regist ();
	}
}*/
