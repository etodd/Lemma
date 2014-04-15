//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using ComponentBind;
using System.Collections.Generic;
using System.IO;
#pragma warning disable 0219, 0414

// This script deals with termination of the Wwise audio engine.  
// It must be present on one Game Object that gets destroyed last in the game.
// It must be executed AFTER any other monoBehaviors that use AkSoundEngine.
// For more information about Wwise initialization and termination see the Wwise SDK doc:
// Wwise SDK | Sound Engine Integration Walkthrough | Initialize the Different Modules of the Sound Engine 
// and also, check AK::SoundEngine::Init & Term.
public class AkGlobalSoundEngineTerminator : Component<BaseMain>
{
	static private AkGlobalSoundEngineTerminator ms_Instance = null;

	public override void InitializeProperties()
	{
		if (ms_Instance != null)
			return; //Don't init twice

		ms_Instance = this;
		// Do nothing. AkGlobalSoundEngineTerminator handles sound engine initialization.
	}

	public override void delete()
	{
		base.delete();
		this.Terminate();
	}

	public void Terminate()
	{
		if (ms_Instance == null)
		{
			return; //Don't term twice
		}

		// NOTE: Do not check AkGlobalSoundEngine.IsInitialized()
		//  since its OnDestroy() has been called first in the project exec priority list.
		AkSoundEngine.Term();
		ms_Instance = null;

	}
	 
}
