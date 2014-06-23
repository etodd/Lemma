using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Windows.Forms;
using GeeUI.Managers;
using GeeUI.ViewLayouts;
using GeeUI.Views;
using Lemma.GInterfaces;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Reflection;
using System.Collections;
using Microsoft.Xna.Framework.Input;
using System.Xml.Serialization;
using Lemma.Util;
using ComponentBind;
using Keys = Microsoft.Xna.Framework.Input.Keys;
using ListView = GeeUI.Views.ListView;
using View = GeeUI.Views.View;

namespace Lemma.Components
{
	public class EditorGeeUI : Component<Main>
	{
		public struct EditorCommand
		{
			public string Description;
			public PCInput.Chord Chord;
			public Command Action;
			public Func<bool> Enabled;
		}

		private View RootEditorView;

		private TabHost TabViews;

		public PanelView MapPanelView;

		private PanelView EntityPanelView;

		private PanelView VoxelPanelView;

		private DropDownView CreateDropDownView;

		private DropDownView SelectDropDownView;

		private PanelView PropertiesView;

		private PanelView LinkerView;

		private PanelView EntityListView;

		public ListProperty<Entity> SelectedEntities = new ListProperty<Entity>();

		public Property<bool> VoxelEditMode = new Property<bool>();

		public Property<Editor.TransformModes> TransformMode = new Property<Editor.TransformModes>();

		public ListProperty<EditorCommand> EntityCommands = new ListProperty<EditorCommand>();

		public Dictionary<string, PropertyEntry> VoxelProperties = new Dictionary<string, PropertyEntry>();

		public ListProperty<EditorCommand> MapCommands = new ListProperty<EditorCommand>();

		public ListProperty<EditorCommand> VoxelCommands = new ListProperty<EditorCommand>();

		public ListProperty<EditorCommand> AddEntityCommands = new ListProperty<EditorCommand>();

		public Property<bool> NeedsSave = new Property<bool>();

		public Command ShowContextMenu = new Command();

		public Property<bool> PickNextEntity = new Property<bool>();

		public Command<Entity> EntityPicked = new Command<Entity>();

		private DropDownView entityPickerDropDown;
		private View reactivateViewAfterPickingEntity;
		private ButtonView resetSelectButtonAfterPickingEntity;

		private SpriteFont MainFont;

		private TextView selectPrompt;

