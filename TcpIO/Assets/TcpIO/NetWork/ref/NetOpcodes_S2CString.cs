﻿//------------------------------------------------------------------------------
// <auto-generated>
//     此代码由工具生成。
//     运行时版本:4.0.30319.2008
//
//     对此文件的更改可能会导致不正确的行为，并且如果
//     重新生成代码，这些更改将会丢失。
// </auto-generated>
//------------------------------------------------------------------------------
using System;
namespace GEM_NET_LIB
{
    namespace NetClient
    {
        public class NetOpcodes_S2CString
        {
            private static NetOpcodes_S2CString singleton = null;
            public static NetOpcodes_S2CString Instance
            {
                get
                {
                    if (null == singleton)
                        singleton = new NetOpcodes_S2CString();
                    return singleton;
                }
            }
            private NetOpcodes_S2CString()
            {
            }

            public bool GetString(int msgID, out string msgName)
            {
                //to do
                msgName = "Not Defined Name yet";
                return true;
            }
        }
    }
}
