#region License
/*
 * SocketIO.cs
 *
 * The MIT License
 *
 * Copyright (c) 2014 Fabio Panettieri
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */

#endregion

//#define SOCKET_IO_DEBUG			// Uncomment this for debug
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using WebSocketSharp;
using WebSocketSharp.Net;

namespace SocketIO
{
	public class SocketIOComponent : MonoBehaviour
	{
		#region Public Properties

		public string url = "ws://183.131.145.158:3653";
		public bool autoConnect = true;
		public int reconnectDelay = 5;
		//public float ackExpirationTime = 1800f;
		//public float pingInterval = 25f;
		//public float pingTimeout = 60f;

		public WebSocket socket { get { return ws; } }
		public string sid { get; set; }
		public bool IsConnected { get { return connected; } }

		#endregion

		#region Private Properties

		private volatile bool connected;
		//private volatile bool thPinging;
		//private volatile bool thPong;
		private volatile bool wsConnected;

		private Thread socketThread;
		//private Thread pingThread;
		private WebSocket ws;

		private Encoder encoder;
		private Decoder decoder;
		//private Parser parser;

		private Dictionary<string, List<Action<SocketIOEvent>>> handlers;
		//private List<Ack> ackList;
		//private int packetId;

		private object eventQueueLock;
		private Queue<SocketIOEvent> eventQueue;

		//private object ackQueueLock;
		//private Queue<Packet> ackQueue;

		#endregion

		#if SOCKET_IO_DEBUG
		public Action<string> debugMethod;
		#endif

		#region Unity interface

		public void Awake()
		{
			encoder = new Encoder();
			decoder = new Decoder();
			//parser = new Parser();
			handlers = new Dictionary<string, List<Action<SocketIOEvent>>>();
			//ackList = new List<Ack>();
			sid = null;
			//packetId = 0;

			ws = new WebSocket(url);
			ws.OnOpen += OnOpen;
			ws.OnMessage += OnMessage;
			ws.OnError += OnError;
			ws.OnClose += OnClose;
			wsConnected = false;

			eventQueueLock = new object();
			eventQueue = new Queue<SocketIOEvent>();

			//ackQueueLock = new object();
			//ackQueue = new Queue<Packet>();

			connected = false;

			#if SOCKET_IO_DEBUG
			if(debugMethod == null) { debugMethod = Debug.Log; };
			#endif
		}

		public void Start()
		{
			if (autoConnect) { Connect(); }
		}

		public void Update()
		{
			lock(eventQueueLock){ 
				while(eventQueue.Count > 0){
					EmitEvent(eventQueue.Dequeue());
				}
			}

			/*
			lock(ackQueueLock){
				while(ackQueue.Count > 0){
					InvokeAck(ackQueue.Dequeue());
				}
			}*/

			if(wsConnected != ws.IsConnected){
				wsConnected = ws.IsConnected;
				if(wsConnected){
					EmitEvent("connect");
				} else {
					EmitEvent("disconnect");
				}
			}

			// GC expired acks
			/*
			if(ackList.Count == 0) { return; }
			if(DateTime.Now.Subtract(ackList[0].time).TotalSeconds < ackExpirationTime){ return; }
			ackList.RemoveAt(0);*/
		}

		public void OnDestroy()
		{
			if (socketThread != null) 	{ socketThread.Abort(); }
			//if (pingThread != null) 	{ pingThread.Abort(); }
		}

		public void OnApplicationQuit()
		{
			Close();
		}

		#endregion



		#region Public Interface
		
		public void Connect()
		{
			connected = true;

			socketThread = new Thread(RunSocketThread);
			socketThread.Start(ws);

			//pingThread = new Thread(RunPingThread);
			//pingThread.Start(ws);
		}

		public void Close()
		{
			EmitClose();
			connected = false;
		}

		public void On(string ev, Action<SocketIOEvent> callback)
		{
			if (!handlers.ContainsKey(ev)) {
				handlers[ev] = new List<Action<SocketIOEvent>>();
			}
			handlers[ev].Add(callback);
		}

