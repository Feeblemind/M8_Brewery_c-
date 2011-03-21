using System;
using System.Threading;
using System.Text;

using Microsoft.SPOT;

using GHIElectronics.NETMF.Net;
using GHIElectronics.NETMF.Net.Sockets;
using GHIElectronics.NETMF.Net.NetworkInformation;
using Socket = GHIElectronics.NETMF.Net.Sockets.Socket;

namespace M8_Brewery
{
    class M8_ClientRequest
    {
            private Socket m_clientSocket;
            private M8_SSR m_ssr;
            private M8_PID m_pid;
            private M8_TempMgr m_tempMgr;

            /// <summary>
            /// The constructor calls another method to handle the request, but can 
            /// optionally do so in a new thread.
            /// </summary>
            /// <param name="clientSocket"></param>
            /// <param name="asynchronously"></param>
            public M8_ClientRequest(Socket clientSocket, Boolean asynchronously, M8_TempMgr tempMgr, M8_PID pid, M8_SSR ssr)
            {
                m_clientSocket = clientSocket;
                m_ssr = ssr;
                m_pid = pid;
                m_tempMgr = tempMgr;

                if (asynchronously)
                    // Spawn a new thread to handle the request.
                    new Thread(ProcessRequest).Start();
                else ProcessRequest();
            }

            // <SUMMARY>         
            // The following comes from http://www.fezzer.com/project/97/readwrite-from-sdusd-card/
            // Converts a byte array to a string         
            // </SUMMARY>         
            // <PARAM name="bytes"></PARAM>         
            // <RETURNS></RETURNS>         
            private static string bytesToString(byte[] bytes)
            {
                string s = string.Empty;
                for (int i = 0; i < bytes.Length; ++i)
                {
                    s += (char)bytes[i];
                }
                return s;
            }

