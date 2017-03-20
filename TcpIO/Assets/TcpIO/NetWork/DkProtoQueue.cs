using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using ProtoBuf;
using GEM_NET_LIB.proto;

namespace GEM_NET_LIB
{
    public class DkRspMsg
    {
        public proto_header head;
        public object body;
    }

    public class DkProtoQueue
    {
        private Queue<DkRspMsg> msgQueue = new Queue<DkRspMsg>(); //

        private static DkProtoQueue s_instance = null;

        public static DkProtoQueue Instance
        {
            get
            {
                if (s_instance == null)
                {
                    s_instance = new DkProtoQueue();
                }

                return s_instance;
            }
        }

        public int QueueSize()
        {
            //都在同一个线程中执行，不用lock
            //lock (this) 
            {
                return msgQueue.Count;
            }
        }

        public void push(DkRspMsg msg)
        {
            //lock (this)
            {
                msgQueue.Enqueue(msg);
            }
        }

        public DkRspMsg pop()
        {
            //lock (this)
            {
                if (msgQueue.Count > 0)
                {
                    return msgQueue.Dequeue();
                }
                else return null;
            }
        }
    }
}