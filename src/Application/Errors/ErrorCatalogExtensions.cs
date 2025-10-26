using Authentication.Application.Dtos;
using Authentication.Application.Interfaces;

namespace Authentication.Application.Errors;

public static class ErrorCatalogExtensions
{
    public static Result<T> Fail<T>(this IErrorCatalog errors, string code)
    {
        var e = errors.GetError(code);
        return Result<T>.Fail(e.Message, e.Code);
    }
}

