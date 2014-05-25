using System;
using System.Collections;
using System.Threading;
using Microsoft.SPOT;
using Microsoft.SPOT.Presentation;
using Microsoft.SPOT.Presentation.Controls;
using Microsoft.SPOT.Presentation.Media;
using Microsoft.SPOT.Presentation.Shapes;
using Microsoft.SPOT.Touch;

using Gadgeteer.Networking;
using GT = Gadgeteer;
using GTM = Gadgeteer.Modules;
using Gadgeteer.Modules.SchreiberDominik;

namespace Temp7410_TestApp
{
    public partial class Program
    {
        float minTemp;
        float maxTemp;
        float averageTemp;

        Temp7410 tempSensor;
        // This method is run when the mainboard is powered up or reset.   
        void ProgramStarted()
        {
            /*******************************************************************************************
            Modules added in the Program.gadgeteer designer view are used by typing 
            their name followed by a period, e.g.  button.  or  camera.
            
            Many modules generate useful events. Type +=<tab><tab> to add a handler to an event, e.g.:
                button.ButtonPressed +=<tab><tab>
            
            If you want to do something periodically, use a GT.Timer and handle its Tick event, e.g.:
                GT.Timer timer = new GT.Timer(1000); // every second (1000ms)
                timer.Tick +=<tab><tab>
                timer.Start();
            *******************************************************************************************/


            // Use Debug.Print to show messages in Visual Studio's "Output" window during debugging.
            Debug.Print("Program Started");

            tempSensor = new Temp7410(1, 0x48);
            tempSensor.Resolution = Resolution.High;
            var overtemp = tempSensor.OverTemperature;
            tempSensor.OverTemperature = 32;
            tempSensor.CriticalTemperature = 45;
            tempSensor.Hysteresis = 3;
            Debug.Print("Overtemp: " + tempSensor.OverTemperature);

            tempSensor.OverTemperatureEvent += tempSensor_OverTemperatureEvent;
            tempSensor.UnderTemperatureEvent += tempSensor_UnderTemperatureEvent;
            tempSensor.CriticalTemperatureEvent += tempSensor_CriticalTemperatureEvent;

            GT.Timer timer = new GT.Timer(1000);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        void tempSensor_CriticalTemperatureEvent(Temp7410 sender, Temp7410.LimitState state)
        {
            if (state == Temp7410.LimitState.Exceeded)
                Debug.Print("Criticaltemperature exeeded!");
            else Debug.Print("Fallen below Criticaltemperature");
        }

        void tempSensor_UnderTemperatureEvent(Temp7410 sender, Temp7410.LimitState state)
        {
            if (state == Temp7410.LimitState.Exceeded)
                Debug.Print("Undertemperature exeeded!");
            else Debug.Print("Fallen below Undertemperature");
        }

        void tempSensor_OverTemperatureEvent(Temp7410 sender, Temp7410.LimitState state)
        {
            if (state == Temp7410.LimitState.Exceeded)
                Debug.Print("Overtemperature exeeded!");
            else Debug.Print("Fallen below Overtemperature");
        }

        void timer_Tick(GT.Timer timer)
        {
            MeasureTemperature();
        }

        private void MeasureTemperature()
        {
            var temp = tempSensor.GetTemperature();
            Debug.Print("Temp: " + temp);
        }
    }
}
