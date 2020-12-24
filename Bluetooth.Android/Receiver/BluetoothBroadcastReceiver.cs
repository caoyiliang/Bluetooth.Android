using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace Bluetooth.Android.Receiver
{
    public class BluetoothBroadcastReceiver : BroadcastReceiver
    {
        public event EventHandler<BluetoothDevice> OnDeviceDiscovered;
        public event EventHandler OnDeviceFinish;
        public event EventHandler<BluetoothDevice> OnDeviceBonded;
        public override void OnReceive(Context context, Intent intent)
        {
            var action = intent.Action;

            if (BluetoothAdapter.ActionDiscoveryStarted.Equals(action))
            {
                Toast.MakeText(context, "Start Discovery", ToastLength.Short).Show();
            }
            else if (BluetoothAdapter.ActionDiscoveryFinished.Equals(action))
            {
                Toast.MakeText(context, "Finish Discovery", ToastLength.Short).Show();
                OnDeviceFinish?.Invoke(this, null);
            }
            else if (BluetoothDevice.ActionFound.Equals(action))
            {
                var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                if (device.BondState != Bond.Bonded)
                {
                    OnDeviceDiscovered?.Invoke(this, device);
                }
            }
            else if (BluetoothDevice.ActionBondStateChanged.Equals(action))
            {
                var device = (BluetoothDevice)intent.GetParcelableExtra(BluetoothDevice.ExtraDevice);
                if (device.BondState == Bond.Bonded)
                {
                    OnDeviceBonded?.Invoke(this, device);
                }
            }
        }
    }
}