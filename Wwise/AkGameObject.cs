//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using Microsoft.Xna.Framework;
using ComponentBind;

// This component is added automatically to all Unity Game Object that are passed to Wwise API (see AkSoundEngine.cs).  
// It manages registration of the game object inside the Wwise Sound Engine
public class AkGameObject : ComponentBind.Component<BaseMain>
{
	public override void Awake()
	{				
		base.Awake();
		this.Serialize = false;
		this.EnabledWhenPaused = false;

		// Register a Game Object in the sound engine, with its name.		
		AkSoundEngine.RegisterGameObj(this.Entity, this.Entity.ToString());
		this.Update();
	}

	public virtual void Update()
	{
		Transform transform = this.Entity.Get<Transform>();
		if (transform != null)
		{
			Vector3 position = transform.Position;
			Vector3 forward = transform.Forward;
				
			// Set the original position
			AkSoundEngine.SetObjectPosition
			(
				this.Entity,
				position.X, 
				position.Y, 
				position.Z, 
				forward.X,
				forward.Y, 
				forward.Z
			);
		}
	}

	public override void delete()
	{
		base.delete();
		if (AkSoundEngine.IsInitialized())
			AkSoundEngine.UnregisterGameObj(this.Entity);
	}
}
