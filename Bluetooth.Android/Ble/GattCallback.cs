using Android.Bluetooth;
using Android.Runtime;
using Java.Util;
using System;
using System.Collections.Generic;

namespace Bluetooth.Android.Ble
{
    class GattCallback : BluetoothGattCallback
    {
        public event EventHandler StateChangeEvent;
        public event EventHandler ServicesDiscoveredEvent;
        public event EventHandler GetValueEvent;

        public override void OnConnectionStateChange(BluetoothGatt gatt, [GeneratedEnum] GattStatus status, [GeneratedEnum] ProfileState newState)
        {
            base.OnConnectionStateChange(gatt, status, newState);

            if (status == GattStatus.Success)
            {
                //连接成功
                if (newState == ProfileState.Connected)
                {
                    //发现服务
                    gatt.DiscoverServices();
                    StateChangeEvent?.Invoke(true, null);
                }
            }
            else
            {
                //连接失败
                StateChangeEvent?.Invoke(false, null);
            }
        }

        public override void OnCharacteristicChanged(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic)
        {
            base.OnCharacteristicChanged(gatt, characteristic);

            GetValueEvent?.Invoke(characteristic.GetValue(), null);
        }

        public override void OnServicesDiscovered(BluetoothGatt gatt, [GeneratedEnum] GattStatus status)
        {
            base.OnServicesDiscovered(gatt, status);
            ServicesDiscoveredEvent?.Invoke(gatt, null);
        }
    }
}