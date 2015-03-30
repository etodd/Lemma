using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Serialization;
using ComponentBind;
using Lemma.Util;
using Microsoft.Xna.Framework;

namespace Lemma.Components
{
	public class Sound : Component<Main>
	{
		public Property<uint> PlayCue = new Property<uint>();
		public Property<uint> StopCue = new Property<uint>();
		public Property<bool> Is3D = new Property<bool>();
		public Property<Vector3> Position = new Property<Vector3>();

		[XmlIgnore]
		public Command Play = new Command();

		public static void AttachTracker(Entity entity, Property<Matrix> property = null)
		{
			AkGameObjectTracker tracker = entity.Get<AkGameObjectTracker>();
			if (tracker == null)
			{
				tracker = new AkGameObjectTracker();
				entity.Add(tracker);
				if (property == null)
					property = entity.Get<Transform>().Matrix;
				tracker.Add(new Binding<Matrix>(tracker.Matrix, property));
				AkAuxSendArray aux = new AkAuxSendArray(Zone.MaxAuxSend);
				tracker.Add(new NotifyBinding(delegate() { tracker.AuxSend(aux, Zone.AuxSend(property.Value.Translation, aux)); }, property));
			}
		}

		public static void AttachTracker(Entity entity, Property<Vector3> property)
		{
			AkGameObjectTracker tracker = entity.Get<AkGameObjectTracker>();
			if (tracker == null)
			{
				tracker = new AkGameObjectTracker();
				entity.Add(tracker);
				tracker.Add(new Binding<Matrix, Vector3>(tracker.Matrix, x => Microsoft.Xna.Framework.Matrix.CreateTranslation(x), property));
				AkAuxSendArray aux = new AkAuxSendArray(Zone.MaxAuxSend);
				tracker.Add(new NotifyBinding(delegate() { tracker.AuxSend(aux, Zone.AuxSend(property, aux)); }, property));
			}
		}

		public static uint RegisterTemp(Vector3 pos)
		{
			AkAuxSendArray array = new AkAuxSendArray(Zone.MaxAuxSend);
			Zone.AuxSend(pos, array);
			return AkSoundEngine.RegisterTemp(pos, array);
		}

		public static void PostEvent(uint e, Vector3 pos)
		{
			AkAuxSendArray array = new AkAuxSendArray(Zone.MaxAuxSend);
			Zone.AuxSend(pos, array);
			AkSoundEngine.PostEvent(e, pos, array);
		}

		public override void Awake()
		{
			base.Awake();

			Sound.AttachTracker(this.Entity, this.Position);

			this.Entity.CannotSuspendByDistance = !this.Is3D;
			this.Entity.Add(new NotifyBinding(delegate()
			{
				this.Entity.CannotSuspendByDistance = !this.Is3D;
			}, this.Is3D));

			this.Play.Action = this.play;
			this.Add(new CommandBinding(this.Disable, (Action)this.stop));
			this.Add(new CommandBinding(this.OnSuspended, (Action)this.stop));
		}

		public void EditorProperties()
		{
			this.Entity.Add("Play", this.Play);
			this.Entity.Add("PlayCue", this.PlayCue, new PropertyEntry.EditorData { Options = WwisePicker.Get(main) });
			this.Entity.Add("StopCue", this.StopCue, new PropertyEntry.EditorData { Options = WwisePicker.Get(main) });
			this.Entity.Add("Is3D", this.Is3D);
		}

		private void play()
		{
			if (this.PlayCue != 0 && this.Enabled && !this.Suspended && !this.main.EditorEnabled)
				AkSoundEngine.PostEvent(this.PlayCue, this.Entity);
		}

		private void stop()
		{
			if (this.StopCue != 0)
				AkSoundEngine.PostEvent(this.StopCue, this.Entity);
		}

		public override void delete()
		{
			base.delete();
			this.stop();
		}
	}
}