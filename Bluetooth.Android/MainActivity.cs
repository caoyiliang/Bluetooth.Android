using System;
using System.Collections.Generic;
using Android;
using Android.App;
using Android.Bluetooth;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.AppCompat.App;
using Bluetooth.Android.Adapter;
using Bluetooth.Android.Fragments;
using Google.Android.Material.FloatingActionButton;
using Google.Android.Material.Snackbar;

namespace Bluetooth.Android
{
    [Activity(Label = "@string/app_name", Theme = "@style/AppTheme.NoActionBar", MainLauncher = true,
               ConfigurationChanges = ConfigChanges.KeyboardHidden | ConfigChanges.Orientation)]
    public class MainActivity : AppCompatActivity
    {
        private CommunicationFragment _communicationFragment;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            Xamarin.Essentials.Platform.Init(this, savedInstanceState);
            SetContentView(Resource.Layout.activity_main);

            var toolbar = FindViewById<AndroidX.AppCompat.Widget.Toolbar>(Resource.Id.toolbar);
            SetSupportActionBar(toolbar);

            if (savedInstanceState == null)
            {
                var tx = SupportFragmentManager.BeginTransaction();
                _communicationFragment = new CommunicationFragment();
                tx.Replace(Resource.Id.content_fragment, _communicationFragment);
                tx.Commit();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();
        }
    }
}
