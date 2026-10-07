using Wukong.Core;

namespace Wukong.Automation;

/// <summary>Recognizes known prompts without accepting agreements.</summary>
public static class StartupScreen
{
    public static bool NeedsContinue(OcrPage page, IEnumerable<string> labels) =>
        labels.Any(label => !string.IsNullOrWhiteSpace(label) && page.Lines.Any(line =>
            OcrPage.Normalize(line.Text).Contains(OcrPage.Normalize(label), StringComparison.Ordinal)));

    public static bool NeedsManualAgreement(OcrPage page) =>
        new[] { "privacy policy", "privacy agreement", "user agreement", "license agreement", "политика конфиденциальности", "пользовательское соглашение", "лицензионное соглашение" }
            .Any(label => page.Find(label) is not null);
}
