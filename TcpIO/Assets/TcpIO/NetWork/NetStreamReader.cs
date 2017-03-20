//======================================================
// 该文件主要实现且包功能.
// 
//======================================================
/*
 Message Stream Format:Size(4),MsgID(4),NOTUSE(4),PB(0~)
 */
using UnityEngine;
using System.Collections;
using System.IO;
using System;
using System.Net;
using GEM_NET_LIB.proto;
//using protoSources.GameProto;

namespace GEM_NET_LIB
{
	public interface IReaderHandleMessage
	{
		void HandleMessage (int msgID,int data_type,MemoryStream data);
	}

	public class CNetStreamReader : INetMessageReader
	{

		private static readonly int m_nMaxDataSize = 200 * 1024;
		//-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
		private SocketBuffer m_sockBuffer = new SocketBuffer();
		private SocketBuffer m_p2pBuffer = new SocketBuffer();

		byte [] _bodyData; //包体内容.
		int _bodyCursor = 0; //当前包体的指针(位置).
		proto_header _header; //包头.

		public enum ReadPacketState
		{
			None,
			ReadingHead, //正在读取包头.
			ReadHeadFinish, //包头读取完整.
			ReadingBody, //正在读取包体.
			ReadingBodyFinish, //包体读取完成.
			Max,
		}

		ReadPacketState _readState =  ReadPacketState.None; //简单状态机.

