using System.Xml;

namespace MIG.Interface
{
    using System;
    using System.Collections;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Threading;
    using MIG.Interface.Enums;
    using NLog;

    public class HikEvents
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private CameraState cameraState = CameraState.Start;

        private CameraStatus hikAlarmStatus = CameraStatus.Unknown;

        private Socket sock;

        private string buffer = string.Empty;

        private DateTime lastAlertMsg;

        /// <summary>
        /// The thread loop used to actually communicate with the camera.  It will attempt to
        /// continually reconnect with the camera if the connection fails.
        /// </summary>
        public void HikEventClient(IPAddress ipAddress, int port, string username, string password)
        {
            // Data buffer for incoming data.
            const int bufferSizeMax = 2048;
            var byteBuffer = new byte[bufferSizeMax];

            // Connect to a remote device.
            try
            {
                // set the status to unknown until connected
                this.SetDeviceStatus(CameraStatus.Unknown);

                // Establish the remote endpoint for the socket.
                var remoteEP = new IPEndPoint(ipAddress, port);
                var authenticationStr = Base64Encode(username + ":" + password);

                while (true)
                {
                    try
                    {
                        switch (this.cameraState)
                        {
                            case CameraState.Start:
                            {
                                // Create a TCP/IP  socket.
                                this.buffer = string.Empty;
                                this.sock = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                                // Begin the connection. This will end either with an exception or a callback.
                                this.sock.BeginConnect(remoteEP, new AsyncCallback(this.ConnectCallback), this);
                                this.cameraState = CameraState.Connect;
                                break;
                            }

                            case CameraState.Connect:
                            {
                                // Wait for connection
                                Thread.Sleep(100);
                                break;
                            }

                            case CameraState.Send:
                            {
                                Log.Info("Connected to camera");

                                // Encode the data string into a byte array.
                                var header = "GET /Event/notification/alertStream HTTP/1.1\r\n"
                                                + "Authorization: Basic " + authenticationStr + "\r\n"
                                                + "Connection: keep-alive\r\n"
                                                + "\r\n";

                                // Send the data to start the alert stream
                                var bytesSent = this.sock.Send(Encoding.ASCII.GetBytes(header));
                                this.cameraState = CameraState.Receive;

                                this.lastAlertMsg = DateTime.Now;
                                break;
                            }

                            case CameraState.Receive:
                            {
                                // Wait on the socket for data
                                var readList = new ArrayList {this.sock};
                                Socket.Select(readList, null, null, 5000);

                                if (this.sock.Available > 0)
                                {
                                    // add new data to the buffer
                                    var length = this.sock.Receive(byteBuffer);
                                    this.buffer += System.Text.Encoding.UTF8.GetString(byteBuffer, 0, length);

                                    this.ProcessBuffer();
                                }

                                // Check if too long between messages
                                var span = DateTime.Now - this.lastAlertMsg;

                                if (span.TotalMilliseconds > 2000)
                                {
                                    // Attempt to reconnect
                                    Log.Info(span.TotalMilliseconds + " ms since last message. Attempt to reconnect.");
                                    this.sock.Shutdown(SocketShutdown.Both);
                                    this.sock.Close();
                                    this.cameraState = CameraState.Wait;

                                    // status is unknown
                                    this.SetDeviceStatus(CameraStatus.Unknown);
                                }

                                break;
                            }

                            case CameraState.Wait:
                            {
                                Thread.Sleep(100);
                                this.cameraState = CameraState.Start;
                                break;
                            }
                        }
                    }
                    catch (SocketException se)
                    {
                        Log.Info("Socket exception: " + se);
                        if (this.sock.Connected)
                        {
                            this.sock.Shutdown(SocketShutdown.Both);
                            this.sock.Close();
                        }

                        this.cameraState = CameraState.Start;
                        Thread.Sleep(100);
                    }
                }

            }
            catch (Exception e)
            {
                Log.Info("Unexpected Exception : " + e);
            }

            // reset the status to unknown when exiting
            this.SetDeviceStatus(CameraStatus.Unknown);
        }

        /// <summary>
        /// Encode a string using base64.
        /// </summary>
        /// <param name="plainText">The text to encode.</param>
        /// <returns>The text encoded using base64.</returns>
        private static string Base64Encode(string plainText)
        {
            var plainTextBytes = System.Text.Encoding.UTF8.GetBytes(plainText);
            return System.Convert.ToBase64String(plainTextBytes);
        }

        /// <summary>
        /// Processes the buffer to look for an appropriate xml message.  The message is then
        /// extracted for the type of event ("eventType") and the state("eventState").
        /// Any alarm from the camera will trigger a motion event in HomeSeer.
        /// </summary>
        private void ProcessBuffer()
        {
            const string xmlEndStr = "</EventNotificationAlert>";
            const string xmlStartStr = "<EventNotification";
            const string eventTypeVideoloss = "videoloss";
            const string eventStateInactive = "inactive";

            // check for message boundary
            var index = this.buffer.IndexOf(xmlEndStr);
            if (index >= 0)
            {
                var xmlStart = this.buffer.IndexOf(xmlStartStr);
                var xmlEnd = index + xmlEndStr.Length;
                var xmlLength = index + xmlEndStr.Length - xmlStart;
                if (xmlStart >= 0)
                {
                    try
                    {
                        var xmlStr = this.buffer.Substring(xmlStart, xmlLength);

                        var doc = new XmlDocument();
                        doc.LoadXml(xmlStr);
                        var eventType = doc.GetElementsByTagName("eventType");
                        var eventState = doc.GetElementsByTagName("eventState");

                        if ((eventType[0].InnerXml != eventTypeVideoloss) || (eventState[0].InnerXml != eventStateInactive))
                        {
                            if (this.hikAlarmStatus != CameraStatus.Motion)
                            {
                                // motion detected and status changing
                                this.SetDeviceStatus(CameraStatus.Motion);
                                Log.Info(eventType[0].InnerXml);
                            }
                        }
                        else if (this.hikAlarmStatus != CameraStatus.NoMotion)
                        {
                            // no motion and status changing
                            this.SetDeviceStatus(CameraStatus.NoMotion);
                            Log.Info("Inactive");
                        }

                        this.lastAlertMsg = DateTime.Now;
                    }
                    catch (Exception ex)
                    {
                        this.SetDeviceStatus(CameraStatus.Unknown);
                        Log.Info("Xml processing error: " + ex);
                    }
                }
                // remove data through the boundary
                this.buffer = this.buffer.Substring(index + xmlEndStr.Length);
            }
        }

        /// <summary>
        /// Asynchronous callback for when the camera makes a connection or times out.
        /// </summary>
        /// <param name="ar">Access to the thread.</param>
        private void ConnectCallback(IAsyncResult ar)
        {

            try
            {
                // Finish the connection
                this.sock.EndConnect(ar);
                this.cameraState = CameraState.Send;
            }
            catch (Exception)
            {
                // connection failed
                Log.Info("Connection failed, retrying");
                this.cameraState = CameraState.Wait;
            }
        }


        /// <summary>
        /// Sets the device status.
        /// </summary>
        /// <param name="state">The new state.</param>
        private void SetDeviceStatus(CameraStatus status)
        {
            // plugin.SetDeviceValue(refId, (double)status);
            // todo raise event in hg..
            this.hikAlarmStatus = status;
        }
    }
}
