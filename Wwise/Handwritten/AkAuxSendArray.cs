#if ! (UNITY_DASHBOARD_WIDGET || UNITY_STANDALONE_LINUX || UNITY_WEBPLAYER || UNITY_WII || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY || UNITY_WP8) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Runtime.InteropServices;

public class AkAuxSendArray
{
	public AkAuxSendArray(uint in_Count)
	{
        m_Buffer = Marshal.AllocHGlobal((int)in_Count * (sizeof(uint) + sizeof(float)));
        m_Current = m_Buffer;
        m_MaxCount = in_Count;
        m_Count = 0;
    }

	~AkAuxSendArray()
	{
        Marshal.FreeHGlobal(m_Buffer);
        m_Buffer = IntPtr.Zero;
	}
	
	public void Reset()
	{
		m_Current = m_Buffer;        
        m_Count = 0;
	}
	
    public void Add(uint in_EnvID, float in_fValue)
    {
        if (m_Count >= m_MaxCount)
            throw new IndexOutOfRangeException("Out of range access in AkAuxSendArray");
                          
        Marshal.WriteInt32(m_Current, (int)in_EnvID);
        m_Current = (IntPtr)(m_Current.ToInt64() + sizeof(uint));		
        Marshal.WriteInt32(m_Current, BitConverter.ToInt32(BitConverter.GetBytes(in_fValue), 0));  //Marshal doesn't do floats.  So copy the bytes themselves.  Grrr.
        m_Current = (IntPtr)(m_Current.ToInt64() + sizeof(float));
        m_Count++;
    }

    public IntPtr m_Buffer;    
    private IntPtr m_Current;
    private uint m_MaxCount;
    public uint m_Count;
};
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_STANDALONE_LINUX || UNITY_WEBPLAYER || UNITY_WII || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY || UNITY_WP8) // Disable under unsupported platforms.
