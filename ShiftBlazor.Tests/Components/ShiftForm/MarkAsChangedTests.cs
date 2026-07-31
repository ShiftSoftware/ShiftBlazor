using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.ShiftForm;

/// <summary>
/// Covers the public <see cref="ShiftFormBasic{T}.MarkAsChanged"/> API and the close-result
/// contract it drives. A form that marked itself hands its Value back, so
/// <c>ShiftList.OpenDialog</c> reloads the grid; a form that did not closes as a cancel, so the
/// grid is left alone. That distinction is the whole point of the API — a form whose record was
/// changed only by a side panel never saves, and without marking it would close as a cancel.
/// </summary>
public class MarkAsChangedTests : ShiftBlazorTestContext
{
    private const string Path = "Product";

    /// <summary>
    /// Shows <typeparamref name="TForm"/> in a real MudBlazor dialog, so the close path under test
    /// is the production one: <c>Cancel</c> -> <c>ShiftModal.Close</c> -> <c>Cancel()</c> or
    /// <c>Close(data)</c> on the dialog -> the <see cref="DialogResult"/> a ShiftList would read.
    /// </summary>
    private async Task<(IRenderedComponent<IncludeMudProviders> Host, IRenderedComponent<TForm> Form, IDialogReference Dialog)>
        ShowFormInDialogAsync<TForm>(DialogParameters? parameters = null) where TForm : ComponentBase
    {
        var host = RenderComponent<IncludeMudProviders>();
        var dialogService = Services.GetRequiredService<IDialogService>();

        IDialogReference dialog = default!;
        await host.InvokeAsync(async () =>
            dialog = await dialogService.ShowAsync<TForm>("", parameters ?? new DialogParameters()));

        return (host, host.FindComponent<TForm>(), dialog);
    }

    [Fact]
    public async Task ShouldCloseWithDataWhenMarkedAsChanged()
    {
        var (host, form, dialog) = await ShowFormInDialogAsync<ShiftFormBasic<SampleDTO>>();

        // Closing disposes the form component, so grab what it should hand back while it lives.
        var value = form.Instance.Value;

        // Stands in for a side panel that PUT its own endpoint; the form itself never saved.
        await host.InvokeAsync(() => form.Instance.MarkAsChanged());
        await host.InvokeAsync(() => form.Instance.Cancel());

        var result = await dialog.Result;

        Assert.NotNull(result);
        // ShiftList.OpenDialog reloads the grid exactly when Canceled is not true.
        Assert.False(result.Canceled);
        Assert.Same(value, result.Data);
    }

    [Fact]
    public async Task ShouldCloseAsCancelledWhenNotMarkedAsChanged()
    {
        var (host, form, dialog) = await ShowFormInDialogAsync<ShiftFormBasic<SampleDTO>>();

        // No MarkAsChanged call: behaviour must be identical to before the API existed.
        await host.InvokeAsync(() => form.Instance.Cancel());

        var result = await dialog.Result;

        Assert.NotNull(result);
        Assert.True(result.Canceled);
        Assert.Null(result.Data);
    }

    [Fact]
    public async Task ShouldBeIdempotentWhenMarkedRepeatedly()
    {
        var (host, form, dialog) = await ShowFormInDialogAsync<ShiftFormBasic<SampleDTO>>();

        // Several panels may report on the same open; the flag is a latch, not a counter.
        await host.InvokeAsync(() => form.Instance.MarkAsChanged());
        await host.InvokeAsync(() => form.Instance.MarkAsChanged());
        await host.InvokeAsync(() => form.Instance.Cancel());

        var result = await dialog.Result;

        Assert.False(result!.Canceled);
        Assert.NotNull(result.Data);
    }

    /// <summary>
    /// ShiftEntityForm inherits the API, which is what makes it reachable from
    /// <c>ShiftForm&lt;TPage, TDTO&gt;</c> in a consuming assembly.
    /// </summary>
    [Fact]
    public async Task ShouldInheritMarkAsChangedOnShiftEntityForm()
    {
        var parameters = new DialogParameters<ShiftEntityForm<SampleDTO>>
        {
            { x => x.Endpoint, Path },
        };

        var (host, form, dialog) = await ShowFormInDialogAsync<ShiftEntityForm<SampleDTO>>(parameters);

        var value = form.Instance.Value;

        await host.InvokeAsync(() => form.Instance.MarkAsChanged());
        await host.InvokeAsync(() => form.Instance.Cancel());

        var result = await dialog.Result;

        Assert.False(result!.Canceled);
        Assert.Same(value, result.Data);
    }

    [Fact]
    public async Task ShouldNotCloseShiftEntityFormWithDataWhenNotMarkedAsChanged()
    {
        var parameters = new DialogParameters<ShiftEntityForm<SampleDTO>>
        {
            { x => x.Endpoint, Path },
        };

        var (host, form, dialog) = await ShowFormInDialogAsync<ShiftEntityForm<SampleDTO>>(parameters);

        await host.InvokeAsync(() => form.Instance.Cancel());

        var result = await dialog.Result;

        Assert.True(result!.Canceled);
        Assert.Null(result.Data);
    }

    /// <summary>
    /// The embedded-component route: a child that only sees the cascaded <see cref="IShiftForm"/>
    /// — it does not know the form's DTO type — can still report its change.
    /// </summary>
    [Fact]
    public async Task ShouldMarkAsChangedThroughCascadedIShiftForm()
    {
        RenderFragment<FormChildContext<SampleDTO>> childContent = _ => builder =>
        {
            builder.OpenComponent<MarkAsChangedProbe>(0);
            builder.CloseComponent();
        };

        var parameters = new DialogParameters<ShiftFormBasic<SampleDTO>>
        {
            { x => x.ChildContent, childContent },
        };

        var (host, form, dialog) = await ShowFormInDialogAsync<ShiftFormBasic<SampleDTO>>(parameters);

        var probe = host.FindComponent<MarkAsChangedProbe>();
        Assert.NotNull(probe.Instance.ShiftForm);

        var value = form.Instance.Value;

        await host.InvokeAsync(() => probe.Instance.ReportChange());
        await host.InvokeAsync(() => form.Instance.Cancel());

        var result = await dialog.Result;

        Assert.False(result!.Canceled);
        Assert.Same(value, result.Data);
    }
}
