using System;
using System.IO;
using ProtoBuf;
using GEM_NET_LIB.proto;

namespace GEM_NET_LIB
{
    public class DkReqProto
    {
        //消息头
        public proto_header head;

		/*
        public UInt32 UIN
        {
            get { return head.iUin; }

            set { head.iUin = value; }
        }*/

        //是否p2p
        public bool isP2P = false;
        public bool isGroup = false; //是否组内广播

        //p2p接收者
        //public int[] remotes;
        MemoryStream m_bytes = new MemoryStream();

        //protobuf请求数据
        public IExtensible req;

        virtual public void request()
        {
            ProtobufSerializer serializer = new ProtobufSerializer();

            //using (MemoryStream temp = new MemoryStream())

            serializer.Serialize(m_bytes, req);
            //m_bytes.Position = 0;

            CNetWorkGlobal.Instance.SendProto(this);
        }

        public MemoryStream bytes
        {
            get
            {
                return m_bytes;
            }
        }
    }

}
