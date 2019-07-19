﻿using System.Runtime.InteropServices;
using ASCOM.DeviceInterface;
using System.Collections;
using System.Threading;

namespace ASCOM.Meade.net
{
    #region Rate class
    //
    // The Rate class implements IRate, and is used to hold values
    // for AxisRates. You do not need to change this class.
    //
    // The Guid attribute sets the CLSID for ASCOM.Meade.net.Rate
    // The ClassInterface/None addribute prevents an empty interface called
    // _Rate from being created and used as the [default] interface
    //
    [Guid("288838d1-bbf9-4ce0-9ee1-86ecf38b45c9")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class Rate : IRate
    {
        private double _maximum = 0;
        private double _minimum = 0;

        //
        // Default constructor - Internal prevents public creation
        // of instances. These are values for AxisRates.
        //
        internal Rate(double minimum, double maximum)
        {
            _maximum = maximum;
            _minimum = minimum;
        }

        #region Implementation of IRate

        public void Dispose()
        {
            // TODO Add any required object cleanup here
        }

        public double Maximum
        {
            get => _maximum;
            set => _maximum = value;
        }

        public double Minimum
        {
            get => _minimum;
            set => _minimum = value;
        }

        #endregion
    }
    #endregion

    #region AxisRates
    //
    // AxisRates is a strongly-typed collection that must be enumerable by
    // both COM and .NET. The IAxisRates and IEnumerable interfaces provide
    // this polymorphism. 
    //
    // The Guid attribute sets the CLSID for ASCOM.Meade.net.AxisRates
    // The ClassInterface/None addribute prevents an empty interface called
    // _AxisRates from being created and used as the [default] interface
    //
    [Guid("436de2dd-a77a-41ad-8a9e-14c3695f18f2")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class AxisRates : IAxisRates, IEnumerable
    {
        private TelescopeAxes _axis;
        private readonly Rate[] _rates;

        //
        // Constructor - Internal prevents public creation
        // of instances. Returned by Telescope.AxisRates.
        //
        internal AxisRates(TelescopeAxes axis)
        {
            this._axis = axis;
            //
            // This collection must hold zero or more Rate objects describing the 
            // rates of motion ranges for the Telescope.MoveAxis() method
            // that are supported by your driver. It is OK to leave this 
            // array empty, indicating that MoveAxis() is not supported.
            //
            // Note that we are constructing a rate array for the axis passed
            // to the constructor. Thus we switch() below, and each case should 
            // initialize the array for the rate for the selected axis.
            //
            switch (axis)
            {
                case TelescopeAxes.axisPrimary:
                    // TODO Initialize this array with any Primary axis rates that your driver may provide
                    // Example: m_Rates = new Rate[] { new Rate(10.5, 30.2), new Rate(54.0, 43.6) }
                    //this.rates = new Rate[0];
                    _rates = new Rate[] { new Rate(1, 1), new Rate(2, 2), new Rate(3, 3), new Rate(4, 4) };
                    break;
                case TelescopeAxes.axisSecondary:
                    // TODO Initialize this array with any Secondary axis rates that your driver may provide
                    //this.rates = new Rate[0];
                    _rates = new Rate[] { new Rate(1, 1), new Rate(2, 2), new Rate(3, 3), new Rate(4, 4) };
                    break;
                case TelescopeAxes.axisTertiary:
                    // TODO Initialize this array with any Tertiary axis rates that your driver may provide
                    _rates = new Rate[0];
                    break;
            }
        }

        #region IAxisRates Members

        public int Count => _rates.Length;

        public void Dispose()
        {
            // TODO Add any required object cleanup here
        }

        public IEnumerator GetEnumerator()
        {
            return _rates.GetEnumerator();
        }

        public IRate this[int index] => _rates[index - 1];

        #endregion
    }
    #endregion

    #region TrackingRates
    //
    // TrackingRates is a strongly-typed collection that must be enumerable by
    // both COM and .NET. The ITrackingRates and IEnumerable interfaces provide
    // this polymorphism. 
    //
    // The Guid attribute sets the CLSID for ASCOM.Meade.net.TrackingRates
    // The ClassInterface/None addribute prevents an empty interface called
    // _TrackingRates from being created and used as the [default] interface
    //
    // This class is implemented in this way so that applications based on .NET 3.5
    // will work with this .NET 4.0 object.  Changes to this have proved to be challenging
    // and it is strongly suggested that it isn't changed.
    //
    [Guid("8e9aa30e-ab24-4a20-8af3-4a057defb1ff")]
    [ClassInterface(ClassInterfaceType.None)]
    [ComVisible(true)]
    public class TrackingRates : ITrackingRates, IEnumerable, IEnumerator
    {
        private readonly DriveRates[] _trackingRates;

        // this is used to make the index thread safe
        private readonly ThreadLocal<int> _pos = new ThreadLocal<int>(() => { return -1; });
        private static readonly object LockObj = new object();

        //
        // Default constructor - Internal prevents public creation
        // of instances. Returned by Telescope.AxisRates.
        //
        internal TrackingRates()
        {
            //
            // This array must hold ONE or more DriveRates values, indicating
            // the tracking rates supported by your telescope. The one value
            // (tracking rate) that MUST be supported is driveSidereal!
            //
            _trackingRates = new[] { DriveRates.driveSidereal, DriveRates.driveLunar };
            // TODO Initialize this array with any additional tracking rates that your driver may provide
        }

        #region ITrackingRates Members

        public int Count => _trackingRates.Length;

        public IEnumerator GetEnumerator()
        {
            _pos.Value = -1;
            return this as IEnumerator;
        }

        public void Dispose()
        {
            // TODO Add any required object cleanup here
        }

        public DriveRates this[int index] => _trackingRates[index - 1];

        #endregion

        #region IEnumerable members

        public object Current
        {
            get
            {
                lock (LockObj)
                {
                    if (_pos.Value < 0 || _pos.Value >= _trackingRates.Length)
                    {
                        throw new System.InvalidOperationException();
                    }
                    return _trackingRates[_pos.Value];
                }
            }
        }

        public bool MoveNext()
        {
            lock (LockObj)
            {
                if (++_pos.Value >= _trackingRates.Length)
                {
                    return false;
                }
                return true;
            }
        }

        public void Reset()
        {
            _pos.Value = -1;
        }
        #endregion
    }
    #endregion
}
