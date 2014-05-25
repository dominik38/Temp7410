using System;
using Microsoft.SPOT;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using GTI = Gadgeteer.Interfaces;
using Microsoft.SPOT.Hardware;

namespace Gadgeteer.Modules.SchreiberDominik
{
    /// <summary>
    /// A Temp7410 module for Microsoft .NET Gadgeteer
    /// </summary>
    public class Temp7410 : GTM.Module
   {
        #region fields
        I2CDevice _adt7410;

        private float ResolutionCalculationFactor
        {
            get
            {
                if (Resolution == Resolution.Default)
                    return 16;
                else if (Resolution == Resolution.High)
                    return 128;
                else return 1;
            }
        }

        private GTI.DigitalInput criticalOverTemp;
        private bool lastCriticalInputState;
        #endregion;

        #region constants
        private const byte TemperatureMSBRegister = 0x00;
        private const byte UnderTemperatureMSBRegister = 0x06;
        private const byte OverTemperatureMSBRegister = 0x04;
        private const byte CriticalTemperatureMSBRegister = 0x08;
        private const byte HysteresisRegister = 0x0A;
        private const byte ResetRegister = 0x2F;
        private const short ClockRateKhz = 400;
        private const byte TemperatureRegister = 0x00;
        private const byte ConfigurationRegister = 0x03;

        #endregion

        #region properties
        private byte _address;
        public byte Address
        {
            get
            {
                return _address;
            }
            set
            {
                if (value != 0x48) // todo: andere 3 möglichkeiten in if abfrage aufnehmen
                    throw new NotSupportedException("Selected address is not supported. Possible addresses are: 0x48, 0xXX, 0xXX, 0xXX");
                _address = value;
            }
        }

        private Resolution _resolution;
        public Resolution Resolution
        {
            get
            {
                return _resolution;
            }
            set
            {
                UpdateResolution(value);
                _resolution = value;
            }
        }

        public int OverTemperature
        {
            get
            {
                return (int)Read16BitRegister(OverTemperatureMSBRegister);
            }
            set
            {
                Write16BitRegister(OverTemperatureMSBRegister, value);
            }
        }

        public int UnderTemperature
        {
            get
            {
                return (int)Read16BitRegister(UnderTemperatureMSBRegister);
            }
            set
            {
                Write16BitRegister(UnderTemperatureMSBRegister, value);
            }
        }

        public int Hysteresis
        {
            get
            {
                var result = I2CTransactions.Read(_adt7410, HysteresisRegister);
                return result[0];
                //return (int)Read16BitRegister(HysteresisRegister);
            }
            set
            {
                if (value < 0 || value > 15)
                    throw new NotSupportedException("Hysteresis value can not be less than 1 or more than 15!");
                //Write16BitRegister(HysteresisRegister, value);
                I2CTransactions.Write(_adt7410, HysteresisRegister, (byte)value);
            }
        }

        public int CriticalTemperature
        {
            get
            {
                return (int)Read16BitRegister(CriticalTemperatureMSBRegister);
            }
            set
            {
                Write16BitRegister(CriticalTemperatureMSBRegister, value);
            }
        }

        #endregion    

        #region constructor
        public Temp7410(int socketNumber, byte address)
        {
            // This finds the Socket instance from the user-specified socket number.  
            // This will generate user-friendly error messages if the socket is invalid.
            // If there is more than one socket on this module, then instead of "null" for the last parameter, 
            // put text that identifies the socket to the user (e.g. "S" if there is a socket type S)
            Socket socket = Socket.GetSocket(socketNumber, true, this, null);
            socket.EnsureTypeIsSupported('I', this);
            Address = address;
            this.input = new GTI.InterruptInput(socket, GT.Socket.Pin.Three, GTI.GlitchFilterMode.On, GTI.ResistorMode.PullUp, GTI.InterruptMode.RisingAndFallingEdge, this);
            this.input.Interrupt += new GTI.InterruptInput.InterruptEventHandler(this.overTemperatureInterrupt);
            this.criticalOverTemp = new GTI.DigitalInput(socket, Socket.Pin.Four, GTI.GlitchFilterMode.On, GTI.ResistorMode.Disabled, this);
            
            Timer pollCrit = new Timer(1000);
            pollCrit.Tick += pollCrit_Tick;
            pollCrit.Start();

            I2CDevice.Configuration i2cConfig = new I2CDevice.Configuration(Address,ClockRateKhz);
            _adt7410 = new I2CDevice(i2cConfig);
            Initialize();
        }

