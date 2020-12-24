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

namespace Bluetooth.Android.Adapter
{
    class MyBaseAdapter<T> : BaseAdapter<T>
    {
        private LayoutInflater _Inflater;
        private int _id;
        private List<T> _list;
        private Action<View, T> _action;

        public MyBaseAdapter(Context context, int id, Action<View, T> action)
        {
            _Inflater = LayoutInflater.From(context);
            _id = id;
            _list = new List<T>();
            _action = action;
        }

        public MyBaseAdapter(Context context, int id, List<T> list, Action<View, T> action)
        {
            _Inflater = LayoutInflater.From(context);
            _id = id;
            _list = list;
            _action = action;
        }

        public override T this[int position] => _list[position];

        public override int Count => _list.Count;

        public override long GetItemId(int position) => position;

        public override View GetView(int position, View convertView, ViewGroup parent)
        {
            T t = this._list[position];

            View v = convertView;
            if (v == null)
            {
                v = _Inflater.Inflate(_id, null);
            }

            _action?.Invoke(v, t);

            return v;
        }

        public void Add(T t)
        {
            _list.Add(t);
            NotifyDataSetChanged();
        }

        public void AddRange(IEnumerable<T> ts)
        {
            _list.AddRange(ts);
            NotifyDataSetChanged();
        }

        public T Find(Predicate<T> match)
        {
            return _list.Find(match);
        }

        public void Refresh()
        {
            NotifyDataSetChanged();
        }
    }
}