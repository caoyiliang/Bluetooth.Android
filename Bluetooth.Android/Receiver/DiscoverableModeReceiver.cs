using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bluetooth.Android.Receiver
{
    public class BluetoothDiscoveryModeArgs : EventArgs
    {
        public BluetoothDiscoveryModeArgs(bool inDiscoveryMode)
        {
            InDiscoveryMode = inDiscoveryMode;
        }
        public bool InDiscoveryMode { get; private set; }
    }

    public class DiscoverableModeReceiver : BroadcastReceiver
    {
        public event EventHandler<BluetoothDiscoveryModeArgs> BluetoothDiscoveryModeChanged;


        public override void OnReceive(Context context, Intent intent)
        {
            var currentScanMode = intent.GetIntExtra(BluetoothAdapter.ExtraScanMode, -1);
            var previousScanMode = intent.GetIntExtra(BluetoothAdapter.ExtraPreviousScanMode, -1);


            bool inDiscovery = currentScanMode == (int)ScanMode.ConnectableDiscoverable;

            BluetoothDiscoveryModeChanged?.Invoke(this, new BluetoothDiscoveryModeArgs(inDiscovery));
        }
    }
}