        void pollCrit_Tick(Timer timer)
        {
            if (!criticalOverTemp.Read() && !lastCriticalInputState)
            {
                lastCriticalInputState = true;
                this.OnCriticalTemperatureEvent(this, LimitState.Exceeded);
            }
            if (criticalOverTemp.Read() && lastCriticalInputState)
            {
                lastCriticalInputState = false;
                this.OnCriticalTemperatureEvent(this, LimitState.FallenBelow);
            }              
        }
        #endregion

        private void Initialize()
        {
            I2CTransactions.Write(_adt7410, ConfigurationRegister, (byte)0x10); //reset on comparator mode (bit 4 = 1)
            Resolution = Resolution.Default;
        }

        private void UpdateResolution(Resolution resolution)
        {
            byte config = I2CTransactions.Read(_adt7410, ConfigurationRegister)[0];
            if (resolution == Resolution.High)
                config |= (1 << 7);
            else config = (byte)(config & ~(1 << 7));
                //set bit no 7; 1 = 16bit, 0 = 13bit resolution
            I2CTransactions.Write(_adt7410, ConfigurationRegister, config);
        }

        public float GetTemperature()
        {
            return Read16BitRegister(TemperatureMSBRegister);           
        }

        public void ResetRegisterValues()
        {
            I2CTransactions.Write(_adt7410, ResetRegister, 1);
        }

        private void Write16BitRegister(byte register, int value)
        {
            short res = (short)((Int16)value * ResolutionCalculationFactor);
            byte lsb = (byte)res;
            byte msb = (byte)((Int16)res >> 8);
            I2CTransactions.Write(_adt7410, register, new byte[] { msb, lsb });
        }

        private float Read16BitRegister(byte register)
        {
            byte[] result = I2CTransactions.Read(_adt7410, register,2);
            return ((result[0] << 8) | result[1]) / ResolutionCalculationFactor;
        }

        //#region Overtemperature handling
        private void overTemperatureInterrupt(GTI.InterruptInput input, bool value)
        {
            this.OnOverTemperatureEvent(this, value ? LimitState.FallenBelow : LimitState.Exceeded);
        }

        private GTI.InterruptInput input;

        ///// <summary>
        ///// Gets a value that indicates whether the button of the Temp7410 is pressed.
        ///// </summary>
        //public bool IsPressed
        //{
        //    get
        //    {
        //        return this.input.Read();
        //    }
        //}

        ///// <summary>
        ///// Represents the state of button of the <see cref="Temp7410"/>.
        ///// </summary>
        public enum LimitState
        {
            /// <summary>
            /// Temperature fallen below value of UnderTemperature property.
            /// </summary>
            FallenBelow = 0,
            /// <summary>
            /// Temperature exceeds value of UnderTemperature property.
            /// </summary>
            Exceeded= 1
        }

        #region UnderTemperatureEvent

        /// <summary>
        /// Represents the delegate that is used to handle the <see cref="UnderTemperatureEvent"/>
        /// and <see cref="ButtonReleased"/> events.
        /// </summary>
        /// <param name="sender">The <see cref="ButtonExtender"/> object that raised the event.</param>
        /// <param name="state">The state of the button of the <see cref="ButtonExtender"/></param>
        public delegate void UnderTemperatureEventHandler(Temp7410 sender, LimitState state);

        /// <summary>
        /// Raised when a button of the <see cref="ButtonExtender"/> is pressed.
        /// </summary>
        /// <remarks>
        /// Implement this event handler and/or the <see cref="ButtonReleased"/> event handler
        /// when you want to provide an action associated with button events.
        /// Since the state of the button is passed to the <see cref="UnderTemperatureEventHandler"/> delegate,
        /// you can use the same event handler for both button states.
        /// </remarks>
        public event UnderTemperatureEventHandler UnderTemperatureEvent;

        private UnderTemperatureEventHandler onUnderTemperature;

