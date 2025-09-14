using PetStoreHandler;
using Xunit;

public class PetStoreCommandHandlerTests
{
    [Fact]
    public void ShouldExposeCommandAndService()
    {
        var handler = new GetPetCommandHandler();
        Assert.Contains("pet-get", handler.Commands);
        Assert.Equal("petstore", handler.ServiceName);
    }
}
