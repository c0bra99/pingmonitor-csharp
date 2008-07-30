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
using System.Text;
using System.Net.NetworkInformation;
using System.Net;

namespace PingMonitor
{
    /// <summary>
    /// This class holds information about a ping reply that
    /// is going to be logged.
    /// </summary>
    class PingReplyLogEntry
    {
        public IPAddress IPAddress { get; set; }
        public DateTime DateTime { get; set; }
        public IPStatus IPStatus { get; set; }
        public long RoundtripTime { get; set; }
        public int Ttl { get; set; }
        public bool DontFragment { get; set; }
        public int BufferSize { get; set; }


        /// <summary>
        /// Default constructor
        /// </summary>
        public PingReplyLogEntry()
        { }


        /// <summary>
        /// Constructor taking a DateTime the ping replied, and a
        /// PingReply object with information about the reply
        /// </summary>
        /// 
        /// <param name="dateTime">The time the ping replied</param>
        /// <param name="reply">Information about the reply</param>
        public PingReplyLogEntry(DateTime dateTime, PingReply reply)
        {
            this.IPAddress = reply.Address;
            this.DateTime = dateTime;
            this.IPStatus = reply.Status;

            if (reply.Status == IPStatus.Success)
            {
                this.RoundtripTime = reply.RoundtripTime;
                this.Ttl = reply.Options.Ttl;
                this.DontFragment = reply.Options.DontFragment;
                this.BufferSize = reply.Buffer.Length;
            }
        }


        /// <summary>
        /// Gets a Comma seperated value line with information
        /// about this ping reply
        /// </summary>
        public string GetCSVLine()
        {
            char delimiter = ',';
            StringBuilder sb = new StringBuilder();

            sb.Append(IPAddress.ToString());
            sb.Append(delimiter);
            sb.Append(DateTime.ToString());
            sb.Append(delimiter);
            sb.Append(IPStatus.ToString());
            sb.Append(delimiter);
            sb.Append(RoundtripTime);
            sb.Append(delimiter);
            sb.Append(Ttl);
            sb.Append(delimiter);
            sb.Append(DontFragment);
            sb.Append(delimiter);
            sb.Append(BufferSize);

            return sb.ToString();
        }
    }
}
