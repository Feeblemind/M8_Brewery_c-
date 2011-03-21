using System;
using Microsoft.SPOT;

namespace M8_Brewery
{
    class M8_PID
    {
        //Set some static constants for the PIDs
        public static readonly float defaultPGain = 40;
        public static readonly float defaultIGain = 0.01F;
        public static readonly float defaultDGain = 0;

        //The PID function terms = these are calculated.
        float _pTerm;
        float _iTerm;
        float _dTerm;

        //The PID function gains - these are set.
        float _pGain;
        float _iGain;
        float _dGain;

        //Used to limit the iTerm, if they are the same or Max < Min then the iTerm isn't limited.
        float _iTermMin;
        float _iTermMax;

        //State variables for the PID function
        float _iState;
        float _dState;

        //The value we calculated using calcPID(...)
        float _value;

        public M8_PID(float pGain, float iGain, float dGain, float iTermMin, float iTermMax)
        {
            setPGain(pGain);
            setIGain(iGain);
            setDGain(dGain);

            this._iTermMin = iTermMin;
            this._iTermMax = iTermMax;

            this._pTerm = 0;
            this._iTerm = 0;
            this._dTerm = 0;

            this._iState = 0;
            this._dState = 0;
        }
        public M8_PID(float pGain, float iGain, float dGain)
        {
            setPGain(pGain);
            setIGain(iGain);
            setDGain(dGain);

            this._iTermMin = 0;
            this._iTermMax = 0;

            this._pTerm = 0;
            this._iTerm = 0;
            this._dTerm = 0;

            this._iState = 0;
            this._dState = 0;
        }

        //This is the way we'll get the value from the PID
        public float getValue( )
        {
            return this._value;
        }

        //This is how the SSR will get the value (we limit it for the SSR)
        public int getSSRValue()
        {
            // The SSR only speaks %. 0-100 Lets limit the output to such.
            if (this._value > 100)
                return 100;
            else if (this._value < 0)
                return 0;
            else
                return (int)this._value;
        }

        //This is how we calculate the PID value
        public void calcPID( float currTemp, float error )
        {
            this._pTerm = _calcPTerm( error );
            this._iTerm = _calcITerm(error);
            this._dTerm = _calcDTerm(currTemp);

            this._value = this._pTerm + this._iTerm + this._dTerm;
        }

        //Helper functions to generate the 3 Terms
        private float _calcPTerm( float error )
        {
            return (this._pGain * error);
        }
        private float _calcITerm( float error )
        {
            this._iState += error;

            //if we're not limited, limit us
            if (this._iUnlimited())
            {
                if (this._iState > this._iTermMax)
                    this._iState = this._iTermMax;
                if (this._iState < this._iTermMin)
                    this._iState = this._iTermMin;
            }

            return (this._iGain * this._iState);
        }
        private float _calcDTerm( float currTemp )
        {
            float tempDTerm;

            tempDTerm = (this._dGain * (this._dState - currTemp));

            this._dState = tempDTerm;

            return tempDTerm;
        }

        // Helper functon to deturimine if the iTerm is limited
        private Boolean _iUnlimited( )         
        {
            if ((this._iTermMin == this._iTermMax) || (this._iTermMax < this._iTermMin))
                return true;
            else
                return false;
        }

        // Lets declare a bunch of accessor methods (They're my fav!)
        public void setPGain(float pGain)
        {
            this._pGain = pGain;
        }
        public float getPGain( )
        {
            return this._pGain;
        }
        public void setIGain(float iGain)
        {
            this._iGain = iGain;
        }
        public float getIGain( )
        {
            return this._iGain;
        }
        public void setDGain(float dGain)
        {
            this._dGain = dGain;
        }
        public float getDGain( )
        {
            return this._dGain;
        }

        public float getPTerm( )
        {
            return this._pTerm;
        }
        public float getITerm( )
        {
            return this._iTerm;
        }
        public float getDTerm( )
        {
            return this._dTerm;
        }
    }
}
