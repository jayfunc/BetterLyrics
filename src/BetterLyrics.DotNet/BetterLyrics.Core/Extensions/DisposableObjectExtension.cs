using System.Reflection;

namespace BetterLyrics.Core.Extensions;

public static class DisposableObjectExtension
{
    extension(IDisposable? obj)
    {
        // Credit/Copyright to https://gist.github.com/tcartwright/dab50ebaff7c59f05013de0fb349cabd
        public bool IsDisposed()
        {
            /*
             TIM C: This hacky code is because MSFT does not provide a standard way to interrogate if an object is disposed or not.
                I wrote this based upon streams, but it should work for many other types of MSFT objects (maybe).
            */
            if (obj == null) return true;

            var objType = obj.GetType();
            //var foo = new System.IO.BufferedStream();

            // the _disposed pattern should catch a lot of msft objects.... hopefully
            var isDisposedField = objType.GetField("_disposed", BindingFlags.NonPublic | BindingFlags.Instance) ??
                                  objType.GetField("disposed", BindingFlags.NonPublic | BindingFlags.Instance);

            if (isDisposedField != null) return Convert.ToBoolean(isDisposedField.GetValue(obj));

            isDisposedField = objType.GetField("_isOpen", BindingFlags.NonPublic | BindingFlags.Instance);

            if (isDisposedField != null) return !Convert.ToBoolean(isDisposedField.GetValue(obj));

            // Windows.Graphics.Imaging.SoftwareBitmap
            isDisposedField = objType.GetField("_objRef_global__System_IDisposable",
                BindingFlags.NonPublic | BindingFlags.Instance);
            if (isDisposedField != null) return !Convert.ToBoolean(isDisposedField.GetValue(obj));

            // System.IO.FileStream
            var strategyField = objType.GetField("_strategy", BindingFlags.NonPublic | BindingFlags.Instance);
            if (strategyField != null)
            {
                var strategy = strategyField.GetValue(obj);
                var isClosedField = strategy.GetType()
                    .GetProperty("IsClosed", BindingFlags.NonPublic | BindingFlags.Instance);
                if (isClosedField != null) return Convert.ToBoolean(isClosedField.GetValue(strategy));
            }

            // other streams that use this pattern to determine if they are disposed
            if (obj is Stream stream) return !stream.CanRead && !stream.CanWrite;

            return false;
        }
    }
}