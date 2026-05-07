#if WPF
using System.Globalization;
using System.Linq;
using System.Windows.Markup;
using System.Windows.Media;

namespace PCL.Core.Utils.Exts;

public static class LanguageSpecificStringDictionaryExtensions
{
    public static string GetForCurrentUiCulture(this LanguageSpecificStringDictionary dict, string? fallback = null)
    {
        var ui = CultureInfo.CurrentUICulture;

        if (TryFromTag(dict, ui.IetfLanguageTag, out var v))
            return v;

        var tag = ui.IetfLanguageTag;
        for (var dash = tag.LastIndexOf('-'); dash > 0; dash = tag.LastIndexOf('-'))
        {
            tag = tag.Substring(0, dash);
            if (TryFromTag(dict, tag, out v))
                return v;
        }

        if (!ui.IsNeutralCulture && TryFromTag(dict, ui.Parent.IetfLanguageTag, out v))
            return v;

        if (dict.Count > 0) return dict.Values!.First();
        return fallback ?? string.Empty;

        static bool TryFromTag(LanguageSpecificStringDictionary d, string ietf, out string value)
            => d.TryGetValue(XmlLanguage.GetLanguage(ietf), out value!);
    }
}
#endif
