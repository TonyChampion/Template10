﻿using Prism.Ioc;
using Prism.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Media.Animation;
using Windows.UI.Xaml.Navigation;

namespace Template10.Navigation
{
    public static class NavigationEventArgsExtensions
    {
        public static bool TryGetParameter<T>(this NavigationEventArgs args, string name, out T value)
        {
            try
            {
                var www = new WwwFormUrlDecoder(args.Parameter.ToString());
                var result = www.GetFirstValueByName(name);
                value = (T)Convert.ChangeType(result, typeof(T));
                return true;
            }
            catch
            {
                value = default(T);
                return false;
            }
        }

        public static bool TryGetParameters<T>(this NavigationEventArgs args, string name, out IEnumerable<T> values)
        {
            try
            {
                var www = new WwwFormUrlDecoder(args.Parameter.ToString());
                values = www
                    .Where(x => x.Name == name)
                    .Select(x => (T)Convert.ChangeType(x.Value, typeof(T)));
                return true;
            }
            catch
            {
                values = default(IEnumerable<T>);
                return false;
            }
        }
    }
}
