﻿/* ******************************************************************
**       This head file is generated by program,                   **
**            Please do not change it directly.                    **
**        author  ouyangjiangping   QQ: 75260062                   **
** 		                                						  **
** *****************************************************************/

//   
 // User Define Macros.   

using System.IO;
using System;
using System.Net;
using System.Collections;
namespace GEM_NET_LIB
{
    namespace proto
    {
		//根据golang的协议定义....
        public class proto_header
        {
            public proto_header()
            {
                shMsgID = 0;
                iMsgBodyLen = 0;

				/*
                iSubCmdID = 0;
                uMsgContext = 0;
                iUin = 0;
                uHostTag = 0;
                uClientSeq = 0;*/
            }

			public int iMsgBodyLen;
            public int shMsgID;
           
			/*
            public int iSubCmdID;
            public uint iUin;
            public UInt32 uMsgContext;
            public UInt32 uHostTag;
            public UInt32 uClientSeq; //add in 20150728,message suqence number for client and server
			*/
            public static readonly int MODULE_HEAD_LENGTH = 4; //default len = 8 bits...
        }
    }
}
