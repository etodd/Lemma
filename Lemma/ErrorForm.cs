using System; using ComponentBind;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Lemma.Components;

namespace Lemma
{
	public partial class ErrorForm : Form
	{
		private string error;
		private string anonymousId;

#if ANALYTICS
		public Session.Recorder Session;
#endif
		public ErrorForm(string error, string anonymousId)
		{
			this.error = error;
			this.anonymousId = anonymousId;
			InitializeComponent();
		}

		private void ErrorForm_Load(object sender, EventArgs e)
		{
			this.textBox1.Text = this.error;
			this.anonymousIdLabel.Text = this.anonymousId;
#if ANALYTICS
			if (this.Session.Uploading)
				this.timer1.Start();
#endif
			this.updateUploadStatus();
			Cursor.Show();
		}

		private void updateUploadStatus()
		{
#if ANALYTICS
			if (this.Session.Uploading)
				this.label1.Text = "Uploading crash report... please wait.";
			else
				this.label1.Text = "Crash report uploaded. Thank you!";
#endif
		}

		private void button1_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			ProcessStartInfo sInfo = new ProcessStartInfo("mailto:support@lemmagame.com");
			Process.Start(sInfo);
		}

		private void timer1_Tick(object sender, EventArgs e)
		{
			this.updateUploadStatus();
		}
	}
}
