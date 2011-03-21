using System;
using System.Collections;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using GHIElectronics.NETMF.Hardware;
using GHIElectronics.NETMF.FEZ;

/*
namespace Test
{
    public class Program
    {
        public static void Main()
        {
            // Change this your correct pin!
            Cpu.Pin myPin = (Cpu.Pin)FEZ_Pin.Digital.Di4;

            OneWire ow = new OneWire(myPin);
            ushort temperature;
            // read every second
            while (true)
            {
                if (ow.Reset())
                {
                    ow.WriteByte(0xCC); // Skip ROM
                    ow.WriteByte(0x44); // Start temperature conversion
                    while (ow.ReadByte() == 0) ; // wait while busy
                    ow.Reset();
                    ow.WriteByte(0xCC); // skip ROM
                    ow.WriteByte(0xBE); // Read Scratchpad
                    temperature = ow.ReadByte(); // LSB
                    temperature |= (ushort)(ow.ReadByte() << 8); // MSB
                    Debug.Print("Temperature: " + temperature / 16);
                    Thread.Sleep(1000);
                }
                else
                {
                    Debug.Print("Device is not detected.");
                }
                Thread.Sleep(1000);
            }
        }
    }
}
*/

namespace M8_Brewery
{
    class M8_DS18B20
    {
        public byte[] _address;
        public float _temp;
        public float _target;

        public M8_DS18B20( )
        {
            this._address = new byte[8];

            for (int i = 0; i < 8; i++)
                this._address[i] = 0;

            this._temp = 0;
            this._target = 30;
        }
        public void setAddress( byte[] address )
        {
            for (int i = 0; i < 8; i++)
                this._address[i] = address[i];
        }
        public override String ToString()
        {
            //We've got to make a string with a string, so load the 1st address
            String temp = "Addr in dec:";

            //It helps becouse we can add the ','s before addresses so we don't have one "dangling" at the end
            for (int i=0;i<8;i++ )
                temp = temp + ' ' + this._address[i].ToString();

            temp = temp + " Temp " + this._temp.ToString();

            return temp;
        }
    }

    class M8_TempMgr
    {
        Cpu.Pin _owPin;
        OneWire _owBus;

        int _pidThermometer;

        ArrayList _thermometers;

        public M8_TempMgr(Cpu.Pin owPin)
        {
            this._owPin = owPin;
            this._owBus = new OneWire(this._owPin);

            this._thermometers = new ArrayList();
            this._getThermometers();
        }

        private void _convertCommand(byte[] address)
        {
            this._owBus.Reset();

            this._owBus.WriteByte(0x55);
            this._owBus.Write(address, 0, 8);
            this._owBus.WriteByte(0x44);
        }

        private float _readTemp( byte[] address )
        {
            int temp=0;
            int cpc = 0; //Count per Celcius Degree
            int cr = 0; //Count remain
            byte[] data = new byte[9];

            this._owBus.Reset();

            this._owBus.WriteByte(0x55);
            this._owBus.Write(address, 0, 8);
            this._owBus.WriteByte(0xBE); // Read Scratchpad

            // Read in 9 bytes worth of info
            for (int i = 0; i < 9; i++)
                data[i] = this._owBus.ReadByte();

            // Copy the 8 LSB bits
            temp = (int)data[0];

/*            //Check for a negitive number
            if (data[1] > 0x80)
            {
                temp = !temp + 1; // twos complement adjustment
                temp = temp * -1; // Make it negitive
            }
 */

            cpc = data[7];
            cr = data[6];

            temp = temp >> 1;

            return (float)(temp - 0.25 + ( ( cpc -cr )/(float)cpc ));
        }

        public int getPidThermometer()
        {
            return this._pidThermometer;
        }
        public void setPidThermometer(int thermometer)
        {
            this._pidThermometer = thermometer;

            if ( this._pidThermometer < 0)
                this._pidThermometer = 0;
            if (this._pidThermometer > this.getCount())
                this._pidThermometer = this.getCount();
        }

        public void update()
        {

            // Time to update the temps
            foreach (M8_DS18B20 thermo in this._thermometers)
            {
                thermo._temp = this._readTemp(thermo._address);
                this._convertCommand(thermo._address);
            }
        }

        public float getTarget(int thermometer)
        {
            return ((M8_DS18B20)this._thermometers[thermometer])._target;
        }
        public float getTarget()
        {
            return this.getTarget(this._pidThermometer);
        }
        public void setTarget(int thermometer, float target)
        {
            ((M8_DS18B20)this._thermometers[thermometer])._target = target;
        }
        public void setTarget(float target)
        {
            this.setTarget(this._pidThermometer, target);
        }

        public float getTemp(int thermometer)
        {
            return ((M8_DS18B20)this._thermometers[thermometer])._temp;
        }
        public float getTemp()
        {
            return this.getTemp(this._pidThermometer);
        }

        public float getError(int thermometer)
        {
            return (((M8_DS18B20)this._thermometers[thermometer])._target - ((M8_DS18B20)this._thermometers[thermometer])._temp);
        }
        public float getError()
        {
            return this.getError(this._pidThermometer);
        }

        private void _getThermometers()
        {
            this._owBus.Reset();

            //Remove the current thermometers
            this._thermometers.Clear();

            //Check to see if we find a new thermometer
            byte[] address = new byte[8];

            while (this._owBus.Search_GetNextDevice(address))
            {
                //if we do and its a DS18B20 add it to the list

                Debug.Print("Device Found!");

                M8_DS18B20 temp = new M8_DS18B20();
                temp.setAddress(address);

                Debug.Print(temp.ToString());

                this._thermometers.Add(temp);
            }
        }

        public int getCount( )
        {
            return this._thermometers.Count;
        }
    }
}
