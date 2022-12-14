// This implements a console application that can be used to test an ASCOM driver
//

// This is used to define code in the template that is specific to one class implementation
// unused code can be deleted and this definition removed.

#define Telescope
// remove this to bypass the code that uses the chooser to select the driver
#define UseChooser

using System;
using System.Linq;
using System.Threading;
using ASCOM.DeviceInterface;
using ASCOM.DriverAccess;

namespace ASCOM.Meade.net
{
    public static class Program
    {
        public static void Main()
        {
            // Uncomment the code that's required
//#if UseChooser
            // choose the device
            string id = Telescope.Choose("ASCOM.Meade.net.Telescope");
            if (string.IsNullOrEmpty(id))
                return;
            // create this device
            Telescope device = new Telescope(id);
//#else
            // this can be replaced by this code, it avoids the chooser and creates the driver class directly.
            //ASCOM.DriverAccess.Telescope device = new ASCOM.DriverAccess.Telescope("ASCOM.Meade.net.Telescope");
//#endif
            // now run some tests, adding code to your driver so that the tests will pass.
            // these first tests are common to all drivers.

            
            Console.WriteLine("name " + device.Name);
            Console.WriteLine("description " + device.Description);
            Console.WriteLine("DriverInfo " + device.DriverInfo);
            Console.WriteLine("driverVersion " + device.DriverVersion);

            // TODO add more code to test the driver.
            device.Connected = true;


            //device.SlewToAltAz(150, 50);

            //device.CommandBlind(":Sa+30*00'00#", true);
            //device.CommandBlind(":Sz50*00#", true);
            //device.CommandBlind(":MA#", true);
            //Console.WriteLine($"Ra {device.RightAscension}");
            //Console.WriteLine($"Dec {device.Declination}");

            //Console.WriteLine($"Altitude {device.Altitude}");
            //Console.WriteLine($"Azimuth {device.Azimuth}");

            var seconds = 10;

            Console.WriteLine("Slewing tests 10 second in each direction");
            Console.WriteLine("test 1");
            device.MoveAxis(TelescopeAxes.axisPrimary, 4);
            Thread.Sleep(seconds * 1000);
            device.MoveAxis(TelescopeAxes.axisPrimary, 0);
            Console.WriteLine("test 2");
            device.MoveAxis(TelescopeAxes.axisPrimary, -4);
            Thread.Sleep(seconds * 1000);
            device.MoveAxis(TelescopeAxes.axisPrimary, 0);

            Console.WriteLine("test 3");
            device.MoveAxis(TelescopeAxes.axisSecondary, 4);
            Thread.Sleep(seconds * 1000);
            device.MoveAxis(TelescopeAxes.axisSecondary, 0);

            Console.WriteLine("test 4");
            device.MoveAxis(TelescopeAxes.axisSecondary, -4);
            Thread.Sleep(seconds * 1000);
            device.MoveAxis(TelescopeAxes.axisSecondary, 0);
            Console.WriteLine("Slewing tests complete");


            seconds = 120;

            Console.WriteLine($"Guiding for {seconds} seconds!");

            foreach( var direction in Enum.GetValues(typeof(GuideDirections)).Cast<GuideDirections>())
            { 
                Console.WriteLine($"{direction.ToString()}");
                device.PulseGuide(direction, seconds* 1000);
            }
            Console.WriteLine("Guiding Finished");



            device.Connected = false;
            Console.WriteLine("Press Enter to finish");
            Console.ReadLine();
        }
    }
}
