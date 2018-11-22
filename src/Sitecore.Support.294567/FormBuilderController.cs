using Sitecore.Data;
using Sitecore.Diagnostics;
using Sitecore.ExperienceForms.Models;
using Sitecore.ExperienceForms.Mvc;
using Sitecore.ExperienceForms.Mvc.Constants;
using Sitecore.ExperienceForms.Mvc.Extensions;
using Sitecore.ExperienceForms.Mvc.Filters;
using Sitecore.ExperienceForms.Mvc.Pipelines.GetModel;
using Sitecore.ExperienceForms.Processing;
using Sitecore.ExperienceForms.Tracking;
using Sitecore.Globalization;
using Sitecore.Mvc.Filters;
using Sitecore.Mvc.Pipelines;
using Sitecore.Web;
using System;
using System.Web.Mvc;

namespace Sitecore.ExperienceForms.Mvc.Controllers
{
  public class FormBuilderController : Controller
  {
    protected IFormSubmitHandler FormSubmitHandler
    {
      get;
      set;
    }

    protected IFormRenderingContext FormRenderingContext
    {
      get;
    }

    public FormBuilderController(IFormRenderingContext formRenderingContext, IFormSubmitHandler formSubmitHandler)
    {
      Assert.ArgumentNotNull(formRenderingContext, "formRenderingContext");
      Assert.ArgumentNotNull(formSubmitHandler, "formSubmitHandler");
      FormRenderingContext = formRenderingContext;
      FormSubmitHandler = formSubmitHandler;
    }

    [HttpGet]
    [SetFormMode(Editing = false)]
    public ActionResult Index()
    {
      FormRenderingContext.SessionId = ID.NewID.ToClientIdString();
      return RenderForm(FormRenderingContext.RenderingFormId);
    }

    [HttpGet]
    [SitecoreAuthorize(Roles = "sitecore\\Forms Editor")]
    [SetFormMode(Editing = true)]
    public ActionResult Load(string id)
    {
      FormRenderingContext.SessionId = ID.NewID.ToClientIdString();
      return RenderForm(id);
    }

    [HttpPost]
    [ValidateFormRequest]
    public ActionResult Index(FormDataModel data)
    {
      if (data == null)
      {
        return Index();
      }
      if (data.NavigationData.NavigationType == NavigationType.Submit)
      {
        FormRenderingContext.RegisterFormEvent(new FormEventData
        {
          FormId = Guid.Parse(data.FormItemId),
          EventId = FormPageEventIds.FormSubmitEventId
        });
      }
      FormRenderingContext.StorePostedFields(data.Fields);
      bool flag = true;
      if (base.ModelState.IsValid)
      {
        FormSubmitContext formSubmitContext = new FormSubmitContext(data.NavigationData.ButtonId)
        {
          FormId = Guid.Parse(data.FormItemId),
          SessionId = Guid.Parse(data.SessionData?.SessionId ?? Guid.Empty.ToString()),
          Fields = FormRenderingContext.PostedFields
        };
        FormSubmitHandler.Submit(formSubmitContext);
        flag = formSubmitContext.HasErrors;
        if (!flag)
        {
          if (data.NavigationData.NavigationType == NavigationType.Submit)
          {
            return GetSubmitActionResult(formSubmitContext);
          }
        }
        else
        {
          foreach (FormActionError error in formSubmitContext.Errors)
          {
            base.ModelState.AddModelError(FormRenderingContext.Prefix, error.ErrorMessage);
          }
        }
      }
      if (flag && data.NavigationData.NavigationType != NavigationType.Back)
      {
        FormRenderingContext.RegisterFormEvent(new FormEventData
        {
          FormId = Guid.Parse(data.FormItemId),
          EventId = FormPageEventIds.FormErrorEventId
        });
        data.NavigationData.Step = 0;
      }
      FormRenderingContext.NavigationData = data.NavigationData;
      return RenderForm(data.FormItemId);
    }

    protected ActionResult GetSubmitActionResult(FormSubmitContext submitContext)
    {
      Assert.ArgumentNotNull(submitContext, "submitContext");
      FormRenderingContext.RegisterFormEvent(new FormEventData
      {
        FormId = submitContext.FormId,
        EventId = FormPageEventIds.FormSubmitSuccessEventId
      });
      FormRenderingContext.ResetFormSessionData();
      if (submitContext.RedirectOnSuccess && !string.IsNullOrEmpty(submitContext.RedirectUrl) && !WebUtil.IsOnPage(submitContext.RedirectUrl))
      {
        if (base.Request.IsAjaxRequest())
        {
          return new JavaScriptResult
          {
            Script = "window.location='" + submitContext.RedirectUrl + "';"
          };
        }
        return Redirect(submitContext.RedirectUrl);
      }
      return Index();
    }

    protected virtual ActionResult RenderForm(string id)
    {
      using (GetModelEventArgs getModelEventArgs = new GetModelEventArgs())
      {
        getModelEventArgs.ItemId = id;
        getModelEventArgs.TemplateId = TemplateIds.FormTemplateId;
        GetModelEventArgs getModelEventArgs2 = PipelineService.Get().RunPipeline("forms.getModel", getModelEventArgs, (GetModelEventArgs a) => a);
        if (getModelEventArgs2.ViewModel == null)
        {
          return HttpNotFound(Translate.Text("Item not found"));
        }
        return PartialView(getModelEventArgs2.RenderingSettings.ViewPath, getModelEventArgs2.ViewModel);
      }
    }
  }
}