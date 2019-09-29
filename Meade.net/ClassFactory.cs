using System;
using System.Collections;
using System.Runtime.InteropServices;

namespace ASCOM.Meade.net
{

    #region C# Definition of IClassFactory
    //
    // Provide a definition of theCOM IClassFactory interface.
    //
    [
      ComImport,                                                // This interface originated from COM.
      ComVisible(false),                                        // Must not be exposed to COM!!!
      InterfaceType(ComInterfaceType.InterfaceIsIUnknown),      // Indicate that this interface is not IDispatch-based.
      Guid("00000001-0000-0000-C000-000000000046")              // This GUID is the actual GUID of IClassFactory.
    ]
    public interface IClassFactory
    {
        void CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject);
        void LockServer(bool fLock);
    }
    #endregion

    //
    // Universal ClassFactory. Given a type as a parameter of the 
    // constructor, it implements IClassFactory for any interface
    // that the class implements. Magic!!!
    //
    public class ClassFactory : IClassFactory
    {

        #region Access to ole32.dll functions for class factories

        // Define two common GUID objects for public usage.
        private static readonly Guid IidIUnknown = new Guid("{00000000-0000-0000-C000-000000000046}");
        private static readonly Guid IidIDispatch = new Guid("{00020400-0000-0000-C000-000000000046}");

        [Flags]
        enum Clsctx : uint
        {
            ClsctxInprocServer = 0x1,
            ClsctxInprocHandler = 0x2,
            ClsctxLocalServer = 0x4,
            ClsctxInprocServer16 = 0x8,
            ClsctxRemoteServer = 0x10,
            ClsctxInprocHandler16 = 0x20,
            ClsctxReserved1 = 0x40,
            ClsctxReserved2 = 0x80,
            ClsctxReserved3 = 0x100,
            ClsctxReserved4 = 0x200,
            ClsctxNoCodeDownload = 0x400,
            ClsctxReserved5 = 0x800,
            ClsctxNoCustomMarshal = 0x1000,
            ClsctxEnableCodeDownload = 0x2000,
            ClsctxNoFailureLog = 0x4000,
            ClsctxDisableAaa = 0x8000,
            ClsctxEnableAaa = 0x10000,
            ClsctxFromDefaultContext = 0x20000,
            ClsctxInproc = ClsctxInprocServer | ClsctxInprocHandler,
            ClsctxServer = ClsctxInprocServer | ClsctxLocalServer | ClsctxRemoteServer,
            ClsctxAll = ClsctxServer | ClsctxInprocHandler
        }

        [Flags]
        enum Regcls : uint
        {
            RegclsSingleuse = 0,
            RegclsMultipleuse = 1,
            RegclsMultiSeparate = 2,
            RegclsSuspended = 4,
            RegclsSurrogate = 8
        }
        //
        // CoRegisterClassObject() is used to register a Class Factory
        // into COM's internal table of Class Factories.
        //
        [DllImport("ole32.dll")]
        static extern int CoRegisterClassObject(
            [In] ref Guid rclsid,
            [MarshalAs(UnmanagedType.IUnknown)] object pUnk,
            uint dwClsContext,
            uint flags,
            out uint lpdwRegister);
        //
        // Called by a COM EXE Server that can register multiple class objects 
        // to inform COM about all registered classes, and permits activation 
        // requests for those class objects. 
        // This function causes OLE to inform the SCM about all the registered 
        // classes, and begins letting activation requests into the server process.
        //
        [DllImport("ole32.dll")]
        static extern int CoResumeClassObjects();
        //
        // Prevents any new activation requests from the SCM on all class objects
        // registered within the process. Even though a process may call this API, 
        // the process still must call CoRevokeClassObject for each CLSID it has 
        // registered, in the apartment it registered in.
        //
        [DllImport("ole32.dll")]
        static extern int CoSuspendClassObjects();
        //
        // CoRevokeClassObject() is used to unregister a Class Factory
        // from COM's internal table of Class Factories.
        //
        [DllImport("ole32.dll")]
        static extern int CoRevokeClassObject(uint dwRegister);
        #endregion

        #region Constructor and Private ClassFactory Data

        private readonly Type _mClassType;
        private Guid _mClassId;
        private readonly ArrayList _mInterfaceTypes;
        private uint _mCookie;
        //private readonly string _mProgid;

        public ClassFactory(Type type)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            _mClassType = type;

            //PWGS Get the ProgID from the MetaData
            //_mProgid = Marshal.GenerateProgIdForType(type);
            _mClassId = Marshal.GenerateGuidForType(type);		// Should be nailed down by [Guid(...)]
            ClassContext = (uint)Clsctx.ClsctxLocalServer;	// Default
            Flags = (uint)Regcls.RegclsMultipleuse |			// Default
                        (uint)Regcls.RegclsSuspended;
            _mInterfaceTypes = new ArrayList();
            foreach (Type T in type.GetInterfaces())            // Save all of the implemented interfaces
                _mInterfaceTypes.Add(T);
        }

        #endregion

        #region Common ClassFactory Methods

        private uint ClassContext { get; }

        public Guid ClassId
        {
            get => _mClassId;
            set => _mClassId = value;
        }

        private uint Flags { get; }

        public bool RegisterClassObject()
        {
            // Register the class factory
            int i = CoRegisterClassObject
                (
                ref _mClassId,
                this,
                ClassContext,
                Flags,
                out _mCookie
                );
            return i == 0;
        }

        public bool RevokeClassObject()
        {
            int i = CoRevokeClassObject(_mCookie);
            return i == 0;
        }

        public static bool ResumeClassObjects()
        {
            int i = CoResumeClassObjects();
            return i == 0;
        }

        public static bool SuspendClassObjects()
        {
            int i = CoSuspendClassObjects();
            return i == 0;
        }
        #endregion

        #region IClassFactory Implementations
        //
        // Implement creation of the type and interface.
        //
        void IClassFactory.CreateInstance(IntPtr pUnkOuter, ref Guid riid, out IntPtr ppvObject)
        {
            IntPtr nullPtr = new IntPtr(0);
            ppvObject = nullPtr;

            //
            // Handle specific requests for implemented interfaces
            //
            foreach (Type iType in _mInterfaceTypes)
            {
                if (riid == Marshal.GenerateGuidForType(iType))
                {
                    ppvObject = Marshal.GetComInterfaceForObject(Activator.CreateInstance(_mClassType), iType);
                    return;
                }
            }
            //
            // Handle requests for IDispatch or IUnknown on the class
            //
            if (riid == IidIDispatch)
            {
                ppvObject = Marshal.GetIDispatchForObject(Activator.CreateInstance(_mClassType));
            }
            else if (riid == IidIUnknown)
            {
                ppvObject = Marshal.GetIUnknownForObject(Activator.CreateInstance(_mClassType));
            }
            else
            {
                //
                // Oops, some interface that the class doesn't implement
                //
                throw new COMException("No interface", unchecked((int)0x80004002));
            }
        }

        void IClassFactory.LockServer(bool bLock)
        {
            if (bLock)
                Server.CountLock();
            else
                Server.UncountLock();
            // Always attempt to see if we need to shutdown this server application.
            Server.ExitIf();
        }
        #endregion
    }
}
