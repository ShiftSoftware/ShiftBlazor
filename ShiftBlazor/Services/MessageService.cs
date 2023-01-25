using MudBlazor;

namespace ShiftSoftware.ShiftBlazor.Services
{

    public class MessageService
    {
        private readonly ISnackbar Snackbar;
        private readonly IDialogService DialogService;
        private readonly ClipboardService ClipboardService;
        public MessageService(ISnackbar snackbar, IDialogService dialogService, ClipboardService clipboardService)
        {
            Snackbar = snackbar;
            DialogService = dialogService;
            ClipboardService = clipboardService;
        }

        /// <summary>
        /// Display a Snackbar error message with an Action button to open a dialog with more details. 
        /// </summary>
        /// <param name="text">Text to be shown on the Snackbar</param>
        /// <param name="title">The Dialog title</param>
        /// <param name="detail">The Dialog body</param>
        /// <param name="buttonText">The action button's text, default value is "View"</param>
        /// <remarks>If either title or details is null the Action button will not be rendered.</remarks>
        public void Error(string text, string? title = null, string? detail = null, string? buttonText = null)
        {
            this.Show(text, title, detail, severity: Severity.Error, variant: Variant.Text, buttonText: buttonText);
        }

        /// <summary>
        /// Display a Snackbar Info message with an Action button to open a dialog with more details. 
        /// </summary>
        /// <param name="text">Text to be shown on the Snackbar</param>
        /// <param name="title">The Dialog title</param>
        /// <param name="detail">The Dialog body</param>
        /// <param name="buttonText">The action button's text, default value is "View"</param>
        /// <remarks>If either title or details is null the Action button will not be rendered.</remarks>
        public void Info(string text, string? title = null, string? detail = null, string? buttonText = null)
        {
            this.Show(text, title, detail, severity: Severity.Info, variant: Variant.Text, buttonText: buttonText);
        }

        /// <summary>
        /// Display a Snackbar Success message with an Action button to open a dialog with more details. 
        /// </summary>
        /// <param name="text">Text to be shown on the Snackbar</param>
        /// <param name="title">The Dialog title</param>
        /// <param name="detail">The Dialog body</param>
        /// <param name="buttonText">The action button's text, default value is "View"</param>
        /// <remarks>If either title or details is null the Action button will not be rendered.</remarks>
        public void Success(string text, string? title = null, string? detail = null, string? buttonText = null)
        {
            this.Show(text, title, detail, severity: Severity.Success, variant: Variant.Text, buttonText: buttonText);
        }

        /// <summary>
        /// Display a Snackbar Normal message with an Action button to open a dialog with more details. 
        /// </summary>
        /// <param name="text">Text to be shown on the Snackbar</param>
        /// <param name="title">The Dialog title</param>
        /// <param name="detail">The Dialog body</param>
        /// <param name="buttonText">The action button's text, default value is "View"</param>
        /// <remarks>If either title or details is null the Action button will not be rendered.</remarks>
        public void Normal(string text, string? title = null, string? detail = null, string? buttonText = null)
        {
            this.Show(text, title, detail, severity: Severity.Normal, variant: Variant.Text, buttonText: buttonText);
        }

        /// <summary>
        /// Display a Snackbar Warning message with an Action button to open a dialog with more details. 
        /// </summary>
        /// <param name="text">Text to be shown on the Snackbar</param>
        /// <param name="title">The Dialog title</param>
        /// <param name="detail">The Dialog body</param>
        /// <param name="buttonText">The action button's text, default value is "View"</param>
        /// <remarks>If either title or details is null the Action button will not be rendered.</remarks>
        public void Warning(string text, string? title = null, string? detail = null, string? buttonText = null)
        {
            this.Show(text, title, detail, severity: Severity.Warning, variant: Variant.Text, buttonText: buttonText);
        }

        public void Show(string text, string? title = null, string? detail = null, Severity severity = Severity.Normal, Variant? variant = null, string? buttonText = null, Variant? buttonVariant = null, Color buttonColor = Color.Inherit)
        {
            Snackbar.Add(
                text,
                severity: severity,
                config =>
                {
                    if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(detail))
                    {
                        config.Action = string.IsNullOrWhiteSpace(buttonText) ? "View" : buttonText;
                        config.ActionColor = buttonColor;
                        config.Onclick = snackbar =>
                        {
                            ShowDetail(title, detail);
                            return Task.CompletedTask;
                        };

                        if (buttonVariant.HasValue)
                        {
                            config.ActionVariant = buttonVariant.Value;
                        }

                        if (severity == Severity.Error)
                        {
                            config.RequireInteraction = true;
                            config.CloseAfterNavigation = false;
                        }
                    }

                    if (variant.HasValue)
                    {
                        config.SnackbarVariant = variant.Value;
                    }
                }
            );
        }

        private async void ShowDetail(string title, string detail)
        {
            var dialogOptions = new DialogOptions
            {
                MaxWidth = MaxWidth.Medium,
            };

            var result = await DialogService.ShowMessageBox(
                title,
                detail,
                yesText: "Copy",
                cancelText: "Close",
                options: dialogOptions);

            if (result.HasValue && result.Value)
            {
                await ClipboardService.WriteTextAsync(detail);
                Snackbar.Add("Copied to clipboard", Severity.Success);
            }
        }

    }
}
