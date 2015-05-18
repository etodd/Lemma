using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft.Xna.Framework.Graphics;

namespace Lemma
{
	public partial class AdapterSelectorForm : Form
	{
		public bool VR;
		public int Monitor;
		public bool Go;
		public AdapterSelectorForm(bool vr)
		{
			InitializeComponent();
			this.VR = vr;
		}

		private class Entry
		{
			public int ID { get; set; }
			public string Display { get; set; }
		}

		private void AdapterSelectorForm_Load(object sender, EventArgs e)
		{
			this.checkBox1.Checked = this.VR;
			this.comboBox1.ValueMember = "ID";
			this.comboBox1.DisplayMember = "Display";
			int selected = 0;
			for (int i = 0; i < GraphicsAdapter.Adapters.Count; i++)
			{
				this.comboBox1.Items.Add(new Entry { ID = i, Display = (i + 1).ToString() });
				if (GraphicsAdapter.Adapters[i] == GraphicsAdapter.DefaultAdapter)
					selected = i;
			}
			this.comboBox1.SelectedIndex = selected;
		}

		private void button1_Click(object sender, EventArgs e)
		{
			this.VR = this.checkBox1.Checked;
			this.Monitor = this.comboBox1.SelectedIndex;
			this.Go = true;
			this.Close();
		}
	}
}
