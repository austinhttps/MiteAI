using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;

namespace MiteAI.App;

public class ClipboardManager
{
    public async Task<string> ReadTextAsync()
    {
        return await GetTextAsync();
    }

    public static async Task<string> GetTextAsync()
    {
        try
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView != null && dataPackageView.Contains(StandardDataFormats.Text))
            {
                return await dataPackageView.GetTextAsync();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to read clipboard text: {ex.Message}");
        }

        return string.Empty;
    }
}
