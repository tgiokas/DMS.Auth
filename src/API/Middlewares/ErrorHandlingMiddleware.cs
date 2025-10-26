﻿using System.Net;
using System.Text.Json;
using Serilog;
using Authentication.Application.Dtos;

namespace Authentication.Api.Middlewares;

public class ErrorHandlingMiddleware
{
    private readonly RequestDelegate _next;

    public ErrorHandlingMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task Invoke(HttpContext context, ILogger<ErrorHandlingMiddleware> logger)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "An unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private static Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        if (context.Response.HasStarted)
        {            
            Log.Warning("The response has already started, the error handling middleware will not be executed.");
            return Task.CompletedTask;
        }

        context.Response.Clear();
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        var result = Result<string>.Fail(errorCode: "AUTH-000", message:"An unexpected error occurred");
        result.Data = exception.Message;

        var json = JsonSerializer.Serialize(result);
        return context.Response.WriteAsync(json);
    }
}
