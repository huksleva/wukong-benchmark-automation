using Wukong.Core;

namespace Wukong.Automation;

public static class UiDialogs
{
    public static OcrLine? Confirmation(OcrPage page, string[] confirmLabels, string[] applyLabels)
    {
        if (StartupScreen.NeedsManualAgreement(page)) return null;
        var graphicsQuestion = page.Find("новые настройки", "new graphics settings", "apply the graphics settings") is not null;
        var labels = graphicsQuestion ? confirmLabels.Concat(applyLabels).ToArray() : confirmLabels;
        return page.Lines.FirstOrDefault(line => line.Bounds.CenterY < page.Height * .8
            && line.Bounds.CenterX > page.Width * .2
            && labels.Any(label => OcrPage.Normalize(line.Text) == OcrPage.Normalize(label)));
    }
}
