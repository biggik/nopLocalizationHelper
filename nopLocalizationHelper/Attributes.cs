using System;

namespace nopLocalizationHelper
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class LocaleStringProviderAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Field, AllowMultiple = true)]
    public class LocaleStringAttribute : Attribute
    {
        public LocaleStringAttribute(string culture, string value, string hint = null)
        {
            Culture = culture;
            Value = value;
            Hint = hint;
        }

        public string Culture { get; }
        public string Value { get; }
        public string Hint { get; }
    }
}
