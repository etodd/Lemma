
using ComponentBind;
using GeeUI;
using GeeUI.Managers;
using GeeUI.Structs;
using GeeUI.ViewLayouts;
using GeeUI.Views;
using Lemma.Components;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma.GeeUI.Composites
{
	public class ToolTip : View
	{
		private NinePatch Patch;
		private TextView TextView;
		private View realParent;

		public Property<string> ToolTipText = new Property<string>();

		private const int ChildrenPadding = 4;

		public override Rectangle BoundBox
		{
			get
			{
				return new Rectangle((int)RealPosition.X, (int)RealPosition.Y, Width + (ChildrenPadding * 2) + Patch.LeftWidth + Patch.RightWidth, Height + (ChildrenPadding * 2) + Patch.TopHeight + Patch.BottomHeight);
			}
		}

		public override Rectangle ContentBoundBox
		{
			get
			{
				return new Rectangle((int)RealPosition.X + Patch.LeftWidth + ChildrenPadding, (int)RealPosition.Y + Patch.TopHeight + ChildrenPadding, Width, Height);
			}
		}



		public ToolTip(GeeUIMain theGeeUI, View parentView, View linkedTo, string text, SpriteFont textFont)
			: base(theGeeUI, parentView)
		{
			this.Patch = GeeUIMain.NinePatchPanelUnselected;

			ToolTipText.Value = text;
			TextView = new TextView(theGeeUI, this, text, Vector2.Zero, textFont);
			this.ChildrenLayouts.Add(new ExpandToFitLayout());
			this.ChildrenLayouts.Add(new VerticalViewLayout(0, false));

			this.realParent = linkedTo;

			if (this.realParent != null)
				this.Add(new NotifyBinding(this.CheckActive, this.realParent.Active, this.realParent.Attached));
			
			this.Add(new NotifyBinding(this.CheckActive, this.Active));

			this.AnchorPoint.Value = new Vector2(0f, 1f);
			this.Position.Value = InputManager.GetMousePosV();

			this.Add(new NotifyBinding(CheckActive, this.Active, realParent.Active));
			this.AnimateIn();
		}

		private void CheckActive()
		{
			if (realParent == null || !realParent.Active || !this.Active)
			{
				if (this.Active)
				this.Active.Value = false;

				if (this.ParentView.Value != null)
					ParentView.Value.Children.Remove(this);
			}
			if (realParent != null)
			{
				if (!realParent.Attached)
				{
					if (this.Active)
						this.Active.Value = false;

					if (this.ParentView.Value != null)
						ParentView.Value.Children.Remove(this);
				}
			}
		}

		private void AnimateIn()
		{
			this.Active.Value = true;
		}

		public override void Draw(SpriteBatch spriteBatch)
		{
			Patch.Draw(spriteBatch, AbsolutePosition, Width, Height, 0f, EffectiveOpacity);
			base.Draw(spriteBatch);
		}
	}
}
