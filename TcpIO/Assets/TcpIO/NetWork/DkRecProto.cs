using System;
using System.IO;
using GEM_NET_LIB;
using ProtoBuf;
using UnityEngine;
using GEM_NET_LIB.proto;

namespace GEM_NET_LIB
{
    public class DkRecProto // : DkEventDispatch
    {
        public proto_header m_head;
        public Type type; // protobuf解析类型
        public bool isP2P; //响应也分p2p方便分发处理

        //protected int m_hostId;		
        protected object m_rec;
        protected bool isMsgPushQueue = true; //应用层由于本身位于同一线程默认不加队列

        virtual public void regist()
        {
            if (m_head != null)
            {
                CNetWorkGlobal.Instance.registProto(this);
            }
            else
            {
                Debug.LogError("MsgHead is null !");
            }
        }

        virtual public void respond(proto_header head, byte[] block)
        {
            if (type != null)
            {
                ProtobufSerializer serializer = new ProtobufSerializer();
                using (MemoryStream temp = new MemoryStream(block, 0, block.Length, true, true))
                {
                    m_rec = serializer.Deserialize(temp, null, type);
                }

                if (m_rec != null)
                {
                    DkRspMsg item = new DkRspMsg();
                    item.head = head;
                    item.body = m_rec;

                    if (isMsgPushQueue)
                    {
                        DkProtoQueue.Instance.push(item);
                    }
                    else
                    {
                        onRspHandler(item);
                    }
                }
                else
                {
                    Debug.LogError("cmd " + head.shMsgID + " deserialize failed, please check !");
                }
            }
            else
            {
                Debug.LogError("cmd " + head.shMsgID + " deserialize type is null !");
            }
        }

        virtual public void onRspHandler(DkRspMsg item)
        {

        }
    }
}
