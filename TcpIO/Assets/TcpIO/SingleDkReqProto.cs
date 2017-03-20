using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using GEM_NET_LIB;
using GEM_NET_LIB.proto;
using protoSources.GameProto;
using ProtoBuf;



public class SingleDkReqProto : DkReqProto  {

	/// <summary>
	/// Sends the message to sever.
	/// TODO:设置bReliable为true，超时后可以重传该请求.
	/// </summary>
	/// <param name="shMsgID">Sh message I.</param>
	/// <param name="data">Data.</param>
	/// <param name="bReliable">If set to <c>true</c> b reliable.</param>
	public static void sendMsgSever(int shMsgID, ProtoBuf.IExtensible data)
	{
		SingleDkReqProto drp = new SingleDkReqProto();
		drp.head = new proto_header();
		drp.head.shMsgID = shMsgID;
		drp.req = data;
			
		Log.info("SingleDkReqProto", "[SingleDkReqProto][sendMsgSever] Send Message. ID " + (GameCmdCode)shMsgID);
		drp.request();
	}

	/// <summary>
	/// Sends the message to sever.P2P Message.
	/// NetWork retransmit.
	/// </summary>
	/// <param name="shMsgID">Sh message I.</param>
	/// <param name="data">Data.</param>
	/// <param name="bReliable">If set to <c>true</c> b reliable.</param>
	public static void sendMsgUdp(int shMsgID, ProtoBuf.IExtensible data)
	{
		//Must set self-playerid.
		SingleDkReqProto drp = new SingleDkReqProto();
		drp.head = new proto_header();
		drp.head.shMsgID = shMsgID;
		drp.req = data;
		drp.isP2P = true;

		Log.info("SingleDkReqProto", "[SingleDkReqProto][sendMsgUdp] Send Message. ID " + (GameCmdCode)shMsgID);
		drp.request();
	}
}