		public void Off(string ev, Action<SocketIOEvent> callback)
		{
			if (!handlers.ContainsKey(ev)) {
				#if SOCKET_IO_DEBUG
				debugMethod.Invoke("[SocketIO] No callbacks registered for event: " + ev);
				#endif
				return;
			}

			List<Action<SocketIOEvent>> l = handlers [ev];
			if (!l.Contains(callback)) {
				#if SOCKET_IO_DEBUG
				debugMethod.Invoke("[SocketIO] Couldn't remove callback action for event: " + ev);
				#endif
				return;
			}

			l.Remove(callback);
			if (l.Count == 0) {
				handlers.Remove(ev);
			}
		}
		#endregion


		#region Private Methods
		//不间断连接，断开连接的话,开启startReceving接收线程...
		private void RunSocketThread(object obj)
		{
			WebSocket webSocket = (WebSocket)obj;
			while(connected){
				if(webSocket.IsConnected){
					Thread.Sleep(reconnectDelay);
				} else {
					webSocket.Connect();
				}
			}
			webSocket.Close();
		}

		//TODO: 待实现...
		/*
		private void RunPingThread(object obj)
		{
			WebSocket webSocket = (WebSocket)obj;

			int timeoutMilis = Mathf.FloorToInt(pingTimeout * 1000);
			int intervalMilis = Mathf.FloorToInt(pingInterval * 1000);

			DateTime pingStart;

			while(connected)
			{
				if(!wsConnected){
					Thread.Sleep(reconnectDelay);
				} else {
					thPinging = true;
					thPong =  false;
					
					EmitPacket(new Packet(EnginePacketType.PING));
					pingStart = DateTime.Now;
					
					while(webSocket.IsConnected && thPinging && (DateTime.Now.Subtract(pingStart).TotalSeconds < timeoutMilis)){
						Thread.Sleep(200);
					}
					
					if(!thPong){
						webSocket.Close();
					}

					Thread.Sleep(intervalMilis);
				}
			}
		}*/

		private void EmitMessage(string raw)
		{
			EmitPacket(new Packet(new JSONObject(raw)));
		}

		private void EmitClose()
		{
			//断开连接消息，待转成logout....
			//EmitPacket(new Packet(EnginePacketType.MESSAGE, SocketPacketType.DISCONNECT, 0, "/", -1, new JSONObject("")));
			//EmitPacket(new Packet(EnginePacketType.CLOSE));
		}

		//same as EmitRawMessage..
		private void EmitPacket(Packet packet)
		{
			#if SOCKET_IO_DEBUG
			debugMethod.Invoke("[SocketIO] " + packet);
			#endif
			
			try {
				ws.Send(encoder.Encode(packet));
			} catch(SocketIOException ex) {
				#if SOCKET_IO_DEBUG
				debugMethod.Invoke(ex.ToString());
				#endif
				Log.info (ex.ToString());
			}
		}


		//betMsg
		public void EmitRawMessage(string rawpacket)
		{
			#if SOCKET_IO_DEBUG
			debugMethod.Invoke("[SocketIO] " + packet);
			#endif

			try {
				ws.Send(rawpacket);
			} catch(SocketIOException ex) {
				#if SOCKET_IO_DEBUG
				debugMethod.Invoke(ex.ToString());
				#endif
				Log.info (ex.ToString());
			}
		}

		private void OnOpen(object sender, EventArgs e)
		{
			Debug.Log ("OnOpen:");
			//EmitRawMessage ("{\"GameAccountRegisterReq\":{\"Name\":\"leaftest6\",\"Password\":\"leaftest6\"}}");
			EmitEvent("open");
		}

		private void OnMessage(object sender, MessageEventArgs e)
		{
			#if SOCKET_IO_DEBUG
			debugMethod.Invoke("[SocketIO] Raw message: " + e.Data);
			#endif
			Debug.Log ("[SocketIO] OnMessage e.Data: " + e.Data);

			//for to unserizlized
			JSONObject jsonObject = new JSONObject( e.Data );

			/*
			Packet packet = decoder.Decode(e);*/
			Debug.Log ("jsonObject.className:" + jsonObject.className);

			switch (jsonObject.className) {
				case GameAccountRegisterRsp.CLASS_NAME: 	HandJsonObject(jsonObject);		break;
			    case GameAccountLoginRsp.CLASS_NAME: 	HandLoginJsonObject(jsonObject);		break;
				case GameBetRsp.CLASS_NAME: 	HandBetJsonObject(jsonObject);		break;
				//case EnginePacketType.CLOSE: 	EmitEvent("close");		break;
				//case EnginePacketType.PING:		HandlePing();	   		break;
				//case EnginePacketType.PONG:		HandlePong();	   		break;
				//case EnginePacketType.MESSAGE: 	HandleMessage(packet);	break;
			}
		}

