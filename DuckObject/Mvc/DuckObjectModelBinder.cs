using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Web.Mvc;
using DalSoft.Dynamic.Extensions;

namespace DalSoft.Dynamic.Mvc
{
    public sealed class DuckObjectModelBinder : DefaultModelBinder
    {
        public override object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            if (bindingContext.ModelType.IsSubclassOf(typeof(DuckObject)))
            {
                if (!String.IsNullOrEmpty(bindingContext.ModelName) && !bindingContext.ValueProvider.ContainsPrefix(bindingContext.ModelName))
                {
                    if (bindingContext.FallbackToEmptyPrefix)
                        bindingContext = NewModelBindingContext(bindingContext);
                    else
                        return null;
                }

                var model = bindingContext.Model ?? CreateModel(controllerContext, bindingContext, bindingContext.ModelType);
                var newBindingContext = CreateComplexElementalModelBindingContext(controllerContext, bindingContext, model);

                OnModelUpdating(controllerContext, newBindingContext);

                return newBindingContext.Model;
            }

            return base.BindModel(controllerContext, bindingContext);
        }

        protected override bool OnModelUpdating(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var isModelUpdating = base.OnModelUpdating(controllerContext, bindingContext);
            if (isModelUpdating)
            {
                BindProperties(controllerContext, bindingContext);
                OnModelUpdated(controllerContext, bindingContext);
            }

            return isModelUpdating;
        }

        private ModelBindingContext CreateComplexElementalModelBindingContext(ControllerContext controllerContext, ModelBindingContext bindingContext, object model)
        {
            var bindAttr = (BindAttribute)GetTypeDescriptor(controllerContext, bindingContext).GetAttributes()[typeof(BindAttribute)];
            var newPropertyFilter = (bindAttr != null)
                ? propertyName => bindAttr.IsPropertyAllowed(propertyName) && bindingContext.PropertyFilter(propertyName)
                : bindingContext.PropertyFilter;

            var newBindingContext = NewModelBindingContext(bindingContext, ModelMetadataProviders.Current.GetMetadataForType(() => model, bindingContext.ModelType), newPropertyFilter);

            return newBindingContext;
        }

        private void BindProperties(ControllerContext controllerContext, ModelBindingContext bindingContext)
        {
            var staticProperties = GetFilteredModelProperties(controllerContext, bindingContext).ToList();
            var jsonValueProvider = ((ValueProviderCollection)bindingContext.ValueProvider).OfType<ExtendedDictionaryValueProvider<object>>().FirstOrDefault() ?? new ExtendedDictionaryValueProvider<object>(new Collection<KeyValuePair<string, object>>(), CultureInfo.CurrentCulture);
            var dynamicProperties = new Dictionary<string, object>();

            //Add dynamic properties order is important
            controllerContext.HttpContext.Request.Files.CopyTo(dynamicProperties);
            controllerContext.HttpContext.Request.QueryString.CopyTo(dynamicProperties, true);
            controllerContext.RouteData.Values.CopyTo(dynamicProperties, true);
            controllerContext.HttpContext.Request.Form.CopyTo(dynamicProperties, true);
            
            //Allow Json to always win
            jsonValueProvider.Prefixes.Where(x => !string.IsNullOrEmpty(x)).ToList().ForEach(x => dynamicProperties.Add(x, jsonValueProvider.GetValue(x).RawValue)); //Add properties from jsonValueProvider

            //Bind static properties
            staticProperties.Where(x => x.Name != "Item" && x.Name != "Keys" && x.Name != "Values").ToList().ForEach(x=>BindProperty(controllerContext, bindingContext, x));

            //Remove static properties from dynamic properties
            staticProperties.ForEach(x => dynamicProperties.Remove(x.Name));
            
            //Bind dynamic properties
            BindDynamicProperties(controllerContext, bindingContext, dynamicProperties.ToDictionary(x => x.Key, x => x.Value));
        }

        private static ModelBindingContext NewModelBindingContext(ModelBindingContext bindingContext, ModelMetadata modelMetadata = null, Predicate<string> propertyFilter = null)
        {
            return new ModelBindingContext
            {
                ModelMetadata = modelMetadata ?? bindingContext.ModelMetadata,
                ModelState = bindingContext.ModelState,
                PropertyFilter = propertyFilter ?? bindingContext.PropertyFilter,
                ValueProvider = bindingContext.ValueProvider
            };
        }
        
        /// <summary>
        /// This is naive it doesn't deal with things like validation and bind filtering yet
        /// </summary>
        private static void BindDynamicProperties(ControllerContext controllerContext, ModelBindingContext bindingContext, Dictionary<string, object> values)
        {
            var extendedDictionaryValueProvider = new ExtendedDictionaryValueProvider<object>(values, CultureInfo.CurrentCulture);
            
            foreach (var property in extendedDictionaryValueProvider.Prefixes.Where(property => !string.IsNullOrWhiteSpace(property)))
            {
                ((DuckObject) bindingContext.Model)[property] = extendedDictionaryValueProvider.GetValue(property).RawValue;
            }
        }
    }

}