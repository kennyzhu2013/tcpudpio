using System;
using System.Collections.Generic;

public class GameBetReq
{
	public uint Uid;
	public uint Multiples;
	public const string CLASS_NAME="GameBetReq";

	private JSONObject _jsonObect = null;
	public string ToJsonStirng()
	{
		//JSONObject(Dictionary<string, JSONObject> dic)
		Dictionary<string, JSONObject> dicionary = new Dictionary<string, JSONObject>();

		dicionary.Add ("Uid", new JSONObject(Uid));
		dicionary.Add ("Multiples", new JSONObject(Multiples));
		_jsonObect = new JSONObject (dicionary);
		_jsonObect.className = CLASS_NAME;

		//StringBuilder builder = new StringBuilder();
		//_jsonObect.Stringify (2, builder, false);
		dicionary = null;
		return _jsonObect.Print(false);
		//return "";
	}
}

