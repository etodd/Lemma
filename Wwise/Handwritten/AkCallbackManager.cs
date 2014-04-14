#if ! (UNITY_DASHBOARD_WIDGET || UNITY_STANDALONE_LINUX || UNITY_WEBPLAYER || UNITY_WII || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY || UNITY_WP8) // Disable under unsupported platforms.
//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;
using Microsoft.Xna.Framework;
using ComponentBind;

#if UNITY_EDITOR
using UnityEditor;
#endif

#pragma warning disable 0618 // Marshal.SizeOf() => Marshal.SizeOf<T>() (.NET 4.5.1)

static public class AkCallbackManager
{
    public delegate void EventCallback(object in_cookie, AkCallbackType in_type, object in_info);
    public delegate void MonitoringCallback(ErrorCode in_errorCode, ErrorLevel in_errorLevel, uint in_playingID, IntPtr in_gameObjID, string in_msg);
    public delegate void BankCallback(uint in_bankID, IntPtr in_InMemoryBankPtr, AKRESULT in_eLoadResult, uint in_memPoolId, object in_Cookie);

    public class EventCallbackPackage
    {
        public EventCallbackPackage(EventCallback in_cb, object in_cookie)
        {
            m_Callback = in_cb;
            m_Cookie = in_cookie;

            m_mapEventCallbacks[GetHashCode()] = this;          
		}
        public object m_Cookie;
        public EventCallback m_Callback;
    };

