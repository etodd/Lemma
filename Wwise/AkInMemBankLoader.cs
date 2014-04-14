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

public class AkInMemBankLoader : ComponentBind.Component<BaseMain>
{
	public Property<string> Name = new Property<string>();
	public Property<bool> IsLocalized = new Property<bool>();
	private GCHandle pinnedArray;
	private IntPtr pointer = IntPtr.Zero;
	public Property<uint> ID = new Property<uint> { Value = AkSoundEngine.AK_INVALID_BANK_ID };

	public override void InitializeProperties()
	{
		string path = this.IsLocalized ? AkInMemBankLoader.GetLocalizedBankPath(this.main, this.Name) : AkInMemBankLoader.GetNonLocalizedBankPath(this.main, this.Name);

		uint id;
		AkInMemBankLoader.DoLoadBank(path, out id, out this.pinnedArray, out this.pointer);
		this.ID.Value = id;
	}

	protected override void delete()
	{
		base.delete();
		if (this.pointer != IntPtr.Zero)
		{
			AKRESULT result = AkSoundEngine.UnloadBank(this.ID, this.pointer);
			if (result == AKRESULT.AK_Success)
				this.pinnedArray.Free();	
		}
	}

	public static string GetNonLocalizedBankPath(BaseMain main, string filename)
	{
		return Path.Combine(AkBankPath.GetPlatformBasePath(main), filename);
	}

	public static string GetLocalizedBankPath(BaseMain main, string filename)
	{
		return Path.Combine(Path.Combine(AkBankPath.GetPlatformBasePath(main), AkGlobalSoundEngineInitializer.GetCurrentLanguage()), filename);
	}

	public static AKRESULT LoadBank(string bankPath)
	{
		GCHandle pinnedArray;
		IntPtr pointer;
		uint id;
		return AkInMemBankLoader.DoLoadBank(bankPath, out id, out pinnedArray, out pointer);
	}

	private static AKRESULT DoLoadBank(string in_bankPath, out uint id, out GCHandle pinnedArray, out IntPtr pointer)
	{
		byte[] data = File.ReadAllBytes(in_bankPath);

		uint bankSize = 0;
		try
		{
			pinnedArray = GCHandle.Alloc(data, GCHandleType.Pinned);
			pointer = pinnedArray.AddrOfPinnedObject();
			bankSize = (uint)data.Length;	
		}
		catch
		{
			id = 0;
			pinnedArray = default(GCHandle);
			pointer = IntPtr.Zero;
			return AKRESULT.AK_Fail;
		}
		
		AKRESULT result = AkSoundEngine.LoadBank(pointer, bankSize, out id);
		
		return result;
	}
}