		private void HandleOpen(Packet packet)
		{
			#if SOCKET_IO_DEBUG
			debugMethod.Invoke("[SocketIO] Socket.IO sid: " + packet.json["sid"].str);
			#endif
			sid = packet.json["sid"].str;
			EmitEvent("open");
		}

		/*
		private void HandlePing()
		{
			EmitPacket(new Packet(EnginePacketType.PONG));
		}

		private void HandlePong()
		{
			thPong = true;
			thPinging = false;
		}*/

		//比如这里是响应根据classname分别处理...
		private void HandJsonObject(JSONObject jsonObject)
		{
			//根据classname的map找到类型是GameAccountRegisterRsp....
			GameAccountRegisterRsp rsp = new GameAccountRegisterRsp();
			rsp.ParseJsonObject (jsonObject);
			Debug.Log ("[SocketIO] HandJsonObject rsp.Uid: " + rsp.Uid);
		}

		private void HandLoginJsonObject(JSONObject jsonObject)
		{
			//根据classname的map找到类型是GameAccountRegisterRsp....
			GameAccountLoginRsp rsp = new GameAccountLoginRsp();
			rsp.ParseJsonObject (jsonObject);
			Debug.Log ("[SocketIO] HandLoginJsonObject rsp.Uid: " + rsp.Uid);
		}
		private void HandBetJsonObject(JSONObject jsonObject)
		{
			//根据classname的map找到类型是GameAccountRegisterRsp....
			GameBetRsp rsp = new GameBetRsp();
			rsp.ParseJsonObject (jsonObject);
			Debug.Log ("[SocketIO] HandBetJsonObject rsp.Gold: " + rsp.Gold);
		}

		/*
		private void HandleMessage(Packet packet)
		{
			if(packet.json == null) { return; }
			Debug.Log ("HandleMessage: packet.json:" + packet.json.ToString());

			if(packet.socketPacketType == SocketPacketType.ACK){
				for(int i = 0; i < ackList.Count; i++){
					if(ackList[i].packetId != packet.id){ continue; }
					lock(ackQueueLock){ ackQueue.Enqueue(packet); }
					return;
				}

				#if SOCKET_IO_DEBUG
				debugMethod.Invoke("[SocketIO] Ack received for invalid Action: " + packet.id);
				#endif
			}

			if (packet.socketPacketType == SocketPacketType.EVENT) {
				SocketIOEvent e = parser.Parse(packet.json);
				lock(eventQueueLock){ eventQueue.Enqueue(e); }
			}
		}*/

		private void OnError(object sender, ErrorEventArgs e)
		{
			Debug.Log ("OnError: " + e.Message);
			//EmitEvent("error");
		}

		private void OnClose(object sender, CloseEventArgs e)
		{
			Debug.Log ("OnClose:" + e.Reason);
			//EmitEvent("close");
		}

		private void EmitEvent(string type)
		{
			EmitEvent(new SocketIOEvent(type));
		}

		private void EmitEvent(SocketIOEvent ev)
		{
			if (!handlers.ContainsKey(ev.name)) { return; }
			foreach (Action<SocketIOEvent> handler in this.handlers[ev.name]) {
				try{
					handler(ev);
				} catch(Exception ex){
					#if SOCKET_IO_DEBUG
					debugMethod.Invoke(ex.ToString());
					#endif
					Log.info (ex.ToString());
				}
			}
		}
		/*
		private void InvokeAck(Packet packet)
		{
			Ack ack;
			for(int i = 0; i < ackList.Count; i++){
				if(ackList[i].packetId != packet.id){ continue; }
				ack = ackList[i];
				ackList.RemoveAt(i);
				ack.Invoke(packet.json);
				return;
			}
		}*/

		#endregion
	}
}
