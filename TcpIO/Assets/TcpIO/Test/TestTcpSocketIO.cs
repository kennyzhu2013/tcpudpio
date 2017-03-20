using UnityEngine;
using System.Collections;

public class TestTcpSocketIO : MonoBehaviour {
	//private TcpIOComponent socket;

	public void Start() 
	{
		//GameObject go = GameObject.Find("SocketIO");
		//socket = go.GetComponent<TcpIOComponent>();

		//socket.On("open", TestOpen);
		//socket.On("boop", TestBoop);
		//socket.On("error", TestError);
		//socket.On("close", TestClose);
		S2CProtobufTest.register();
		StartCoroutine("BeepBoop");
	}

	private IEnumerator BeepBoop()
	{
		// wait 1 seconds and continue
		yield return new WaitForSeconds(5);
		C2SProtobufTest.SendUserRegister ();


		// wait 3 seconds and continue
		yield return new WaitForSeconds(3);

		//socket.Emit("beep");
		C2SProtobufTest.SendUserLogin();


		// wait 2 seconds and continue
		//yield return new WaitForSeconds(2);

		//socket.Emit("beep");

		// wait ONE FRAME and continue
		//yield return null;

		//socket.Emit("beep");
		//socket.Emit("beep");
	}


}
