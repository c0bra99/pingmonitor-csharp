/*  Copyright 2008 David Morrison 
 * 
 *  This file is part of PingMonitor.
 *
 *  PingMonitor is free software: you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation, either version 3 of the License, or
 *  (at your option) any later version.
 *  
 *  PingMonitor is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *  
 *  You should have received a copy of the GNU General Public License
 *  along with PingMonitor.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Net.NetworkInformation;
using System.Threading;
using System.IO;

namespace PingMonitor
{
    public partial class frmMain : Form
    {
        private List<PingReplyLogEntry> _pingReplyLog = new List<PingReplyLogEntry>();
        private bool _packetOut = false;

        public frmMain()
        {
            InitializeComponent();
        }

        /// <summary>
        /// This will send an asynchronous ping to the host passed in
        /// </summary>
        /// 
        /// <remarks>
        /// Used and modified sample code from msdn site:
        /// http://msdn.microsoft.com/en-us/library/system.net.networkinformation.ping.aspx
        /// </remarks>
        /// 
        /// <param name="host">The IP or hostname of the host to ping</param>
        public void ping(string host)
        {
            if (host.Length == 0)
            {
                throw new ArgumentException("Ping needs a host or IP Address.");
            }
            _packetOut = true;

            AutoResetEvent waiter = new AutoResetEvent(false);

            Ping pingSender = new Ping();

            //When the PingCompleted event is raised,
            //the PingCompletedCallback method is called.
            pingSender.PingCompleted += new PingCompletedEventHandler(PingCompletedCallback);

            //Create a buffer of 32 bytes of data to be transmitted.
            string data = "abcdefghijklmnopqrstuvwxyzabcdef";
            byte[] buffer = Encoding.ASCII.GetBytes(data);

            //Wait 12 seconds for a reply.
            int timeout = 12000;

            //Set options for transmission:
            //The data can go through 64 gateways or routers
            //before it is destroyed, and the data packet
            //cannot be fragmented.
            PingOptions options = new PingOptions(64, true);

            //Send the ping asynchronously.
            //Use the waiter as the user token.
            //When the callback completes, it can wake up this thread.
            pingSender.SendAsync(host, timeout, buffer, options, waiter);
        }


        private void PingCompletedCallback(object sender, PingCompletedEventArgs e)
        {
            _packetOut = false;

            //If the operation was canceled, display a message to the user.
            if (e.Cancelled)
            {
                //Let the main thread resume. 
                //UserToken is the AutoResetEvent object that the main thread 
                //is waiting for.
                ((AutoResetEvent)e.UserState).Set();
            }

            //If an error occurred, display the exception to the user.
            if (e.Error != null)
            {
                //Let the main thread resume. 
                ((AutoResetEvent)e.UserState).Set();
            }

            PingReply reply = e.Reply;

            DisplayReply(reply);

            //Let the main thread resume.
            ((AutoResetEvent)e.UserState).Set();
        }


        /// <summary>
        /// Delegate for Displaying the reply if an Invoke is required
        /// </summary>
        /// 
        /// <param name="reply">The reply from the ping</param>
        public delegate void DisplayReplyDelegate(PingReply reply);


        /// <summary>
        /// Logs the PingReply and refreshes the grid datasource
        /// </summary>
        /// 
        /// <remarks>
        /// TODO: Make it so it doesnt have to refresh entire grid.
        /// </remarks>
        /// 
        /// <param name="reply">The reply from the ping</param>
        public void DisplayReply(PingReply reply)
        {
            if (reply == null)
            {
                return;
            }

            if (dgvGrid.InvokeRequired)
            {
                this.Invoke(new DisplayReplyDelegate(DisplayReply), new object[] { reply });
            }
            else
            {
                _pingReplyLog.Add(new PingReplyLogEntry(DateTime.Now, reply));

                dgvGrid.DataSource = _pingReplyLog;
                dgvGrid.SuspendLayout();
                dgvGrid.DataSource = null;
                dgvGrid.DataSource = _pingReplyLog;
                dgvGrid.ResumeLayout();
            }
        }


        /// <summary>
        /// Event that fires when the start button is clicked.
        /// Switches button enabling, starts the timer.
        /// </summary>
        private void btnStart_Click(object sender, EventArgs e)
        {
            btnStart.Enabled = false;
            btnStop.Enabled = true;
            tmrPingTimer.Interval = (int)nudInterval.Value;
            tmrPingTimer.Start();
        }


        /// <summary>
        /// Event that fires when the timer ticks. Waits for the
        /// current ping reply to be received, then sends another ping.
        /// </summary>
        private void tmrPingTimer_Tick(object sender, EventArgs e)
        {
            tmrPingTimer.Enabled = false;

            while (_packetOut)
            {
                lblWaiting.Visible = true;
                Application.DoEvents();
                Thread.Sleep(1);
            }
            lblWaiting.Visible = false;
            
            ping(txtAddress.Text);

            tmrPingTimer.Interval = (int)nudInterval.Value;
            tmrPingTimer.Enabled = true;
        }


        /// <summary>
        /// Event that fires when stop button is clicked.
        /// Disables the timer, switches buttons, etc...
        /// </summary>
        private void btnStop_Click(object sender, EventArgs e)
        {
            tmrPingTimer.Enabled = false;
            btnStart.Enabled = true;
            btnStop.Enabled = false;
        }


        /// <summary>
        /// When the export button is clicked, it will export to
        /// a CSV File.
        /// </summary>
        private void btnExport_Click(object sender, EventArgs e)
        {
            SaveFileDialog fileDialog = new SaveFileDialog();
            fileDialog.CheckFileExists = false;
            fileDialog.OverwritePrompt = false;
            fileDialog.DefaultExt = ".csv";
            fileDialog.FileName = "output.csv";
            fileDialog.Filter = "Comma Seperated Value files (*.csv)|*.csv|All files (*.*)|*.*";

            DialogResult fileSelectResult = fileDialog.ShowDialog();
            if (fileSelectResult == DialogResult.OK)
            {
                bool appendFile = true;

                if (File.Exists(fileDialog.FileName))
                {
                    DialogResult appendFileResult = MessageBox.Show("File already exists!\r\nWould you like to append to the existing file instead of overwriting?",
                                                    "File already exists!", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Warning);
                    if (appendFileResult == DialogResult.No)
                    {
                        appendFile = false;
                    }
                    else if (appendFileResult == DialogResult.Cancel)
                    {
                        //if they cancel, just return and get out of here
                        return;
                    }
                }

                using (StreamWriter writer = new StreamWriter(fileDialog.FileName, appendFile))
                {
                    foreach (PingReplyLogEntry entry in _pingReplyLog)
                    {
                        writer.WriteLine(entry.GetCSVLine());
                    }
                }

                MessageBox.Show("Export complete!", "Finished");
            }
        }

    }
}
