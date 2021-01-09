using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Xamarin.Essentials;

namespace Bluetooth.Android.ToastUtils
{
    class ToastUtils
    {
        private static Toast mToast;

        public static void ShowToast(Context context, string str, ToastLength toastLength = ToastLength.Short)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (mToast != null)
                {
                    mToast.Cancel();
                    mToast = Toast.MakeText(context, str, toastLength);
                }
                else
                {
                    mToast = Toast.MakeText(context, str, toastLength);
                }

                mToast.Show();
            });
        }

        public static void ShowToast(Context context, int strId, ToastLength toastLength = ToastLength.Short)
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                if (mToast != null)
                {
                    mToast.Cancel();
                    mToast = Toast.MakeText(context, strId, toastLength);
                }
                else
                {
                    mToast = Toast.MakeText(context, strId, toastLength);
                }

                mToast.Show();
            });
        }
    }
}