		public override void Awake()
		{
			base.Awake();
			MainFont = main.Content.Load<SpriteFont>("EditorFont");

			this.RootEditorView = new View(this.main.GeeUI, this.main.GeeUI.RootView);

			this.selectPrompt = new TextView(this.main.GeeUI, this.main.GeeUI.RootView, "Select an entity", Vector2.Zero, this.MainFont);
			this.selectPrompt.Add(new Binding<Vector2, MouseState>(this.selectPrompt.Position, x => new Vector2(x.X + 16, x.Y + 16), this.main.MouseState));
			this.selectPrompt.Add(new Binding<bool>(this.selectPrompt.Active, this.PickNextEntity));

			this.TabViews = new TabHost(this.main.GeeUI, this.RootEditorView, Vector2.Zero, this.MainFont);
			this.MapPanelView = new PanelView(this.main.GeeUI, null, Vector2.Zero);
			this.MapPanelView.ChildrenLayouts.Add(new VerticalViewLayout(4, true, 4));

			this.EntityPanelView = new PanelView(this.main.GeeUI, null, Vector2.Zero);
			this.EntityPanelView.ChildrenLayouts.Add(new VerticalViewLayout(4, true, 4));

			this.VoxelPanelView = new PanelView(this.main.GeeUI, null, Vector2.Zero);
			this.VoxelPanelView.ChildrenLayouts.Add(new VerticalViewLayout(4, true, 4));

			TabViews.AddTab("Map", this.MapPanelView);

			this.CreateDropDownView = new DropDownView(main.GeeUI, MapPanelView, Vector2.Zero, MainFont);
			this.CreateDropDownView.Label.Value = "Add";
			this.CreateDropDownView.FilterThreshhold.Value = 0;
			this.SelectDropDownView = new DropDownView(main.GeeUI, MapPanelView, Vector2.Zero, MainFont);
			this.SelectDropDownView.FilterThreshhold.Value = 0;
			this.SelectDropDownView.Label.Value = "Select";

			this.ShowContextMenu.Action = delegate()
			{
				if (this.TabViews.GetActiveTab() == "Voxel")
				{
					if (this.voxelMaterialDropDown != null && !this.voxelMaterialDropDown.DropDownShowing)
						this.voxelMaterialDropDown.ShowDropDown();
				}
				else if (!this.CreateDropDownView.DropDownShowing)
				{
					this.TabViews.SetActiveTab(this.TabViews.TabIndex("Map"));
					this.CreateDropDownView.ShowDropDown();
				}
			};

			this.PropertiesView = new PanelView(main.GeeUI, main.GeeUI.RootView, new Vector2(5, 5));
			var ListPropertiesView = new ListView(main.GeeUI, PropertiesView) { Name = "PropertiesList" };
			ListPropertiesView.Position.Value = new Vector2(5, 5);
			ListPropertiesView.ChildrenLayouts.Add(new VerticalViewLayout(4, false));
			ListPropertiesView.ChildrenLayouts.Add(new ExpandToFitLayout(false, true) { ExtraWidth = 5 });

			ListPropertiesView.Add(new Binding<int>(ListPropertiesView.Height, PropertiesView.Height));

			PropertiesView.ChildrenLayouts.Add(new ExpandToFitLayout(false, true));

			RootEditorView.Add(new Binding<int, Point>(RootEditorView.Width, point => point.X, main.ScreenSize));
			TabViews.Add(new Binding<int, int>(TabViews.Width, i => i, RootEditorView.Width));

			// Linker view
			{
				LinkerView = new PanelView(main.GeeUI, main.GeeUI.RootView, Vector2.Zero);
				LinkerView.Width.Value = 400;
				LinkerView.Height.Value = 300;
				LinkerView.Draggable = false;
				LinkerView.Active.Value = false;

				var addButton = new ButtonView(main.GeeUI, LinkerView, "Link", Vector2.Zero, MainFont) { Name = "AddButton" };

				var DestLayout = new View(main.GeeUI, LinkerView) { Name = "DestLayout" };
				DestLayout.ChildrenLayouts.Add(new VerticalViewLayout(2, false));
				DestLayout.ChildrenLayouts.Add(new ExpandToFitLayout());
				new TextView(main.GeeUI, DestLayout, "Target:", Vector2.Zero, MainFont);
				var entityDropDownLayout = new View(main.GeeUI, DestLayout);
				entityDropDownLayout.ChildrenLayouts.Add(new HorizontalViewLayout());
				entityDropDownLayout.ChildrenLayouts.Add(new ExpandToFitLayout());
				var destEntityDropDown = new DropDownView(main.GeeUI, entityDropDownLayout, new Vector2(), MainFont)
				{
					Name = "DestEntityDropDown"
				};
				destEntityDropDown.FilterThreshhold.Value = 0;
				destEntityDropDown.AllowRightClickExecute.Value = false;

				var selectButton = new ButtonView(main.GeeUI, entityDropDownLayout, "Select", Vector2.Zero, MainFont);
				selectButton.OnMouseClick += delegate(object sender, EventArgs e)
				{
					this.LinkerView.Active.Value = false;
					this.PickNextEntity.Value = true;
					this.entityPickerDropDown = destEntityDropDown;
					this.reactivateViewAfterPickingEntity = this.LinkerView;
					selectButton.Text = "Select ->";
					this.resetSelectButtonAfterPickingEntity = selectButton;
				};

				//Padding
				new View(main.GeeUI, DestLayout).Height.Value = 5;
				new TextView(main.GeeUI, DestLayout, "Command:", Vector2.Zero, MainFont);
				new DropDownView(main.GeeUI, DestLayout, new Vector2(), MainFont)
				{
					Name = "DestCommandDropDown"
				};

				var container = new PanelView(main.GeeUI, LinkerView, Vector2.Zero);
				var list = new ListView(main.GeeUI, container) { Name = "CurLinksList" };
				list.Position.Value = new Vector2(5);

				list.ChildrenLayouts.Add(new VerticalViewLayout(1, false));
				container.Add(new Binding<int>(container.Width, (i) => i - 10, LinkerView.Width));
				list.Add(new Binding<int>(list.Width, container.Width));

				container.Position.Value = new Vector2(5, 110);

				container.Height.Value = list.Height.Value = (LinkerView.Height.Value - container.Y) - 5;
				DestLayout.Position.Value = new Vector2(10, 10);

				addButton.AnchorPoint.Value = new Vector2(1, 1);
				addButton.Position.Value = new Vector2(LinkerView.Width - 10, container.Y - 10);
				addButton.Active.Value = false;

				HideLinkerView();
			}

			this.EntityPicked.Action = delegate(Entity e)
			{
				if (e != null)
					this.entityPickerDropDown.SetSelectedOption(this.entityString(e));
				this.PickNextEntity.Value = false;
				if (this.reactivateViewAfterPickingEntity != null)
				{
					this.reactivateViewAfterPickingEntity.Active.Value = true;
					this.reactivateViewAfterPickingEntity = null;
				}
				if (this.resetSelectButtonAfterPickingEntity != null)
				{
					this.resetSelectButtonAfterPickingEntity.Text = "Select";
					this.resetSelectButtonAfterPickingEntity = null;
				}
			};

			// Entity list view
			{
				EntityListView = new PanelView(main.GeeUI, main.GeeUI.RootView, Vector2.Zero);
				EntityListView.Width.Value = 400;
				EntityListView.Height.Value = 300;
				EntityListView.Draggable = false;
				EntityListView.Active.Value = false;

				var addButton = new ButtonView(main.GeeUI, EntityListView, "[+]", Vector2.Zero, MainFont) { Name = "AddButton" };

				var entityDropDownLayout = new View(main.GeeUI, EntityListView);
				entityDropDownLayout.ChildrenLayouts.Add(new HorizontalViewLayout());
				entityDropDownLayout.ChildrenLayouts.Add(new ExpandToFitLayout());
				var entityDropDown = new DropDownView(main.GeeUI, entityDropDownLayout, new Vector2(), MainFont)
				{
					Name = "EntityDropDown"
				};
				entityDropDown.FilterThreshhold.Value = 0;
				entityDropDown.AllowRightClickExecute.Value = false;

				var selectButton = new ButtonView(main.GeeUI, entityDropDownLayout, "Select", Vector2.Zero, MainFont);
				selectButton.OnMouseClick += delegate(object sender, EventArgs e)
				{
					this.EntityListView.Active.Value = false;
					this.PickNextEntity.Value = true;
					this.entityPickerDropDown = entityDropDown;
					this.reactivateViewAfterPickingEntity = this.EntityListView;
					selectButton.Text = "Select ->";
					this.resetSelectButtonAfterPickingEntity = selectButton;
				};

				var container = new PanelView(main.GeeUI, EntityListView, Vector2.Zero);
				var list = new ListView(main.GeeUI, container) { Name = "EntityList" };
				list.Position.Value = new Vector2(5);

				list.ChildrenLayouts.Add(new VerticalViewLayout(1, false));
				container.Add(new Binding<int>(container.Width, (i) => i - 10, EntityListView.Width));
				list.Add(new Binding<int>(list.Width, container.Width));

				container.Position.Value = new Vector2(5, 110);

				container.Height.Value = list.Height.Value = (EntityListView.Height.Value - container.Y) - 5;
				entityDropDownLayout.Position.Value = new Vector2(10, 10);

				addButton.AnchorPoint.Value = new Vector2(1, 1);
				addButton.Position.Value = new Vector2(EntityListView.Width - 10, container.Y - 10);

				this.EntityListView.Active.Value = false;
			}

			RootEditorView.Height.Value = 160;
			TabViews.Height.Value = 160;

			PropertiesView.Add(new Binding<Vector2, int>(PropertiesView.Position, i => new Vector2(0, i - 3), TabViews.Height));
			PropertiesView.Add(new Binding<int, Point>(PropertiesView.Height, point => point.Y - PropertiesView.AbsoluteBoundBox.Top - 15, main.ScreenSize));
			PropertiesView.Height.Value = main.ScreenSize.Value.Y - PropertiesView.AbsoluteBoundBox.Top - 15;

			this.SelectedEntities.ItemAdded += (index, item) => this.refresh();
			this.SelectedEntities.ItemRemoved += (index, item) => this.refresh();
			this.SelectedEntities.ItemChanged += (index, old, newValue) => this.refresh();
			this.SelectedEntities.Cleared += this.refresh;

			this.Add(new NotifyBinding(this.RecomputeEntityCommands, this.VoxelEditMode, this.TransformMode));
			this.Add(new NotifyBinding(this.RecomputeVoxelCommands, this.VoxelEditMode, this.TransformMode));

			this.EntityCommands.ItemAdded += (index, command) => RecomputeEntityCommands();
			this.EntityCommands.ItemChanged += (index, old, value) => RecomputeEntityCommands();
			this.EntityCommands.ItemRemoved += (index, command) => RecomputeEntityCommands();

			this.MapCommands.ItemAdded += (index, command) => RecomputeMapCommands();
			this.MapCommands.ItemChanged += (index, old, value) => RecomputeMapCommands();
			this.MapCommands.ItemRemoved += (index, command) => RecomputeMapCommands();

			this.AddEntityCommands.ItemAdded += (index, command) => RecomputeAddCommands();
			this.AddEntityCommands.ItemChanged += (index, old, value) => RecomputeAddCommands();
			this.AddEntityCommands.ItemRemoved += (index, command) => RecomputeAddCommands();

			this.Add(new CommandBinding<Entity>(main.EntityAdded, e => RebuildSelectDropDown()));
			this.Add(new CommandBinding<Entity>(main.EntityRemoved, e => RebuildSelectDropDown()));
		}

