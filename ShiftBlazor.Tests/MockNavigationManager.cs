namespace ShiftSoftware.ShiftBlazor.Tests;

public class MockNavigationManager : NavigationManager
{
    public MockNavigationManager()
    {
        Initialize("http://localhost:2112/", "http://localhost:2112/test");
    }

    public bool WasNavigateInvoked { get; private set; }

    protected override void NavigateToCore(string uri, bool forceLoad)
    {
        WasNavigateInvoked = true;
    }
}