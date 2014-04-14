//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using Microsoft.Xna.Framework;
using ComponentBind;

//Add this script on a Game Object that will emit sounds and that will move during gameplay.
//For more information, see the Wwise SDK doc in the section AK::SoundEngine::SetObjectPosition
public class AkGameObjectTracker : AkGameObject, IUpdateableComponent
{    
	private Vector3 pos;
	private Vector3 forward;

	private bool hasMoved;

	public bool HasMovedInLastFrame()
	{
		return this.hasMoved;
	}

	public Property<Matrix> Matrix = new Property<Matrix>();

	public static void Attach(Entity result, Property<Matrix> property = null)
	{
		AkGameObjectTracker tracker = result.GetOrCreate<AkGameObjectTracker>();
		if (property == null)
			property = result.Get<Transform>().Matrix;
		tracker.Add(new Binding<Matrix>(tracker.Matrix, property));
	}

	public static void Attach(Entity result, Property<Vector3> property)
	{
		AkGameObjectTracker tracker = result.GetOrCreate<AkGameObjectTracker>();
		tracker.Add(new Binding<Matrix, Vector3>(tracker.Matrix, x => Microsoft.Xna.Framework.Matrix.CreateTranslation(x), property));
	}
	
	public void Update(float dt)
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

		//Update position
		AkSoundEngine.SetObjectPosition(
			this.Entity, 
			pos.X, 
			pos.Y, 
			pos.Z, 
			forward.X,
			forward.Y, 
			forward.Z);

		//Update Object-Listener distance RTPC, if needed. (TODO)
	}   
}
