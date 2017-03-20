using System.Collections;
using UnityEngine;
using SocketIO;

class GameAccountRegisterRsp {
	public uint Result;
	public uint Uid;
	public const string CLASS_NAME="GameAccountRegisterRsp";

	public bool ParseJsonObject(JSONObject _jsonObect)
	{
		//JSONObject(Dictionary<string, JSONObject> dic)
		if (_jsonObect.className != CLASS_NAME)
			return false;

		_jsonObect.GetField (ref Result, "Result");

		_jsonObect.GetField (ref Uid, "Uid");
		return true;
	}

} 
