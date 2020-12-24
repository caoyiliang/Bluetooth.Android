using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Views.InputMethods;
using Android.Widget;
using AndroidX.AppCompat.App;
using AndroidX.Fragment.App;
using Bluetooth.Android.Activitys;
using Bluetooth.Android.Devices;
using Bluetooth.Android.Receiver;
using Google.Android.Material.Snackbar;
using Java.Util;
using Parser.Parsers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using TopPortLib;
using TopPortLib.Interfaces;
using Utils;
using appResult = Android.App.Result;

namespace Bluetooth.Android.Fragments
{
    public class CommunicationFragment : Fragment
    {
        const int REQUEST_CONNECT_DEVICE = 1;
        const int REQUEST_ENABLE_BT = 2;

        private BluetoothAdapter _bluetoothAdapter;
        Device _device;
        ITopPort _topPort;
        private DiscoverableModeReceiver _receiver;

        private ListView conversationView;
        private EditText outEditText;
        private Button sendButton;

        private ArrayAdapter<string> conversationArrayAdapter;

        private string[] _eType = new string[] { "UTF-8", "Hex" };
        private DataEncode _EType = DataEncode.UTF8;

        WriteListener writeListener;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            HasOptionsMenu = true;
            _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;

            _receiver = new DiscoverableModeReceiver();
            _receiver.BluetoothDiscoveryModeChanged += (sender, e) =>
            {
                Activity.InvalidateOptionsMenu();
            };

            if (_bluetoothAdapter == null)
            {
                Toast.MakeText(Activity, "Bluetooth is not available.", ToastLength.Long).Show();
                Activity.FinishAndRemoveTask();
            }

            writeListener = new WriteListener(this);
        }
        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            var view = inflater.Inflate(Resource.Layout.layout_Communication, container, false);

            conversationView = view.FindViewById<ListView>(Resource.Id.@in);
            conversationArrayAdapter = new ArrayAdapter<string>(Context, Resource.Layout.view_message);
            conversationView.Adapter = conversationArrayAdapter;

            outEditText = view.FindViewById<EditText>(Resource.Id.edit_text_out);
            outEditText.SetOnEditorActionListener(writeListener);

