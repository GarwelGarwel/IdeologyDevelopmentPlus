using Verse;

namespace IdeologyDevelopmentPlus
{
    enum LogLevel
    {
        Message = 0,
        Warning,
        Error
    };

    static class LogUtility
    {
        internal static void Log(string message, LogLevel logLevel = LogLevel.Message)
        {
            message = $"[ID+] {message}";
            switch (logLevel)
            {
                case LogLevel.Message:
                    if (Prefs.DevMode || Prefs.LogVerbose)
                        Verse.Log.Message(message);
                    break;

                case LogLevel.Warning:
                    Verse.Log.Warning(message);
                    break;

                case LogLevel.Error:
                    Verse.Log.Error(message);
                    break;
            }
        }
    }
}
