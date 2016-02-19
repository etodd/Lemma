using System;
using System.Windows.Forms;
using System.Diagnostics;

namespace Lemma
{
	public partial class ErrorForm : Form
	{
		private string error;
		private string anonymousId;

#if ANALYTICS
		public Session.Recorder Session;
		private bool enableUpload;
#endif
		public ErrorForm(string error, string anonymousId, bool upload)
		{
			this.error = error;
			this.anonymousId = anonymousId;
#if ANALYTICS
			this.enableUpload = upload;
#endif
			InitializeComponent();
		}

		private void ErrorForm_Load(object sender, EventArgs e)
		{
			this.textBox1.Text = this.error;
			this.anonymousIdLabel.Text = this.anonymousId;
#if ANALYTICS
			if (this.Session != null && this.Session.Uploading)
				this.timer1.Start();
#endif
			this.updateUploadStatus();
			Cursor.Show();
		}

		private void updateUploadStatus()
		{
#if ANALYTICS
			if (this.Session != null && this.Session.Uploading)
				this.label1.Text = "Uploading crash report... please wait.";
			else if (this.enableUpload && this.Session != null)
				this.label1.Text = "Crash report uploaded. Thank you!";
			else
				this.label1.Text = "Error trace:";
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
