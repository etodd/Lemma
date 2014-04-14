//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using System.Collections.Generic;
using System.IO;
using System;
using ComponentBind;
#pragma warning disable 0219, 0414

// This script deals with initialization, and frame updates of the Wwise audio engine.  
// It must be present on one Game Object at the beginning of the game to initialize the audio properly.
// It must be executed BEFORE any other monoBehaviors that use AkSoundEngine.
// For more information about Wwise initialization and termination see the Wwise SDK doc:
// Wwise SDK | Sound Engine Integration Walkthrough | Initialize the Different Modules of the Sound Engine 
// and also, check AK::SoundEngine::Init & Term.
public class AkGlobalSoundEngineInitializer : Component<BaseMain>
{
	public string basePath = AkBankPath.GetDefaultPath();	//Path for the soundbanks.  This must contain one sub folder per platform (see AkBankPath for the names).
	public string language = "English(US)";
	public int defaultPoolSize = 4096; //4 megs for the metadata pool
	public int lowerPoolSize = 2048; //2 megs for the processing pool
	public int streamingPoolSize = 1024; //1 meg for disk streaming.
	public float memoryCutoffThreshold = 0.95f;   //When reaching 95% of used memory, lowest priority sounds are killed.

	private static AkGlobalSoundEngineInitializer ms_Instance;
	
	public static string GetBasePath()
	{
		return ms_Instance.basePath;
	}

	public static string GetCurrentLanguage()
	{
	   return ms_Instance.language;
	}

	public override void InitializeProperties()
	{
		if (ms_Instance != null)
			return; //Don't init twice
		
#if UNITY_ANDROID && !UNITY_EDITOR
		InitalizeAndroidSoundBankIO();
#endif

		Log.d("WwiseUnity: Initialize sound engine ...");

		//Use default properties for most SoundEngine subsystem.  
		//The game programmer should modify these when needed.  See the Wwise SDK documentation for the initialization.
		//These settings may very well change for each target platform.
		AkMemSettings memSettings = new AkMemSettings();
		memSettings.uMaxNumPools = 20;

		AkDeviceSettings deviceSettings = new AkDeviceSettings();
		AkSoundEngine.GetDefaultDeviceSettings(deviceSettings);

		AkStreamMgrSettings streamingSettings = new AkStreamMgrSettings();
		streamingSettings.uMemorySize = (uint)streamingPoolSize * 1024;

		AkInitSettings initSettings = new AkInitSettings();
		AkSoundEngine.GetDefaultInitSettings(initSettings);
		initSettings.uDefaultPoolSize = (uint)defaultPoolSize * 1024;

		AkPlatformInitSettings platformSettings = new AkPlatformInitSettings();
		AkSoundEngine.GetDefaultPlatformInitSettings(platformSettings);
		platformSettings.uLEngineDefaultPoolSize = (uint)lowerPoolSize * 1024;
		platformSettings.fLEngineDefaultPoolRatioThreshold = memoryCutoffThreshold;

		AkMusicSettings musicSettings = new AkMusicSettings();
		AkSoundEngine.GetDefaultMusicSettings(musicSettings);

		AKRESULT result = AkSoundEngine.Init(memSettings, streamingSettings, deviceSettings, initSettings, platformSettings, musicSettings);
		if (result != AKRESULT.AK_Success)
		{
			Log.d("Wwise: Failed to initialize the sound engine. Abort.");
			return; //AkSoundEngine.Init should have logged more details.
		}

		ms_Instance = this;

		AkBankPath.UsePlatformSpecificPath();
		string platformBasePath = AkBankPath.GetPlatformBasePath(this.main);
// Note: Android low-level IO uses relative path to "assets" folder of the apk as SoundBank folder.
// Unity uses full paths for general path checks. We thus don't use DirectoryInfo.Exists to test 
// our SoundBank folder for Android.
#if !UNITY_ANDROID && !UNITY_METRO && !UNITY_PSP2
		if ( ! AkBankPath.Exists(platformBasePath) )
		{
			string errorMsg = string.Format("Wwise: Failed to find soundbank folder: {0}. Abort.", platformBasePath);
			Log.d(errorMsg);
			ms_Instance = null;
			return;
		}
#endif // #if !UNITY_ANDROID

		AkSoundEngine.SetBasePath(platformBasePath);
		AkSoundEngine.SetCurrentLanguage(language);
		
		result = AkCallbackManager.Init();
		if (result != AKRESULT.AK_Success)
		{
			Log.d("Wwise: Failed to initialize Callback Manager. Terminate sound engine.");
			AkSoundEngine.Term();	
			ms_Instance = null;
			return;
		}
		
		AkCallbackManager.SetMonitoringCallback(ErrorLevel.ErrorLevel_All, null);
		
		//Debug.Log("WwiseUnity: Sound engine initialized.");
		
		//The sound engine should not be destroyed once it is initialized.
		//DontDestroyOnLoad(this);
		
		//Load the init bank right away.  Errors will be logged automatically.
		uint BankID;
#if UNITY_ANDROID && !UNITY_METRO && AK_LOAD_BANK_IN_MEMORY
	result = AkInMemBankLoader.LoadNonLocalizedBank("Init.bnk");
#else
		result = AkSoundEngine.LoadBank("Init.bnk", AkSoundEngine.AK_DEFAULT_POOL_ID, out BankID);
#endif // #if UNITY_ANDROID && !UNITY_METRO && AK_ANDROID_BANK_IN_OBB
		if (result != AKRESULT.AK_Success)
		{
			Log.d("Wwise: Failed load Init.bnk with result: " + result.ToString());
		}
	}
	
