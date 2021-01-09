using Android.App;
using Android.Bluetooth.LE;
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
    class LeAdvertiseCallback : AdvertiseCallback
    {
        public event EventHandler StartSuccessEvent;
        public event EventHandler StartFailureEvent;
        public event EventHandler StartStatusEvent;
        public override void OnStartSuccess(AdvertiseSettings settingsInEffect)
        {
            base.OnStartSuccess(settingsInEffect);

            var advertiseInfo = new StringBuilder("启动Ble广播成功");
            //连接性
            if (settingsInEffect.IsConnectable)
            {
                advertiseInfo.Append(", 可连接");
            }
            else
            {
                advertiseInfo.Append(", 不可连接");
            }
            //广播时长
            if (settingsInEffect.Timeout == 0)
            {
                advertiseInfo.Append(", 持续广播");
            }
            else
            {
                advertiseInfo.Append(", 广播时长 ${settingsInEffect.timeout} ms");
            }
            StartSuccessEvent?.Invoke(advertiseInfo, null);
            StartStatusEvent?.Invoke(true, null);
        }

        public override void OnStartFailure([GeneratedEnum] AdvertiseFailure errorCode)
        {
            base.OnStartFailure(errorCode);
            if (errorCode == AdvertiseFailure.DataTooLarge)
            {
                StartFailureEvent?.Invoke("启动Ble广播失败 数据报文超出31字节", null);
            }
            else
            {
                StartFailureEvent?.Invoke($"启动Ble广播失败 errorCode = {errorCode}", null);
            }
            StartStatusEvent?.Invoke(false, null);
        }
    }
}