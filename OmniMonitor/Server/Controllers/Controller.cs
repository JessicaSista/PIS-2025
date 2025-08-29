using Microsoft.AspNetCore.Mvc;
using System.Runtime.CompilerServices;

namespace OmniMonitor.Server.Controllers
{
    [ApiController]
    public class Controller(ILogger<Controller> logger) : ControllerBase
    {
        private readonly ILogger<Controller> _logger = logger;

        protected void LogDebug(string format = "", object[]? args = null, [CallerMemberName] string callerName = "")
        {
            args ??= [];
            format += " {Controller}:{Method}";
            Array.Resize(ref args, args.Length + 2);
            args[^2] = GetType().Name;
            args[^1] = callerName;
            _logger.LogDebug(format, args);
        }

        protected void LogInformation(string format = "", object[]? args = null, [CallerMemberName] string callerName = "")
        {
            args ??= [];
            format += " {Controller}:{Method}";
            Array.Resize(ref args, args.Length + 2);
            args[^2] = GetType().Name;
            args[^1] = callerName;
            _logger.LogInformation(format, args);
        }

        protected void LogWarning(string format = "", object[]? args = null, [CallerMemberName] string callerName = "")
        {
            args ??= [];
            format += " {Controller}:{Method}";
            Array.Resize(ref args, args.Length + 2);
            args[^2] = GetType().Name;
            args[^1] = callerName;
            _logger.LogWarning(format, args);
        }

        protected void LogError(string format = "", object[]? args = null, [CallerMemberName] string callerName = "")
        {
            args ??= [];
            format += " {Controller}:{Method}";
            Array.Resize(ref args, args.Length + 2);
            args[2] = GetType().Name;
            args[3] = callerName;
            _logger.LogError(format, args);
        }

        protected void LogError(Exception exception, [CallerMemberName] string callerName = "")
        {
            string format = "{Controller}:{Method}:\n{Exception}";
            string[] args = [];
            Array.Resize(ref args, args.Length + 3);
            args[^3] = GetType().Name;
            args[^2] = callerName;
            args[^1] = exception.ToString();
            _logger.LogError(format, args);
        }
    }
}
