namespace ApotekApp;

/// <summary>Dialog helper that always displays dialogs from the real application host page.
/// The master pages are hosted by putting their Content inside MainPage, so calling
/// ContentPage.DisplayAlert() on those detached pages can fail on Windows.
/// </summary>
public static class AppDialog
{
    private static Page? HostPage => Application.Current?.MainPage;

    public static async Task AlertAsync(string title, string message, string accept = "OK")
    {
        var page = HostPage;
        if (page is null)
            return;

        await page.DisplayAlert(title, message, accept);
    }

    public static async Task<bool> ConfirmAsync(
        string title,
        string message,
        string accept = "Ya, Hapus",
        string cancel = "Batal")
    {
        var page = HostPage;
        if (page is null)
            return false;

        return await page.DisplayAlert(title, message, accept, cancel);
    }
}
