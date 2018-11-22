using Microsoft.Extensions.DependencyInjection;
using Sitecore;
using Sitecore.DependencyInjection;
using Sitecore.ExperienceForms.Mvc;
using System;
using System.Web.Helpers;

namespace Sitecore.ExperienceForms.Mvc.Filters
{
  internal class ValidateFormRequestContext
  {
    internal virtual bool IsExperienceEditor => Context.PageMode.IsExperienceEditor;

    internal virtual IFormRenderingContext FormRenderingContext => ServiceLocator.ServiceProvider.GetService<IFormRenderingContext>();

    internal virtual Action Validate => AntiForgery.Validate;
  }
}