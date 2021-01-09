using Android.App;
using Android.Bluetooth;
using Android.Bluetooth.LE;
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

        private static UUID UUID_service = UUID.FromString("0000ff12-0000-1000-8000-00805f9b34fb");
        private static UUID write_UUID_chara = UUID.FromString("0000ff01-0000-1000-8000-00805f9b34fb");
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

        private BluetoothLeAdvertiser _mBluetoothLeAdvertiser;
        private LeAdvertiseCallback _Lac;
        private BluetoothManager _bluetoothManager;
        private BluetoothGattCharacteristic mGattReadCharacteristic;
        private BluetoothGattServer mBluetoothGattServer;

        private bool useBle = false;
        private bool isOpen = false;
        public bool IsOpen => useBle ? isOpen : (_socket != null && _socket.IsConnected);

        public Device(Context context, bool useBle = false)
        {
            _isClient = false;
            _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            _context = context;
            this.useBle = useBle;
            _bluetoothAdapter.SetName("宇宙联盟");
        }

        public void InitSevice()
        {
            if (useBle)
            {
                _bluetoothManager = (BluetoothManager)_context.GetSystemService(Context.BluetoothService);
                _Lac = new LeAdvertiseCallback();
                _Lac.StartStatusEvent += Lac_StartStatusEvent;
                _Lac.StartSuccessEvent += Lac_StartSuccessEvent;
                _Lac.StartFailureEvent += Lac_StartFailureEvent;
                ConnectEvent?.Invoke(this, null);
            }
            else
            {
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
                    ToastUtils.ToastUtils.ShowToast(_context, "蓝牙服务端等待客户端接入...", ToastLength.Long);
                    ((AlertDialog)d).Dismiss();
                    ConnectEvent?.Invoke(this, null);
                });
                builder.Show();
            }
        }

        public Device(Context context, BluetoothDevice bluetoothDevice)
        {
            _bluetoothAdapter = BluetoothAdapter.DefaultAdapter;
            _bluetoothAdapter.SetName("宇宙联盟");
            _bluetoothDevice = bluetoothDevice;
            _context = context;
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

        public async Task CloseAsync()
        {
            if (useBle && (!_isClient))
            {
                _mBluetoothLeAdvertiser?.StopAdvertising(_Lac);
            }
            else { _socket?.Close(); }
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
                if (useBle)
                {
                    await ConnectServerLeAsync();
                }
                else
                {
                    await ConnectServerClassicAsync();
                }
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
                ToastUtils.ToastUtils.ShowToast(_context, "蓝牙连接初始化失败");
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
                ToastUtils.ToastUtils.ShowToast(_context, "蓝牙连接初始化失败");
                throw;
            }

            await Task.Run(() =>
            {
                _socket = _serverSocket.Accept();
            });
            ToastUtils.ToastUtils.ShowToast(_context, "蓝牙服务端接收到新客户端");
        }

        private async Task ConnectClientLeAsync()
        {
            var gattCallback = new GattCallback();
            gattCallback.StateChangeEvent += GattCallback_StateChangeEvent;
            gattCallback.ServicesDiscoveredEvent += GattCallback_ServicesDiscoveredEvent;
            gattCallback.GetValueEvent += GattCallback_GetValueEvent;
            _tcsConnect = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                _bluetoothGatt = _bluetoothDevice.ConnectGatt(_context, false, gattCallback, BluetoothTransports.Le);
            }
            else
            {
                _bluetoothGatt = _bluetoothDevice.ConnectGatt(_context, false, gattCallback);
            }
            var ConnectRs = await _tcsConnect.Task;
            _tcsServerInit = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            if (ConnectRs)
            {
                ToastUtils.ToastUtils.ShowToast(_context, "低功耗蓝牙已连接");

                await _tcsServerInit.Task;

                BluetoothGattCharacteristic characteristic = _bluetoothGatt.GetService(UUID_service).GetCharacteristic(read_UUID_chara);
                _bluetoothGatt.SetCharacteristicNotification(characteristic, true);

                ToastUtils.ToastUtils.ShowToast(_context, "低功耗蓝牙初始化完成");
            }
            else
            {
                ToastUtils.ToastUtils.ShowToast(_context, "低功耗蓝牙连接失败");
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

        private async Task ConnectServerLeAsync()
        {
            //初始化广播设置
            var mAdvertiseSettings = new AdvertiseSettings.Builder()
                   //设置广播模式，以控制广播的功率和延迟。 ADVERTISE_MODE_LOW_LATENCY为高功率，低延迟
                   .SetAdvertiseMode(AdvertiseMode.LowLatency)
                   //设置蓝牙广播发射功率级别
                   .SetTxPowerLevel(AdvertiseTx.PowerHigh)
                   //广播时限。最多180000毫秒。值为0将禁用时间限制。（不设置则为无限广播时长）
                   .SetTimeout(0)
                   //设置广告类型是可连接还是不可连接。
                   .SetConnectable(true)
                   .Build();

            //设置广播报文
            var mAdvertiseData = new AdvertiseData.Builder()
                //设置广播包中是否包含设备名称。
                .SetIncludeDeviceName(true)
                //设置广播包中是否包含发射功率
                .SetIncludeTxPowerLevel(true)
                //设置UUID
                .AddServiceUuid(new ParcelUuid(UUID_service))
                .Build();

            //设置广播扫描响应报文(可选)
            //var mScanResponseData = new AdvertiseData.Builder()
            //      //自定义服务数据，将其转化为字节数组传入
            //      //.AddServiceData(new ParcelUuid(UUID_SERVICE), new byte[] { 2, 3, 4 })
            //      //设备厂商自定义数据，将其转化为字节数组传入
            //      //.AddManufacturerData(0x06, new byte[] { 1, 2, 3 })
            //      .Build();

            //获取BLE广播操作对象
            //官网建议获取mBluetoothLeAdvertiser时，先做mBluetoothAdapter.isMultipleAdvertisementSupported判断，
            // 但部分华为手机支持Ble广播却还是返回false,所以最后以mBluetoothLeAdvertiser是否不为空且蓝牙打开为准
            _mBluetoothLeAdvertiser = _bluetoothAdapter.BluetoothLeAdvertiser;
            //蓝牙关闭或者不支持
            if (_mBluetoothLeAdvertiser != null && _bluetoothAdapter.IsEnabled)
            {
                //开始广播（不附带扫描响应报文）
                //mBluetoothLeAdvertiser.startAdvertising(mAdvertiseSettings, mAdvertiseData, mAdvertiseCallback)
                //开始广播（附带扫描响应报文）
                _tcsConnect = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _mBluetoothLeAdvertiser?.StartAdvertising(mAdvertiseSettings, mAdvertiseData, _Lac);
                if (!await _tcsConnect.Task) throw new Exception("StartAdvertise Error");

                //初始化Service
                //创建服务，并初始化服务的UUID和服务类型。
                //BluetoothGattService.SERVICE_TYPE_PRIMARY 为主要服务类型
                var mGattService = new BluetoothGattService(UUID_service, GattServiceType.Primary);
                //初始化特征(添加写权限)
                //在服务端配置特征时，设置BluetoothGattCharacteristic.PROPERTY_WRITE_NO_RESPONSE,
                //那么onCharacteristicWriteRequest()回调时，不需要GattServer进行response才能进行响应。
                var mGattCharacteristic = new BluetoothGattCharacteristic(write_UUID_chara, GattProperty.WriteNoResponse, GattPermission.Write);

                //设置只读的特征 （只写同理）
                mGattReadCharacteristic = new BluetoothGattCharacteristic(read_UUID_chara, GattProperty.Notify, GattPermission.Read);

                //Service添加特征值
                mGattService.AddCharacteristic(mGattCharacteristic);
                mGattService.AddCharacteristic(mGattReadCharacteristic);
                //初始化GattServer回调
                var mBluetoothGattServerCallback = new GattServerCallback();
                mBluetoothGattServerCallback.GetValueEvent += MBluetoothGattServerCallback_GetValueEvent;
                mBluetoothGattServerCallback.ServiceAddedEvent += MBluetoothGattServerCallback_ServiceAddedEvent;
                mBluetoothGattServerCallback.StateChangeEvent += MBluetoothGattServerCallback_StateChangeEvent;
                mBluetoothGattServerCallback.StateSuccessEvent += MBluetoothGattServerCallback_StateSuccessEvent;

                //添加服务
                if (_bluetoothManager != null)
                {
                    _tcsConnect = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                    mBluetoothGattServer = _bluetoothManager.OpenGattServer(_context, mBluetoothGattServerCallback);
                    mBluetoothGattServer.AddService(mGattService);
                    ToastUtils.ToastUtils.ShowToast(_context, "等待设备连接...", ToastLength.Long);
                    await _tcsConnect.Task;
                }
                else
                {
                    throw new Exception("Error");
                }
            }
            else
            {
                //前面已经确保在蓝牙开启时才广播，排除蓝牙未开启
                ToastUtils.ToastUtils.ShowToast(_context, "该手机芯片不支持BLE广播");
                throw new Exception("该手机芯片不支持BLE广播");
            }
        }

        private void Lac_StartStatusEvent(object sender, EventArgs e)
        {
            if ((_tcsConnect != null) && (!_tcsConnect.Task.IsCompleted))
                _tcsConnect.TrySetResult((bool)sender);
        }

        private void MBluetoothGattServerCallback_StateSuccessEvent(object sender, EventArgs e)
        {
            _bluetoothDevice = (BluetoothDevice)sender;
            if ((_tcsConnect != null) && (!_tcsConnect.Task.IsCompleted))
                _tcsConnect.TrySetResult(true);
        }

        private void MBluetoothGattServerCallback_StateChangeEvent(object sender, EventArgs e)
        {
            isOpen = (bool)sender;
            if (!isOpen)
            {
                ToastUtils.ToastUtils.ShowToast(_context, "等待设备连接...", ToastLength.Long);
            }
        }

        private void MBluetoothGattServerCallback_ServiceAddedEvent(object sender, EventArgs e)
        {
            ToastUtils.ToastUtils.ShowToast(_context, sender.ToString());
        }

        private void MBluetoothGattServerCallback_GetValueEvent(object sender, EventArgs e)
        {
            if ((_tcsGetValue != null) && (!_tcsGetValue.Task.IsCompleted))
                _tcsGetValue.TrySetResult((byte[])sender);
        }

        private void Lac_StartFailureEvent(object sender, EventArgs e)
        {
            ToastUtils.ToastUtils.ShowToast(_context, sender.ToString());
        }

        private void Lac_StartSuccessEvent(object sender, EventArgs e)
        {
            ToastUtils.ToastUtils.ShowToast(_context, sender.ToString());
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
                if (_isClient)
                {
                    BluetoothGattService service = _bluetoothGatt.GetService(UUID_service);
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
                            await Task.Delay(20);
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
                    //改变特征值
                    mGattReadCharacteristic.SetValue(data);
                    //回复客户端,让客户端读取该特征新赋予的值，获取由服务端发送的数据
                    mBluetoothGattServer.NotifyCharacteristicChanged(_bluetoothDevice, mGattReadCharacteristic, false);
                }
            }
            else
            {
                await _socket.OutputStream.WriteAsync(data, 0, data.Length, cancellationToken);
            }
        }
    }
}