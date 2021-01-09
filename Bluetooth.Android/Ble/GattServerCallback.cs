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

namespace Bluetooth.Android.Ble
{
    class GattServerCallback : BluetoothGattServerCallback
    {
        public event EventHandler StateChangeEvent;
        public event EventHandler StateSuccessEvent;
        public event EventHandler ServiceAddedEvent;
        public event EventHandler GetValueEvent;

        public override void OnConnectionStateChange(BluetoothDevice device, [GeneratedEnum] ProfileState status, [GeneratedEnum] ProfileState newState)
        {
            base.OnConnectionStateChange(device, status, newState);
            if (status == (int)GattStatus.Success)
            {
                if (newState == ProfileState.Connected)
                {
                    StateChangeEvent?.Invoke(true, null);
                    StateSuccessEvent?.Invoke(device, null);
                }
                else if (newState == ProfileState.Disconnected)
                {
                    StateChangeEvent?.Invoke(false, null);
                }
            }
            else
            {
                StateChangeEvent?.Invoke(false, null);
            }
        }

        public override void OnServiceAdded([GeneratedEnum] GattStatus status, BluetoothGattService service)
        {
            base.OnServiceAdded(status, service);
            if (status == GattStatus.Success)
            {
                ServiceAddedEvent?.Invoke($"添加Gatt服务成功 UUUID = {service.Uuid}", null);
            }
            else
            {
                ServiceAddedEvent?.Invoke("添加Gatt服务失败", null);
            }
        }

        public override void OnCharacteristicWriteRequest(BluetoothDevice device, int requestId, BluetoothGattCharacteristic characteristic, bool preparedWrite, bool responseNeeded, int offset, byte[] value)
        {
            base.OnCharacteristicWriteRequest(device, requestId, characteristic, preparedWrite, responseNeeded, offset, value);
            //刷新该特征值
            characteristic.SetValue(value);
            GetValueEvent?.Invoke(value, null);
        }
    }
}