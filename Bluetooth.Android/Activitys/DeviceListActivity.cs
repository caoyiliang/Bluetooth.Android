using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Bluetooth.Android.Adapter;
using Bluetooth.Android.Receiver;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Bluetooth.Android.Activitys
{
    [Activity(Label = "@string/select_device",
              Theme = "@android:style/Theme.Holo.Dialog",
              ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation)]
    public class DeviceListActivity : Activity
    {
        public const string EXTRA_DEVICE_ADDRESS = "device_address";

        BluetoothAdapter _btAdapter;
        MyBaseAdapter<BluetoothDevice> _pairedDevicesArrayAdapter;
        static MyBaseAdapter<BluetoothDevice> _newDevicesArrayAdapter;
        BluetoothBroadcastReceiver _receiver;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            RequestWindowFeature(WindowFeatures.IndeterminateProgress);
            SetContentView(Resource.Layout.activity_device_list);

            SetResult(Result.Canceled);

            var scanButton = FindViewById<Button>(Resource.Id.button_scan);
            scanButton.Click += (sender, e) =>
            {
                DoDiscovery();
                (sender as View).Visibility = ViewStates.Gone;
            };

            Action<View, BluetoothDevice> action = (v, t) =>
            {
                v.FindViewById<TextView>(Resource.Id.deviceName).Text = t.Name;
                v.FindViewById<TextView>(Resource.Id.deviceAddr).Text = t.Address;
            };
            _pairedDevicesArrayAdapter = new MyBaseAdapter<BluetoothDevice>(this, Resource.Layout.deviceslayout_listview, action);
            _newDevicesArrayAdapter = new MyBaseAdapter<BluetoothDevice>(this, Resource.Layout.deviceslayout_listview, action);

            var pairedListView = FindViewById<ListView>(Resource.Id.paired_devices);
            pairedListView.Adapter = _pairedDevicesArrayAdapter;
            pairedListView.ItemClick += PairedListView_ItemClick; ;

            var newDevicesListView = FindViewById<ListView>(Resource.Id.new_devices);
            newDevicesListView.Adapter = _newDevicesArrayAdapter;
            newDevicesListView.ItemClick += DeviceListView_ItemClick;

            IntentFilter filter = new IntentFilter();
            //filter.AddAction(BluetoothAdapter.ActionRequestEnable);
            //开始查找
            filter.AddAction(BluetoothAdapter.ActionDiscoveryStarted);
            //结束查找
            filter.AddAction(BluetoothAdapter.ActionDiscoveryFinished);
            //查找设备
            filter.AddAction(BluetoothDevice.ActionFound);
            //设备扫描模式改变
            filter.AddAction(BluetoothAdapter.ActionScanModeChanged);
            //绑定状态
            filter.AddAction(BluetoothDevice.ActionBondStateChanged);

            _receiver = new BluetoothBroadcastReceiver();
            _receiver.OnDeviceDiscovered += this.Bbr_OnDeviceDiscovered;
            _receiver.OnDeviceFinish += Receiver_OnDeviceFinish;
            _receiver.OnDeviceBonded += Bbr_OnDeviceBonded;

            RegisterReceiver(_receiver, filter);

            _btAdapter = BluetoothAdapter.DefaultAdapter;

            var pairedDevices = _btAdapter.BondedDevices;

            if (pairedDevices.Count > 0)
            {
                FindViewById(Resource.Id.title_paired_devices).Visibility = ViewStates.Visible;
                _pairedDevicesArrayAdapter.AddRange(pairedDevices);
            }
        }

        private void Receiver_OnDeviceFinish(object sender, EventArgs e)
        {
            SetProgressBarIndeterminateVisibility(false);
            SetTitle(Resource.String.select_device);
        }

        private void Bbr_OnDeviceBonded(object sender, BluetoothDevice e)
        {
            //throw new NotImplementedException();
        }

        private void Bbr_OnDeviceDiscovered(object sender, BluetoothDevice e)
        {
            if (e.Name == null) return;
            if (_newDevicesArrayAdapter.Find(x => x.Address == e.Address) != null) return;
            _newDevicesArrayAdapter.Add(e);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (_btAdapter != null)
            {
                _btAdapter.CancelDiscovery();
            }

            UnregisterReceiver(_receiver);
        }

        void DoDiscovery()
        {
            SetProgressBarIndeterminateVisibility(true);
            SetTitle(Resource.String.scanning);

            FindViewById<View>(Resource.Id.title_new_devices).Visibility = ViewStates.Visible;

            if (_btAdapter.IsDiscovering)
            {
                _btAdapter.CancelDiscovery();
            }

            _btAdapter.StartDiscovery();
        }

        private void PairedListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            _btAdapter.CancelDiscovery();

            var intent = new Intent();
            intent.PutExtra(EXTRA_DEVICE_ADDRESS, _pairedDevicesArrayAdapter[e.Position].Address);

            SetResult(Result.Ok, intent);
            Finish();
        }

        void DeviceListView_ItemClick(object sender, AdapterView.ItemClickEventArgs e)
        {
            _btAdapter.CancelDiscovery();

            var intent = new Intent();
            intent.PutExtra(EXTRA_DEVICE_ADDRESS, _newDevicesArrayAdapter[e.Position].Address);

            SetResult(Result.Ok, intent);
            Finish();
        }
    }
}