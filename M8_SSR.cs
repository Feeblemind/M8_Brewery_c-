using System;
using Microsoft.SPOT;
using Microsoft.SPOT.Hardware;
using GHIElectronics.NETMF.FEZ;

namespace M8_Brewery
{
    class M8_SSR
    {
        //Number of jiffies per millisecond = 16.6...
        public static readonly float jiffyPerMillis = 16.6666667F;
        //number of jiffies per percent = 1.6...
        public static readonly float jiffyPerPercent = 1.6666667F;

        //Pin the SSR is on
        OutputPort _outputPin;
        //Number of jiffies that the SSR should be "on"
        int _jiffy;
        Boolean _on = true;

        public M8_SSR(Cpu.Pin pin)
        {
            this._outputPin = new OutputPort(pin, false);

            this.setPower(0);
        }

        //This will be the function that needs to be updated
        public void update()
        {
            long ms = DateTime.Now.Millisecond;

            if (((ms % 1000) < (_jiffy * jiffyPerMillis )) && ( _on == true ))
                this._outputPin.Write(true);
            else
               this._outputPin.Write(false);
        }

        public void off()
        {
            _on = false;
        }

        //sets the power of the SSR (in Jiffies) by a percentage
        public void setPower(int power)
        {
            if (power > 100)
                power = 100;
            if (power < 0)
                power = 0;

            //A jiffy is the time between the 0s on the AC wave (the 100/60 is 60Hz)
            this._jiffy = (int)System.Math.Round((power) / M8_SSR.jiffyPerPercent);
        }
        public int getPower()
        {
            //A jiffy is the time between the 0s on the AC wave (the 100/60 is 60Hz)
            return (int)System.Math.Round((this._jiffy) * M8_SSR.jiffyPerPercent);
        }

        public void setJiffy(int jiffy)
        {
            if (jiffy > 100)
                jiffy = 100;
            if (jiffy < 0)
                jiffy = 0;
            this._jiffy = jiffy;
        }
        public int getJiffy()
        {
            return this._jiffy;
        }
    }
}