		public delegate bool ReceiveCallDelegate(proto_header head, byte[] block);
		public ReceiveCallDelegate OnReceivePacketFinish; //读取一个完整的包时的回调.
		/// <summary>
		/// 从流中读取数据,.
		/// </summary>
		/// <returns><c>true</c>, if read data1 was dided, <c>false</c> otherwise.</returns>
		/// <param name="data">Data.</param>
		/// <param name="size">Size.</param>
#if true
		void INetMessageReader.DidReadData(byte[] data, int size)
		{
			int PDULen_ = proto_header.MODULE_HEAD_LENGTH + 4; //body lenth and message key..

			m_sockBuffer.PushBack(data, 0, size);
		
			if ( CNetWorkGlobal.Instance.IsDebug )
				Log.info(this, "[CNetStreamReader][DidReadData] Receive data " + size + " total " + m_sockBuffer.DataCount);

			bool bBreak = false;
			while (bBreak == false)
			{
				switch(_readState)
				{
				case ReadPacketState.None:
				case ReadPacketState.ReadingHead: //注意这里的header是一次性读取，没有做包缓存..
				{

					if ( CNetWorkGlobal.Instance.IsDebug )
						Log.info(this, "[CNetStreamReader][DidReadData] ReadingHead , head len : "  + PDULen_ 
							+ ",  stream len(include 4 bytes length)  "  + m_sockBuffer.DataCount);
					_header = new proto_header();
					if(m_sockBuffer.DataCount  < PDULen_)
					{
						bBreak =  true;
						return;
					}


					byte[] nowData = new byte[PDULen_];

					int ret = m_sockBuffer.ReadFront(nowData, 0, PDULen_);
					if(ret != PDULen_)
					{
						Debug.LogError ("[CNetStreamReader][DidReadData] Invalid read expect: " + PDULen_ + " return : " + ret);
						throw new System.NotSupportedException("[CNetStreamReader][DidReadData] Net work exception , cannot read packet");
					}

					//Read Header.

					_header.iMsgBodyLen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(nowData, 0));
					_header.shMsgID = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(nowData, 4));

				    /*
					_header.iSubCmdID = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(nowData, 8));
					_header.uMsgContext = (UInt32)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(nowData, 12));
					_header.iUin = (UInt32)IPAddress.NetworkToHostOrder(BitConverter.ToInt32(nowData, 16));
					_header.uHostTag = (UInt32)IPAddress.NetworkToHostOrder((BitConverter.ToInt32(nowData, 20)));
					_header.uClientSeq= (UInt32)IPAddress.NetworkToHostOrder((BitConverter.ToInt32(nowData, 24)));
					*/
					_readState = ReadPacketState.ReadHeadFinish;
					if ( CNetWorkGlobal.Instance.IsDebug )
						Log.info(this, "[CNetStreamReader][DidReadData] Reading head , iMsgBodyLen "+ _header.iMsgBodyLen + " _header.shMsgID " + _header.shMsgID );
				}
					break;
				case ReadPacketState.ReadHeadFinish: //读完包头，准备度body.
				{
					if ( CNetWorkGlobal.Instance.IsDebug )
						Log.info(this, "[CNetStreamReader][DidReadData] Reading head finish");

					//长度减去key头长度。。..
					int bodylen = _header.iMsgBodyLen - proto_header.MODULE_HEAD_LENGTH;
					if ( bodylen < 0 )
					{
						Debug.LogError ("[CNetStreamReader][DidReadData] Invalid bodylen : " + bodylen);
						throw new System.NotSupportedException("[CNetStreamReader][DidReadData] Net work exception , cannot read packet");
					}

					_bodyData = new byte[bodylen];
					_bodyCursor = 0;
					_readState = ReadPacketState.ReadingBody;
				}
					break;
				case ReadPacketState.ReadingBody:
				{
					int bodyLen = _header.iMsgBodyLen - proto_header.MODULE_HEAD_LENGTH - _bodyCursor;
					int ret = m_sockBuffer.ReadFront(_bodyData, _bodyCursor, bodyLen);
					if ( CNetWorkGlobal.Instance.IsDebug )
						Log.info(this, "[CNetStreamReader][DidReadData] Reading body len " + bodyLen + " ret " + ret + " total " 
						           + m_sockBuffer.DataCount + " _header.iMsgBodyLen " + _header.iMsgBodyLen + " cursor " + _bodyCursor);
					_bodyCursor += ret;
			
					if(ret == bodyLen)
					{
						_readState = ReadPacketState.ReadingBodyFinish;
						if(OnReceivePacketFinish != null)
						{
							if ( CNetWorkGlobal.Instance.IsDebug )
								Log.info(this, "[CNetStreamReader][DidReadData] Call Receive " + OnReceivePacketFinish);
							OnReceivePacketFinish( _header, _bodyData );
						}
							
					}
					else
					{
						bBreak = true;
						return; //body读不完的时候，表明没有数据读,直接退出该函数.
					}

				}
					break;
				case ReadPacketState.ReadingBodyFinish:
				{
					if ( CNetWorkGlobal.Instance.IsDebug )
						Log.info(this, "reading body Finish");
					_readState = ReadPacketState.ReadingHead; //读完一个完整的包，继续读Head，尝试下个包.
				}
					break;
				default:
					Debug.LogError("[INetMessageReader][ReadPacket] Invalid Inner read state " + _readState);
					bBreak = true;
					throw new System.NotSupportedException("[INetMessageReader][ReadPacket] Invalid Inner read state");
					return ;
					break;
				}
			}
		}
