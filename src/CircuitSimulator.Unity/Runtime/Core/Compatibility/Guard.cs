#nullable enable
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
namespace CircuitSimulator.Core
{

internal static class Guard
{
    public static void NotNull<T>(T value, string parameterName)
        where T : class
    {
        if (value is null)
        {
            throw new ArgumentNullException(parameterName);
        }
    }

    public static bool IsFinite(double value) =>
        !double.IsNaN(value) && !double.IsInfinity(value);
}

}
