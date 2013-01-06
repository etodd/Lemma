using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;

namespace Lemma
{
	public partial class AnalyticsForm : Form
	{
		private string error;
		public AnalyticsForm(string error)
		{
			InitializeComponent();
			this.error = error;
		}

		private void AnalyticsForm_Load(object sender, EventArgs e)
		{
			if (this.error != null)
				this.label2.Text = "You found a bug!";
			Cursor.Show();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			// TODO: Analytics data upload
		}

		private void button2_Click(object sender, EventArgs e)
		{
			this.Close();
		}
	}
}
