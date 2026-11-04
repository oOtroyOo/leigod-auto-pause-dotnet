using System.Net.Http;
using System.Text.Json;
using MessageBoxResult = Wpf.Ui.Controls.MessageBoxResult;

namespace LeigodPause.Views.Pages;

/// <summary>
/// Interaction logic for SettingsPage.xaml
/// </summary>
public partial class SettingsPage
{
    private ILogger<SettingsPage> logger = App.Host.Services.GetRequiredService<ILogger<SettingsPage>>();

    public SettingsPage()
    {
        InitializeComponent();

        AppVersionTextBlock.Text = $"雷神暂停工具 - {GetAssemblyVersion()}";

        if (ApplicationThemeManager.GetAppTheme() == ApplicationTheme.Dark)
        {
            DarkThemeRadioButton.IsChecked = true;
        }
        else
        {
            LightThemeRadioButton.IsChecked = true;
        }

        this.ApplyTheme();
    }

    private void OnLightThemeRadioButtonChecked(object sender, RoutedEventArgs e)
    {
        ApplicationThemeManager.Apply(ApplicationTheme.Light);
    }

    private void OnDarkThemeRadioButtonChecked(object sender, RoutedEventArgs e)
    {
        ApplicationThemeManager.Apply(ApplicationTheme.Dark);
    }

    private string GetAssemblyVersion()
    {
        return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
               ?? string.Empty;
    }

    private void OnTestButtonClick(object sender, RoutedEventArgs e)
    {
        ToastUtility.Show(this.ExampleText.Text, this.WindowTitle);
    }

    private void OnCheckUpdateButtonClick(object sender, RoutedEventArgs e)
    {
        _ = Task.Run(CheckUpdateAsync);
    }

    private async Task CheckUpdateAsync()
    {
        try
        {
            var clientFactory = App.Host.Services.GetRequiredService<IHttpClientFactory>();
            using var httpClient = clientFactory.CreateClient("proxy");
            var response = await httpClient.GetAsync("https://api.github.com/repos/oOtroyOo/leigod-auto-pause-dotnet/releases/latest").ConfigureAwait(false);
            var json = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
            var release = JsonNode.Parse(json);
            if (release is JsonObject releaseJson)
            {
                var latestTagName = releaseJson["tag_name"]?.GetValue<string>().Substring(1);
                if (latestTagName != null && Version.TryParse(latestTagName, out var latestVersion))
                {
                    if (Version.TryParse(GetAssemblyVersion(), out var currentVersion))
                    {
                        if (latestVersion > currentVersion)
                        {
                            _ = Dispatcher.Invoke(async () =>
                            {
                                var messageBox = new MessageBox
                                {
                                    Content = $"发现新版本：{latestTagName}",
                                    Title = "检查更新",
                                    PrimaryButtonAppearance = Wpf.Ui.Controls.ControlAppearance.Success,
                                    PrimaryButtonText = "前往下载",
                                    CloseButtonText = "取消",
                                };

                                if ((await messageBox.ShowDialogAsync().ConfigureAwait(false)) == MessageBoxResult.Primary)
                                {
                                    Process.Start("https://github.com/oOtroyOo/leigod-auto-pause-dotnet/releases/latest");
                                }
                            });
                        }
                        else
                        {
                            ToastUtility.Show("已是最新版本", "检查更新");
                        }
                    }
                }
            }
        }
        catch (Exception e)
        {
            logger.LogError(e, "CheckUpdateAsync failed");
            ToastUtility.Show(e.Message, "检查更新失败");
        }
    }
}