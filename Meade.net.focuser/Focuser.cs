#define Focuser

using System;
using System.Collections;
using System.Reflection;
using System.Runtime.InteropServices;
using ASCOM.DeviceInterface;
using ASCOM.Meade.net.Wrapper;
using ASCOM.Utilities;
using ASCOM.Utilities.Interfaces;

namespace ASCOM.Meade.net
{
    //
    // Your driver's DeviceID is ASCOM.Meade.net.Focuser
    //
    // The Guid attribute sets the CLSID for ASCOM.Meade.net.Focuser
    // The ClassInterface/None addribute prevents an empty interface called
    // _Meade.net from being created and used as the [default] interface
    //
    // Replace the not implemented exceptions with code to implement the function or
    // throw the appropriate ASCOM exception.
    //

    /// <summary>
    /// ASCOM Focuser Driver for Meade.net.
    /// </summary>
    [Guid("a32ac647-bf0f-42f9-8ab0-d166fa5884ad")]
    [ProgId("ASCOM.MeadeGeneric.focuser")]
    [ServedClassName("Meade Generic")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class Focuser : MeadeTelescopeBase, IFocuserV3
    {
        /// <summary>
        /// ASCOM DeviceID (COM ProgID) for this driver.
        /// The DeviceID is used by ASCOM applications to load the driver at runtime.
        /// </summary>
        //internal static string driverID = "ASCOM.Meade.net.Focuser";
        private static readonly string DriverId = Marshal.GenerateProgIdForType(MethodBase.GetCurrentMethod().DeclaringType ?? throw new System.InvalidOperationException());

        /// <summary>
        /// Private variable to hold an ASCOM Utilities object
        /// </summary>
        private readonly IUtil _utilities;

        /// <summary>
        /// Initializes a new instance of the <see cref="Meade.net"/> class.
        /// Must be public for COM registration.
        /// </summary>
        public Focuser()
        {
            //todo move this out to IOC
            var util = new Util(); //Initialise util object
            _utilities = util;

            Initialise(nameof(Focuser));
        }

        public Focuser(IUtil util, ISharedResourcesWrapper sharedResourcesWrapper) : base(sharedResourcesWrapper)
        {
            _utilities = util;

            Initialise(nameof(Focuser));
        }

        //
        // PUBLIC COM INTERFACE IFocuserV3 IMPLEMENTATION
        //

        #region Common properties and methods.

        /// <summary>
        /// Displays the Setup Dialog form.
        /// If the user clicks the OK button to dismiss the form, then
        /// the new settings are saved, otherwise the old values are reloaded.
        /// THIS IS THE ONLY PLACE WHERE SHOWING USER INTERFACE IS ALLOWED!
        /// </summary>
        public void SetupDialog()
        {
            Tl.LogMessage("SetupDialog", "Opening setup dialog");
            SharedResourcesWrapper.SetupDialog();
            ReadProfile();
            Tl.LogMessage("SetupDialog", "complete");
        }

        public ArrayList SupportedActions
        {
            get
            {
                Tl.LogMessage("SupportedActions Get", "Returning empty arraylist");
                return new ArrayList();
            }
        }

        public string Action(string actionName, string actionParameters)
        {
            LogMessage("", "Action {0}, parameters {1} not implemented", actionName, actionParameters);
            throw new ActionNotImplementedException();
        }

        public void CommandBlind(string command, bool raw)
        {
            LogMessage("CommandBlind", "raw: {0} command {0}", raw, command);
            CheckConnected("CommandBlind");
            // Call CommandString and return as soon as it finishes
            //this.CommandString(command, raw);
            SharedResourcesWrapper.SendBlind(command, raw);
            // or
            //throw new ASCOM.MethodNotImplementedException("CommandBlind");
            // DO NOT have both these sections!  One or the other
            LogMessage("CommandBlind", "Completed");
        }

        public bool CommandBool(string command, bool raw)
        {
            LogMessage("CommandBool", "raw: {0} command {0}", raw, command);
            CheckConnected("CommandBool");
            var result = SharedResourcesWrapper.SendBool(command, raw);
            LogMessage("CommandBool", "Completed: {0}", result);
            return result;
            // or
            //throw new MethodNotImplementedException("CommandBool");
            // DO NOT have both these sections!  One or the other
        }

        public string CommandString(string command, bool raw)
        {
            LogMessage("CommandString", "raw: {0} command {0}", raw, command);
            CheckConnected("CommandString");
            // it's a good idea to put all the low level communication with the device here,
            // then all communication calls this function
            // you need something to ensure that only one command is in progress at a time
            var result = SharedResourcesWrapper.SendString(command, raw);
            LogMessage("CommandBool", "Completed: {0}", result);
            return result;
            //throw new ASCOM.MethodNotImplementedException("CommandString");
        }

        public void Dispose()
        {
            // Clean up the tracelogger and util objects
            Tl.Enabled = false;
            Tl.Dispose();
            Tl = null;
        }

        public bool Connected
        {
            get
            {
                LogMessage("Connected", "Get {0}", IsConnected);
                return IsConnected;
            }
            set
            {
                Tl.LogMessage("Connected", "Set {0}", value);
                if (value == IsConnected)
                    return;

                if (value)
                {
                    try
                    {
                        ReadProfile();
                        SharedResourcesWrapper.Connect("Serial", DriverId, Tl);
                        try
                        {
                            IsConnected = true;
                        }
                        catch (Exception)
                        {
                            SharedResourcesWrapper.Disconnect("Serial", DriverId);
                            throw;
                        }
                    }
                    catch (Exception ex)
                    {
                        LogMessage("Connected Set", "Error connecting to port {0} - {1}", _profileProperties.ComPort, ex.Message);
                    }
                }
                else
                {
                    LogMessage("Connected Set", "Disconnecting from port {0}", _profileProperties.ComPort);
                    SharedResourcesWrapper.Disconnect("Serial", DriverId);
                    IsConnected = false;
                }
            }
        }

        public short InterfaceVersion
        {
            // set by the driver wizard
            get
            {
                LogMessage("InterfaceVersion Get", "3");
                return Convert.ToInt16("3");
            }
        }

        public string Name
        {
            get
            {
                //string name = "Short driver name - please customise";
                string name = DriverDescription;
                Tl.LogMessage("Name Get", name);
                return name;
            }
        }

        #endregion

        #region IFocuser Implementation

        public bool Absolute
        {
            get
            {
                CheckConnected("Absolute Get");

                Tl.LogMessage("Absolute Get", false.ToString());
                return false; // This is a relative focuser
            }
        }

        public void Halt()
        {
            Tl.LogMessage("Halt", "Halting");

            CheckConnected("Halt");

            //todo fix this issue: A single halt command is sometimes missed by the #909 apm, so let's do it a few times to be safe.

            SharedResourcesWrapper.SendBlind("FQ");
            //:FQ# Halt Focuser Motion
            //Returns: Nothing
        }

        public bool IsMoving
        {
            get
            {
                Tl.LogMessage("IsMoving Get", false.ToString());
                return false; // This focuser always moves instantaneously so no need for IsMoving ever to be True
            }
        }

        public bool Link
        {
            get
            {
                Tl.LogMessage("Link Get", Connected.ToString());
                return Connected; // Direct function to the connected method, the Link method is just here for backwards compatibility
            }
            set
            {
                Tl.LogMessage("Link Set", value.ToString());
                Connected = value; // Direct function to the connected method, the Link method is just here for backwards compatibility
            }
        }

        private readonly int _maxIncrement = 7000;
        public int MaxIncrement
        {
            get
            {
                Tl.LogMessage("MaxIncrement Get", _maxIncrement.ToString());
                return _maxIncrement; // Maximum change in one move
            }
        }

        private readonly int _maxStep = 7000;

        public int MaxStep
        {
            get
            {
                Tl.LogMessage("MaxStep Get", _maxStep.ToString());
                return _maxStep;
            }
        }

        public void Move(int position)
        {
            Tl.LogMessage("Move", position.ToString());
            CheckConnected("Move");

            if (position < -MaxIncrement || position > MaxIncrement)
            {
                throw new InvalidValueException($"position out of range {-MaxIncrement} < {position} < {MaxIncrement}");
            }

            if (position == 0)
                return;

            var direction = position > 0;
            if (_profileProperties.ReverseFocusDirection)
                direction = !direction;
            
            //backlash compensation.
            var backlashCompensationSteps = direction ? Math.Abs(_profileProperties.BacklashCompensation) : 0;

            var steps = Math.Abs(position) + backlashCompensationSteps;


            MoveFocuser(direction, steps);


            //todo refactor the backlash compensation to combine the commands into as few moves as practicle.
            //ApplyBacklashCompensation(direction);
            if (direction & backlashCompensationSteps != 0)
            {
                Tl.LogMessage("Move", "Applying backlash compensation");
                MoveFocuser(!direction, backlashCompensationSteps);
            }

            DynamicBreaking(direction);
            //todo implement dynamic braking
            //dynamic breaking is sending the command to move in the opposite direction immediatly followed by the command to stop.
        }

        private void DynamicBreaking(bool directionOut)
        {
            if (!_profileProperties.DynamicBreaking)
                return;

            Tl.LogMessage("Move", "Applying dynamic breaking");

            PerformFocuserMove(directionOut);
            Halt();
        }

        private void MoveFocuser(bool directionOut, int steps)
        {
            //_sharedResourcesWrapper.SendBlind("FF");
            //:FF# Set Focus speed to fastest setting
            //Returns: Nothing

            //:FS# Set Focus speed to slowest setting
            //Returns: Nothing

            //:F<n># Autostar, Autostar II - set focuser speed to <n> where <n> is an ASCII digit 1..4
            //Returns: Nothing
            //All others - Not Supported
            _utilities.WaitForMilliseconds(100);

            PerformFocuserMove(directionOut);

            _utilities.WaitForMilliseconds(steps);

            Halt();
        }

        private void PerformFocuserMove(bool directionOut)
        {
            SharedResourcesWrapper.SendBlind(directionOut ? "F+" : "F-");
            //:F+# Start Focuser moving inward (toward objective)
            //Returns: None

            //:F-# Start Focuser moving outward (away from objective)
            //Returns: None
        }

        public int Position => throw new PropertyNotImplementedException("Position", false);

        public double StepSize
        {
            get
            {
                Tl.LogMessage("StepSize Get", "Not implemented");
                throw new PropertyNotImplementedException("StepSize", false);
            }
        }

        public bool TempComp
        {
            get
            {
                Tl.LogMessage("TempComp Get", false.ToString());
                return false;
            }
            // ReSharper disable once ValueParameterNotUsed
            set
            {
                Tl.LogMessage("TempComp Set", "Not implemented");
                throw new PropertyNotImplementedException("TempComp", false);
            }
        }

        public bool TempCompAvailable
        {
            get
            {
                Tl.LogMessage("TempCompAvailable Get", false.ToString());
                return false; // Temperature compensation is not available in this driver
            }
        }

        public double Temperature
        {
            get
            {
                Tl.LogMessage("Temperature Get", "Not implemented");
                throw new PropertyNotImplementedException("Temperature", false);
            }
        }

        #endregion

        #region Private properties and methods
        // here are some useful properties and methods that can be used as required
        // to help with driver development

        #region ASCOM Registration

        // Register or unregister driver for ASCOM. This is harmless if already
        // registered or unregistered.
        //
        /// <summary>
        /// Register or unregister the driver with the ASCOM Platform.
        /// This is harmless if the driver is already registered/unregistered.
        /// </summary>
        /// <param name="bRegister">If <c>true</c>, registers the driver, otherwise unregisters it.</param>
        private static void RegUnregAscom(bool bRegister)
        {
            using (IProfileWrapper p = ProfileFactory.Create())
            {
                p.DeviceType = nameof(Focuser);
                if (bRegister)
                {
                    p.Register(DriverId, DriverDescription);
                }
                else
                {
                    p.Unregister(DriverId);
                }
            }
        }

        /// <summary>
        /// This function registers the driver with the ASCOM Chooser and
        /// is called automatically whenever this class is registered for COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is successfully built.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During setup, when the installer registers the assembly for COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually register a driver with ASCOM.
        /// </remarks>
        [ComRegisterFunction]
        public static void RegisterAscom(Type t)
        {
            RegUnregAscom(true);
        }

        /// <summary>
        /// This function unregisters the driver from the ASCOM Chooser and
        /// is called automatically whenever this class is unregistered from COM Interop.
        /// </summary>
        /// <param name="t">Type of the class being registered, not used.</param>
        /// <remarks>
        /// This method typically runs in two distinct situations:
        /// <list type="numbered">
        /// <item>
        /// In Visual Studio, when the project is cleaned or prior to rebuilding.
        /// For this to work correctly, the option <c>Register for COM Interop</c>
        /// must be enabled in the project settings.
        /// </item>
        /// <item>During uninstall, when the installer unregisters the assembly from COM Interop.</item>
        /// </list>
        /// This technique should mean that it is never necessary to manually unregister a driver from ASCOM.
        /// </remarks>
        [ComUnregisterFunction]
        public static void UnregisterAscom(Type t)
        {
            RegUnregAscom(false);
        }

        #endregion

        /// <summary>
        /// Use this function to throw an exception if we aren't connected to the hardware
        /// </summary>
        /// <param name="message"></param>
        private void CheckConnected(string message)
        {
            if (!IsConnected)
            {
                throw new NotConnectedException($"Not connected to focuser when trying to execute: {message}");
            }
        }
        #endregion
    }
}
