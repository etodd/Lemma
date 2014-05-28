//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using ComponentBind;
using Microsoft.Xna.Framework;

//Add this script on the game object that represent an audio listener.  It will track its position in Wwise.
//More information about Listeners in the Wwise SDK documentation : 
//Wwise SDK ﾻ Sound Engine Integration Walkthrough ﾻ Integrate Wwise Elements into Your Game ﾻ Integrating Listeners 
public class AkListener : Component<BaseMain>, IUpdateableComponent
{
	public Property<int> ListenerID = new Property<int>();	//Wwise supports up to 8 listeners.  [0-7]
	public Property<Vector3> Position = new Property<Vector3>();
	public Property<Vector3> Forward = new Property<Vector3>();
	public Property<Vector3> Up = new Property<Vector3>();

	private Vector3 lastPosition;
	private Vector3 lastForward;
	private Vector3 lastUp;
	
	public void Update(float dt)
	{
		Vector3 forward = -this.Forward.Value;
		Vector3 up = this.Up;
		Vector3 pos = this.Position;
		if (forward.Equals(this.lastForward) && up.Equals(this.lastUp) && pos.Equals(this.lastPosition))
			return;	// Position didn't change, no need to update.

		// Update position
		AkSoundEngine.SetListenerPosition(    
			forward.X,
			forward.Y, 
			forward.Z,
			up.X,
			up.Y, 
			up.Z,
			pos.X, 
			pos.Y, 
			pos.Z,
#if UNITY_PS3 && !UNITY_EDITOR
			(ulong)this.ListenerID.Value);
#else
			(uint)this.ListenerID.Value);
#endif // #if UNITY_PS3

		this.lastPosition = pos;
		this.lastUp = up;
		this.lastForward = forward;
	}
}
