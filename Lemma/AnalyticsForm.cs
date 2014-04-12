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
	public partial class AnalyticsForm : Form
	{
		private string error;
		private GameMain main;
		private bool success;
		private bool cancelled;
		public AnalyticsForm(GameMain main, string error)
		{
			this.InitializeComponent();
			this.main = main;
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
			this.button1.Enabled = false;
			this.button1.Text = "Uploading...";
			this.label2.Text = "Uploading (0%)";
			this.label1.Visible = false;
			this.label4.Visible = false;
			this.label5.Visible = false;
			this.label7.Visible = false;
			this.button2.Visible = true;
			this.backgroundWorker1.RunWorkerAsync();
		}

		private void button2_Click(object sender, EventArgs e)
		{
			if (this.backgroundWorker1.IsBusy)
			{
				this.cancelled = true;
				this.backgroundWorker1.CancelAsync();
				this.button2.Enabled = false;
				this.button2.Text = "Cancelling...";
			}
			else
				this.Close();
		}

		private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
		{
			try
			{
#if ANALYTICS // Just to prevent compile errors
				BackgroundWorker worker = sender as BackgroundWorker;
				string[] sessionFiles = this.main.AnalyticsSessionFiles;
				int i = 0;
				foreach (string file in sessionFiles)
				{
					if (worker.CancellationPending == true)
					{
						e.Cancel = true;
						break;
					}
					else
					{

						Session.Recorder.UploadSession(file);
						i++;
						worker.ReportProgress((int)(((float)i / (float)sessionFiles.Length) * 100.0f));
					}
				}
#endif
				this.success = true;
			}
			catch (Exception)
			{

			}
		}

		private void backgroundWorker1_ProgressChanged(object sender, ProgressChangedEventArgs e)
		{
			this.label2.Text = "Uploading (" + e.ProgressPercentage.ToString() + "%)";
		}

		private void backgroundWorker1_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
		{
			this.label2.Text = this.cancelled ? "Upload cancelled." : (this.success ? "Upload complete. Thanks!" : "Error, please try again later.");
			this.button1.Text = "Done";
			this.button2.Enabled = true;
			this.button2.Text = "Close";
		}
	}
}
