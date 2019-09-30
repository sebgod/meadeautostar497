// This implements a console application that can be used to test an ASCOM driver
//

// This is used to define code in the template that is specific to one class implementation
// unused code can be deleted and this definition removed.

#define Telescope
// remove this to bypass the code that uses the chooser to select the driver
#define UseChooser

using System;
using ASCOM.DriverAccess;

namespace ASCOM.Meade.net
{
    class Program
    {
        static void Main(string[] args)
        {
            // Uncomment the code that's required
#if UseChooser
            // choose the device
            string id = Telescope.Choose("ASCOM.MeadeGeneric.Telescope");
            if (string.IsNullOrEmpty(id))
                return;
            // create this device
            Telescope device = new Telescope(id);
#else
            // this can be replaced by this code, it avoids the chooser and creates the driver class directly.
            ASCOM.DriverAccess.Telescope device = new ASCOM.DriverAccess.Telescope("ASCOM.Meade.net.Telescope");
#endif
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

            Console.WriteLine($"Altitude {device.Altitude}");
            Console.WriteLine($"Azimuth {device.Azimuth}");
            
            device.Connected = false;
            Console.WriteLine("Press Enter to finish");
            Console.ReadLine();
        }
    }
}
