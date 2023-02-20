using Microsoft.AspNetCore.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ShiftSoftware.ShiftBlazor.Tests
{
    public class MockNavigationManager : NavigationManager
    {
        public MockNavigationManager() : base() =>
            this.Initialize("http://localhost:2112/", "http://localhost:2112/test");

        protected override void NavigateToCore(string uri, bool forceLoad) =>
            this.WasNavigateInvoked = true;

        public bool WasNavigateInvoked { get; private set; }
    }
}
