using Zootify_Backend.Enums;

namespace Zootify_Backend.Internationalization;

public interface IErrorMessageLocalizer
{
    string Localize(ErrorCode errorCode);
    
}