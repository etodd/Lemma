//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using ComponentBind;
using System.Collections.Generic;
using System.IO;
using System;
using System.Collections;
using System.Runtime.InteropServices;

public class AkBankLoader
{
	public static string GetNonLocalizedBankPath(string filename)
	{
		return Path.Combine(AkBankPath.GetPlatformBasePath(), filename);
	}

	public static string GetLocalizedBankPath(string filename)
	{
		return Path.Combine(Path.Combine(AkBankPath.GetPlatformBasePath(), AkGlobalSoundEngineInitializer.GetCurrentLanguage()), filename);
	}

	public static AKRESULT LoadBank(string in_bankPath)
	{
		uint BankID;

		AKRESULT result = AkSoundEngine.LoadBank(in_bankPath, AkSoundEngine.AK_DEFAULT_POOL_ID, out BankID);
		
		return result;
	}
}
