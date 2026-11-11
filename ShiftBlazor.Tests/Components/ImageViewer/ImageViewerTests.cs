using Microsoft.Extensions.DependencyInjection;
using MudBlazor;

namespace ShiftSoftware.ShiftBlazor.Tests.Components.ImageViewer;

/// <summary>
/// The gallery decides per file how to show it: images in the zoomable view, videos in a player and
/// anything else as a download card. Shown in a real MudBlazor dialog, the way FileUploader opens it.
/// </summary>
public class ImageViewerTests : ShiftBlazorTestContext
{
    private Task<IRenderedComponent<IncludeMudProviders>> ShowAsync(params ShiftFileDTO[] files) => ShowAtAsync(0, files);

    private async Task<IRenderedComponent<IncludeMudProviders>> ShowAtAsync(int startIndex, params ShiftFileDTO[] files)
    {
        var host = Render<IncludeMudProviders>();
        var dialogService = Services.GetRequiredService<IDialogService>();

        var parameters = new DialogParameters { { "Values", files.ToList() }, { "StartIndex", startIndex } };
        await host.InvokeAsync(() => dialogService.ShowAsync<ShiftBlazor.Components.ImageViewer>("", parameters));

        return host;
    }

    [Fact]
    public async Task PlaysAVideoByContentType()
    {
        var host = await ShowAsync(new ShiftFileDTO { Name = "clip", Url = "https://files/clip", ContentType = "video/mp4" });

        var video = host.Find("video.siv-video");
        Assert.Equal("https://files/clip", video.GetAttribute("src"));
        Assert.True(video.HasAttribute("controls"));
        Assert.Empty(host.FindAll("img.siv-image"));
    }

    [Fact]
    public async Task PlaysAVideoByFileNameWhenThereIsNoContentType()
    {
        var host = await ShowAsync(new ShiftFileDTO { Name = "walkaround.mp4", Url = "https://files/walkaround.mp4" });

        Assert.Single(host.FindAll("video.siv-video"));
    }

    [Fact]
    public async Task ShowsImagesAndOtherFilesWithoutAPlayer()
    {
        var host = await ShowAsync(new ShiftFileDTO { Name = "photo.jpg", Url = "https://files/photo.jpg", ContentType = "image/jpeg" });

        Assert.Single(host.FindAll("img.siv-image"));
        Assert.Empty(host.FindAll("video.siv-video"));
    }

    [Fact]
    public async Task MovingToAnotherItemReplacesTheVideo()
    {
        var host = await ShowAsync(
            new ShiftFileDTO { Name = "a.mp4", Url = "https://files/a.mp4" },
            new ShiftFileDTO { Name = "b.pdf", Url = "https://files/b.pdf" },
            new ShiftFileDTO { Name = "c.mp4", Url = "https://files/c.mp4" });

        Assert.Equal("https://files/a.mp4", host.Find("video.siv-video").GetAttribute("src"));
        // The thumbnail strip shows each video as a video tile, the PDF as a file icon.
        Assert.Equal(2, host.FindAll(".siv-thumb .shift-video-thumb").Count);

        host.FindAll(".siv-thumb")[1].Click();
        Assert.Empty(host.FindAll("video.siv-video"));

        host.FindAll(".siv-thumb")[2].Click();
        Assert.Equal("https://files/c.mp4", host.Find("video.siv-video").GetAttribute("src"));
    }

    [Theory]
    [InlineData(2, 2)]
    [InlineData(99, 2)]
    [InlineData(-1, 0)]
    public async Task OpensOnTheRequestedItem(int startIndex, int shown)
    {
        var files = new[]
        {
            new ShiftFileDTO { Name = "a.jpg", Url = "https://files/a.jpg" },
            new ShiftFileDTO { Name = "b.jpg", Url = "https://files/b.jpg" },
            new ShiftFileDTO { Name = "c.mp4", Url = "https://files/c.mp4" },
        };

        var host = await ShowAtAsync(startIndex, files);

        Assert.Equal($"{shown + 1} / 3", host.Find(".siv-counter").TextContent.Trim());
        var thumbs = host.FindAll(".siv-thumb");
        Assert.Equal([shown], Enumerable.Range(0, thumbs.Count).Where(i => thumbs[i].ClassList.Contains("active")));
    }
}
