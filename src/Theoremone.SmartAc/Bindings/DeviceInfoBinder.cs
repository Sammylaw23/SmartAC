using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Theoremone.SmartAc.Bindings;

public class DeviceInfoBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext.FieldName == "serialNumber")
        {
            var serialNumber = bindingContext.ActionContext.HttpContext.User.Identity?.Name ?? string.Empty;
            bindingContext.Result = ModelBindingResult.Success(serialNumber);
        }

        return Task.CompletedTask;
    }
}