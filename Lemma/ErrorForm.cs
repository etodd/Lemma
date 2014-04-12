using System; using ComponentBind;
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
	public partial class ErrorForm : Form
	{
		private string error;
		public ErrorForm(string error)
		{
			this.error = error;
			InitializeComponent();
		}

		private void ErrorForm_Load(object sender, EventArgs e)
		{
			textBox1.Text = this.error;
			Cursor.Show();
		}

		private void button1_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			ProcessStartInfo sInfo = new ProcessStartInfo("http://et1337.wordpress.com/");
			Process.Start(sInfo);
		}
	}
}
