using System;

public class GameAccountLoginRsp
{
	public uint Result;
	public uint Uid;
	public int  Expericens;
	public int  Gold;
	public const string CLASS_NAME="GameAccountLoginRsp";

	public bool ParseJsonObject(JSONObject _jsonObect)
	{
		//JSONObject(Dictionary<string, JSONObject> dic)
		if (_jsonObect.className != CLASS_NAME)
			return false;

		_jsonObect.GetField (ref Result, "Result");
		_jsonObect.GetField (ref Uid, "Uid");

		_jsonObect.GetField (ref Expericens, "Expericens");
		_jsonObect.GetField (ref Gold, "Gold");
		return true;
	}
}
