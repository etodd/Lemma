//////////////////////////////////////////////////////////////////////
//
// Copyright (c) 2012 Audiokinetic Inc. / All Rights Reserved
//
//////////////////////////////////////////////////////////////////////

using ComponentBind;
using System.Collections.Generic;

//TODO potential optimization: split this class into 2 objects, one for moving objects and one for static objects.

//This component should be added to any object that needs to have environment effects (e.g. reverb) applied to it.
//This works hand in hand with AkAuxSend-derived classes (e.g. AkBoxAuxSendironment).  
//When the AkAuxSendAware is within an AkAuxSend, an environment percentage value is computed (the amount of wet
//signal the AuxSendironment is contributing) and applied to this object.
public class AkAuxSendAware : Component<BaseMain>, IUpdateableComponent
{			
	private List<AkAuxSend> m_activeAuxSends = new List<AkAuxSend>();
	private AkAuxSendArray m_auxSendValues;
	private Transform transform;

	public Command<Entity> OnEnter = new Command<Entity>();
	public Command<Entity> OnExit = new Command<Entity>();
	
	//When starting, check if any of our parent objects have the AkAuxSend component.
	//We'll assume that this object is then affected by the same AuxSendironment setting.
	public override void InitializeProperties()
	{
		base.InitializeProperties();
		this.Serialize = false;
		this.transform = this.Entity.Get<Transform>();

		//When entering an AuxSendironment, add it to the list of active AuxSendironments
		this.OnEnter.Action = this.AddAuxSend;
	
		//When exiting an AuxSendironment, remove it from active AuxSendironments
		this.OnExit.Action = delegate(Entity other)
		{
			AkAuxSend AuxSend = other.Get<AkAuxSend>();
			if (AuxSend != null)
			{
				m_activeAuxSends.Remove(AuxSend);
				m_auxSendValues = null;
				UpdateAuxSend();			
			}
		};

		this.AddAuxSend(this.Entity);
	}
	
	
	void AddAuxSend(Entity in_AuxSendObject)
	{
		AkAuxSend AuxSend = in_AuxSendObject.Get<AkAuxSend>();
		if (AuxSend != null)
		{
			m_activeAuxSends.Add(AuxSend);
			m_auxSendValues = null;
			UpdateAuxSend();			
		}
	}
	
	public void Update(float dt)
	{
		//For this example, we assume:
		//- The AkAuxSend objects don't move.
		//- The Game Object has a AkGameObjectTracker component.
		//- The Collider position anchor is at the center.
				
		//If we know this object hasn't moved, don't update the AuxSendironment data uselessly.				
		AkGameObjectTracker tracker = this.Entity.Get<AkGameObjectTracker>();
		if (tracker != null && tracker.HasMovedInLastFrame())
		{
			UpdateAuxSend();
		}		
	}
	
	void UpdateAuxSend()
	{
		if (m_auxSendValues == null)
			m_auxSendValues = new AkAuxSendArray((uint)m_activeAuxSends.Count);
		else
			m_auxSendValues.Reset();				
				
		foreach(AkAuxSend AuxSend in m_activeAuxSends)
			m_auxSendValues.Add(AuxSend.GetAuxBusID(), AuxSend.GetAuxSendValueForPosition(this.transform.Position));
		
		AkSoundEngine.SetGameObjectAuxSendValues(this.Entity, m_auxSendValues, (uint)m_activeAuxSends.Count);
	}
}
