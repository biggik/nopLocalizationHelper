using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace nopLocalizationHelper
{
#if !(NopAsync)
    public class LocaleStringHelper<T>  where T : class
    {
        private readonly Assembly pluginAssembly;
        private readonly IEnumerable<(int languageId, string culture)> languageCultures;
        private readonly Func<string, int, T> getResource;
        private readonly Func<int, string, string, T> createResource;
        private readonly Action<T> insertResource;
        private readonly Action<T, string> updateResource;
        private readonly Action<T> deleteResource;
        private readonly Func<T, string, bool> areResourcesEqual;

        public LocaleStringHelper(
            Assembly pluginAssembly,
            IEnumerable<(int languageId, string culture)> languageCultures,
            Func<string, int, T> getResource,
            Func<int, string, string, T> createResource,
            Action<T> insertResource,
            Action<T, string> updateResource,
            Action<T> deleteResource,
            Func<T, string, bool> areResourcesEqual
            )
        {
            this.pluginAssembly = pluginAssembly;
            this.languageCultures = languageCultures;
            this.getResource = getResource;
            this.createResource = createResource;
            this.insertResource = insertResource;
            this.updateResource = updateResource;
            this.deleteResource = deleteResource;
            this.areResourcesEqual = areResourcesEqual;
        }

        public void CreateLocaleStrings()
        {
            ManageLocaleStrings(Create);
        }

        public void DeleteLocaleStrings()
        {
            ManageLocaleStrings(Delete);
        }

        private void ManageLocaleStrings(Action<string, int, string> action)
        {
            foreach (var resource in PluginResources)
            {
                foreach (var resourceLanguage in resource.localeStrings)
                {
                    action.Invoke(resource.resourceName, resourceLanguage.languageId, resourceLanguage.lsa.Value);

                    if (!string.IsNullOrWhiteSpace(resourceLanguage.lsa.Hint))
                    {
                        action.Invoke(resource.resourceName + ".Hint", resourceLanguage.languageId, resourceLanguage.lsa.Hint);
                    }
                }
            }
        }

        private IEnumerable<(string resourceName, IEnumerable<(LocaleStringAttribute lsa, int languageId)> localeStrings)> PluginResources
        {
            get
            {
                var availableLanguages = languageCultures.ToDictionary(x => x.culture, y => y);

                IEnumerable<(LocaleStringAttribute lsa, int languageId)> AsLSA(object[] values)
                {
                    foreach (var v in values)
                    {
                        var lsa = v as LocaleStringAttribute;
                        if (availableLanguages.ContainsKey(lsa.Culture))
                        {
                            yield return (lsa, availableLanguages[lsa.Culture].languageId);
                        }
                    }
                }

                return from type in pluginAssembly.GetTypes()
                       where type.CustomAttributes.Any(x => x.AttributeType == typeof(LocaleStringProviderAttribute))
                       from field in type.GetFields()
                       select (resourceName: field.GetValue(null).ToString(),
                               localeStrings: AsLSA(field.GetCustomAttributes(typeof(LocaleStringAttribute), false)));
            }
        }


        private void Create(string resourceName, int languageId, string resourceValue)
        {
            var lsr = getResource(resourceName, languageId);
            if (lsr == null)
            {
                lsr = createResource(languageId, resourceName, resourceValue);
                insertResource(lsr);
            }
            else if (!areResourcesEqual(lsr, resourceValue))
            {
                updateResource(lsr, resourceValue);
            }
        }

        private void Delete(string resourceName, int languageId, string notUsed)
        {
            var lsr = getResource(resourceName, languageId);
            if (lsr != null)
            {
                deleteResource(lsr);
            }
        }
    }
#endif
}
