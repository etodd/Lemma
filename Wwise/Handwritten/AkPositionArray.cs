#if ! (UNITY_DASHBOARD_WIDGET || UNITY_STANDALONE_LINUX || UNITY_WEBPLAYER || UNITY_WII || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY || UNITY_WP8) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System;
using System.Collections;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

public class AkPositionArray : IDisposable
{
    public AkPositionArray(uint in_Count)
    {
        m_Buffer = Marshal.AllocHGlobal((int)in_Count * sizeof(float) * 6);
        m_Current = m_Buffer;
        m_MaxCount = in_Count;
        m_Count = 0;
    }

    ~AkPositionArray()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (m_Buffer != IntPtr.Zero)
        {
            Marshal.FreeHGlobal(m_Buffer);
            m_Buffer = IntPtr.Zero;
            m_MaxCount = 0;
        }
    }

    public void Reset()
    {
        m_Current = m_Buffer;
        m_Count = 0;
    }

    public void Add(Vector3 in_Pos, Vector3 in_Forward)
    {
        if (m_Count >= m_MaxCount)
            throw new IndexOutOfRangeException("Out of range access in AkPositionArray");

        //Marshal doesn't do floats.  So copy the bytes themselves.  Grrr.
        Marshal.WriteInt32(m_Current, BitConverter.ToInt32(BitConverter.GetBytes(in_Pos.X), 0));  
        m_Current = (IntPtr)(m_Current.ToInt64() + sizeof(float));
        Marshal.WriteInt32(m_Current, BitConverter.ToInt32(BitConverter.GetBytes(in_Pos.Y), 0));
        m_Current = (IntPtr)(m_Current.ToInt64() + sizeof(float));
        Marshal.WriteInt32(m_Current, BitConverter.ToInt32(BitConverter.GetBytes(in_Pos.Z), 0));
        m_Current = (IntPtr)(m_Current.ToInt64() + sizeof(float));
        Marshal.WriteInt32(m_Current, BitConverter.ToInt32(BitConverter.GetBytes(in_Forward.X), 0));
        m_Current = (IntPtr)(m_Current.ToInt64() + sizeof(float));
        Marshal.WriteInt32(m_Current, BitConverter.ToInt32(BitConverter.GetBytes(in_Forward.Y), 0));
        m_Current = (IntPtr)(m_Current.ToInt64() + sizeof(float));
        Marshal.WriteInt32(m_Current, BitConverter.ToInt32(BitConverter.GetBytes(in_Forward.Z), 0));
        m_Current = (IntPtr)(m_Current.ToInt64() + sizeof(float));

        m_Count++;
    }

    public IntPtr m_Buffer;
    private IntPtr m_Current;
    private uint m_MaxCount;
    private uint m_Count;
};
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_STANDALONE_LINUX || UNITY_WEBPLAYER || UNITY_WII || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY || UNITY_WP8) // Disable under unsupported platforms.
