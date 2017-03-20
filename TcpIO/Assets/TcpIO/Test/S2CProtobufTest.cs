using UnityEngine;
using System.Collections;
using GEM_NET_LIB;
using protoSources.GameProto;

public class S2CProtobufTest : SingleDKProto {

	// Use this for initialization
	/**注册协议**/
	public static void register()
	{
		baseRegister((short)GameCmdCode.CMD_CODE_REGISTER_RSP, typeof(protoSources.GameProto.GameAccountRegisterRsp), S2CProtobufTest.TestRegisterInfoRsp);
		baseRegister((short)GameCmdCode.CMD_CODE_LOGIN_RSP, typeof(protoSources.GameProto.GameAccountLoginRsp), S2CProtobufTest.TestLoginRsp);
	}

	public static void TestRegisterInfoRsp(DkRspMsg msg)
	{
		protoSources.GameProto.GameAccountRegisterRsp res = (protoSources.GameProto.GameAccountRegisterRsp)msg.body;
		if (res == null) {
			Log.error ("S2CProtobufTest", "GameAccountRegisterRsp is null");
			return;
		}

		//debug...
		Log.info("S2CProtobufTest", "res.result:" + res.result);
		Log.info("S2CProtobufTest", "res.uid:" + res.uid);
	}

	public static void TestLoginRsp(DkRspMsg msg)
	{
		protoSources.GameProto.GameAccountLoginRsp res = (protoSources.GameProto.GameAccountLoginRsp)msg.body;
		if (res == null) {
			Log.error ("S2CProtobufTest", "GameAccountLoginRsp is null");
			return;
		}

		Log.info("S2CProtobufTest", "res.result:" + res.result);
		Log.info("S2CProtobufTest", "res.uid:" + res.uid);
		Log.info("S2CProtobufTest", "res.money:" + res.money);
	}
}