		private string entityString(Entity e)
		{
			return e == null ? "[null]" : string.Format("{0} [{1}]", e.ID.Value ?? e.GUID.ToString(), e.Type);
		}

		public void RebuildSelectDropDown()
		{
			this.SelectDropDownView.RemoveAllOptions();
			foreach (var ent in main.Entities)
			{
				if (ent != this.Entity)
				{
					Entity ent1 = ent;
					SelectDropDownView.AddOption(this.entityString(ent), () =>
					{
						Editor editor = Entity.Get<Editor>();
						editor.SelectedEntities.Clear();
						editor.SelectedEntities.Add(ent1);
					}, MainFont, ent);
				}
			}
		}


		public void HideLinkerView()
		{
			LinkerView.Active.Value = false;
		}

		public void ShowLinkerView(ButtonView button, Command.Entry select)
		{
			if (SelectedEntities.Count != 1) return;
			LinkerView.Active.Value = true;
			LinkerView.Position.Value = new Vector2(PropertiesView.AbsoluteBoundBox.Right, InputManager.GetMousePosV().Y);
			LinkerView.ParentView.BringChildToFront(LinkerView);
			BindLinker(button, SelectedEntities[0], select);
			if (LinkerView.AbsoluteBoundBox.Bottom > main.GeeUI.RootView.AbsoluteBoundBox.Bottom)
			{
				LinkerView.Y -= (LinkerView.AbsoluteBoundBox.Bottom - main.GeeUI.RootView.AbsoluteBoundBox.Bottom);
			}
		}

		private View.MouseClickEventHandler currentLinkerViewClickAwayHandler;
		public void BindLinker(ButtonView button, Entity e, Command.Entry selectedCommand)
		{
			var destEntityDrop = ((DropDownView)LinkerView.FindFirstChildByName("DestEntityDropDown"));
			var destCommDrop = ((DropDownView)LinkerView.FindFirstChildByName("DestCommandDropDown"));
			var addButton = ((ButtonView)LinkerView.FindFirstChildByName("AddButton"));
			var listView = ((ListView)LinkerView.FindFirstChildByName("CurLinksList"));

			LinkerView.OnMouseClickAway -= this.currentLinkerViewClickAwayHandler;

			this.currentLinkerViewClickAwayHandler = (sender, args) =>
			{
				if (LinkerView.Active && !LinkerView.AbsoluteBoundBox.Contains(InputManager.GetMousePos()))
				{
					int links = (from l in e.LinkedCommands where l.SourceCommand == selectedCommand.Key select l).Count();
					button.Text = string.Format("Link [{0}]", links);
					HideLinkerView();
				}
			};
			LinkerView.OnMouseClickAway += this.currentLinkerViewClickAwayHandler;

			addButton.ResetOnMouseClick();

			destEntityDrop.RemoveAllOptions();
			destCommDrop.RemoveAllOptions();

			#region Actions
			Action fillDestEntity = () =>
			{
				foreach (var ent in main.Entities)
				{
					if (ent != this.Entity)
						destEntityDrop.AddOption(this.entityString(ent), () => { }, null, ent);
				}
			};

			Action<Entity> fillDestCommand = (ent) =>
			{
				if (ent == null) return;
				foreach (var comm in ent.Commands)
				{
					var c = (Command.Entry)comm.Value;
					if (c.Permissions == Command.Perms.Linkable || c.Permissions == Command.Perms.LinkableAndExecutable)
						destCommDrop.AddOption(comm.Key.ToString(), () => { }, null, c);
				}
			};

			Action<bool, bool> recompute = (destEntityChanged, destCommChanged) =>
			{
				if (destEntityChanged)
				{
					destCommDrop.RemoveAllOptions();
					var selected = destEntityDrop.GetSelectedOption();
					if (selected == null) return;
					fillDestCommand(selected.Related as Entity);
				}

				destEntityDrop.Active.Value = true;
				destCommDrop.Active.Value = destEntityDrop.LastItemSelected.Value != -1 && destEntityDrop.Active;
				addButton.Active.Value = destCommDrop.Active && destCommDrop.LastItemSelected.Value != -1;
			};

			Action populateList = null;
			populateList = () =>
			{
				listView.RemoveAllChildren();
				listView.ContentOffset.Value = Vector2.Zero;
				int count = 0;
				List<Entity.CommandLink> toRemove = new List<Entity.CommandLink>();
				foreach (var link in e.LinkedCommands)
				{
					if (link.SourceCommand != selectedCommand.Key) continue;
					Entity target = link.TargetEntity.Target;
					if (target == null || !target.Active)
					{
						toRemove.Add(link);
						continue;
					}
					View container = new View(main.GeeUI, listView);
					container.ChildrenLayouts.Add(new HorizontalViewLayout(4));
					container.ChildrenLayouts.Add(new ExpandToFitLayout());
					var entView = new TextView(main.GeeUI, container, this.entityString(target), Vector2.Zero, MainFont);
					var destView = new TextView(main.GeeUI, container, link.TargetCommand, Vector2.Zero, MainFont);

					entView.AutoSize.Value = false;
					destView.AutoSize.Value = false;

					entView.Width.Value = 240;
					destView.Width.Value = 100;
					entView.TextJustification = TextJustification.Left;
					destView.TextJustification = TextJustification.Right;

					var removeButton = new ButtonView(main.GeeUI, container, "-", Vector2.Zero, MainFont);
					removeButton.OnMouseClick += (sender, args) =>
					{
						e.LinkedCommands.Remove(link);
						populateList();
						this.NeedsSave.Value = true;
					};

					count++;
				}
				foreach (var remove in toRemove)
					e.LinkedCommands.Remove(remove);
				
				button.Text = string.Format("Link [{0}] ->", count);
			};

			Action addItem = () =>
			{
				if (!addButton.Active) return;
				Entity.CommandLink link = new Entity.CommandLink();
				var entity = ((Entity)destEntityDrop.GetSelectedOption().Related);
				link.TargetEntity = new Entity.Handle() { Target = entity };
				link.TargetCommand = destCommDrop.GetSelectedOption().Text;
				link.SourceCommand = selectedCommand.Key;
				e.LinkedCommands.Add(link);
				populateList();
				addButton.Active.Value = false;
				this.NeedsSave.Value = true;
			};
			#endregion

			destEntityDrop.Add(new NotifyBinding(() => recompute(true, false), destEntityDrop.LastItemSelected));
			destCommDrop.Add(new NotifyBinding(() => recompute(false, true), destCommDrop.LastItemSelected));

			addButton.OnMouseClick += (sender, args) => addItem();

			fillDestEntity();
			populateList();

		}

