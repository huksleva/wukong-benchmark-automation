using System.Text.RegularExpressions;
using Wukong.Core;

namespace Wukong.Automation;

public static class UiValues
{
    public static string Normalize(string value)
    {
        var text = OcrPage.Normalize(value.Replace('×', 'x').Replace('х', 'x').Trim('<', '>', ' ', '%'));
        text = Regex.Replace(text, @"(\d+)\s*x\s*(\d+)", "$1x$2");
        return text switch
        {
            "выкл" or "откл" or "выключено" or "отключено" => "off",
            "вкл" or "включено" => "on",
            "низкое" or "низкий" or "низкая" => "low",
            "высокое" or "высокий" or "высокая" => "high",
            "очень высокое" or "очень высокий" or "очень высокая" => "very high",
            "кинематографическое" or "кинематографический" or "кинематографическая" or "кинематографичный" => "cinematic",
            "пользовательское" or "пользовательский" or "свои" or "вручную" => "custom",
            "без ограничений" or "без ограничения" or "не ограничено" or "неограниченно" => "unlimited",
            _ => text
        };
    }
}
