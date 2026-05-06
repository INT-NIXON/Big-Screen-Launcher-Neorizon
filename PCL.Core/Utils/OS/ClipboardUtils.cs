#if WPF
namespace PCL.Core.Utils.OS;

using System;
using System.Windows;

public static class ClipboardUtils {
    public static void SetClipboardFiles(string[] paths) {
        if (paths == null || paths.Length == 0) {
            throw new ArgumentException("Paths cannot be null or empty.", nameof(paths));
        }

        var dataObject = new DataObject();
        dataObject.SetData(DataFormats.FileDrop, paths);
        Clipboard.SetDataObject(dataObject);
    }
}
#endif
