using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Bluetooth.Android.Ble;
using Bluetooth.Android.Receiver;
using Communication.Interfaces;
using Java.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Bluetooth.Android.Devices
{
    class Device : IPhysicalPort
    {
        public event EventHandler ConnectEvent;

        private static UUID MY_UUID = UUID.FromString("00001101-0000-1000-8000-00805f9b34fb");

        private static UUID write_UUID_service = UUID.FromString("0000ff12-0000-1000-8000-00805f9b34fb");
        private static UUID write_UUID_chara = UUID.FromString("0000ff01-0000-1000-8000-00805f9b34fb");
        private static UUID read_UUID_service = UUID.FromString("0000ff12-0000-1000-8000-00805f9b34fb");
        private static UUID read_UUID_chara = UUID.FromString("0000ff02-0000-1000-8000-00805f9b34fb");

        private Context _context;
        private BluetoothAdapter _bluetoothAdapter;
        private BluetoothDevice _bluetoothDevice;
        private BluetoothSocket _socket;
        private BluetoothServerSocket _serverSocket;
        private BluetoothGatt _bluetoothGatt;
        private string[] _connectType = new string[] { "SECURE", "INSECURE" };
        private bool _secure = true;
        private bool _isClient = true;
        private TaskCompletionSource<byte[]> _tcsGetValue;
        private TaskCompletionSource<bool> _tcsConnect;
        private TaskCompletionSource<bool> _tcsServerInit;
        private BluetoothBroadcastReceiver _receiver;

        public Device(Context context)
        {
            _isClient = false;
            _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            _context = context;
            bool secure = _secure;
            AlertDialog.Builder builder = new AlertDialog.Builder(_context);
            builder.SetTitle("请选择连接类型");
            builder.SetCancelable(false);
            builder.SetSingleChoiceItems(_connectType, 0, (d, w) =>
            {
                secure = w.Which == 0;
            });
            builder.SetNegativeButton("取消", (IDialogInterfaceOnClickListener)null);
            builder.SetPositiveButton("确定", (d, w) =>
            {
                _secure = secure;
                Toast.MakeText(_context, "蓝牙服务端等待客户端接入...", ToastLength.Long).Show();
                ((AlertDialog)d).Dismiss();
                ConnectEvent?.Invoke(this, null);
            });
            builder.Show();
        }

        public Device(Context context, BluetoothDevice bluetoothDevice)
        {
            _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            _bluetoothDevice = bluetoothDevice;
            _context = context;
            InitClient();
        }

        public void InitClient()
        {
            switch (_bluetoothDevice.Type)
            {
                case BluetoothDeviceType.Classic:
                    bool secure = _secure;
                    AlertDialog.Builder builder = new AlertDialog.Builder(_context);
                    builder.SetTitle("请选择连接类型");
                    builder.SetCancelable(false);
                    builder.SetSingleChoiceItems(_connectType, 0, (d, w) =>
                    {
                        secure = w.Which == 0;
                    });
                    builder.SetNegativeButton("取消", (IDialogInterfaceOnClickListener)null);
                    builder.SetPositiveButton("确定", (d, w) =>
                    {
                        _secure = secure;
                        ConnectEvent?.Invoke(this, null);
                    });
                    builder.Show();
                    break;
                case BluetoothDeviceType.Dual:
                case BluetoothDeviceType.Le:
                    useBle = true;
                    IntentFilter filter = new IntentFilter();
                    //绑定状态
                    filter.AddAction(BluetoothDevice.ActionBondStateChanged);
                    _receiver = new BluetoothBroadcastReceiver();
                    _receiver.OnDeviceBonded += _receiver_OnDeviceBonded;
                    _context.RegisterReceiver(_receiver, filter);
                    if (_bluetoothDevice.BondState != Bond.Bonded)
                    {
                        _bluetoothDevice.CreateBond();
                    }
                    else
                    {
                        ConnectEvent?.Invoke(this, null);
                    }
                    break;
                case BluetoothDeviceType.Unknown:
                    break;
                default:
                    break;
            }
        }

        private void _receiver_OnDeviceBonded(object sender, BluetoothDevice e)
        {
            ConnectEvent?.Invoke(this, null);
        }

        private bool useBle = false;
        private bool isOpen = false;

        public bool IsOpen => useBle ? isOpen : (_socket != null && _socket.IsConnected);

        public async Task CloseAsync()
        {
            _socket.Close();
        }

        public void Dispose()
        {
            CloseAsync().Wait();
            if (_receiver != null)
                _context.UnregisterReceiver(_receiver);
        }

        public async Task OpenAsync()
        {
            if (_isClient)
            {
                if (useBle)
                {
                    await ConnectClientLeAsync();
                    if (_receiver != null)
                        _context.UnregisterReceiver(_receiver);
                }
                else
                {
                    await ConnectClientClassicAsync();
                }
            }
            else
            {
                await ConnectServerClassicAsync();
            }
        }

        private async Task ConnectClientClassicAsync()
        {
            try
            {
                if (_secure)
                {
                    _socket = _bluetoothDevice.CreateRfcommSocketToServiceRecord(MY_UUID);
                }
                else
                {
                    _socket = _bluetoothDevice.CreateInsecureRfcommSocketToServiceRecord(MY_UUID);
                }
            }
            catch (Exception)
            {
                Toast.MakeText(_context, "蓝牙连接初始化失败", ToastLength.Short).Show();
                throw;
            }

            _bluetoothAdapter.CancelDiscovery();

            _socket.Connect();
        }

        private async Task ConnectServerClassicAsync()
        {
            try
            {
                if (_secure)
                {
                    _serverSocket = _bluetoothAdapter.ListenUsingRfcommWithServiceRecord("BluetoothSecure", MY_UUID);
                }
                else
                {
                    _serverSocket = _bluetoothAdapter.ListenUsingInsecureRfcommWithServiceRecord("BluetoothInSecure", MY_UUID);
                }
            }
            catch (Exception)
            {
                Toast.MakeText(_context, "蓝牙连接初始化失败", ToastLength.Short).Show();
                throw;
            }

            await Task.Run(() =>
            {
                _socket = _serverSocket.Accept();
            });
            Toast.MakeText(_context, "蓝牙服务端接收到新客户端", ToastLength.Short).Show();
        }

        private async Task ConnectClientLeAsync()
        {
            var gattCallback = new GattCallback();
            gattCallback.StateChangeEvent += GattCallback_StateChangeEvent;
            gattCallback.ServicesDiscoveredEvent += GattCallback_ServicesDiscoveredEvent;
            gattCallback.GetValueEvent += GattCallback_GetValueEvent;
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                _bluetoothGatt = _bluetoothDevice.ConnectGatt(_context, false, gattCallback, BluetoothTransports.Le);
            }
            else
            {
                _bluetoothGatt = _bluetoothDevice.ConnectGatt(_context, false, gattCallback);
            }
            _tcsConnect = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var ConnectRs = await _tcsConnect.Task;
            _tcsServerInit = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (ConnectRs)
            {
                Toast.MakeText(_context, "低功耗蓝牙已连接", ToastLength.Short).Show();

                await _tcsServerInit.Task;

                BluetoothGattCharacteristic characteristic = _bluetoothGatt.GetService(read_UUID_service).GetCharacteristic(read_UUID_chara);
                _bluetoothGatt.SetCharacteristicNotification(characteristic, true);

                Toast.MakeText(_context, "低功耗蓝牙初始化完成", ToastLength.Short).Show();
            }
            else
            {
                Toast.MakeText(_context, "低功耗蓝牙连接失败", ToastLength.Short).Show();
                throw new Exception("低功耗蓝牙连接失败");
            }
        }

        private void GattCallback_ServicesDiscoveredEvent(object sender, EventArgs e)
        {
            if ((_tcsServerInit != null) && (!_tcsServerInit.Task.IsCompleted))
                _tcsServerInit.TrySetResult(true);
        }

        private void GattCallback_GetValueEvent(object sender, EventArgs e)
        {
            if ((_tcsGetValue != null) && (!_tcsGetValue.Task.IsCompleted))
                _tcsGetValue.TrySetResult((byte[])sender);
        }

        private void GattCallback_StateChangeEvent(object sender, EventArgs e)
        {
            isOpen = (bool)sender;
            if ((_tcsConnect != null) && (!_tcsConnect.Task.IsCompleted))
                _tcsConnect.TrySetResult(isOpen);
        }

        public async Task<ReadDataResult> ReadDataAsync(int count, CancellationToken cancellationToken)
        {
            if (useBle)
            {
                _tcsGetValue = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
                var data = await _tcsGetValue.Task;
                return new ReadDataResult
                {
                    Length = data.Length,
                    Data = data
                };
            }
            else
            {
                byte[] data = new byte[count];
                int Length = await _socket.InputStream.ReadAsync(data, 0, count, cancellationToken);
                return new ReadDataResult
                {
                    Length = Length,
                    Data = data
                };
            }
        }

        public async Task SendDataAsync(byte[] data, CancellationToken cancellationToken)
        {
            if (useBle)
            {
                BluetoothGattService service = _bluetoothGatt.GetService(write_UUID_service);
                BluetoothGattCharacteristic charaWrite = service.GetCharacteristic(write_UUID_chara);
                if (data.Length > 20)
                {
                    //数据大于20个字节 分批次写入
                    int num = 0;
                    if (data.Length % 20 != 0)
                    {
                        num = data.Length / 20 + 1;
                    }
                    else
                    {
                        num = data.Length / 20;
                    }
                    for (int i = 0; i < num; i++)
                    {
                        byte[] tempArr;
                        if (i == num - 1)
                        {
                            tempArr = new byte[data.Length - i * 20];
                            Array.Copy(data, i * 20, tempArr, 0, data.Length - i * 20);
                        }
                        else
                        {
                            tempArr = new byte[20];
                            Array.Copy(data, i * 20, tempArr, 0, 20);
                        }
                        charaWrite.SetValue(tempArr);
                        _bluetoothGatt.WriteCharacteristic(charaWrite);
                    }
                }
                else
                {
                    charaWrite.SetValue(data);
                    _bluetoothGatt.WriteCharacteristic(charaWrite);
                }
            }
            else
            {
                await _socket.OutputStream.WriteAsync(data, 0, data.Length, cancellationToken);
            }
        }
    }
}