            sendButton = view.FindViewById<Button>(Resource.Id.button_send);
            sendButton.Click += async (sender, e) =>
            {
                var msg = outEditText.Text;
                await SendMessage(msg);
            };
            return view;
        }

        public override void OnStart()
        {
            base.OnStart();

            BluetoothInit();
        }

        private void BluetoothInit()
        {
            if (!Activity.HasLocationPermissions())
            {
                this.RequestPermissionsForApp();
                return;
            }

            if (!_bluetoothAdapter.IsEnabled)
            {
                var enableIntent = new Intent(BluetoothAdapter.ActionRequestEnable);
                StartActivityForResult(enableIntent, REQUEST_ENABLE_BT);
            }

            var filter = new IntentFilter(BluetoothAdapter.ActionScanModeChanged);
            Activity.RegisterReceiver(_receiver, filter);
        }

        public override void OnPrepareOptionsMenu(IMenu menu)
        {
            var menuItem = menu.FindItem(Resource.Id.discoverable);
            if (menuItem != null)
            {
                menuItem.SetEnabled(_bluetoothAdapter.ScanMode == ScanMode.ConnectableDiscoverable);
            }
        }

        public override void OnCreateOptionsMenu(IMenu menu, MenuInflater inflater)
        {
            inflater.Inflate(Resource.Menu.menu_main, menu);
        }

        public override bool OnOptionsItemSelected(IMenuItem item)
        {
            switch (item.ItemId)
            {
                case Resource.Id.discovery:
                    Discovery();
                    return true;
                case Resource.Id.discoverable:
                    EnsureDiscoverable();
                    return true;
                case Resource.Id.server:
                    EnsureDiscoverable();
                    _device = new Device(Context);
                    _device.ConnectEvent += _device_ConnectEvent;
                    _topPort = new TopPort(_device, new TimeParser(200));
                    _topPort.OnReceiveParsedData += _topPort_OnReceiveParsedData;
                    return true;
                case Resource.Id.EType:
                    AlertDialog.Builder builder = new AlertDialog.Builder(Context);
                    builder.SetTitle("请选择数据类型");
                    builder.SetSingleChoiceItems(_eType, (int)_EType, (d, w) =>
                    {
                        _EType = (DataEncode)w.Which;
                        ((IDialogInterface)d).Dismiss();
                    });
                    builder.Show();
                    return true;
                case Resource.Id.about:
                    Snackbar.Make(View, "宇宙联盟", Snackbar.LengthShort).SetAction("Action", (View.IOnClickListener)null).Show();
                    return true;
                default:
                    break;
            }

            return base.OnOptionsItemSelected(item);
        }

        private void Discovery()
        {
            var intent = new Intent(Activity, typeof(DeviceListActivity));
            StartActivityForResult(intent, REQUEST_CONNECT_DEVICE);
        }

        public override async void OnActivityResult(int requestCode, int resultCode, Intent data)
        {
            switch (requestCode)
            {
                case REQUEST_CONNECT_DEVICE:
                    if ((int)appResult.Ok == resultCode)
                    {
                        var address = data.Extras.GetString(DeviceListActivity.EXTRA_DEVICE_ADDRESS);
                        var device = _bluetoothAdapter.GetRemoteDevice(address);
                        _device = new Device(Context, device);
                        _device.ConnectEvent += _device_ConnectEvent;
                        _device.InitClient();
                        _topPort = new TopPort(_device, new TimeParser(200));
                        _topPort.OnReceiveParsedData += _topPort_OnReceiveParsedData;
                        _device.InitClient();
                    }
                    break;
                case REQUEST_ENABLE_BT:
                    if ((int)appResult.Ok != resultCode)
                    {
                        Toast.MakeText(Activity, Resource.String.bt_not_enabled_leaving, ToastLength.Short).Show();
                        Activity.FinishAndRemoveTask();
                    }
                    break;
            }
        }

        private async void _device_ConnectEvent(object sender, EventArgs e)
        {
            try
            {
                await _topPort.OpenAsync();
            }
            catch (Exception)
            {
                Toast.MakeText(Context, "蓝牙连接失败", ToastLength.Short).Show();
            }
        }

        private async Task _topPort_OnReceiveParsedData(byte[] data)
        {
            View.Post(() =>
            {
                switch (_EType)
                {
                    case DataEncode.UTF8:
                        conversationArrayAdapter.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} 收到:\n{Encoding.UTF8.GetString(data)}");
                        break;
                    case DataEncode.Hex:
                        conversationArrayAdapter.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} 收到:\n{StringByteUtils.BytesToString(data)}");
                        break;
                    default:
                        break;
                }
            });
        }

        private async Task SendMessage(string message)
        {
            if ((_topPort == null) || (!_device.IsOpen))
            {
                Toast.MakeText(Context, "蓝牙未连接", ToastLength.Short).Show();
                return;
            }
            if (message.Length > 0)
            {
                var bytes = new byte[0];
                switch (_EType)
                {
                    case DataEncode.UTF8:
                        bytes = Encoding.UTF8.GetBytes(message);
                        break;
                    case DataEncode.Hex:
                        bytes = StringByteUtils.StringToBytes(message);
                        break;
                    default:
                        break;
                }

                await _topPort.SendAsync(bytes);
                conversationArrayAdapter.Add($"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff} 发送:\n{message}");
            }
        }

        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
        {
            var allGranted = grantResults.AllPermissionsGranted();
            if (requestCode == PermissionUtils.RC_LOCATION_PERMISSIONS)
            {
                BluetoothInit();
            }
        }

        void EnsureDiscoverable()
        {
            if (_bluetoothAdapter.ScanMode != ScanMode.ConnectableDiscoverable)
            {
                var discoverableIntent = new Intent(BluetoothAdapter.ActionRequestDiscoverable);
                discoverableIntent.PutExtra(BluetoothAdapter.ExtraDiscoverableDuration, 300);
                StartActivity(discoverableIntent);
            }
        }

        /// <summary>
        /// Listen for return key being pressed.
        /// </summary>
        class WriteListener : Java.Lang.Object, TextView.IOnEditorActionListener
        {
            CommunicationFragment host;
            public WriteListener(CommunicationFragment frag)
            {
                host = frag;
            }
            public bool OnEditorAction(TextView v, [GeneratedEnum] ImeAction actionId, KeyEvent e)
            {
                if (actionId == ImeAction.ImeNull && e.Action == KeyEventActions.Up)
                {
                    host.SendMessage(v.Text).Wait();
                }
                return true;
            }
        }
    }
}