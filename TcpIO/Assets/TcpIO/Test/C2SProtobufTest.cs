using UnityEngine;
using System.Collections;

//protobuf request test...
using protoSources.GameProto;


public class C2SProtobufTest : SingleDKProto {
	//Register...
	public static void SendUserRegister()
	{
		protoSources.GameProto.GameAccountRegisterReq data = new protoSources.GameProto.GameAccountRegisterReq();
		data.name = "Test20160815_1";
		data.passwd = "Test20160815_1"; //mistake， this is user password...
		SingleDkReqProto.sendMsgSever((short)GameCmdCode.CMD_CODE_REGISTER_REQ, data);
	}

	//Login...
	public static void SendUserLogin()
	{
		protoSources.GameProto.GameAccountLoginReq data = new protoSources.GameProto.GameAccountLoginReq();
		data.name = "Test20160815_1";
		data.passwd = "Test20160815_1"; //mistake， this is user password...
		SingleDkReqProto.sendMsgSever((short)(GameCmdCode.CMD_CODE_LOGIN_REQ), data);
	}
}
