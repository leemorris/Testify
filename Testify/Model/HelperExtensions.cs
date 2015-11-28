﻿using System;

namespace Leem.Testify.Model
{
    internal static class HelperExtensions
    {
        public static TRet Maybe<T, TRet>(this T value, Func<T, TRet> action, TRet defValue = default(TRet))
            where T : class
        {
            return (value != null) ? action(value) : defValue;
        }

        public static T Do<T>(this T value, Action<T> action)
            where T : class
        {
            if (value != null)
                action(value);
            return value;
        }
    }
}