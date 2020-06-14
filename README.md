# nopLocalizationHelper
This is a package to assist with nopCommerce localizations. It enables use of static classes to refer to resources, with attribute based
localization of default supported languages for the plugin

## NuGet
This package is available on NuGet [https://www.nuget.org/packages/nopLocalizationHelper]

## Example
```
    [LocaleStringProvider]
    public static class MyPluginResources
    {
        [LocaleString(Cultures.EN, "Details")]
        [LocaleString(Cultures.IS, "Nánar")]
        public const string Details = "MyCompany.MyPlugin.Details";

        [LocaleString(Cultures.EN, "Title", "The title for the page")]
        [LocaleString(Cultures.IS, "Titill", "Titill blaðsíðunnar")]
        public const string Title = "MyCompany.MyPlugin.Title";
    }
```

The above example uses attributes provided by this package. The LocaleStringProviderAttribute marks a class one that provides one or more LocaleStringAttribute resources.

The LocaleStringHelper class then can manage all resources for you on plugin Install and Uninstall with a single line of code.

Aside from the benefit of nearly automatic management of language resources, you can now stop using hardcoded strings in your code and
instead use references like MyPluginResources.Details instead of "MyCompany.MyPlugin.Details"

## Initialization of LocaleStringHelper
Since the aim of this package was to be nopCommerce agnostic, and have no reliance on nopCommerce, the only complexity comes in the initialization of the LocaleStringHelper class, which requires a few delagates in order to use nopCommerce services and classes. 

The constructor for LocaleStringHelper is defined as

```
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
```
i.e. it takes function delegates for all actions that are required by the helper. The helper doesn't even know about nopCommerce's LocaleStringResource

The following utility method is an example of instantiating this helper

```
    private LocaleStringHelper<LocaleStringResource> ResourceHelper() =>
        new LocaleStringHelper<LocaleStringResource>
        (
            GetType().Assembly,
            from lang in _languageService.GetAllLanguages() select (lang.Id, lang.LanguageCulture),
            (resourceName, languageId) => _localizationService.GetLocaleStringResourceByName(resourceName, languageId, false),
            (languageId, resourceName, resourceValue) => new LocaleStringResource { LanguageId = languageId, ResourceName = resourceName, ResourceValue = resourceValue },
            (lsr) => _localizationService.InsertLocaleStringResource(lsr),
            (lsr, resourceValue) => { lsr.ResourceValue = resourceValue; _localizationService.UpdateLocaleStringResource(lsr); },
            (lsr) => _localizationService.DeleteLocaleStringResource(lsr),
            (lsr, resourceValue) => lsr.ResourceValue == resourceValue
        );
```

And with that, in the plugin's Install method all you have to do is

```
    ResourceHelper().CreateLocaleStrings();
```

and in the Uninstall method

```
    ResourceHelper().DeleteLocaleStrings();
```

## Accessing localized values
Note that you can access localized values anywhere you have access to an instance of ILocalizationService.
For example, it may be useful to create a helper function like

```
    string T(string format) => _localizationService.GetResource(format) ?? format;
```

which you can then use to access localized values as follows

```
    var details = T(MyPluginResources.Details);
```