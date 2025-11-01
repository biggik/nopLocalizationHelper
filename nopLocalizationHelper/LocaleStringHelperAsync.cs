using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace nopLocalizationHelper
{
#if (NopAsync)
    public class LocaleStringHelper<T>  where T : class
    {
        private readonly Assembly pluginAssembly;
        private readonly IEnumerable<(int languageId, string culture)> languageCultures;
        private readonly Func<string, int, Task<T>> getResource;
        private readonly Func<int, string, string, T> createResource;
        private readonly Func<T, Task> insertResource;
        private readonly Func<T, string, Task> updateResource;
        private readonly Func<T, Task> deleteResource;
        private readonly Func<T, string, bool> areResourcesEqual;

        public LocaleStringHelper(
            Assembly pluginAssembly,
            IEnumerable<(int languageId, string culture)> languageCultures,
            Func<string, int, Task<T>> getResource,
            Func<int, string, string, T> createResource,
            Func<T, Task> insertResource,
            Func<T, string, Task> updateResource,
            Func<T, Task> deleteResource,
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

        public async Task CreateLocaleStringsAsync()
        {
            await ManageLocaleStringsAsync(Create);
        }

        public async Task DeleteLocaleStringsAsync()
        {
            await ManageLocaleStringsAsync(Delete);
        }

        private async Task ManageLocaleStringsAsync(Func<string, int, string, Task> action)
        {
            foreach (var resource in PluginResources)
            {
                foreach (var resourceLanguage in resource.localeStrings)
                {
                    await action.Invoke(resource.resourceName, resourceLanguage.languageId, resourceLanguage.lsa.Value);

                    if (!string.IsNullOrWhiteSpace(resourceLanguage.lsa.Hint))
                    {
                        await action.Invoke(resource.resourceName + ".Hint", resourceLanguage.languageId, resourceLanguage.lsa.Hint);
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


        private async Task Create(string resourceName, int languageId, string resourceValue)
        {
            var lsr = await getResource(resourceName, languageId);
            if (lsr == null)
            {
                lsr = createResource(languageId, resourceName, resourceValue);
                await insertResource(lsr);
            }
            else if (!areResourcesEqual(lsr, resourceValue))
            {
                await updateResource(lsr, resourceValue);
            }
        }

        private async Task Delete(string resourceName, int languageId, string notUsed)
        {
            var lsr = await getResource(resourceName, languageId);
            if (lsr != null)
            {
                await deleteResource(lsr);
            }
        }
    }
#endif
}
