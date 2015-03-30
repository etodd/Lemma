//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using Microsoft.Xna.Framework;
using ComponentBind;

//Add this script on a Game Object that will emit sounds and that will move during gameplay.
//For more information, see the Wwise SDK doc in the section AK::SoundEngine::SetObjectPosition
public class AkGameObjectTracker : AkGameObject
{    
	private Vector3 pos;
	private Vector3 forward;

	private bool hasMoved;

	public bool HasMovedInLastFrame()
	{
		bool result = this.hasMoved;
		this.hasMoved = false;
		return result;
	}

	public Property<Matrix> Matrix = new Property<Matrix>();

	public override void Awake()
	{
		base.Awake();
		this.Add(new NotifyBinding(this.Update, this.Matrix));
	}

	public void AuxSend(AkAuxSendArray aux, uint count)
	{
		AkSoundEngine.SetGameObjectAuxSendValues(this.Entity, aux, count);
	}

	public override void Update()
	{
		Matrix m = this.Matrix;
		Vector3 pos = m.Translation;
		Vector3 forward = m.Forward;
		if (this.pos == pos && this.forward == forward)
		{
			this.hasMoved = false;
			return;
		}

		this.pos = pos;
		this.forward = forward;    
		this.hasMoved = true;

		// Update position
		AkSoundEngine.SetObjectPosition
		(
			this.Entity, 
			pos.X, 
			pos.Y, 
			pos.Z, 
			forward.X,
			forward.Y, 
			forward.Z
		);
	}
}