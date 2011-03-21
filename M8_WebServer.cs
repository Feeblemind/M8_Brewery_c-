using System;

using Microsoft.SPOT;

using GHIElectronics.NETMF.Net;
using GHIElectronics.NETMF.Net.Sockets;
using GHIElectronics.NETMF.Net.NetworkInformation;
using Socket = GHIElectronics.NETMF.Net.Sockets.Socket;

namespace M8_Brewery
{
    class M8_WebServer
    {
        Socket _server;
        int _port;

        //Links to the TempMgr, PID and SSR used to update the info
        M8_TempMgr _tempMgr;
        M8_PID _pid;
        M8_SSR _ssr;

        public M8_WebServer(Socket server, M8_TempMgr tempMgr, M8_PID pid, M8_SSR ssr)
        {
            this._server = server;

            this._tempMgr = tempMgr;
            this._ssr = ssr;
            this._pid = pid;
        }

        public void update()
        {
            Socket clientSocket;

            // Wait for a client to connect.
            if (this._server.Available > 0)
            {
                clientSocket = this._server.Accept();

                // Process the client request.  true means asynchronous.
                new M8_ClientRequest(clientSocket, true, this._tempMgr, this._pid, this._ssr);
            }
        }

        public void startServer( int port )
        {
            this._port = port;
            
            this._server.Bind(new IPEndPoint(IPAddress.Any, this._port) );
            this._server.Listen(1);
        }
    }
}