	protected override void delete()
	{	
		base.delete();
		ms_Instance = null;
		// Do nothing. AkGlobalSoundEngineTerminator handles sound engine termination.
	}
	
#if UNITY_ANDROID && !UNITY_EDITOR
	private bool InitalizeAndroidSoundBankIO()
	{
		JavaVM.AttachCurrentThread();

		// Find main activity..
		IntPtr cls_Activity = JNI.FindClass("com/unity3d/player/UnityPlayer");
		int fid_Activity = JNI.GetStaticFieldID(cls_Activity, "currentActivity", "Landroid/app/Activity;");
		IntPtr obj_Activity = JNI.GetStaticObjectField(cls_Activity, fid_Activity);
		if ( obj_Activity == IntPtr.Zero )
		{
			Debug.LogError("WwiseUnity: Failed to get UnityPlayer activity. Aborted.");
			return false;
		}
		
		// Create a JavaClass object...
		const string AkPackageClass = "com/audiokinetic/aksoundengine/SoundBankIOInitalizerJavaClass";
		IntPtr cls_JavaClass = JNI.FindClass(AkPackageClass);
		if ( cls_JavaClass == IntPtr.Zero )
		{
			Debug.LogError("WwiseUnity: Failed to find Java class. Check if plugin JAR file is available in Assets/Plugins/Android. Aborted.");
			return false;
		}

		int mid_JavaClass = JNI.GetMethodID(cls_JavaClass, "<init>", "(Landroid/app/Activity;)V");

		IntPtr obj_JavaClass = JNI.NewObject(cls_JavaClass, mid_JavaClass, obj_Activity);


		// Create a global reference to the JavaClass object and fetch method id(s)..
		IntPtr soundBankIOInitalizerJavaClass = JNI.NewGlobalRef(obj_JavaClass);
		
		string methodName = "LoadLibraryByArch";
		int loadLibraryJavaMethodID = JNI.GetMethodID(cls_JavaClass, methodName, "(I)I");
		if ( loadLibraryJavaMethodID == 0 )
		{
			Debug.LogError(string.Format("WwiseUnity: Failed to find Java class method {0}. Check method name and signature in JNI query. Aborted.", methodName));
			return false;
		}
		
// get the Java String object from the JavaClass object
#if AK_ARCH_ANDROID_ARMEABI
		int archID = 1;
#elif AK_ARCH_ANDROID_ARMEABIV7A
		int archID = 0;
#else
		int archID = 0;
#endif
		JNI.CallObjectMethod(soundBankIOInitalizerJavaClass, loadLibraryJavaMethodID, new IntPtr(archID));
		
		methodName = "SetAssetManager";
		int setAssetManagerMethodID = JNI.GetMethodID(cls_JavaClass, methodName, "()I");
		if ( setAssetManagerMethodID == 0 )
		{
			Debug.LogError(string.Format("WwiseUnity: Failed to find Java class method {0}. Check method name and signature in JNI query. Aborted.", methodName));
			return false;
		}		

		// get the Java String object from the JavaClass object
		IntPtr ret = JNI.CallObjectMethod(soundBankIOInitalizerJavaClass, setAssetManagerMethodID);
		if ( ret != IntPtr.Zero )
		{
			Debug.LogError("WwiseUnity: Failed to set AssetManager for Android SoundBank low-level IO handler. Aborted.");
			return false;
		}

		return true;
		
	}

	void OnApplicationFocus(bool focus)
	{
		if (ms_Instance != null)
		{
			if ( focus )
			{
				uint id = AkSoundEngine.PostEvent("Resume_All_Global", null);
			}
			else
			{
				uint id = AkSoundEngine.PostEvent("Pause_All_Global", null);
			}

			AkSoundEngine.RenderAudio();
		}

	}
#endif

#if UNITY_IOS && !UNITY_EDITOR
	public static object ms_interruptCallbackCookie = null;
	private static bool ms_isAudioSessionInterrupted = false;

	// WG-23508: This may not work reliably due to the Unity platform-switch uncertainty during compile time.
	// Add native C++ calls to the generated AppController.mm delegates instead.
	// See documentation Platform-specific Information section for detail.
	void OnApplicationPause(bool pause)
	{
		AkSoundEngine.ListenToAudioSessionInterruption(pause, false);
	}

	public static AKRESULT AppInterruptCallback(int in_iEnterInterruption, AKRESULT in_prevEngineStepResult, object in_Cookie)
	{
		if (in_iEnterInterruption != 0)
		{
			ms_isAudioSessionInterrupted = true;
			Debug.Log("Wwise: iOS audio session is interrupted by another app. Tap device screen to restore app's audio.");
		}
		
		return AKRESULT.AK_Success;
	}

	void OnGUI()
	{
		if ( ms_isAudioSessionInterrupted && Input.touchCount > 0 ) 
		{
			AKRESULT res = AkSoundEngine.ListenToAudioSessionInterruption(false, false);
			if (res == AKRESULT.AK_Success || res == AKRESULT.AK_Cancelled)
			{
				Debug.Log("Wwise: App audio restored or already restored.");
				ms_isAudioSessionInterrupted = false;
			}
		}
	}
#endif // #if UNITY_IOS && !UNITY_EDITOR

}
