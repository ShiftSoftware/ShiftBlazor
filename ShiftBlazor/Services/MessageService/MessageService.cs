using Microsoft.AspNetCore.Components;
using MudBlazor;
using ShiftSoftware.ShiftBlazor.Components;
using ShiftSoftware.ShiftBlazor.Localization;
using ShiftSoftware.ShiftEntity.Core.Localization;
using ShiftSoftware.ShiftEntity.Model;

namespace ShiftSoftware.ShiftBlazor.Services
{

    public class MessageService
    {
        private readonly ISnackbar Snackbar;
        private readonly IDialogService DialogService;
        private readonly ShiftBlazorLocalizer Loc;

        public MessageService(ISnackbar snackbar, IDialogService dialogService, ShiftBlazorLocalizer loc)
        {
            Snackbar = snackbar;
            DialogService = dialogService;
            Loc = loc;
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
            this.Show(text, title, detail, severity: Severity.Error, buttonText: buttonText, modalColor: Color.Error);
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
            this.Show(text, title, detail, severity: Severity.Info, buttonText: buttonText, modalColor: Color.Info);
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
            this.Show(text, title, detail, severity: Severity.Success, buttonText: buttonText, modalColor: Color.Success);
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
            this.Show(text, title, detail, severity: Severity.Normal, buttonText: buttonText);
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
            this.Show(text, title, detail, severity: Severity.Warning, buttonText: buttonText, modalColor: Color.Warning);
        }

        public void Show(string text, Severity severity = Severity.Normal, Action<SnackbarOptions>? configure = null, string? key = null)
        {
            Snackbar.Add(
                text,
                severity,
                configure,
                key
            );
        }

        public void Show(string text, string? title = null, string? detail = null, Severity severity = Severity.Normal, string? buttonText = null, Variant? buttonVariant = null, Color buttonColor = Color.Inherit, Color modalColor = Color.Inherit, string? icon = Icons.Material.Outlined.Info, string? key = null)
        {

            Show(text, severity, config =>
            {
                if (severity == Severity.Error)
                {
                    config.RequireInteraction = true;
                    config.CloseAfterNavigation = false;
                }

                config.DuplicatesBehavior = SnackbarDuplicatesBehavior.Prevent;
                config.Icon = icon;

                if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(detail))
                {
                    config.Action = string.IsNullOrWhiteSpace(buttonText) ? "View" : buttonText;
                    config.ActionColor = buttonColor;
                    config.OnClick = snackbar =>
                    {
                        ShowDialog(title, detail, modalColor, icon);
                        return Task.CompletedTask;
                    };

                    if (buttonVariant.HasValue)
                    {
                        config.ActionVariant = buttonVariant.Value;
                    }

                }
            }, key);
        }

        private void ShowDialog(string title, string detail, Color color, string? Icon)
        {
            var dialogOptions = new DialogOptions
            {
                MaxWidth = MaxWidth.Medium,
                NoHeader = true,
            };

            var message = new Message
            {
                Title = title,
                Body = detail,
            };

            var dialogParams = new DialogParameters
            {
                { "Message", message },
                { "Color", color },
                { "Icon", Icon },
                { "ConfirmText", Loc["ModalClose"].ToString() },
                { "ReportButton", true },
            };

            DialogService.ShowAsync<PopupMessage>("", dialogParams, dialogOptions);
        }

    }
}
