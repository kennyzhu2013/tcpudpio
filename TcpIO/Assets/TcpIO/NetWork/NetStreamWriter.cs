//======================================================
//  NetWork Stream Writer
//  2011.12.15 created by Wangnannan
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

namespace GEM_NET_LIB
{
	public class CNetStreamWriter : INetMessageWriter
	{
		private MemoryStreamEx m_Buffer = new MemoryStreamEx ();
		//private static UInt32  m_cmdSequece = 0;
		//private byte[] m_NotUseByte = new byte[4]{0,0,0,0};

		//-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=-=
		//兼容新的golang服务器修改....
		byte[] INetMessageWriter.MakeStream (proto_header header, MemoryStream data)
		{
			m_Buffer.Clear();
			//m_cmdSequece ++;

			//先header初始化
			//header.uMsgContext = m_cmdSequece;
			header.iMsgBodyLen = proto_header.MODULE_HEAD_LENGTH + (data != null ?(int)data.Length : 0);
			int net_BodyLen = IPAddress.HostToNetworkOrder (header.iMsgBodyLen);
			byte[] net_BodyLen_byte = BitConverter.GetBytes(net_BodyLen);
			m_Buffer.Write(net_BodyLen_byte,0,net_BodyLen_byte.Length);

			int net_msgID = IPAddress.HostToNetworkOrder (header.shMsgID);
			byte[] net_MsgID_byte = BitConverter.GetBytes(net_msgID);
			m_Buffer.Write(net_MsgID_byte,0,net_MsgID_byte.Length);

			/*
			int net_iSubCmdID = IPAddress.HostToNetworkOrder (header.iSubCmdID);
			byte[] net_iSubCmdID_byte = BitConverter.GetBytes(net_iSubCmdID);

			int net_uMsgContext = IPAddress.HostToNetworkOrder ((int)header.uMsgContext);
			byte[] net_uMsgContext_byte = BitConverter.GetBytes(net_uMsgContext);

			int net_iUin = IPAddress.HostToNetworkOrder ((int)header.iUin);
			byte[] net_iUin_byte = BitConverter.GetBytes(net_iUin);

			int net_uHostTag = IPAddress.HostToNetworkOrder ((int)header.uHostTag);
			byte[] net_uHostTag_byte = BitConverter.GetBytes(net_uHostTag);

			int net_uClientSeq = IPAddress.HostToNetworkOrder ((int)header.uClientSeq);
			byte[] net_uClientSeq_byte = BitConverter.GetBytes(net_uClientSeq);*/

			//body
			//int net_data_size = proto_header.HEAD_LENGTH + (data != null ?(int)data.Length : 0);
			//byte[] net_Data_Size_byte = BitConverter.GetBytes(net_data_size);

			/*
			m_Buffer.Write(net_iSubCmdID_byte,0,net_MsgID_byte.Length);
			m_Buffer.Write(net_uMsgContext_byte,0,net_MsgID_byte.Length);
			m_Buffer.Write(net_iUin_byte,0,net_MsgID_byte.Length);
			m_Buffer.Write(net_uHostTag_byte,0,net_uHostTag_byte.Length);
			m_Buffer.Write(net_uClientSeq_byte,0,net_uClientSeq_byte.Length);*/

			//body
			if (data != null) 
			{
				m_Buffer.Write (data.GetBuffer (), 0, (int)data.Length);

				if ( CNetWorkGlobal.Instance.IsDebug )
					Log.info(this, "[CNetStreamWriter][MakeStream] WritingHead , module head len : "  + proto_header.MODULE_HEAD_LENGTH 
							+ ",  stream len(not include length 4 bytes)  "  + header.iMsgBodyLen);
			}

			return m_Buffer.ToArray();
		}

		/*
		byte[] INetMessageWriter.MakeStream (proto_header header, MemoryStream data)
		{
			m_Buffer.Clear();
			m_cmdSequece ++;

			//先header初始化
			header.uMsgContext = m_cmdSequece;
			header.iMsgBodyLen = (int)data.Length;
			int net_BodyLen = IPAddress.HostToNetworkOrder (header.iMsgBodyLen);
			byte[] net_BodyLen_byte = BitConverter.GetBytes(net_BodyLen);

			int net_msgID = IPAddress.HostToNetworkOrder (header.shMsgID);
			byte[] net_MsgID_byte = BitConverter.GetBytes(net_msgID);

			int net_iSubCmdID = IPAddress.HostToNetworkOrder (header.iSubCmdID);
			byte[] net_iSubCmdID_byte = BitConverter.GetBytes(net_iSubCmdID);

			int net_uMsgContext = IPAddress.HostToNetworkOrder ((int)header.uMsgContext);
			byte[] net_uMsgContext_byte = BitConverter.GetBytes(net_uMsgContext);

			int net_iUin = IPAddress.HostToNetworkOrder ((int)header.iUin);
			byte[] net_iUin_byte = BitConverter.GetBytes(net_iUin);

			int net_uHostTag = IPAddress.HostToNetworkOrder ((int)header.uHostTag);
			byte[] net_uHostTag_byte = BitConverter.GetBytes(net_uHostTag);

			int net_uClientSeq = IPAddress.HostToNetworkOrder ((int)header.uClientSeq);
			byte[] net_uClientSeq_byte = BitConverter.GetBytes(net_uClientSeq);

			//body
			int net_data_size = proto_header.HEAD_LENGTH + (data != null ?(int)data.Length : 0);
			//byte[] net_Data_Size_byte = BitConverter.GetBytes(net_data_size);
			m_Buffer.Write(net_BodyLen_byte,0,net_MsgID_byte.Length);
			m_Buffer.Write(net_MsgID_byte,0,net_MsgID_byte.Length);
			m_Buffer.Write(net_iSubCmdID_byte,0,net_MsgID_byte.Length);
			m_Buffer.Write(net_uMsgContext_byte,0,net_MsgID_byte.Length);
			m_Buffer.Write(net_iUin_byte,0,net_MsgID_byte.Length);
			m_Buffer.Write(net_uHostTag_byte,0,net_uHostTag_byte.Length);
			m_Buffer.Write(net_uClientSeq_byte,0,net_uClientSeq_byte.Length);

			//body
            if (data != null) 
			{
				m_Buffer.Write (data.GetBuffer (), 0, (int)data.Length);

				if ( CNetWorkGlobal.Instance.IsDebug )
					Log.info(this, "[CNetStreamWriter][MakeStream] WritingHead , head len : "  + proto_header.HEAD_LENGTH + ",  stream len  "  + net_data_size);
			}

			return m_Buffer.ToArray();
		}*/


		void INetMessageWriter.Reset ()
		{
			m_Buffer.Clear ();
		}
	}
}