            /// <summary>
            /// Processes the request.
            /// </summary>
            private void ProcessRequest()
            {
                const Int32 c_microsecondsPerSecond = 1000000;

                Boolean closeTags = true; //Do we need to append the closing HTML?

                // 'using' ensures that the client's socket gets closed.
                using (m_clientSocket)
                {
                    // Wait for the client request to start to arrive.
                    Byte[] buffer = new Byte[1024];
                    if (m_clientSocket.Poll(5 * c_microsecondsPerSecond,
                                        SelectMode.SelectRead))
                    {
                        // If 0 bytes in buffer, then the connection has been closed, 
                        // reset, or terminated.
                        if (m_clientSocket.Available == 0)
                            return;
                        // Read the first chunk of the request (we don't actually do 
                        // anything with it).
                        Int32 bytesRead = m_clientSocket.Receive(buffer,
                                                m_clientSocket.Available, SocketFlags.None);

                        /* here is where we need to process the request */
                        //Convert the buffer to something more usable
                        String incoming = bytesToString(buffer);
                        //Remove the "\r"s we'll just worry about the "\n"s
                        incoming.Trim('\r');
                        //Slip the request by the "\n"s

                        //Preload the header into the output string
                        String s = "HTTP/1.1 200 OK\r\nContent-Type: text/html\r\ncharset=utf-8\r\n\r\n<html>";

                        /*
                         * These are the data requests
                         */
                        if (incoming.IndexOf("GET /temp") >= 0)
                            lock (m_tempMgr)
                            {
                                s += "<head><title>TEMP</title></head><body><bold>" + m_tempMgr.getTemp().ToString() + "</bold>";
                            }
                        else if (incoming.IndexOf("GET /target") >= 0)
                            lock (m_tempMgr)
                            {
                                s += "<head><title>TARGET</title></head><body><bold>" + m_tempMgr.getTarget().ToString() + "</bold>";
                            }
                        else if (incoming.IndexOf("GET /pid") >= 0)
                            lock (m_pid)
                            {
                                s += "<head><title>PID</title></head><body><bold>" + m_pid.getValue().ToString() + "</bold>";
                            }
                        else if (incoming.IndexOf("GET /dataasxml") >= 0)
                        {
                            M8_DataLog log = new M8_DataLog();

                            log.addTimeStamp();

                            lock (m_tempMgr)
                            {
                                for (int i = 0; i < m_tempMgr.getCount(); i++)
                                    log.addTempData(i, m_tempMgr.getTemp(i), m_tempMgr.getTarget(i));

                                lock (m_pid)
                                    log.addPIDData(m_tempMgr.getPidThermometer(), m_pid.getSSRValue());
                                lock (m_ssr)
                                    log.addSSRData(m_tempMgr.getPidThermometer(), m_ssr.getPower());
                            }

                            s = "HTTP/1.1 200 OK\r\nContent-Type: text/xml\r\ncharset=utf-8\r\n\r\n";
                            s += log.dumpData();
                            Debug.Print(s);
                            closeTags = false; // we're just going to dump the xml out.
                        }
                        /*
                         * These are the form webpages
                         */
                        else if (incoming.IndexOf("GET /control") >= 0)
                        {
                            s += "<head><title>Change PID</title></head><body>Please enter the index of the sensor to use with the PID control<form name=\"input\" action=\"yournewpid\" method=\"get\">Sensor to use: <input type=\"text\" name=\"pid\" /><input type=\"submit\" value=\"Submit\" /></form>";
                        }
                        /*
                         * These are the changes to data
                         */
                        else if (incoming.IndexOf("GET /yournewpid?pid=") >= 0)
                        {
                            //GET /yournewtemp?newTemp=70 HTTP/1.1
                            //try to get the new set temp
                            string tempStr = incoming.Substring(("GET /yournewpid?pid=").Length, incoming.IndexOf(" HTTP/") - ("GET /yournewpid?pid=").Length);
                            int tempInt = Convert.ToInt32(tempStr);

                            //get rid of the preloaded header, to add a refresh to return the user to the default page
                            s = "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nRefresh: 5; url=http://m8.dyndns-server.com/\r\n\r\n<html><head><title>PID</title></head><body>Setting PID to use sensor <bold>" + tempInt.ToString() + "</bold><br>You will be redirected back";

                            lock (m_tempMgr)
                            {
                                m_tempMgr.setPidThermometer(tempInt);
                            }
                        }
                        else if (incoming.IndexOf("GET /yournewtemp?newTemp=") >= 0)
                        {
                            //GET /yournewtemp?newTemp=70 HTTP/1.1
                            //try to get the new set temp
                            string tempStr = incoming.Substring(("GET /yournewtemp?newTemp=").Length, incoming.IndexOf(" HTTP/") - ("GET /yournewtemp?newTemp=").Length);
                            int tempInt = Convert.ToInt32(tempStr);

                            //Add refresh to the header to return the user to the default page
                            s = "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nRefresh: 5; url=http://m8.dyndns-server.com/\r\n\r\n<html><head><title>PID</title></head><body>Setting Target Temp to <bold>" + tempInt.ToString() + "</bold><br>You will be redirected back";

                            lock (m_tempMgr)
                            {
                                m_tempMgr.setTarget(tempInt);
                            }
                        }
                        /*
                         * This is the default page
                         */
                        else
                        {
                            //This adds a refresh to the default page
                            //                        s = "HTTP/1.1 200 OK\r\nContent-Type: text/html; charset=utf-8\r\nRefresh: 5; url=http://m8.dyndns-server.com/\r\n\r\n<html>";
                            s += "<head><title>M8 Brewery</title></head><body><table border=\"1\" align=\"center\"><tr><th>Sensor</th><th>Current Temp C</th><th>Target Temp</th><th>PID Result</th><th>PID Control</th></tr>";

                            lock (m_tempMgr)
                                for (int i = 0; i < m_tempMgr.getCount(); i++)
                                {
                                    s += "<tr><td>" + i.ToString() + "</td>";
                                    s += "<td>" + m_tempMgr.getTemp(i).ToString() + "</td>";
                                    s += "<td>" + m_tempMgr.getTarget(i).ToString() + "</td>";

                                    s += "<td>";
                                    if (m_tempMgr.getPidThermometer() == i)
                                        lock (m_pid)
                                            s += m_pid.getValue().ToString();
                                    s += "</td>";

                                    s += "<td align=\"center\">";
                                    if (m_tempMgr.getPidThermometer() == i)
                                        s += "X";
                                    s += "</td></tr>";
                                }
                            s += "</table><hr><form name=\"input\" action=\"yournewtemp\" method=\"get\">New Target Temp: <input type=\"text\" name=\"newTemp\" /><input type=\"submit\" value=\"Submit\" /></form>";
                        }
                        if (closeTags == true)
                        {
                            //Close the tags
                            s += "<hr>";
                            s += DateTime.Now.ToString();
                            s += "</body></html>";
                        }
                        else
                            Debug.Print(s);

                        //We don't have a "facicon.icp" and my browser asks for one right after the page request
                        if (incoming.IndexOf("GET /favicon.ico HTTP/1.1") < 0)
                        {
                            byte[] buf = Encoding.UTF8.GetBytes(s);
                            int offset = 0;
                            int ret = 0;
                            int len = buf.Length;
                            while (len > 0)
                            {
                                ret = m_clientSocket.Send(buf, offset, len, SocketFlags.None);
                                len -= ret;
                                offset += ret;
                            }
                        }
                        m_clientSocket.Close();
                    }
                }
            }

    }
}
