using System.ComponentModel;
using System.Reflection;
using Microsoft.Extensions.Localization;
using Zootify_Backend.Enums;
using Zootify_Backend.Resources;

namespace Zootify_Backend.Internationalization;

public class ErrorMessageLocalizer : IErrorMessageLocalizer
{
    private readonly IStringLocalizer<ErrorMessages> _localizer;

    public ErrorMessageLocalizer(IStringLocalizer<ErrorMessages> localizer)
    {
        _localizer = localizer;
    }

    public string Localize(ErrorCode errorCode)
    {
        var key = errorCode.ToString();
        var result = _localizer[key];

        if (result.ResourceNotFound == false)
        {
            return result.Value;
        }

        return GetDescriptionFallback(errorCode);
    }

    private static string GetDescriptionFallback(ErrorCode errorCode)
    {
        var field = typeof(ErrorCode).GetField(errorCode.ToString());

        if (field == null)
        {
            return $"Error {(int)errorCode}";
        }

        var attribute = field.GetCustomAttribute<DescriptionAttribute>();

        if (attribute == null)
        {
            return $"Error {(int)errorCode}: {errorCode}";
        }

        return attribute.Description;
    }
}