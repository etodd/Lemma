//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using ComponentBind;
using System.Collections.Generic;
using System.IO;


// This class is used for returning correct path strings for retrieving various soundbank file locations,
// based on Unity usecases. The main concerns include platform sub-folders and path separator conventions.
// The class makes path string retrieval transparent for all platforms in all contexts. Clients of the class
// only needs to use the public methods to get physically correct path strings after setting flag isToUsePosixPathSeparator. By default, the flag is turned off for non-buildPipeline usecases.
//
// Unity usecases:
// - A BuildPipeline user context uses POSIX path convention for all platforms, including Windows and Xbox360.
// - Other usecases use platform-specific path conventions.

public class AkBankPath
{

	private static string defaultBasePath = Path.Combine("Audio", "GeneratedSoundBanks");	//Default value.  Will be overwritten by the user.
	private static bool isToUsePosixPathSeparator = false;
	private static bool isToAppendTrailingPathSeparator = true;
	
	static AkBankPath ()
	{
		isToUsePosixPathSeparator = false;
	}

	public static void UsePosixPath() { isToUsePosixPathSeparator = true; }
	public static void UsePlatformSpecificPath() { isToUsePosixPathSeparator = false; }
	
	public static void SetToAppendTrailingPathSeparator(bool add) { isToAppendTrailingPathSeparator = add; }

#if !UNITY_METRO
	public static bool Exists(string path)
	{
		DirectoryInfo basePathDir = new DirectoryInfo(path);
		return basePathDir.Exists;
	}
#endif // #if !UNITY_METRO

	public static string GetDefaultPath() { return defaultBasePath; }
	
	public static string GetFullBasePath() 
	{
		// Get full path of base path
#if UNITY_ANDROID && ! UNITY_EDITOR
		// Wwise Android SDK now loads SoundBanks from APKs.
	#if AK_LOAD_BANK_IN_MEMORY
		string fullBasePath = Path.Combine(Application.streamingAssetsPath, AkGlobalSoundEngineInitializer.GetBasePath());
	#else
		string fullBasePath = AkGlobalSoundEngineInitializer.GetBasePath();
	#endif // #if AK_LOAD_BANK_IN_MEMORY
		
#elif UNITY_PS3 && ! UNITY_EDITOR
		// NOTE: Work-around for Unity PS3 (up till 3.5.2) bug: Application.streamingAssetsPath points to wrong location: /app_home/PS3_GAME/USRDIR/Raw
		const string StreamingAssetsPath = "/app_home/PS3_GAME/USRDIR/Media/Raw";
		string fullBasePath = Path.Combine(StreamingAssetsPath, AkGlobalSoundEngineInitializer.GetBasePath());
#else
		string fullBasePath = Path.GetFullPath(AkGlobalSoundEngineInitializer.GetBasePath());
#endif
		LazyAppendTrailingSeparator(ref fullBasePath);
		LazyConvertPathConvention(ref fullBasePath);
		return fullBasePath;
	}
	
	public static string GetPlatformBasePath()
	{
		// Combine base path with platform sub-folder
		string platformBasePath = Path.Combine(GetFullBasePath(), GetPlatformSubDirectory());
		
		LazyAppendTrailingSeparator(ref platformBasePath);

		LazyConvertPathConvention(ref platformBasePath);

		return platformBasePath;
	}

	static public string GetPlatformSubDirectory()
	{
		string platformSubDir = "Undefined platform sub-folder";        
		
#if WINDOWS
		platformSubDir = Path.DirectorySeparatorChar == '/' ? "Mac" : "Windows";
#elif MAC
		platformSubDir = Path.DirectorySeparatorChar == '/' ? "Mac" : "Windows";
#endif
		return platformSubDir;
	}

	public static void LazyConvertPathConvention(ref string path)
	{
		if (isToUsePosixPathSeparator)
			ConvertToPosixPath(ref path);
		else
		{
#if !UNITY_METRO
			if (Path.DirectorySeparatorChar == '/')
				ConvertToPosixPath(ref path);
			else
				ConvertToWindowsPath(ref path);
#else
			ConvertToWindowsPath(ref path);
#endif // #if !UNITY_METRO
		}
	} 
	
	public static void ConvertToWindowsPath(ref string path)
	{
		path.Trim();
		path = path.Replace("/", "\\");
		path = path.TrimStart('\\');
	}

	public static void ConvertToWindowsCommandPath(ref string path)
	{
		path.Trim();
		path = path.Replace("/", "\\\\");
		path = path.Replace("\\", "\\\\");
		path = path.TrimStart('\\');
	}    
	
	public static void ConvertToPosixPath(ref string path)
	{
		path.Trim();
		path = path.Replace("\\", "/");
		path = path.TrimStart('\\');
	}
	
	public static void LazyAppendTrailingSeparator(ref string path)
	{
		if ( ! isToAppendTrailingPathSeparator )
			return;
#if !UNITY_METRO
		if ( ! path.EndsWith(Path.DirectorySeparatorChar.ToString()) )
		{
			path += Path.DirectorySeparatorChar;
		}
#else
		if ( ! path.EndsWith("\\") )
		{
			path += "\\";
		}
#endif // #if !UNITY_METRO
	}
}
