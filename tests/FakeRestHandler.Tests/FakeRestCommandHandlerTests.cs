using FakeRestHandler;
using Xunit;

public class FakeRestCommandHandlerTests
{
    [Fact]
    public void GetHandlerShouldExposeCommandAndService()
    {
        var handler = new GetBookCommandHandler();
        Assert.Contains("book-get", handler.Commands);
        Assert.Equal("fakerest", handler.ServiceName);
    }

    [Fact]
    public void ListHandlerShouldExposeCommandAndService()
    {
        var handler = new ListBooksCommandHandler();
        Assert.Contains("book-list", handler.Commands);
        Assert.Equal("fakerest", handler.ServiceName);
    }

    [Fact]
    public void CreateHandlerShouldExposeCommandAndService()
    {
        var handler = new CreateBookCommandHandler();
        Assert.Contains("book-create", handler.Commands);
        Assert.Equal("fakerest", handler.ServiceName);
    }

    [Fact]
    public void UpdateHandlerShouldExposeCommandAndService()
    {
        var handler = new UpdateBookCommandHandler();
        Assert.Contains("book-update", handler.Commands);
        Assert.Equal("fakerest", handler.ServiceName);
    }

    [Fact]
    public void DeleteHandlerShouldExposeCommandAndService()
    {
        var handler = new DeleteBookCommandHandler();
        Assert.Contains("book-delete", handler.Commands);
        Assert.Equal("fakerest", handler.ServiceName);
    }
}