		public void ShowEntityListView(ButtonView button, ListProperty<Entity.Handle> property)
		{
			if (SelectedEntities.Count != 1) return;
			EntityListView.Active.Value = true;
			EntityListView.Position.Value = new Vector2(PropertiesView.AbsoluteBoundBox.Right, InputManager.GetMousePosV().Y);
			EntityListView.ParentView.BringChildToFront(EntityListView);
			BindEntityListView(button, SelectedEntities[0], property);
			if (EntityListView.AbsoluteBoundBox.Bottom > main.GeeUI.RootView.AbsoluteBoundBox.Bottom)
				EntityListView.Y -= (EntityListView.AbsoluteBoundBox.Bottom - main.GeeUI.RootView.AbsoluteBoundBox.Bottom);
		}

		private View.MouseClickEventHandler currentEntityListViewClickAwayHandler;
		public void BindEntityListView(ButtonView button, Entity e, ListProperty<Entity.Handle> property)
		{
			var entityDrop = ((DropDownView)EntityListView.FindFirstChildByName("EntityDropDown"));
			var addButton = ((ButtonView)EntityListView.FindFirstChildByName("AddButton"));
			var listView = ((ListView)EntityListView.FindFirstChildByName("EntityList"));

			EntityListView.OnMouseClickAway -= this.currentEntityListViewClickAwayHandler;

			this.currentEntityListViewClickAwayHandler = (sender, args) =>
			{
				if (EntityListView.Active && !EntityListView.AbsoluteBoundBox.Contains(InputManager.GetMousePos()))
				{
					button.Text = string.Format("Edit [{0}]", property.Count);
					this.EntityListView.Active.Value = false;
				}
			};
			this.EntityListView.OnMouseClickAway += this.currentEntityListViewClickAwayHandler;

			addButton.ResetOnMouseClick();

			#region Actions
			Action fillDestEntity = () =>
			{
				entityDrop.RemoveAllOptions();
				foreach (var ent in main.Entities)
				{
					if (ent != e && !property.Contains(ent) && ent != this.Entity)
						entityDrop.AddOption(this.entityString(ent), () => { }, null, ent);
				}
				addButton.Active.Value = entityDrop.DropDownOptions.Count > 0;
			};

			Action populateList = null;
			populateList = () =>
			{
				listView.RemoveAllChildren();
				listView.ContentOffset.Value = Vector2.Zero;
				int count = 0;
				List<Entity.Handle> toRemove = new List<Entity.Handle>();
				foreach (var handle in property)
				{
					Entity target = handle.Target;
					if (target == null || !target.Active)
					{
						toRemove.Add(handle);
						continue;
					}
					View container = new View(main.GeeUI, listView);
					container.ChildrenLayouts.Add(new HorizontalViewLayout(4));
					container.ChildrenLayouts.Add(new ExpandToFitLayout());
					var entView = new TextView(main.GeeUI, container, this.entityString(target), Vector2.Zero, MainFont);

					entView.AutoSize.Value = false;

					entView.Width.Value = 340;
					entView.TextJustification = TextJustification.Left;

					var removeButton = new ButtonView(main.GeeUI, container, "[-]", Vector2.Zero, MainFont);
					removeButton.OnMouseClick += (sender, args) =>
					{
						property.Remove(handle);
						populateList();
						fillDestEntity();
						this.NeedsSave.Value = true;
					};

					count++;
				}
				foreach (var remove in toRemove)
					property.Remove(remove);
				
				button.Text = string.Format("Edit [{0}] ->", count);
			};

			Action addItem = () =>
			{
				if (!addButton.Active) return;
				Entity.Handle handle = (Entity)entityDrop.GetSelectedOption().Related;
				property.Add(handle);
				populateList();
				fillDestEntity();
				this.NeedsSave.Value = true;
			};
			#endregion

			addButton.OnMouseClick += (sender, args) => addItem();

			fillDestEntity();
			populateList();
		}

