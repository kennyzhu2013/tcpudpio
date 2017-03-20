using System.Collections;
using UnityEngine;
using SocketIO;
using System.Collections.Generic;

class GameAccountRegisterReq {

	public string Name;
	public string Password;
	public const string CLASS_NAME="GameAccountRegisterReq";

	private JSONObject _jsonObect = null;
	public string ToJsonStirng()
	{
		//JSONObject(Dictionary<string, JSONObject> dic)
		Dictionary<string, string> dicionary = new Dictionary<string, string>();
		dicionary.Add ("Name", Name);
		dicionary.Add ("Password", Password);
		_jsonObect = new JSONObject (dicionary);
		_jsonObect.className = CLASS_NAME;

		//StringBuilder builder = new StringBuilder();
		//_jsonObect.Stringify (2, builder, false);
		dicionary = null;
		return _jsonObect.Print(false);
	}

}