        /// <summary>
        /// Raises the <see cref="UnderTemperatureEvent"/> or <see cref="ButtonReleased"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="ButtonExtender"/> that raised the event.</param>
        /// <param name="buttonState">The state of the button.</param>
        protected virtual void OnUnderTemperatureEvent(Temp7410 sender, LimitState state)
        {
            if (this.onUnderTemperature == null)
                this.onUnderTemperature = new UnderTemperatureEventHandler(this.OnUnderTemperatureEvent);

            if (Program.CheckAndInvoke(UnderTemperatureEvent, this.onUnderTemperature, state))
                this.UnderTemperatureEvent(sender, state);
        }
        #endregion

        #region OverTemperatureEvent

        /// <summary>
        /// Represents the delegate that is used to handle the <see cref="OverTemperatureEvent"/>
        /// and <see cref="ButtonReleased"/> events.
        /// </summary>
        /// <param name="sender">The <see cref="ButtonExtender"/> object that raised the event.</param>
        /// <param name="state">The state of the button of the <see cref="ButtonExtender"/></param>
        public delegate void OverTemperatureEventHandler(Temp7410 sender, LimitState state);

        /// <summary>
        /// Raised when a button of the <see cref="ButtonExtender"/> is pressed.
        /// </summary>
        /// <remarks>
        /// Implement this event handler and/or the <see cref="ButtonReleased"/> event handler
        /// when you want to provide an action associated with button events.
        /// Since the state of the button is passed to the <see cref="OverTemperatureEventHandler"/> delegate,
        /// you can use the same event handler for both button states.
        /// </remarks>
        public event OverTemperatureEventHandler OverTemperatureEvent;

        private OverTemperatureEventHandler onOverTemperature;

        /// <summary>
        /// Raises the <see cref="OverTemperatureEvent"/> or <see cref="ButtonReleased"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="ButtonExtender"/> that raised the event.</param>
        /// <param name="buttonState">The state of the button.</param>
        protected virtual void OnOverTemperatureEvent(Temp7410 sender, LimitState state)
        {
            if (this.onOverTemperature == null)
                this.onOverTemperature = new OverTemperatureEventHandler(this.OnOverTemperatureEvent);

            if (Program.CheckAndInvoke(OverTemperatureEvent, this.onOverTemperature, state))
                this.OverTemperatureEvent(sender, state);
        }
        #endregion
        
        #region CriticalTemperatureEvent

        /// <summary>
        /// Represents the delegate that is used to handle the <see cref="CriticalTemperatureEvent"/>
        /// and <see cref="ButtonReleased"/> events.
        /// </summary>
        /// <param name="sender">The <see cref="ButtonExtender"/> object that raised the event.</param>
        /// <param name="state">The state of the button of the <see cref="ButtonExtender"/></param>
        public delegate void CriticalTemperatureEventHandler(Temp7410 sender, LimitState state);

        /// <summary>
        /// Raised when a button of the <see cref="ButtonExtender"/> is pressed.
        /// </summary>
        /// <remarks>
        /// Implement this event handler and/or the <see cref="ButtonReleased"/> event handler
        /// when you want to provide an action associated with button events.
        /// Since the state of the button is passed to the <see cref="CriticalTemperatureEventHandler"/> delegate,
        /// you can use the same event handler for both button states.
        /// </remarks>
        public event CriticalTemperatureEventHandler CriticalTemperatureEvent;

        private CriticalTemperatureEventHandler onCriticalTemperature;

        /// <summary>
        /// Raises the <see cref="CriticalTemperatureEvent"/> or <see cref="ButtonReleased"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="ButtonExtender"/> that raised the event.</param>
        /// <param name="buttonState">The state of the button.</param>
        protected virtual void OnCriticalTemperatureEvent(Temp7410 sender, LimitState state)
        {
            if (this.onCriticalTemperature == null)
                this.onCriticalTemperature = new CriticalTemperatureEventHandler(this.OnCriticalTemperatureEvent);
                        
            if (Program.CheckAndInvoke(CriticalTemperatureEvent, this.onCriticalTemperature, state))
                this.CriticalTemperatureEvent(sender, state);
        }
        #endregion
        //#endregion
    }

    public enum Resolution
    {
        Default = 0,
        High = 1
    }
}
