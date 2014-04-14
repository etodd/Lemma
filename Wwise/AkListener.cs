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
	public Property<Matrix> Matrix = new Property<Matrix>();

	private Matrix lastMatrix;
	
	public void Update(float dt)
	{
		Matrix m = this.Matrix;
		if (m.Equals(this.lastMatrix))
			return;	// Position didn't change, no need to update.

		// Update position
		Vector3 forward = m.Forward;
		Vector3 up = m.Up;
		Vector3 pos = m.Translation;
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

		this.lastMatrix = m;
	}
}
