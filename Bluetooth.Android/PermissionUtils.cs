using Android;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Fragment.App;
using Google.Android.Material.Snackbar;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using static Android.Resource;

namespace Bluetooth.Android
{
    public static class PermissionUtils
    {
        public const int RC_LOCATION_PERMISSIONS = 1000;

        public static readonly string[] LOCATION_PERMISSIONS = { Manifest.Permission.AccessCoarseLocation, Manifest.Permission.AccessFineLocation };

        public static void RequestPermissionsForApp(this Fragment frag)
        {
            var showRequestRationale = ActivityCompat.ShouldShowRequestPermissionRationale(frag.Activity, Manifest.Permission.AccessFineLocation) ||
                                       ActivityCompat.ShouldShowRequestPermissionRationale(frag.Activity, Manifest.Permission.AccessCoarseLocation);

            if (showRequestRationale)
            {
                var rootView = frag.Activity.FindViewById(Id.Content);
                Snackbar.Make(rootView, Resource.String.request_location_permissions, Snackbar.LengthIndefinite)
                        .SetAction(Resource.String.ok, v =>
                        {
                            frag.RequestPermissions(LOCATION_PERMISSIONS, RC_LOCATION_PERMISSIONS);
                        })
                        .Show();
            }
            else
            {
                frag.RequestPermissions(LOCATION_PERMISSIONS, RC_LOCATION_PERMISSIONS);
            }
        }

        public static bool AllPermissionsGranted(this Permission[] grantResults)
        {
            if (grantResults.Length < 1)
            {
                return false;
            }

            return !grantResults.Any(result => result == Permission.Denied);
        }

        public static bool HasLocationPermissions(this Context context)
        {
            foreach (var perm in LOCATION_PERMISSIONS)
            {
                if (ContextCompat.CheckSelfPermission(context, perm) != Permission.Granted)
                {
                    return false;
                }
            }
            return true;
        }
    }
}