		private DropDownView voxelMaterialDropDown;
		private void RecomputeVoxelCommands()
		{
			VoxelPanelView.RemoveAllChildren();
			this.voxelMaterialDropDown = null;

			if (this.SelectedEntities.Count == 1 && this.SelectedEntities[0].Get<Voxel>() != null)
			{
				foreach (KeyValuePair<string, PropertyEntry> prop in this.VoxelProperties)
				{
					bool sameLine;
					var child = BuildValueView(this.SelectedEntities[0], prop.Value, out sameLine);
					if (prop.Value.Property is Property<Voxel.t>)
						this.voxelMaterialDropDown = (DropDownView)child.FindFirstChildByName("Dropdown");
					View containerLabel = BuildContainerLabel(prop.Key, sameLine);
					containerLabel.AddChild(child);
					if (prop.Value.Visible != null)
						containerLabel.Add(new Binding<bool>(containerLabel.Active, prop.Value.Visible));
					this.VoxelPanelView.AddChild(containerLabel);
					containerLabel.OrderChildren();
					child.OrderChildren();

					containerLabel.SetToolTipText(prop.Value.Description, MainFont);
				}

				foreach (var cmd in this.VoxelCommands)
				{
					if (!cmd.Enabled()) continue;
					string text = cmd.Description;
					if (cmd.Chord.Exists)
						text += " [" + cmd.Chord.ToString() + "]";
					var button = new ButtonView(main.GeeUI, VoxelPanelView, text, Vector2.Zero, MainFont);
					EditorCommand down = cmd;
					button.OnMouseClick += (sender, args) => down.Action.Execute();
				}
				if (this.VoxelEditMode)
					this.TabViews.SetActiveTab(this.TabViews.TabIndex("Voxel"));
			}
		}

		private void RecomputeEntityCommands()
		{
			EntityPanelView.RemoveAllChildren();

			foreach (var dropDown in EntityCommands)
			{
				if (!dropDown.Enabled()) continue;
				string text = dropDown.Description;
				if (dropDown.Chord.Exists)
					text += " [" + dropDown.Chord.ToString() + "]";
				var button = new ButtonView(main.GeeUI, EntityPanelView, text, Vector2.Zero, MainFont);
				EditorCommand down = dropDown;
				button.OnMouseClick += (sender, args) => down.Action.Execute();
			}
		}

		private void RecomputeMapCommands()
		{
			MapPanelView.RemoveAllChildren();
			MapPanelView.AddChild(CreateDropDownView);
			MapPanelView.AddChild(SelectDropDownView);
			foreach (var dropDown in MapCommands)
			{
				if (!dropDown.Enabled()) continue;
				string text = dropDown.Description;
				if (dropDown.Chord.Exists)
					text += " [" + dropDown.Chord.ToString() + "]";
				var button = new ButtonView(main.GeeUI, MapPanelView, text, Vector2.Zero, MainFont);
				EditorCommand down = dropDown;
				button.OnMouseClick += (sender, args) => down.Action.Execute(); 
			}
		}

		private void RecomputeAddCommands()
		{
			this.CreateDropDownView.RemoveAllOptions();
			foreach (var dropDown in AddEntityCommands)
			{
				if (!dropDown.Enabled()) continue;
				string text = dropDown.Description;
				if (dropDown.Chord.Exists)
					text += " [" + dropDown.Chord.ToString() + "]";
				CreateDropDownView.AddOption(text, () =>
				{
					dropDown.Action.Execute();
				});
			}
			CreateDropDownView.Active.Value = CreateDropDownView.DropDownOptions.Count > 0;
		}

		private void show(Entity entity)
		{
			ListView rootEntityView = (ListView)PropertiesView.FindFirstChildByName("PropertiesList");
			rootEntityView.ContentOffset.Value = new Vector2(0);
			rootEntityView.RemoveAllChildren();

			if (entity == null)
				return;

			TextView categoryView = new TextView(main.GeeUI, rootEntityView, "", new Vector2(0, 0), MainFont);
			categoryView.Add(new Binding<string>(categoryView.Text, () => this.entityString(entity), entity.ID));
			categoryView.AutoSize.Value = false;
			categoryView.TextJustification = TextJustification.Center;
			categoryView.Add(new Binding<int>(categoryView.Width, i => { return (int)Math.Max(i, categoryView.TextWidth); }, rootEntityView.Width));

			// ID property
			{
				bool sameLine;
				var child = this.BuildValueView(entity, new PropertyEntry(entity.ID, "ID"), out sameLine);
				TextFieldView textField = (TextFieldView)child.FindFirstChildByName("TextField");
				textField.Validator = delegate(string x)
				{
					Entity e = Entity.GetByID(x);
					return e == null || e == entity;
				};
				View containerLabel = BuildContainerLabel("ID", sameLine);
				containerLabel.AddChild(child);
				rootEntityView.AddChild(containerLabel);
				containerLabel.OrderChildren();
				child.OrderChildren();
			}

			foreach (KeyValuePair<string, PropertyEntry> prop in entity.Properties)
			{
				bool sameLine;
				var child = BuildValueView(entity, prop.Value, out sameLine);
				View containerLabel = BuildContainerLabel(prop.Key, sameLine);
				containerLabel.AddChild(child);
				rootEntityView.AddChild(containerLabel);
				containerLabel.OrderChildren();
				if (prop.Value.Visible != null)
					containerLabel.Add(new Binding<bool>(containerLabel.Active, prop.Value.Visible));
				child.OrderChildren();

				containerLabel.SetToolTipText(prop.Value.Description, MainFont);
			}

			foreach (KeyValuePair<string, Command.Entry> cmd in entity.Commands)
			{
				View containerLabel = BuildContainerLabel(cmd.Key, false);
				containerLabel.AddChild(BuildButton(entity, cmd.Value, "Execute"));
				rootEntityView.AddChild(containerLabel);
				containerLabel.OrderChildren();

				containerLabel.SetToolTipText(cmd.Value.Description, MainFont);
			}

			PropertiesView.OrderChildren();
			rootEntityView.OrderChildren();
		}

