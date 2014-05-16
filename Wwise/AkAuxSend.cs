//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using ComponentBind;
using Microsoft.Xna.Framework;

//This component is the conceptual equivalent of the Reverb zone.  However, any effect can be used.  This is defined in the Wwise project.
//It simply demonstrates one way to manage multiple environements. 
//All the real meat is in AkEnvironementAware.cs.  This class could be replaced by a simple tag on a collider.
public class AkAuxSend : Component<BaseMain>
{
	public string auxBusName;
	public float rollOffDistance;
	private uint m_auxBusID;
	
	public uint GetAuxBusID()
	{
		return m_auxBusID;
	}
	
	public virtual float GetAuxSendValueForPosition(Vector3 in_pos)
	{
		return 1.0f;
	}
	
	public override void Awake()
	{
		base.Awake();
		//Cache the ID to avoid repetitive calls to GetIDFromString that will give the same result.
		m_auxBusID = AkSoundEngine.GetIDFromString(auxBusName);
	}
}
