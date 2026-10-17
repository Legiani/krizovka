using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ApplicationModels;
using Microsoft.AspNetCore.Mvc.Routing;

namespace Krizovka.Shared.AspNetCore;

/// <summary>
/// A convention to add a global prefix to all controller routes.
/// </summary>
public class RoutePrefixConvention : IApplicationModelConvention
{
    private readonly AttributeRouteModel _routePrefix;

    public RoutePrefixConvention(IRouteTemplateProvider routeTemplateProvider)
    {
        _routePrefix = new AttributeRouteModel(routeTemplateProvider);
    }

    public void Apply(ApplicationModel application)
    {
        foreach (var controller in application.Controllers)
        {
            var matchedSelectors = controller.Selectors.Where(x => x.AttributeRouteModel != null).ToList();
            if (matchedSelectors.Any())
            {
                foreach (var selectorModel in matchedSelectors)
                {
                    selectorModel.AttributeRouteModel = AttributeRouteModel.CombineAttributeRouteModel(_routePrefix,
                        selectorModel.AttributeRouteModel);
                }
            }

            var unmatchedSelectors = controller.Selectors.Where(x => x.AttributeRouteModel == null).ToList();
            if (unmatchedSelectors.Any())
            {
                foreach (var selectorModel in unmatchedSelectors)
                {
                    selectorModel.AttributeRouteModel = _routePrefix;
                }
            }
        }
    }
}
