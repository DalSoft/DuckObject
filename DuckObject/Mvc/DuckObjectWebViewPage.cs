using System.Web.Mvc;

namespace DalSoft.Dynamic.Mvc
{
    public abstract class NWidgetTemplate<T> : WebViewPage where T : DuckObject, new()
    {
        private ViewDataDictionary _viewDataDictionary;

        public override void InitHelpers()
        {
            base.InitHelpers();
            _viewDataDictionary = ViewData;
        }

        public new HtmlHelper<TModel> Html<TModel>(TModel myType) where TModel : class, new()
        {
            var viewDataDictionary = new ViewDataDictionary(((T)_viewDataDictionary.Model).AsIf(myType));
            base.SetViewData(viewDataDictionary);
            return new HtmlHelper<TModel>(ViewContext, this);
        }

        public new HtmlHelper<T> Html()
        {
            var viewDataDictionary = new ViewDataDictionary(_viewDataDictionary.Model);
            base.SetViewData(viewDataDictionary);
            return new HtmlHelper<T>(ViewContext, this);
        }

        public new TModel Model<TModel>(TModel myType) where TModel : class, new()
        {
            var viewDataDictionary = new ViewDataDictionary(((T)_viewDataDictionary.Model).AsIf(myType));
            return (TModel)viewDataDictionary.Model;
        }

        public new T Model()
        {
            return ((T)_viewDataDictionary.Model);
        }

        public new TModel Model<TModel>() where TModel : class, new()
        {
            var viewDataDictionary = new ViewDataDictionary(((T)_viewDataDictionary.Model).AsIf<TModel>());
            return (TModel)viewDataDictionary.Model;
        }

    }
}

