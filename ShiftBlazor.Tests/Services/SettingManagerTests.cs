using Microsoft.Extensions.DependencyInjection;
using ShiftSoftware.ShiftBlazor.Services;

namespace ShiftSoftware.ShiftBlazor.Tests.Services;

public class SettingManagerTests : ShiftBlazorTestContext
{
    [Fact]
    public void PageSizeShouldBePerList()
    {
        var settingManager = Services.GetRequiredService<SettingManager>();

        // Changing one list's page size must not affect other lists.
        settingManager.SetListPageSize("Users_UserListDTO", 100);

        Assert.Equal(100, settingManager.GetListPageSize("Users_UserListDTO"));
        Assert.Null(settingManager.GetListPageSize("Products_ProductListDTO"));
    }

    [Fact]
    public void PerListPageSizeShouldFallBackToAppWideValue()
    {
        var settingManager = Services.GetRequiredService<SettingManager>();

        // The app-wide value (Settings UI, or a preference saved before page sizes became
        // per-list) is the default for lists without their own preference.
        settingManager.SetListPageSize(25);

        Assert.Equal(25, settingManager.GetListPageSize("Products_ProductListDTO"));

        // A per-list preference still wins over the app-wide value.
        settingManager.SetListPageSize("Users_UserListDTO", 100);
        Assert.Equal(100, settingManager.GetListPageSize("Users_UserListDTO"));
    }

    [Fact]
    public void SettingAppWidePageSizeShouldClearPerListOverrides()
    {
        var settingManager = Services.GetRequiredService<SettingManager>();

        settingManager.SetListPageSize("Users_UserListDTO", 100);

        // Explicitly picking an app-wide size in the Settings UI means "make every list this
        // size" — per-list overrides are cleared so the control visibly applies everywhere.
        settingManager.SetListPageSize(25);

        Assert.Equal(25, settingManager.GetListPageSize("Users_UserListDTO"));
    }
}