    public class BankCallbackPackage
    {
        public BankCallbackPackage(BankCallback in_cb, object in_cookie)
        {
            m_Callback = in_cb;
            m_Cookie = in_cookie;

            m_mapBankCallbacks[GetHashCode()] = this;
        }
        public object m_Cookie;
        public BankCallback m_Callback;
    };

#if UNITY_PS4 || UNITY_XBOXONE || UNITY_STANDALONE_WIN || (UNITY_EDITOR_WIN && UNITY_STANDALONE_WIN)
    [StructLayout(LayoutKind.Sequential, Pack=4)]
#else
    [StructLayout(LayoutKind.Sequential)]
#endif
    struct AkCommonCallback
    {
        public IntPtr pPackage;     //The C# CallbackPackage to return to C#
        public IntPtr pNext;        //The next callback
		public AkCallbackType eType;    //The type of structure following
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct AkEventCallbackInfo
    {
        public IntPtr pCookie;      ///< User data, passed to PostEvent()
        public IntPtr gameObjID;    ///< Game object ID
        public uint playingID;      ///< Playing ID of Event, returned by PostEvent()
        public uint eventID;        ///< Unique ID of Event, passed to PostEvent()
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AkDynamicSequenceItemCallbackInfo
    {
        public IntPtr pCookie;      ///< User data, passed to PostEvent()
        public IntPtr gameObjID;    ///< Game object ID
        public uint playingID;      ///< Playing ID of Event, returned by PostEvent()
        public uint audioNodeID;    ///< Audio Node ID of finished item
        public IntPtr pCustomInfo;  ///< Custom info passed to the DynamicSequence::Open function
    };

    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public struct AkMarkerCallbackInfo
    {
        public IntPtr pCookie;      ///< User data, passed to PostEvent()
        public IntPtr gameObjID;    ///< Game object ID
        public uint playingID;      ///< Playing ID of Event, returned by PostEvent()
        public uint eventID;        ///< Unique ID of Event, passed to PostEvent()
        public uint uIdentifier;        ///< Cue point identifier
        public uint uPosition;          ///< Position in the cue point (unit: sample frames)        
        //[MarshalAs(UnmanagedType.LPStr)] TODO, figure out why strings aren't marshaled properly
        public string strLabel;         ///< Label of the marker, read from the file
    };

    [StructLayout(LayoutKind.Sequential)]
    public struct AkDurationCallbackInfo
    {
        public IntPtr pCookie;      ///< User data, passed to PostEvent()
        public IntPtr gameObjID;    ///< Game object ID
        public uint playingID;      ///< Playing ID of Event, returned by PostEvent()
        public uint eventID;        ///< Unique ID of Event, passed to PostEvent()
        public float fDuration;             ///< Duration of the sound (unit: milliseconds )
        public float fEstimatedDuration;    ///< Estimated duration of the sound depending on source settings such as pitch. (unit: milliseconds )
        public uint audioNodeID;            ///< Audio Node ID of playing item
    };

    [StructLayout(LayoutKind.Sequential)]
    public class AkMusicSyncCallbackInfoBase
    {
        public IntPtr pCookie;      ///< User data, passed to PostEvent()
        public IntPtr gameObjID;    ///< Game object ID
        public uint playingID;          ///< Playing ID of Event, returned by PostEvent()
        public AkCallbackType musicSyncType;    ///< Would be either AK_MusicSyncEntry, AK_MusicSyncBeat, AK_MusicSyncBar, AK_MusicSyncExit, AK_MusicSyncGrid, AK_MusicSyncPoint or AK_MusicSyncUserCue.
        public float fBeatDuration;         ///< Beat Duration in seconds.
        public float fBarDuration;          ///< Bar Duration in seconds.
        public float fGridDuration;         ///< Grid duration in seconds.
        public float fGridOffset;           ///< Grid offset in seconds.
    }

    [StructLayout(LayoutKind.Sequential)]
    public class AkMusicSyncCallbackInfo : AkMusicSyncCallbackInfoBase
    {
        public string pszUserCueName;       ///< Cue name
    };

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct AkMonitoringMsg
    {
        public ErrorCode errorCode;
        public ErrorLevel errorLevel;
        public uint playingID;
        public IntPtr gameObjID;
        //[MarshalAs(UnmanagedType.LPWStr)] TODO, figure out why strings aren't marshaled properly
        public string msg;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct AkBankInfo
    {
        public uint bankID;
        public IntPtr inMemoryBankPtr;
        public AKRESULT eLoadResult;
        public uint memPoolId;
    }

    static Dictionary<int, EventCallbackPackage> m_mapEventCallbacks = new Dictionary<int, EventCallbackPackage>();
    static Dictionary<int, BankCallbackPackage> m_mapBankCallbacks = new Dictionary<int, BankCallbackPackage>();
    static IntPtr m_pNotifMem;
    static private MonitoringCallback m_MonitoringCB;

#if UNITY_IOS && ! UNITY_EDITOR
    public delegate AKRESULT AudioInterruptionCallback(int in_bEnterInterruption, AKRESULT in_prevEngineStepResult, object in_Cookie);
    private static AudioInterruptionCallbackPackage ms_interruptCallbackPkg = new AudioInterruptionCallbackPackage(AkGlobalSoundEngineInitializer.AppInterruptCallback, AkGlobalSoundEngineInitializer.ms_interruptCallbackCookie);

    [StructLayout(LayoutKind.Sequential)]
    public struct AkAudioInterruptionInfo
    {
        public Int32 bEnterInterruption;
        public AKRESULT prevEngineStepResult;
    }

    public class AudioInterruptionCallbackPackage
    {
        public AudioInterruptionCallbackPackage(AudioInterruptionCallback in_cb, object in_cookie)
        {
            m_Callback = in_cb;
            m_Cookie = in_cookie;
        }
        public object m_Cookie;
        public AudioInterruptionCallback m_Callback;
    };
    
#endif // #if UNITY_IOS && ! UNITY_EDITOR

    static public AKRESULT Init()
    {
        //Allocate 1k for notifications that will happen during one game frame.
        m_pNotifMem = Marshal.AllocHGlobal(1024);
        return AkCallbackSerializer.Init(m_pNotifMem, 1024);
    }

    static public void Term()
    {
        AkCallbackSerializer.Term();
        Marshal.FreeHGlobal(m_pNotifMem);
        m_pNotifMem = IntPtr.Zero;
    }

    static public void SetMonitoringCallback(ErrorLevel in_Level, MonitoringCallback in_CB)
    {
        AkCallbackSerializer.SetLocalOutput((uint)in_Level);
        m_MonitoringCB = in_CB;
    }

	static public void PostCallbacks()
    {
        if ( ! AkSoundEngine.IsInitialized() )
        {
            return;
        }


        if (m_pNotifMem == IntPtr.Zero)
            return;
        
        IntPtr pData = AkCallbackSerializer.Lock();
        if (pData == IntPtr.Zero)
        {
            AkCallbackSerializer.Unlock();
            return;
        }
        
        AkCommonCallback commonCB;
        commonCB.eType = 0;
        commonCB.pPackage = IntPtr.Zero;
        commonCB.pNext = IntPtr.Zero;

        IntPtr callbacksStart = pData;

        commonCB = new AkCommonCallback();
        
        commonCB.pPackage = Marshal.ReadIntPtr(pData);
        GotoEndOfCurrentStructMember_IntPtr(ref pData);

        commonCB.pNext = Marshal.ReadIntPtr(pData);
        GotoEndOfCurrentStructMember_IntPtr(ref pData);
        
        commonCB.eType = (AkCallbackType)Marshal.ReadInt32(pData);
        GotoEndOfCurrentStructMember_EnumType<AkCallbackType>(ref pData);
        
        EventCallbackPackage eventPkg = null;
        BankCallbackPackage bankPkg = null;

        if ( ! SafeExtractCallbackPackages(commonCB, out eventPkg, out bankPkg) )
        {
            AkCallbackSerializer.Unlock();
            return;
        }

        pData = callbacksStart;

        do
        {
            // Point to start of the next callback after commonCallback.
            pData = (IntPtr)(pData.ToInt64() + Marshal.SizeOf(typeof(AkCommonCallback)));
            
            if (commonCB.eType == AkCallbackType.AK_Monitoring)
            {
                AkMonitoringMsg monitorMsg = new AkMonitoringMsg();

                monitorMsg.errorCode = (ErrorCode)Marshal.ReadInt32(pData);
                GotoEndOfCurrentStructMember_EnumType<ErrorCode>(ref pData);

                monitorMsg.errorLevel = (ErrorLevel)Marshal.ReadInt32(pData);
                GotoEndOfCurrentStructMember_EnumType<ErrorLevel>(ref pData);

                monitorMsg.playingID = (uint)Marshal.ReadInt32(pData);
                GotoEndOfCurrentStructMember_ValueType<uint>(ref pData);

                monitorMsg.gameObjID = (IntPtr)Marshal.ReadIntPtr(pData);
                GotoEndOfCurrentStructMember_IntPtr(ref pData);

                // C# implementation of the struct does not include the tail string member, so as we skip sizes, pData is now at the actual start of the string member.
                monitorMsg.msg = SafeMarshalString(pData);
                if (m_MonitoringCB != null)
                {
                    m_MonitoringCB(monitorMsg.errorCode, monitorMsg.errorLevel, monitorMsg.playingID, monitorMsg.gameObjID, monitorMsg.msg);
                }
                else
                {
#if UNITY_EDITOR
                    string msg = "Wwise: " + monitorMsg.msg;
                    if (monitorMsg.gameObjID != (IntPtr)AkSoundEngine.AK_INVALID_GAME_OBJECT)
                    {
                        GameObject obj = EditorUtility.InstanceIDToObject((int)monitorMsg.gameObjID) as GameObject;                 
                        string name = obj != null ? obj.ToString() : monitorMsg.gameObjID.ToString();
                        msg += "(Object: " + name + ")";
                    }
                                                    
                    if (monitorMsg.errorLevel == ErrorLevel.ErrorLevel_Error)
                        Debug.LogError(msg);
                    else
                        Debug.Log(msg);
#endif
                }
            }
            else if (commonCB.eType == AkCallbackType.AK_Bank)
            {
                AkBankInfo bankCB = new AkBankInfo();
                
                bankCB.bankID = (uint)Marshal.ReadInt32(pData);
                GotoEndOfCurrentStructMember_ValueType<uint>(ref pData);
                
                bankCB.inMemoryBankPtr = Marshal.ReadIntPtr(pData);
                GotoEndOfCurrentStructMember_ValueType<IntPtr>(ref pData);

                bankCB.eLoadResult = (AKRESULT)Marshal.ReadInt32(pData);
                GotoEndOfCurrentStructMember_EnumType<AKRESULT>(ref pData);

                bankCB.memPoolId = (uint)Marshal.ReadInt32(pData);
                GotoEndOfCurrentStructMember_ValueType<uint>(ref pData);

                if ( bankPkg != null )
                {
                    bankPkg.m_Callback(bankCB.bankID, bankCB.inMemoryBankPtr, bankCB.eLoadResult, bankCB.memPoolId, bankPkg.m_Cookie);
                }

            }
#if UNITY_IOS && ! UNITY_EDITOR
            else if (commonCB.eType == AkCallbackType.AK_AudioInterruption)
            {
                AkAudioInterruptionInfo cbInfo = new AkAudioInterruptionInfo();
                
                cbInfo.bEnterInterruption = Marshal.ReadInt32(pData);
                GotoEndOfCurrentStructMember_ValueType<Int32>(ref pData);
                
                cbInfo.prevEngineStepResult = (AKRESULT)Marshal.ReadInt32(pData);
                GotoEndOfCurrentStructMember_EnumType<AKRESULT>(ref pData);

                ms_interruptCallbackPkg.m_Callback(cbInfo.bEnterInterruption, cbInfo.prevEngineStepResult, ms_interruptCallbackPkg.m_Cookie);
            }
#endif // #if UNITY_IOS && ! UNITY_EDITOR
            else
            {
                //Get the other parameters                    
                switch (commonCB.eType)
                {
                    case AkCallbackType.AK_EndOfEvent:
                        AkEventCallbackInfo eventCB = new AkEventCallbackInfo();

                        eventCB.pCookie = Marshal.ReadIntPtr(pData);
                        GotoEndOfCurrentStructMember_IntPtr(ref pData);

                        eventCB.gameObjID = Marshal.ReadIntPtr(pData);
                        GotoEndOfCurrentStructMember_IntPtr(ref pData);

                        eventCB.playingID = (uint)Marshal.ReadInt32(pData);
                        GotoEndOfCurrentStructMember_ValueType<uint>(ref pData);

                        eventCB.eventID = (uint)Marshal.ReadInt32(pData);
                        GotoEndOfCurrentStructMember_ValueType<uint>(ref pData);

                        eventPkg.m_Callback(eventPkg.m_Cookie, commonCB.eType, eventCB);
                        break;

                    case AkCallbackType.AK_EndOfDynamicSequenceItem:
                        AkDynamicSequenceItemCallbackInfo dynSeqInfoCB = new AkDynamicSequenceItemCallbackInfo();

                        dynSeqInfoCB.pCookie = Marshal.ReadIntPtr(pData);
                        GotoEndOfCurrentStructMember_IntPtr(ref pData);

                        dynSeqInfoCB.playingID = (uint)Marshal.ReadInt32(pData);
                        GotoEndOfCurrentStructMember_ValueType<uint>(ref pData);

                        dynSeqInfoCB.audioNodeID = (uint)Marshal.ReadInt32(pData);
                        GotoEndOfCurrentStructMember_ValueType<uint>(ref pData);

                        dynSeqInfoCB.pCustomInfo = Marshal.ReadIntPtr(pData);
                        GotoEndOfCurrentStructMember_IntPtr(ref pData);

                        eventPkg.m_Callback(eventPkg.m_Cookie, commonCB.eType, dynSeqInfoCB);
                        break;

                    case AkCallbackType.AK_Marker:
                        AkMarkerCallbackInfo markerInfo = new AkMarkerCallbackInfo();

                        markerInfo.pCookie = Marshal.ReadIntPtr(pData);
                        GotoEndOfCurrentStructMember_IntPtr(ref pData);

                        markerInfo.gameObjID = Marshal.ReadIntPtr(pData);
                        GotoEndOfCurrentStructMember_IntPtr(ref pData);

                        markerInfo.playingID = (uint)Marshal.ReadInt32(pData);
                        GotoEndOfCurrentStructMember_ValueType<uint>(ref pData);

                        markerInfo.eventID = (uint)Marshal.ReadInt32(pData);
                        GotoEndOfCurrentStructMember_ValueType<uint>(ref pData);

                        markerInfo.uIdentifier = (uint)Marshal.ReadInt32(pData);
                        GotoEndOfCurrentStructMember_ValueType<uint>(ref pData);

                        markerInfo.uPosition = (uint)Marshal.ReadInt32(pData);
                        GotoEndOfCurrentStructMember_ValueType<uint>(ref pData);

                        markerInfo.strLabel = SafeMarshalMarkerString(pData);

                        eventPkg.m_Callback(eventPkg.m_Cookie, commonCB.eType, markerInfo);
                        break;
                        
                    case AkCallbackType.AK_Duration:
                        AkDurationCallbackInfo durInfoCB = new AkDurationCallbackInfo();

                        durInfoCB.pCookie = Marshal.ReadIntPtr(pData);
                        GotoEndOfCurrentStructMember_IntPtr(ref pData);

                        durInfoCB.gameObjID = Marshal.ReadIntPtr(pData);
                        GotoEndOfCurrentStructMember_IntPtr(ref pData);

                        durInfoCB.playingID = (uint)Marshal.ReadInt32(pData);
                        GotoEndOfCurrentStructMember_ValueType<uint>(ref pData);

                        durInfoCB.eventID = (uint)Marshal.ReadInt32(pData);
                        GotoEndOfCurrentStructMember_ValueType<uint>(ref pData);

                        durInfoCB.fDuration = MarshalFloat32(pData);
                        GotoEndOfCurrentStructMember_ValueType<float>(ref pData);

                        durInfoCB.fEstimatedDuration = MarshalFloat32(pData);
                        GotoEndOfCurrentStructMember_ValueType<float>(ref pData);

                        durInfoCB.audioNodeID = (uint)Marshal.ReadInt32(pData);
                        GotoEndOfCurrentStructMember_ValueType<uint>(ref pData);

                        eventPkg.m_Callback(eventPkg.m_Cookie, commonCB.eType, durInfoCB);
                        break;

                    case AkCallbackType.AK_MusicSyncUserCue:
                    case AkCallbackType.AK_MusicPlayStarted:
                    case AkCallbackType.AK_MusicSyncBar:
                    case AkCallbackType.AK_MusicSyncBeat:
                    case AkCallbackType.AK_MusicSyncEntry:
                    case AkCallbackType.AK_MusicSyncExit:
                    case AkCallbackType.AK_MusicSyncGrid:
                    case AkCallbackType.AK_MusicSyncPoint:
                        AkMusicSyncCallbackInfo pInfo = new AkMusicSyncCallbackInfo();
                        
                        pInfo.pCookie = Marshal.ReadIntPtr(pData);
                        GotoEndOfCurrentStructMember_IntPtr(ref pData);

                        pInfo.gameObjID = Marshal.ReadIntPtr(pData);
                        GotoEndOfCurrentStructMember_IntPtr(ref pData);

                        pInfo.playingID = (uint)Marshal.ReadInt32(pData);
                        GotoEndOfCurrentStructMember_ValueType<uint>(ref pData);

                        pInfo.musicSyncType = (AkCallbackType)Marshal.ReadInt32(pData);
                        GotoEndOfCurrentStructMember_EnumType<AkCallbackType>(ref pData);

                        pInfo.fBeatDuration = MarshalFloat32(pData);
                        GotoEndOfCurrentStructMember_ValueType<float>(ref pData);

                        pInfo.fBarDuration = MarshalFloat32(pData);
                        GotoEndOfCurrentStructMember_ValueType<float>(ref pData);

                        pInfo.fGridDuration = MarshalFloat32(pData);
                        GotoEndOfCurrentStructMember_ValueType<float>(ref pData);

                        pInfo.fGridOffset = MarshalFloat32(pData);
                        GotoEndOfCurrentStructMember_ValueType<float>(ref pData);

                        // WG-22334: User cues are always ANSI char*.
                        pInfo.pszUserCueName = Marshal.PtrToStringAnsi(pData);

                        eventPkg.m_Callback(eventPkg.m_Cookie, commonCB.eType, pInfo);
                        break;
                    default:
                        string log = string.Format("WwiseUnity: PostCallbacks aborted due to error: Undefined callback type found. Callback object possiblly corrupted.");
                        Log.d(log);
                        AkCallbackSerializer.Unlock();
                        return;
                };

            }

            if (commonCB.pNext == IntPtr.Zero)
            {
                break;
            }

            // Note: At the end of each callback case above, pData points to either end of the callback struct, or right before the tail string member of the struct. 
            pData = commonCB.pNext;

            callbacksStart = pData;
            
            commonCB = new AkCommonCallback();

            commonCB.pPackage = (IntPtr)Marshal.ReadIntPtr(pData);
            GotoEndOfCurrentStructMember_IntPtr(ref pData);
            
            commonCB.pNext = (IntPtr)Marshal.ReadIntPtr(pData);
            GotoEndOfCurrentStructMember_IntPtr(ref pData);
            
            commonCB.eType = (AkCallbackType)Marshal.ReadInt32(pData);
            GotoEndOfCurrentStructMember_EnumType<AkCallbackType>(ref pData);
                                    
            eventPkg = null;
            bankPkg = null;

            if ( ! SafeExtractCallbackPackages(commonCB, out eventPkg, out bankPkg) )
            {
                AkCallbackSerializer.Unlock();
                return;
            }

            pData = callbacksStart;
            
        } while (true);

        AkCallbackSerializer.Unlock();
    }

    static private bool SafeExtractCallbackPackages(AkCommonCallback commonCB, out EventCallbackPackage eventPkg, out BankCallbackPackage bankPkg)
    {
        eventPkg = null;
        bankPkg = null;
        
        // Callbacks without packages are always valid.
        if ( commonCB.eType == AkCallbackType.AK_AudioInterruption )
        {
            return true;
        }

        bool isValidCallback = m_mapEventCallbacks.TryGetValue((int)commonCB.pPackage, out eventPkg);
        if ( ! isValidCallback )
        {
            isValidCallback = m_mapBankCallbacks.TryGetValue((int)commonCB.pPackage, out bankPkg);
            if ( ! isValidCallback )
            {
                return false;
            }
        }
        
        return true;
    }
    
    static private string SafeMarshalString(IntPtr pData)
    {
#if UNITY_EDITOR
    #if !UNITY_METRO
        if (Path.DirectorySeparatorChar == '/')
            return Marshal.PtrToStringAnsi(pData);
        else 
            return Marshal.PtrToStringUni(pData);
    #else
        return Marshal.PtrToStringUni(pData);
    #endif // #if !UNITY_METRO
#elif UNITY_STANDALONE_WIN || UNITY_METRO
    return Marshal.PtrToStringUni(pData);
#else
    return Marshal.PtrToStringAnsi(pData);
#endif
    }

    // Temporary solution
    static private string SafeMarshalMarkerString(IntPtr pData)
    {
        return Marshal.PtrToStringAnsi(pData);
    }

    static private void GotoEndOfCurrentStructMember_ValueType<T>(ref IntPtr pData)
    {
        pData = (IntPtr)(pData.ToInt64() + Marshal.SizeOf(typeof(T)));
    }

    static private void GotoEndOfCurrentStructMember_IntPtr(ref IntPtr pData)
    {
        pData = (IntPtr)(pData.ToInt64() + IntPtr.Size);
    }

    static private void GotoEndOfCurrentStructMember_EnumType<T>(ref IntPtr pData)
    {
        pData = (IntPtr)(pData.ToInt64() + Marshal.SizeOf(Enum.GetUnderlyingType(typeof(T))));
    }

    // WG-21968
    static byte[] floatMarshalBuffer = new byte[4];
    static private float MarshalFloat32(IntPtr pData)
    {
        floatMarshalBuffer[0] = Marshal.ReadByte(pData, 0);
        floatMarshalBuffer[1] = Marshal.ReadByte(pData, 1);
        floatMarshalBuffer[2] = Marshal.ReadByte(pData, 2);
        floatMarshalBuffer[3] = Marshal.ReadByte(pData, 3);
        float value = System.BitConverter.ToSingle(floatMarshalBuffer, 0);
        return value;
    }
};
#endif // #if ! (UNITY_DASHBOARD_WIDGET || UNITY_STANDALONE_LINUX || UNITY_WEBPLAYER || UNITY_WII || UNITY_NACL || UNITY_FLASH || UNITY_BLACKBERRY || UNITY_WP8) // Disable under unsupported platforms.
