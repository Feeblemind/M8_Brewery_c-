using System;
using System.Threading;
using System.IO.Ports;
using System.Text;

using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;

using GHIElectronics.NETMF.FEZ;
using GHIElectronics.NETMF.Net;
using GHIElectronics.NETMF.Net.Sockets;
using GHIElectronics.NETMF.Net.NetworkInformation;
using Socket = GHIElectronics.NETMF.Net.Sockets.Socket;

namespace M8_Brewery
{
    public class M8_BreweryMgr
    {
        public static void Main()
        {
            M8_TempMgr tempMgr;
            M8_PID pid;
            M8_SSR ssr;

            M8_WebServer server;

            DateTime updateIn = new DateTime();

            // We want to update every second
            updateIn = DateTime.Now.AddMilliseconds(1000);

            pid = new M8_PID(M8_PID.defaultPGain, M8_PID.defaultIGain, M8_PID.defaultDGain);
            ssr = new M8_SSR((Cpu.Pin)FEZ_Pin.Digital.Di6);
            tempMgr = new M8_TempMgr((Cpu.Pin)FEZ_Pin.Digital.Di5);
            tempMgr.setPidThermometer(0);

            Debug.Print("W5100.Enable");
            WIZnet_W5100.Enable(SPI.SPI_module.SPI1, (Cpu.Pin)FEZ_Pin.Digital.Di10, (Cpu.Pin)FEZ_Pin.Digital.Di9, false);
            //We need to give the Wiz chip some "alone time"
            Thread.Sleep(1000);

            NetworkInterface.EnableStaticIP(new byte[] { 192, 168, 1, 177 }, new byte[] { 255, 255, 255, 0 }, new byte[] { 192, 168, 1, 1 }, new byte[] { 0x90, 0xA2, 0xDA, 0x00, 0x14, 0x14 });
            NetworkInterface.EnableStaticDns(new byte[] { 192, 168, 1, 1 });

            server = new M8_WebServer( new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp), tempMgr, pid, ssr);

            server.startServer( 80 );

            while (true)
            {
                //Read the temps in
                if (DateTime.Now > updateIn)
                {
                    tempMgr.update();

                    //Calculate the PID value and set it on the SSR
                    pid.calcPID(tempMgr.getTemp(), tempMgr.getError());
                    ssr.setPower(pid.getSSRValue());

                    updateIn = DateTime.Now.AddMilliseconds(1000);

                    Debug.Print("-----------------" + DateTime.Now.ToString());
                }

                //Update the SSR
                ssr.update();

                server.update();
            }
        }
    }          
}
