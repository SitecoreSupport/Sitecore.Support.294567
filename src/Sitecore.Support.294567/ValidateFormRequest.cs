using Sitecore.Diagnostics;
using Sitecore.ExperienceForms.Mvc.Filters;
using System;
using System.Web.Mvc;

namespace Sitecore.Support.ExperienceForms.Mvc.Filters
{
  [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
  internal sealed class ValidateFormRequestAttribute : FilterAttribute, IAuthorizationFilter
  {
    private readonly ValidateFormRequestContext _validationContext;

    public ValidateFormRequestAttribute()
      : this(new ValidateFormRequestContext())
    {
    }

    internal ValidateFormRequestAttribute(ValidateFormRequestContext validateFormAntiForgeryContext)
    {
      Assert.ArgumentNotNull(validateFormAntiForgeryContext, "validateFormAntiForgeryContext");
      _validationContext = validateFormAntiForgeryContext;
    }

    public void OnAuthorization(AuthorizationContext filterContext)
    {
      Assert.ArgumentNotNull(filterContext, "filterContext");
      string name = _validationContext.FormRenderingContext.CreatePropertyName("FormItemId");
      string value = filterContext.RequestContext.HttpContext.Request.Form[name];
      if (!string.IsNullOrEmpty(value) || !_validationContext.IsExperienceEditor)
      {
        _validationContext.Validate();
      }
    }
  }
}