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

	public static void Attach(Entity entity, Property<Matrix> property = null)
	{
		AkGameObjectTracker tracker = entity.Get<AkGameObjectTracker>();
		if (tracker == null)
		{
			tracker = new AkGameObjectTracker();
			entity.Add(tracker);
			if (property == null)
				property = entity.Get<Transform>().Matrix;
			tracker.Add(new Binding<Matrix>(tracker.Matrix, property));
		}
	}

	public static void Attach(Entity entity, Property<Vector3> property)
	{
		AkGameObjectTracker tracker = entity.Get<AkGameObjectTracker>();
		if (tracker == null)
		{
			tracker = new AkGameObjectTracker();
			entity.Add(tracker);
			tracker.Add(new Binding<Matrix, Vector3>(tracker.Matrix, x => Microsoft.Xna.Framework.Matrix.CreateTranslation(x), property));
		}
	}

	public override void Awake()
	{
		base.Awake();
		this.Add(new NotifyBinding(this.Update, this.Matrix));
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