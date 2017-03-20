using System;
using UnityEngine;

public class GameBetRsp
{
	public uint Result;
	public uint Uid;
	public int  Gold;
	public int  Expericens;
	public uint  Multiples;
	public uint[] Iconids;
	public const string CLASS_NAME="GameBetRsp";

	public bool ParseJsonObject(JSONObject _jsonObect)
	{
		//JSONObject(Dictionary<string, JSONObject> dic)
		if (_jsonObect.className != CLASS_NAME)
			return false;

		_jsonObect.GetField (ref Result, "Result");
		_jsonObect.GetField (ref Uid, "Uid");

		_jsonObect.GetField (ref Gold, "Gold");
		_jsonObect.GetField (ref Expericens, "Expericens");
		_jsonObect.GetField (ref Multiples, "Multiples");
		JSONObject obj = _jsonObect.GetField ("Iconids");

		//数组解析...
		GetIconidsResponse (obj);
		Debug.Log ("Iconids is :" + obj.ToString());
		return true;
	}

	public void GetIconidsResponse(JSONObject obj)
	{
		int count = obj.Count;
		Iconids = new uint[count];
		int i = 0;
		foreach (JSONObject value in obj.list) {
			Iconids[i] = (uint)value.n;
			Debug.Log ("Iconids[i]  is :" + Iconids[i] );
			++i;
		}
	}
}