		public bool AnyTextFieldViewsSelected()
		{
			var views = main.GeeUI.GetAllViews(main.GeeUI.RootView);
			foreach (var view in views)
			{
				if (view is TextFieldView && view.Selected)
					return true;
			}
			return false;
		}

		public View BuildContainerLabel(string label, bool horizontal)
		{
			var ret = new View(main.GeeUI, null);
			if (horizontal)
				ret.ChildrenLayouts.Add(new HorizontalViewLayout(6, false));
			else
				ret.ChildrenLayouts.Add(new VerticalViewLayout(2, false));
			ret.ChildrenLayouts.Add(new ExpandToFitLayout());

			new TextView(main.GeeUI, ret, label, Vector2.Zero, MainFont);
			return ret;
		}

		public View BuildButton(Entity ent, Command.Entry entry, string label, Color color = default(Color))
		{
			var container = new View(main.GeeUI, null);
			container.ChildrenLayouts.Add(new HorizontalViewLayout());
			container.ChildrenLayouts.Add(new ExpandToFitLayout());

			if (entry.Permissions == Command.Perms.Executable
				|| entry.Permissions == Command.Perms.LinkableAndExecutable)
			{
				var b = new ButtonView(main.GeeUI, container, label, Vector2.Zero, MainFont);
				b.OnMouseClick += (sender, args) =>
				{
					if (entry != null)
						entry.Command.Execute();
				};
			}
			if (entry.Permissions == Command.Perms.Linkable
				|| entry.Permissions == Command.Perms.LinkableAndExecutable)
			{
				int links = (from l in ent.LinkedCommands where l.SourceCommand == entry.Key select l).Count();

				var link = new ButtonView(main.GeeUI, container, string.Format("Link [{0}]", links), Vector2.Zero, MainFont);
				link.OnMouseClick += (sender, args) =>
				{
					ShowLinkerView(link, entry);
				};
			}
			return container;
		}

		private void refresh()
		{
			TabViews.RemoveTab("Entity");
			TabViews.RemoveTab("Voxel");
			HideLinkerView();

			if (this.SelectedEntities.Count == 0)
				this.show(null);
			else
			{
				TabViews.AddTab("Entity", EntityPanelView);
				TabViews.SetActiveTab(TabViews.TabIndex("Entity"));
				if (this.SelectedEntities.Count == 1)
				{
					if (this.SelectedEntities[0].Get<Voxel>() != null)
					{
						this.TabViews.AddTab("Voxel", this.VoxelPanelView);
						this.TabViews.SetActiveTab(TabViews.TabIndex("Voxel"));
					}
					if (this.VoxelEditMode)
						this.show(null);
					else
						this.show(this.SelectedEntities.First());
				}
				else
					this.show(null);
			}

			RecomputeAddCommands();
			RecomputeEntityCommands();
			RecomputeMapCommands();
			RecomputeVoxelCommands();
		}

		public void BuildValueFieldView(View parent, Type type, IProperty property, VectorElement element, int width = 30)
		{
			TextFieldView textField = new TextFieldView(main.GeeUI, parent, Vector2.Zero, MainFont);
			textField.Height.Value = 15;
			textField.Width.Value = width;
			textField.MultiLine = false;

			if (type.Equals(typeof(Vector2)))
			{
				Property<Vector2> socket = (Property<Vector2>)property;
				textField.Text = socket.Value.GetElement(element).ToString("F");
				socket.AddBinding(new NotifyBinding(() =>
				{
					textField.Text = socket.Value.GetElement(element).ToString("F");
				}, socket));

				textField.Width.Value = (int)(textField.Width.Value * 2);

				Action onChanged = () =>
				{
					float value;
					if (float.TryParse(textField.Text, out value))
					{
						socket.Value = socket.Value.SetElement(element, value);
						this.NeedsSave.Value = true;
					}
					textField.Text = socket.Value.GetElement(element).ToString("F");
					textField.Selected.Value = false;
				};
				textField.ValidationRegex = "^-?\\d+(\\.\\d+)?$";
				textField.OnTextSubmitted = onChanged;
			}
			else if (type.Equals(typeof(Vector3)))
			{
				Property<Vector3> socket = (Property<Vector3>)property;
				textField.Text = socket.Value.GetElement(element).ToString("F");
				socket.AddBinding(new NotifyBinding(() =>
				{
					textField.Text = socket.Value.GetElement(element).ToString("F");
				}, socket));

				textField.Width.Value = (int)(textField.Width.Value * 1.25);

				Action onChanged = () =>
				{
					float value;
					if (float.TryParse(textField.Text, out value))
					{
						socket.Value = socket.Value.SetElement(element, value);
						this.NeedsSave.Value = true;
					}
					textField.Text = socket.Value.GetElement(element).ToString("F");
					textField.Selected.Value = false;
				};
				textField.ValidationRegex = "^-?\\d+(\\.\\d+)?$";
				textField.OnTextSubmitted = onChanged;
			}
			else if (type.Equals(typeof(Voxel.Coord)))
			{
				Property<Voxel.Coord> socket = (Property<Voxel.Coord>)property;

				Direction dir;
				switch (element)
				{
					case VectorElement.X:
						dir = Direction.PositiveX;
						break;
					case VectorElement.Y:
						dir = Direction.PositiveY;
						break;
					default:
						dir = Direction.PositiveZ;
						break;
				}

				textField.Width.Value = (int)(textField.Width.Value * 1.25);

				textField.Text = socket.Value.GetComponent(dir).ToString();
				socket.AddBinding(new NotifyBinding(() =>
				{
					textField.Text = socket.Value.GetComponent(dir).ToString();
				}, socket));


				Action onChanged = () =>
				{
					int value;
					if (int.TryParse(textField.Text, out value))
					{
						Voxel.Coord c = socket.Value;
						c.SetComponent(dir, value);
						socket.Value = c;
						this.NeedsSave.Value = true;
					}
					textField.Text = socket.Value.GetComponent(dir).ToString();
					textField.Selected.Value = false;
				};
				textField.ValidationRegex = "^-?\\d+$";
				textField.OnTextSubmitted = onChanged;
			}
			else if (type.Equals(typeof(Vector4)))
			{
				Property<Vector4> socket = (Property<Vector4>)property;
				textField.Text = socket.Value.GetElement(element).ToString("F");
				socket.AddBinding(new NotifyBinding(() =>
				{
					textField.Text = socket.Value.GetElement(element).ToString("F");
				}, socket));

				Action onChanged = () =>
				{
					float value;
					if (float.TryParse(textField.Text, out value))
					{
						socket.Value = socket.Value.SetElement(element, value);
						this.NeedsSave.Value = true;
					}
					textField.Text = socket.Value.GetElement(element).ToString("F");
					textField.Selected.Value = false;
				};
				textField.ValidationRegex = "^-?\\d+(\\.\\d+)?$";
				textField.OnTextSubmitted = onChanged;
			}
			else if (type.Equals(typeof(Quaternion)))
			{
				Property<Quaternion> socket = (Property<Quaternion>)property;
				textField.Text = socket.Value.GetElement(element).ToString("F");
				socket.AddBinding(new NotifyBinding(() =>
				{
					textField.Text = socket.Value.GetElement(element).ToString("F");
				}, socket));

				Action onChanged = () =>
				{
					float value;
					if (float.TryParse(textField.Text, out value))
					{
						socket.Value = socket.Value.SetElement(element, value);
						this.NeedsSave.Value = true;
					}
					textField.Text = socket.Value.GetElement(element).ToString("F");
					textField.Selected.Value = false;
				};
				textField.ValidationRegex = "^-?\\d+(\\.\\d+)?$";
				textField.OnTextSubmitted = onChanged;
			}
			else if (type.Equals(typeof(Color)))
			{
				Property<Color> socket = (Property<Color>)property;
				textField.Text = socket.Value.GetElement(element).ToString();
				socket.AddBinding(new NotifyBinding(() =>
				{
					textField.Text = socket.Value.GetElement(element).ToString();
				}, socket));

				textField.Width.Value = (int)(textField.Width.Value * 1.25);

				Action onChanged = () =>
				{
					byte value;
					if (byte.TryParse(textField.Text, out value))
					{
						socket.Value = socket.Value.SetElement(element, value);
						this.NeedsSave.Value = true;
					}
					textField.Text = socket.Value.GetElement(element).ToString();
					textField.Selected.Value = false;
				};
				textField.ValidationRegex = "^?\\d+$";
				textField.OnTextSubmitted = onChanged;
			}
		}

