//
// ASCOM.Meade.net Local COM Server
//
// This is the core of a managed COM Local Server, capable of serving
// multiple instances of multiple interfaces, within a single
// executable. This implementes the equivalent functionality of VB6
// which has been extensively used in ASCOM for drivers that provide
// multiple interfaces to multiple clients (e.g. Meade Telescope
// and Focuser) as well as hubs (e.g., POTH).
//
// Written by: Robert B. Denny (Version 1.0.1, 29-May-2007)
// Modified by Chris Rowland and Peter Simpson to allow use with multiple devices of the same type March 2011
//
//

using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Principal;
using System.Threading;
using System.Windows.Forms;
using ASCOM.Meade.net.Properties;
using ASCOM.Utilities;
using Microsoft.Win32;

namespace ASCOM.Meade.net
{
    public static class Server
    {

        private const string DriverName = "Meade Generic";

        #region Access to kernel32.dll, user32.dll, and ole32.dll functions

        //// CoInitializeEx() can be used to set the apartment model
        //// of individual threads.
        //[DllImport("ole32.dll")]
        //static extern int CoInitializeEx(IntPtr pvReserved, uint dwCoInit);

        //// CoUninitialize() is used to uninitialize a COM thread.
        //[DllImport("ole32.dll")]
        //static extern void CoUninitialize();

        // PostThreadMessage() allows us to post a Windows Message to
        // a specific thread (identified by its thread id).
        // We will need this API to post a WM_QUIT message to the main 
        // thread in order to terminate this application.
        [DllImport("user32.dll")]
        static extern bool PostThreadMessage(uint idThread, uint msg, UIntPtr wParam,
            IntPtr lParam);

        // GetCurrentThreadId() allows us to obtain the thread id of the
        // calling thread. This allows us to post the WM_QUIT message to
        // the main thread.
        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();
        #endregion

        #region Private Data
        private static int _objsInUse;                       // Keeps a count on the total number of objects alive.
        private static int _serverLocks;                     // Keeps a lock count on this application.
        private static FrmMain _sMainForm;               // Reference to our main form
        //private static ArrayList _sComObjectAssys;              // Dynamically loaded assemblies containing served COM objects
        private static ArrayList _sComObjectTypes;              // Served COM object types
        private static ArrayList _sClassFactories;              // Served COM object class factories
        private static string _sAppId = "{4e68ec46-5ffc-49e7-b298-38a548df0bfd}";	// Our AppId
        private static readonly Object LockObject = new object();
        #endregion

        // This property returns the main thread's id.
        private static uint MainThreadId { get; set; }   // Stores the main thread's thread id.

        // Used to tell if started by COM or manually
        private static bool StartedByCom { get; set; }   // True if server started by COM (-embedding)


        #region Server Lock, Object Counting, and AutoQuit on COM startup
        // Returns the total number of objects alive currently.
        private static int ObjectsCount
        {
            get
            {
                lock (LockObject)
                {
                    return _objsInUse;
                }
            }
        }

        // This method performs a thread-safe incrementation of the objects count.
        public static void CountObject()
        {
            // Increment the global count of objects.
            Interlocked.Increment(ref _objsInUse);
        }

        // This method performs a thread-safe decrementation the objects count.
        public static void UncountObject()
        {
            // Decrement the global count of objects.
            Interlocked.Decrement(ref _objsInUse);
        }

        // Returns the current server lock count.
        private static int ServerLockCount
        {
            get
            {
                lock (LockObject)
                {
                    return _serverLocks;
                }
            }
        }

        // This method performs a thread-safe incrementation the 
        // server lock count.
        public static void CountLock()
        {
            // Increment the global lock count of this server.
            Interlocked.Increment(ref _serverLocks);
        }

        // This method performs a thread-safe decrementation the 
        // server lock count.
        public static void UncountLock()
        {
            // Decrement the global lock count of this server.
            Interlocked.Decrement(ref _serverLocks);
        }

        // AttemptToTerminateServer() will check to see if the objects count and the server 
        // lock count have both dropped to zero.
        //
        // If so, and if we were started by COM, we post a WM_QUIT message to the main thread's
        // message loop. This will cause the message loop to exit and hence the termination 
        // of this application. If hand-started, then just trace that it WOULD exit now.
        //
        public static void ExitIf()
        {
            lock (LockObject)
            {
                if ((ObjectsCount <= 0) && (ServerLockCount <= 0))
                {
                    if (StartedByCom)
                    {
                        UIntPtr wParam = new UIntPtr(0);
                        IntPtr lParam = new IntPtr(0);
                        PostThreadMessage(MainThreadId, 0x0012, wParam, lParam);
                    }
                }
            }
        }
        #endregion

