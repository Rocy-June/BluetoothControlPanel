using System;
using System.Collections.Generic;
using System.Text;

namespace BluetoothControlPanel.Common.Extensions;

public static class ObjectExtensions
{
    extension(object? obj)
    {
        public int ToInt32(int? def = null)
        {
            var flag = obj.TryToInt32(out var result);
            if (!flag)
            {
                return def ?? throw new InvalidCastException("Cannot convert this object to Int32.");
            }

            return result;
        }

        public bool TryToInt32(out int result, int def = 0)
        {
            result = def;
            if (obj is null)
            {
                return false;
            }

            try
            {
                result = Convert.ToInt32(obj);
                return true;
            }
            catch
            {
                return false;
            }
        }
    }
}
