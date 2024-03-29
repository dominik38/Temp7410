﻿using System;
using Microsoft.SPOT;

using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using GTI = Gadgeteer.Interfaces;

namespace Gadgeteer.Modules.SchreiberDominik
{
    /// <summary>
    /// A Temp7410 module for Microsoft .NET Gadgeteer
    /// </summary>
    public class Temp7410 : GTM.Module
    {
        // This example implements a driver in managed code for a simple Gadgeteer module.  This module uses a 
        // single GTI.InterruptInput to interact with a button that can be in either of two states: pressed or released.
        // The example code shows the recommended code pattern for exposing a property (IsPressed). 
        // The example also uses the recommended code pattern for exposing two events: Pressed and Released. 
        // The triple-slash "///" comments shown will be used in the build process to create an XML file named
        // GTM.SchreiberDominik.Temp7410. This file will provide IntelliSense and documentation for the
        // interface and make it easier for developers to use the Temp7410 module.        

        // Note: A constructor summary is auto-generated by the doc builder.
        /// <summary></summary>
        /// <param name="socketNumber">The socket that this module is plugged in to.</param>
        /// <param name="socketNumberTwo">The second socket that this module is plugged in to.</param>
        public Temp7410(int socketNumber, int socketNumberTwo)
        {
            // This finds the Socket instance from the user-specified socket number.  
            // This will generate user-friendly error messages if the socket is invalid.
            // If there is more than one socket on this module, then instead of "null" for the last parameter, 
            // put text that identifies the socket to the user (e.g. "S" if there is a socket type S)
            Socket socket = Socket.GetSocket(socketNumber, true, this, null);

            // This creates an GTI.InterruptInput interface. The interfaces under the GTI namespace provide easy ways to build common modules.
            // This also generates user-friendly error messages automatically, e.g. if the user chooses a socket incompatible with an interrupt input.
            this.input = new GTI.InterruptInput(socket, GT.Socket.Pin.Three, GTI.GlitchFilterMode.On, GTI.ResistorMode.PullUp, GTI.InterruptMode.RisingAndFallingEdge, this);

            // This registers a handler for the interrupt event of the interrupt input (which is bereleased)
            this.input.Interrupt += new GTI.InterruptInput.InterruptEventHandler(this._input_Interrupt);
        }

        private void _input_Interrupt(GTI.InterruptInput input, bool value)
        {
            this.OnButtonEvent(this, value ? ButtonState.Released : ButtonState.Pressed);
        }

        private GTI.InterruptInput input;

        /// <summary>
        /// Gets a value that indicates whether the button of the Temp7410 is pressed.
        /// </summary>
        public bool IsPressed
        {
            get
            {
                return this.input.Read();
            }
        }

        /// <summary>
        /// Represents the state of button of the <see cref="Temp7410"/>.
        /// </summary>
        public enum ButtonState
        {
            /// <summary>
            /// The button is released.
            /// </summary>
            Released = 0,
            /// <summary>
            /// The button is pressed.
            /// </summary>
            Pressed = 1
        }

        /// <summary>
        /// Represents the delegate that is used to handle the <see cref="ButtonPressed"/>
        /// and <see cref="ButtonReleased"/> events.
        /// </summary>
        /// <param name="sender">The <see cref="Temp7410"/> object that raised the event.</param>
        /// <param name="state">The state of the button of the <see cref="Temp7410"/></param>
        public delegate void ButtonEventHandler(Temp7410 sender, ButtonState state);

        /// <summary>
        /// Raised when the button of the <see cref="Temp7410"/> is pressed.
        /// </summary>
        /// <remarks>
        /// Implement this event handler and/or the <see cref="ButtonReleased"/> event handler
        /// when you want to provide an action associated with button events.
        /// Since the state of the button is passed to the <see cref="ButtonEventHandler"/> delegate,
        /// so you can use the same event handler for both button states.
        /// </remarks>
        public event ButtonEventHandler ButtonPressed;

        /// <summary>
        /// Raised when the button of the <see cref="Temp7410"/> is released.
        /// </summary>
        /// <remarks>
        /// Implement this event handler and/or the <see cref="ButtonPressed"/> event handler
        /// when you want to provide an action associated with button events.
        /// Since the state of the button is passed to the <see cref="ButtonEventHandler"/> delegate,
        /// you can use the same event handler for both button states.
        /// </remarks>
        public event ButtonEventHandler ButtonReleased;

        private ButtonEventHandler onButton;

        /// <summary>
        /// Raises the <see cref="ButtonPressed"/> or <see cref="ButtonReleased"/> event.
        /// </summary>
        /// <param name="sender">The <see cref="Temp7410"/> that raised the event.</param>
        /// <param name="buttonState">The state of the button.</param>
        protected virtual void OnButtonEvent(Temp7410 sender, ButtonState buttonState)
        {
            if (this.onButton == null)
            {
                this.onButton = new ButtonEventHandler(this.OnButtonEvent);
            }

            if (buttonState == ButtonState.Pressed)
            {
                // Program.CheckAndInvoke helps event callers get onto the Dispatcher thread.  
                // If the event is null then it returns false.
                // If it is called while not on the Dispatcher thread, it returns false but also re-invokes this method on the Dispatcher.
                // If on the thread, it returns true so that the caller can execute the event.
                if (Program.CheckAndInvoke(ButtonPressed, this.onButton, sender, buttonState))
                {
                    this.ButtonPressed(sender, buttonState);
                }
            }
            else
            {
                if (Program.CheckAndInvoke(ButtonReleased, this.onButton, sender, buttonState))
                {
                    this.ButtonReleased(sender, buttonState);
                }
            }
        }
    }
}