        // -----------------
        // PRIVATE FUNCTIONS
        // -----------------

        #region Dynamic Driver Assembly Loader
        //
        // Load the assemblies that contain the classes that we will serve
        // via COM. These will be located in the same folder as
        // our executable.
        //
        private static bool LoadComObjectAssemblies()
        {
            //_sComObjectAssys = new ArrayList();
            _sComObjectTypes = new ArrayList();

            // put everything into one folder, the same as the server.
            string assyPath = Assembly.GetEntryAssembly()?.Location;
            assyPath = Path.GetDirectoryName(assyPath);
            if (assyPath == null)
                throw new System.InvalidOperationException();

            DirectoryInfo d = new DirectoryInfo(assyPath);
            foreach (FileInfo fi in d.GetFiles("*.dll"))
            {
                string aPath = fi.FullName;
                //
                // First try to load the assembly and get the types for
                // the class and the class factory. If this doesn't work ????
                //
                try
                {
                    Assembly so = Assembly.LoadFrom(aPath);
                    //PWGS Get the types in the assembly
                    Type[] types = so.GetTypes();
                    foreach (Type type in types)
                    {
                        // PWGS Now checks the type rather than the assembly
                        // Check to see if the type has the ServedClassName attribute, only use it if it does.
                        MemberInfo info = type;

                        object[] attrbutes = info.GetCustomAttributes(typeof(ServedClassNameAttribute), false);
                        if (attrbutes.Length > 0)
                        {
                            //MessageBox.Show("Adding Type: " + type.Name + " " + type.FullName);
                            _sComObjectTypes.Add(type); //PWGS - much simpler
                            //_sComObjectAssys.Add(so);
                        }
                    }
                }
                catch (BadImageFormatException)
                {
                    // Probably an attempt to load a Win32 DLL (i.e. not a .net assembly)
                    // Just swallow the exception and continue to the next item.
                }
                catch (Exception e)
                {
                    MessageBox.Show(string.Format(Resources.Server_LoadComObjectAssemblies_Failed_to_load_served_COM_class_assembly__0_____1_, fi.Name, e.Message),
                        DriverName, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return false;
                }
            }
            return true;
        }
        #endregion

        #region COM Registration and Unregistration
        //
        // Test if running elevated
        //
        private static bool IsAdministrator
        {
            get
            {
                WindowsIdentity i = WindowsIdentity.GetCurrent();
                WindowsPrincipal p = new WindowsPrincipal(i);
                return p.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        //
        // Elevate by re-running ourselves with elevation dialog
        //
        private static void ElevateSelf(string arg)
        {
            var si = new ProcessStartInfo
            {
                Arguments = arg,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Application.ExecutablePath,
                Verb = "runas"
            };
            try { Process.Start(si); }
            catch (Win32Exception)
            {
                MessageBox.Show(string.Format(Resources.Server_ElevateSelf_The__0__was_not__1__because_you_did_not_allow_it_, DriverName, (arg == "/register" ? "registered" : "unregistered")), DriverName, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), DriverName, MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        //
        // Do everything to register this for COM. Never use REGASM on
        // this exe assembly! It would create InProcServer32 entries 
        // which would prevent proper activation!
        //
        // Using the list of COM object types generated during dynamic
        // assembly loading, it registers each one for COM as served by our
        // exe/local server, as well as registering it for ASCOM. It also
        // adds DCOM info for the local server itself, so it can be activated
        // via an outboiud connection from TheSky.
        //
        private static void RegisterObjects()
        {
            if (!IsAdministrator)
            {
                ElevateSelf("/register");
                return;
            }
            //
            // If reached here, we're running elevated
            //

            Assembly assy = Assembly.GetExecutingAssembly();
            Attribute attr = Attribute.GetCustomAttribute(assy, typeof(AssemblyTitleAttribute));
            string assyTitle = ((AssemblyTitleAttribute)attr).Title;
            attr = Attribute.GetCustomAttribute(assy, typeof(AssemblyDescriptionAttribute));
            string assyDescription = ((AssemblyDescriptionAttribute)attr).Description;

            //
            // Local server's DCOM/AppID information
            //
            try
            {
                //
                // HKCR\APPID\appid
                //
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey("APPID\\" + _sAppId))
                {
                    key?.SetValue(null, assyDescription);
                    key?.SetValue("AppID", _sAppId);
                    key?.SetValue("AuthenticationLevel", 1, RegistryValueKind.DWord);
                }
                //
                // HKCR\APPID\exename.ext
                //
                using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(
                    $"APPID\\{Application.ExecutablePath.Substring(Application.ExecutablePath.LastIndexOf('\\') + 1)}"))
                {
                    key?.SetValue("AppID", _sAppId);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(string.Format(Resources.Server_RegisterObjects_, ex),
                    DriverName, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                return;
            }

            //
            // For each of the driver assemblies
            //
            foreach (Type type in _sComObjectTypes)
            {
                bool bFail = false;
                try
                {
                    //
                    // HKCR\CLSID\clsid
                    //
                    string clsid = Marshal.GenerateGuidForType(type).ToString("B");
                    string progid = Marshal.GenerateProgIdForType(type);
                    //PWGS Generate device type from the Class name
                    string deviceType = type.Name;

                    using (RegistryKey key = Registry.ClassesRoot.CreateSubKey($"CLSID\\{clsid}"))
                    {
                        key?.SetValue(null, progid);						// Could be assyTitle/Desc??, but .NET components show ProgId here
                        key?.SetValue("AppId", _sAppId);
                        if (key != null)
                        {
                            using (RegistryKey key2 = key.CreateSubKey("Implemented Categories"))
                            {
                                key2?.CreateSubKey("{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}");
                            }

                            using (RegistryKey key2 = key.CreateSubKey("ProgId"))
                            {
                                key2?.SetValue(null, progid);
                            }

                            key.CreateSubKey("Programmable");
                            using (RegistryKey key2 = key.CreateSubKey("LocalServer32"))
                            {
                                key2?.SetValue(null, Application.ExecutablePath);
                            }
                        }
                    }
                    //
                    // HKCR\CLSID\progid
                    //
                    using (RegistryKey key = Registry.ClassesRoot.CreateSubKey(progid))
                    {
                        key?.SetValue(null, assyTitle);
                        using (RegistryKey key2 = key.CreateSubKey("CLSID"))
                        {
                            key2?.SetValue(null, clsid);
                        }
                    }
                    //
                    // ASCOM 
                    //
                    //assy = type.Assembly;

                    // Pull the display name from the ServedClassName attribute.
                    attr = Attribute.GetCustomAttribute(type, typeof(ServedClassNameAttribute)); //PWGS Changed to search type for attribute rather than assembly
                    string chooserName = ((ServedClassNameAttribute)attr).DisplayName ?? "MultiServer";
                    using (var p = new Profile())
                    {
                        p.DeviceType = deviceType;
                        p.Register(progid, chooserName);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(string.Format(Resources.Server_RegisterObjects_, ex),
                        DriverName, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    bFail = true;
                }

                if (bFail) break;
            }
        }

        //
        // Remove all traces of this from the registry. 
        //
        // If the above does AppID/DCOM stuff, this would have
        // to remove that stuff too.
        //
        private static void UnregisterObjects()
        {
            if (!IsAdministrator)
            {
                ElevateSelf("/unregister");
                return;
            }

            //
            // Local server's DCOM/AppID information
            //
            Registry.ClassesRoot.DeleteSubKey($"APPID\\{_sAppId}", false);
            Registry.ClassesRoot.DeleteSubKey(
                $"APPID\\{Application.ExecutablePath.Substring(Application.ExecutablePath.LastIndexOf('\\') + 1)}", false);

            //
            // For each of the driver assemblies
            //
            foreach (Type type in _sComObjectTypes)
            {
                string clsid = Marshal.GenerateGuidForType(type).ToString("B");
                string progid = Marshal.GenerateProgIdForType(type);
                string deviceType = type.Name;
                //
                // Best efforts
                //
                //
                // HKCR\progid
                //
                Registry.ClassesRoot.DeleteSubKey($"{progid}\\CLSID", false);
                Registry.ClassesRoot.DeleteSubKey(progid, false);
                //
                // HKCR\CLSID\clsid
                //
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsid}\\Implemented Categories\\{{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}}", false);
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsid}\\Implemented Categories", false);
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsid}\\ProgId", false);
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsid}\\LocalServer32", false);
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsid}\\Programmable", false);
                Registry.ClassesRoot.DeleteSubKey($"CLSID\\{clsid}", false);
                try
                {
                    //
                    // ASCOM
                    //
                    using (var p = new Profile())
                    {
                        p.DeviceType = deviceType;
                        p.Unregister(progid);
                    }
                }
                catch (Exception)
                {
                    // ignored
                }
            }
        }
        #endregion

        #region Class Factory Support
        //
        // On startup, we register the class factories of the COM objects
        // that we serve. This requires the class facgtory name to be
        // equal to the served class name + "ClassFactory".
        //
        private static void RegisterClassFactories()
        {
            _sClassFactories = new ArrayList();
            foreach (Type type in _sComObjectTypes)
            {
                ClassFactory factory = new ClassFactory(type);                  // Use default context & flags
                _sClassFactories.Add(factory);
                if (!factory.RegisterClassObject())
                {
                    MessageBox.Show(string.Format(Resources.Server_RegisterClassFactories_Failed_to_register_class_factory_for__0_, type.Name),
                        DriverName, MessageBoxButtons.OK, MessageBoxIcon.Stop);
                    return;
                }
            }
            ClassFactory.ResumeClassObjects();                                  // Served objects now go live
        }

        private static void RevokeClassFactories()
        {
            ClassFactory.SuspendClassObjects();                                 // Prevent race conditions
            foreach (ClassFactory factory in _sClassFactories)
                factory.RevokeClassObject();
        }
        #endregion

        #region Command Line Arguments
        //
        // ProcessArguments() will process the command-line arguments
        // If the return value is true, we carry on and start this application.
        // If the return value is false, we terminate this application immediately.
        //
        private static bool ProcessArguments(string[] args)
        {
            bool bRet = true;

            //
            // -Embedding is "ActiveX start". Prohibit non_AX starting?
            //
            if (args.Length > 0)
            {

                switch (args[0].ToLower())
                {
                    case "-embedding":
                        StartedByCom = true;                                        // Indicate COM started us
                        break;

                    case "-register":
                    case @"/register":
                    case "-regserver":                                          // Emulate VB6
                    case @"/regserver":
                        RegisterObjects();                                      // Register each served object
                        bRet = false;
                        break;

                    case "-unregister":
                    case @"/unregister":
                    case "-unregserver":                                        // Emulate VB6
                    case @"/unregserver":
                        UnregisterObjects();                                    //Unregister each served object
                        bRet = false;
                        break;

                    default:
                        MessageBox.Show(
                            string.Format(Resources.Server_ProcessArguments_, args[0]),
                            DriverName, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        break;
                }
            }
            else
                StartedByCom = false;

            return bRet;
        }
        #endregion

        #region SERVER ENTRY POINT (main)
        //
        // ==================
        // SERVER ENTRY POINT
        // ==================
        //
        [STAThread]
        static void Main(string[] args)
        {
            if (!LoadComObjectAssemblies()) return;                     // Load served COM class assemblies, get types

            if (!ProcessArguments(args)) return;                        // Register/Unregister

            // Initialize critical member variables.
            _objsInUse = 0;
            _serverLocks = 0;
            MainThreadId = GetCurrentThreadId();
            Thread.CurrentThread.Name = "Main Thread";

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            _sMainForm = new FrmMain();
            if (StartedByCom) _sMainForm.WindowState = FormWindowState.Minimized;

            // Register the class factories of the served objects
            RegisterClassFactories();

            // Start up the garbage collection thread.
            var garbageCollector = new GarbageCollection(1000);
            var gcThread = new Thread(garbageCollector.GcWatch)
            {
                Name = "Garbage Collection Thread"
            };
            gcThread.Start();

            //
            // Start the message loop. This serializes incoming calls to our
            // served COM objects, making this act like the VB6 equivalent!
            //
            try
            {
                Application.Run(_sMainForm);
            }
            finally
            {
                // Revoke the class factories immediately.
                // Don't wait until the thread has stopped before
                // we perform revocation!!!
                RevokeClassFactories();

                // Now stop the Garbage Collector thread.
                garbageCollector.StopThread();
                garbageCollector.WaitForThreadToStop();
            }
        }
        #endregion
    }
}
