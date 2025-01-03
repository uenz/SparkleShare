//   SparkleShare, a collaboration and sharing tool.
//   Copyright (C) 2010  Hylke Bons <hi@planetpeanut.uk>
//
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU Lesser General Public License as 
//   published by the Free Software Foundation, either version 3 of the 
//   License, or (at your option) any later version.
//
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
//   GNU General Public License for more details.
//
//   You should have received a copy of the GNU General Public License
//   along with this program. If not, see <http://www.gnu.org/licenses/>.


using System;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Sparkles
{

    public class TcpListener : BaseListener
    {

        private Socket socket = null!;
        private Thread thread = null!;
        private bool is_connected = false;
        private bool is_connecting = false;
        private DateTime last_ping = DateTime.Now;


        public TcpListener(Uri server, string folder_identifier) : base(server, folder_identifier)
        {
        }


        public override bool IsConnected
        {
            get
            {
                return this.is_connected;
            }
        }


        public override bool IsConnecting
        {
            get
            {
                return this.is_connecting;
            }
        }


        // Starts a new thread and listens to the channel
        public override void Connect()
        {
            this.is_connecting = true;

            this.thread = new Thread(() =>
            {
                int port = Server.Port;

                if (port < 0)
                    port = 443;

                try
                {
                    this.socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    {
                        ReceiveTimeout = 5 * 1000,
                        SendTimeout = 5 * 1000
                    };

                    // Try to connect to the server
                    this.socket.Connect(Server.Host, port);

                    this.is_connecting = false;
                    this.is_connected = true;

                    OnConnected();

                }
                catch (Exception e)
                {
                    this.is_connected = false;
                    this.is_connecting = false;

                    if (this.socket != null)
                        this.socket.Close();

                    OnDisconnected(Sparkles.DisconnectReason.TimeOut, e.Message);
                    return;
                }


                byte[] bytes = new byte[4096];
                int bytes_read = 0;
                this.last_ping = DateTime.Now;

                // Wait for messages
                while (this.is_connected)
                {
                    try
                    {
                        int i = 0;
                        int timeout = 300;
                        DisconnectReason reason = DisconnectReason.TimeOut;

                        // This blocks the thread
                        while (this.socket.Available < 1)
                        {
                            try
                            {
                                // We've timed out, let's ping the server to
                                // see if the connection is still up
                                if (i == timeout)
                                {
                                    Logger.LogInfo("ListenerTcp", "Pinging " + Server);

                                    byte[] ping_bytes = Encoding.UTF8.GetBytes("ping\n");
                                    byte[] pong_bytes = new byte[4096];

                                    this.socket.Send(ping_bytes);

                                    if (this.socket.Receive(pong_bytes) < 1)
                                        // 10057 means "Socket is not connected"
                                        throw new SocketException(10057);

                                    Logger.LogInfo("ListenerTcp", "Received pong from " + Server);

                                    i = 0;
                                    this.last_ping = DateTime.Now;

                                }
                                else
                                {

                                    // Check when the last ping occured. If it's
                                    // significantly longer than our regular interval the
                                    // system likely woke up from sleep and we want to
                                    // simulate a disconnect
                                    int sleepiness = DateTime.Compare(
                                        this.last_ping.AddMilliseconds(timeout * 1000 * 1.2),
                                        DateTime.Now
                                    );

                                    if (sleepiness <= 0)
                                    {
                                        Logger.LogInfo("ListenerTcp", "System woke up from sleep");
                                        reason = DisconnectReason.SystemSleep;

                                        // 10057 means "Socket is not connected"
                                        throw new SocketException(10057);
                                    }
                                }

                                // The ping failed: disconnect completely
                            }
                            catch (SocketException e)
                            {
                                Disconnect(reason, "Ping timeout: " + e.Message);
                                return;
                            }

                            Thread.Sleep(1000);
                            i++;
                        }

                    }
                    catch (Exception)
                    {
                        return;
                    }

                    try
                    {
                        if (this.socket.Available > 0)
                            bytes_read = this.socket.Receive(bytes);

                        // Parse the received message
                        if (bytes_read > 0)
                        {
                            string received = Encoding.UTF8.GetString(bytes);
                            string line = received.Substring(0, received.IndexOf("\n"));

                            if (!line.Contains("!"))
                                continue;

                            string folder_identifier = line.Substring(0, line.IndexOf("!"));
                            string message = CleanMessage(line.Substring(line.IndexOf("!") + 1));

                            // We have a message!
                            if (!folder_identifier.Equals("debug") && !string.IsNullOrEmpty(message))
                                OnAnnouncement(new Announcement(folder_identifier, message));
                        }

                    }
                    catch (SocketException e)
                    {
                        Disconnect(DisconnectReason.TimeOut, "Timeout during receiving: " + e.Message);
                        return;
                    }
                }
            });

            this.thread.Start();
        }


        private void Disconnect(DisconnectReason reason, string message)
        {
            this.is_connected = false;
            this.is_connecting = false;

            if (this.socket != null)
            {
                this.socket.Close();
                //this.socket = null;
            }

            OnDisconnected(reason, message);
        }


        protected override void AlsoListenToInternal(string folder_identifier)
        {
            string to_send = "subscribe " + folder_identifier + "\n";

            try
            {
                this.socket.Send(Encoding.UTF8.GetBytes(to_send));
                this.last_ping = DateTime.Now;

            }
            catch (Exception e)
            {
                this.is_connected = false;
                this.is_connecting = false;

                OnDisconnected(DisconnectReason.TimeOut, e.Message);
            }
        }


        protected override void AnnounceInternal(Announcement announcement)
        {
            string to_send = "announce " + announcement.FolderIdentifier + " " + announcement.Message + "\n";

            try
            {
                if (this.socket != null)
                    this.socket.Send(Encoding.UTF8.GetBytes(to_send));

                this.last_ping = DateTime.Now;

            }
            catch (Exception e)
            {
                this.is_connected = false;
                this.is_connecting = false;

                OnDisconnected(DisconnectReason.TimeOut, e.Message);
            }
        }


        public override void Dispose()
        {
            if (this.socket != null)
            {
                this.socket.Close();
                //this.socket = null;
            }


            this.thread.Interrupt();
            this.thread.Join();

            base.Dispose();
        }


        private string CleanMessage(string message)
        {
            message = message.Replace("\n", "");
            message = message.Replace("\0", "");
            return message.Trim();
        }
    }
}