		public View BuildValueView(Entity entity, PropertyEntry entry, out bool shouldSameLine)
		{
			IProperty property = entry.Property;
			shouldSameLine = false;
			View ret = new View(main.GeeUI, null);
			ret.ChildrenLayouts.Add(new HorizontalViewLayout(4));
			ret.ChildrenLayouts.Add(new ExpandToFitLayout());

			if (entry.Readonly)
			{
				PropertyInfo propertyInfo = property.GetType().GetProperty("Value");
				TextView label = new TextView(main.GeeUI, ret, propertyInfo.GetValue(property, null).ToString(), Vector2.Zero, MainFont);
				label.Add(new Binding<string>(label.Text, () => propertyInfo.GetValue(property, null).ToString(), property));
			}
			else if (typeof(IListProperty).IsAssignableFrom(property.GetType()))
			{
				if (property.GetType().GetGenericArguments().First().Equals(typeof(Entity.Handle)))
				{
					ListProperty<Entity.Handle> socket = (ListProperty<Entity.Handle>)property;
					var edit = new ButtonView(main.GeeUI, ret, string.Format("Edit [{0}]", socket.Count), Vector2.Zero, MainFont);
					edit.OnMouseClick += (sender, args) =>
					{
						ShowEntityListView(edit, socket);
					};
				}
			}
			else
			{
				PropertyInfo propertyInfo = property.GetType().GetProperty("Value");
				if (propertyInfo.PropertyType.Equals(typeof(Vector2)))
				{
					foreach (VectorElement field in new[] { VectorElement.X, VectorElement.Y })
						this.BuildValueFieldView(ret, propertyInfo.PropertyType, property, field);
				}
				else if (propertyInfo.PropertyType.Equals(typeof(Vector3)) || propertyInfo.PropertyType.Equals(typeof(Voxel.Coord)))
				{
					foreach (VectorElement field in new[] { VectorElement.X, VectorElement.Y, VectorElement.Z })
						this.BuildValueFieldView(ret, propertyInfo.PropertyType, property, field);
				}
				else if (propertyInfo.PropertyType.Equals(typeof(Vector4)) || propertyInfo.PropertyType.Equals(typeof(Quaternion)) ||
						 propertyInfo.PropertyType.Equals(typeof(Color)))
				{
					foreach (VectorElement field in new[] { VectorElement.X, VectorElement.Y, VectorElement.Z, VectorElement.W })
						this.BuildValueFieldView(ret, propertyInfo.PropertyType, property, field);
				}
				else if (typeof(Enum).IsAssignableFrom(propertyInfo.PropertyType))
				{
					var drop = new DropDownView(main.GeeUI, ret, Vector2.Zero, MainFont) { Name = "Dropdown" };
					drop.AllowRightClickExecute.Value = false;
					foreach (object o in Enum.GetValues(propertyInfo.PropertyType))
					{
						Action onClick = () =>
						{
							propertyInfo.SetValue(property, o, null);
							this.NeedsSave.Value = true;
						};
						drop.AddOption(o.ToString(), onClick);
					}
					drop.SetSelectedOption(propertyInfo.GetValue(property, null).ToString(), false);
					drop.Add(new NotifyBinding(() =>
					{
						drop.SetSelectedOption(propertyInfo.GetValue(property, null).ToString(), false);
					}, (IProperty)property));
				}
				else if (propertyInfo.PropertyType.Equals(typeof(Entity.Handle)))
				{
					Property<Entity.Handle> socket = (Property<Entity.Handle>)property;
					var entityDropDownLayout = new View(main.GeeUI, ret);
					entityDropDownLayout.ChildrenLayouts.Add(new HorizontalViewLayout());
					entityDropDownLayout.ChildrenLayouts.Add(new ExpandToFitLayout());
					var entityDropDown = new DropDownView(main.GeeUI, entityDropDownLayout, new Vector2(), MainFont)
					{
						Name = "DropDown"
					};
					entityDropDown.AllowRightClickExecute.Value = false;
					entityDropDown.FilterThreshhold.Value = 0;
					entityDropDown.AddOption("[null]", delegate()
					{
						socket.Value = null;
						this.NeedsSave.Value = true;
					});
					foreach (Entity e in main.Entities)
					{
						if (e != entity && e != this.Entity)
						{
							entityDropDown.AddOption(this.entityString(e), delegate()
							{
								socket.Value = e;
								this.NeedsSave.Value = true;
							});
						}
					}
					entityDropDown.SetSelectedOption(this.entityString(socket.Value.Target), false);
					entityDropDown.Add(new NotifyBinding(() =>
					{
						entityDropDown.SetSelectedOption(this.entityString(socket.Value.Target), false);
					}, (IProperty)property));

					var selectButton = new ButtonView(main.GeeUI, entityDropDownLayout, "Select", Vector2.Zero, MainFont);
					selectButton.OnMouseClick += delegate(object sender, EventArgs e)
					{
						this.PickNextEntity.Value = true;
						this.entityPickerDropDown = entityDropDown;
						this.reactivateViewAfterPickingEntity = null;
						selectButton.Text = "Select ->";
						this.resetSelectButtonAfterPickingEntity = selectButton;
					};
				}
				else if (entry.Options != null && propertyInfo.PropertyType.Equals(typeof(string)))
				{
					ListProperty<string> options = (ListProperty<string>)entry.Options;
					var drop = new DropDownView(main.GeeUI, ret, Vector2.Zero, MainFont) { Name = "Dropdown" };
					drop.AllowRightClickExecute.Value = false;
					Action populate = delegate()
					{
						drop.RemoveAllOptions();
						foreach (string o in options)
						{
							Action onClick = () =>
							{
								propertyInfo.SetValue(property, o, null);
								this.NeedsSave.Value = true;
							};
							drop.AddOption(o, onClick);
						}
						drop.SetSelectedOption((string)propertyInfo.GetValue(property, null), false);
						if (drop.DropDownOptions.Count > 0)
							propertyInfo.SetValue(property, drop.GetSelectedOption().Text, null);
						else
							propertyInfo.SetValue(property, null, null);
					};
					populate();
					drop.Add(new ListNotifyBinding<string>(populate, options));
				}
				else
				{
					TextFieldView view = new TextFieldView(main.GeeUI, ret, Vector2.Zero, MainFont);
					view.Width.Value = 130;
					view.Height.Value = 15;
					view.Name = "TextField";
					view.MultiLine = false;

					if (propertyInfo.PropertyType.Equals(typeof(int)))
					{
						Property<int> socket = (Property<int>)property;
						view.Text = socket.Value.ToString();
						socket.AddBinding(new NotifyBinding(() =>
						{
							view.Text = socket.Value.ToString();
						}, socket));
						Action onChanged = () =>
						{
							int value;
							if (int.TryParse(view.Text, out value))
							{
								socket.Value = value;
								this.NeedsSave.Value = true;
							}
							view.Text = socket.Value.ToString();
							view.Selected.Value = false;
						};
						view.ValidationRegex = "^-?\\d+$";
						view.OnTextSubmitted = onChanged;
					}
					else if (propertyInfo.PropertyType.Equals(typeof(float)))
					{
						Property<float> socket = (Property<float>)property;
						view.Text = socket.Value.ToString("F");
						socket.AddBinding(new NotifyBinding(() =>
						{
							view.Text = socket.Value.ToString();
						}, socket));
						Action onChanged = () =>
						{
							float value;
							if (float.TryParse(view.Text, out value))
							{
								socket.Value = value;
								this.NeedsSave.Value = true;
							}
							view.Text = socket.Value.ToString("F");
							view.Selected.Value = false;
						};
						view.ValidationRegex = "^-?\\d+(\\.\\d+)?$";
						view.OnTextSubmitted = onChanged;
					}
					else if (propertyInfo.PropertyType.Equals(typeof(bool)))
					{
						shouldSameLine = true;
						//No need for a textfield!
						ret.RemoveChild(view);
						CheckBoxView checkBox = new CheckBoxView(main.GeeUI, ret, Vector2.Zero, "", MainFont);
						Property<bool> socket = (Property<bool>)property;
						checkBox.IsChecked.Value = socket.Value;
						checkBox.Add(new NotifyBinding(() =>
						{
							this.NeedsSave.Value = true;
							socket.Value = checkBox.IsChecked.Value;
						}, checkBox.IsChecked));
					}
					else if (propertyInfo.PropertyType.Equals(typeof(string)))
					{
						Property<string> socket = (Property<string>)property;

						if (socket.Value == null) view.Text = "";
						else view.Text = socket.Value;

						socket.AddBinding(new NotifyBinding(() =>
						{
							var text = socket.Value;
							if (text == null) text = "";
							view.Text = text;
						}, socket));

						//Vast majority of strings won't be multiline.
						if (socket.Value != null)
							view.MultiLine = socket.Value.Contains("\n");
						if (view.MultiLine)
							view.Height.Value = 100;
						Action onChanged = () =>
						{
							if (socket.Value != view.Text)
							{
								socket.Value = view.Text;
								this.NeedsSave.Value = true;
							}
							view.Selected.Value = false;
						};
						view.OnTextSubmitted = onChanged;
					}
				}
			}
			return ret;
		}

		public override void delete()
		{
			base.delete();
			this.main.GeeUI.RootView.RemoveChild(this.RootEditorView);
			this.main.GeeUI.RootView.RemoveChild(this.selectPrompt);
			this.main.GeeUI.RootView.RemoveChild(this.PropertiesView);
		}
	}
}