#endif

		//增加对p2p消息的读取
		void INetMessageReader.ReadP2PData(byte[] data, int size)
		{
			int PDULen_ = proto_header.MODULE_HEAD_LENGTH + 4;
			
			m_p2pBuffer.PushBack(data, 0, size);
			
			if ( CNetWorkGlobal.Instance.IsDebug )
				Log.info(this, "[CNetStreamReader][DidReadData] Receive data " + size + " total " + m_p2pBuffer.DataCount);
			
			bool bBreak = false;
			while ( bBreak == false )
			{
				switch(_readState)
				{
				case ReadPacketState.None:
				case ReadPacketState.ReadingHead: //注意这里的header是一次性读取，没有做包缓存..
				{
					
					if ( CNetWorkGlobal.Instance.IsDebug )
						Log.info(this, "[CNetStreamReader][DidReadData] ReadingHead , head len : "  + PDULen_ + ",  stream len  "  + m_p2pBuffer.DataCount);
					_header = new proto_header();
					if(m_p2pBuffer.DataCount < PDULen_)
					{
						bBreak =  true;
						return;
					}
					
					
					byte[] nowData = new byte[PDULen_];					
					int ret = m_p2pBuffer.ReadFront(nowData, 0, PDULen_);
					if(ret != PDULen_)
					{
						Debug.LogError ("[CNetStreamReader][DidReadData] Invalid read expect: " + PDULen_ + " return : " + ret);
						throw new System.NotSupportedException("[CNetStreamReader][DidReadData] Net work exception , cannot read packet");
					}
					
					//Read Header.
					
					_header.iMsgBodyLen = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(nowData, 0));
					_header.shMsgID = IPAddress.NetworkToHostOrder(BitConverter.ToInt32(nowData, 4));
					_readState = ReadPacketState.ReadHeadFinish;
					if ( CNetWorkGlobal.Instance.IsDebug )
						Log.info(this, "[CNetStreamReader][DidReadData] Reading head , iMsgBodyLen "+ _header.iMsgBodyLen + " _header.shMsgID " + _header.shMsgID);
				}
					break;
				case ReadPacketState.ReadHeadFinish: //读完包头，准备度body.
				{
					if ( CNetWorkGlobal.Instance.IsDebug )
						Log.info(this, "[CNetStreamReader][DidReadData] Reading head finish");
					_bodyData = new byte[_header.iMsgBodyLen];
					_bodyCursor = 0;
					_readState = ReadPacketState.ReadingBody;
				}
					break;
				case ReadPacketState.ReadingBody:
				{
					int bodyLen = _header.iMsgBodyLen - proto_header.MODULE_HEAD_LENGTH - _bodyCursor;
					int ret = m_p2pBuffer.ReadFront(_bodyData, _bodyCursor, bodyLen);
					if ( CNetWorkGlobal.Instance.IsDebug )
						Log.info(this, "[CNetStreamReader][DidReadData] Reading body len " + bodyLen + " ret " + ret 
						           + " total " + m_p2pBuffer.DataCount + " _header.iMsgBodyLen " + _header.iMsgBodyLen + " cursor " + _bodyCursor);
					_bodyCursor += ret;
					
					if(ret == bodyLen)
					{
						_readState = ReadPacketState.ReadingBodyFinish;

						if ( CNetWorkGlobal.Instance.IsDebug )
							Log.info(this, "[CNetStreamReader][DidReadData] Call Receive " + OnReceivePacketFinish);
						CNetWorkGlobal.Instance.ReceiveP2P(_header, _bodyData);				
					}
					else
					{
						bBreak = true;
						return; //body读不完的时候，表明没有数据读,直接退出该函数.
					}
					
				}
					break;
				case ReadPacketState.ReadingBodyFinish:
				{
					if ( CNetWorkGlobal.Instance.IsDebug )
						Log.info(this, "reading body Finish");
					_readState = ReadPacketState.ReadingHead; //读完一个完整的包，继续读Head，尝试下个包.
				}
					break;
				default:
					Debug.LogError("[INetMessageReader][ReadPacket] Invalid Inner read state " + _readState);
					bBreak = true;
					throw new System.NotSupportedException("[INetMessageReader][ReadPacket] Invalid Inner read state");
					return ;
					break;
				}
			}
		}

		void INetMessageReader.Reset ()
		{
			m_sockBuffer.Clear();

			m_p2pBuffer.Clear();
		}
	}

	//实现一个管道，内部用循环缓冲区，方便缓冲区socket使用.
	internal class SocketBuffer
	{
		public byte[] Buffer { get; set; } //存放内存的数组.

		int _bufferBegin = 0; //缓冲区数据部分起始位置.
		int _bufferEnd = 0; //缓冲区数据部分结束位置, 该位置不存任何数据，只作为标记用.

		//总容量，预留一个字节，表示结束位置.
		public int Capacity
		{
			get{ return Buffer.Length - 1;}
		}
		//整个缓冲区大小.
		public int BufferSize
		{
			get{return Buffer.Length;}
		}
		//当前写入数据大小.
		public int DataCount 
		{ 
			get{return (Buffer.Length + _bufferEnd - _bufferBegin) % Buffer.Length; }
		}
		
		public SocketBuffer(int bufferSize = 8192)
		{
			_bufferBegin = 0;
			_bufferEnd = 0;
			Buffer = new byte[bufferSize];
		}

		//剩余的字节数, 去掉站位标志，需要减1.
		public int ReservedCount 
		{
			get{return Buffer.Length - DataCount - 1;}
		}
		//剩余一个位置为满.
		public bool IsFull
		{
			get{return DataCount == Buffer.Length - 1;}
		}

	

		/// <summary>
		/// 首尾相等为空.
		/// </summary>
		/// <returns><c>true</c> if this instance is empty; otherwise, <c>false</c>.</returns>
		public bool IsEmpty	{
			get{return DataCount == 0;}
		}

		//清空所有数据.
		public void Clear()
		{
			_bufferBegin = 0;
			_bufferEnd = 0;
		}

		//追加数据, 缓冲区数量不够时，会重新分配缓冲区.
		public void PushBack(byte[] data, int offset, int count)
		{
			if(DataCount + count > BufferSize)
			{
				ExpandBuffer((DataCount + count) * 2);
			}

			if(_bufferEnd + count < BufferSize) //检查右边界是否超出，未超出则直接拷贝.
			{
				Array.Copy(data, offset, Buffer, _bufferEnd, count);
				_bufferEnd += count;
			}
			else
			{
				int firstPieceLen = BufferSize - _bufferEnd;
				Array.Copy(data, offset, Buffer, _bufferEnd, firstPieceLen);

				int secondPiceceLen = count - firstPieceLen;
				Array.Copy(data, offset + firstPieceLen, Buffer, 0, secondPiceceLen);
				_bufferEnd = secondPiceceLen;
			}

		}

		/// <summary>
		/// 读走前面的数据，内部缓冲区指针后移.
		/// </summary>
		/// <returns>The front.</returns>
		/// <param name="buffer">Buffer.</param>
		/// <param name="offset">Offset.</param>
		/// <param name="count">Count.</param>
		public int ReadFront(byte[] data, int offset, int count)
		{
			//没有足够数据的时候.
			if(DataCount < count)
				count = DataCount;
			if(_bufferBegin + count < BufferSize )
			{
				Array.Copy(Buffer, _bufferBegin, data, offset, count);
				_bufferBegin += count;
			}
			else
			{
				//分段处理.
				int firstPieceLen = BufferSize - _bufferBegin;
				Array.Copy( Buffer, _bufferBegin, data, offset, firstPieceLen);
				
				int secondPiceceLen = count - firstPieceLen;
				Array.Copy(Buffer, 0, data, offset + firstPieceLen,  secondPiceceLen);
				_bufferBegin = secondPiceceLen;
			}
			return count;
		}
		//缓冲区扩容,返回值表示是否产生新的内存分配.
		public bool ExpandBuffer(int newCapacity)
		{
			if(newCapacity < Capacity)
			{
				return false ;
			}

			byte [] tempBuffer = new byte[newCapacity + 1];
			int dataToRead = DataCount;
			int ret = this.ReadFront(tempBuffer, 0, dataToRead);
			if(ret != dataToRead)
			{
				Debug.LogError("[SocketBuffer][ExpandBuffer] Cannot Read old buffer");
				return false;
			}
			//置换掉当前的buffer.
			_bufferBegin = 0;
			_bufferEnd = ret;
			Buffer = tempBuffer;
			return true;
		}
	}
}
