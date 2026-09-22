using InteractHub.Infrastructure.Hubs;
using Microsoft.AspNetCore.SignalR;
using Moq;

namespace InteractHub.Tests.Common;

public static class SignalRMockFactory
{
    /// <summary>
    /// Creates a properly configured mock for IHubContext<PostHub> 
    /// that handles SignalR calls without throwing NullReferenceException
    /// </summary>
    public static Mock<IHubContext<PostHub>> CreatePostHubMock()
    {
        var mockHub = new Mock<IHubContext<PostHub>>();
        
        // Mock IHubClients
        var mockClients = new Mock<IHubClients>();
        mockHub.Setup(h => h.Clients).Returns(mockClients.Object);
        
        // Mock IClientProxy for Group calls
        // Since SendAsync is an extension method, we can't mock it directly
        // Just return a mock that will accept any calls to SendAsync
        var mockClientProxy = new Mock<IClientProxy>(MockBehavior.Loose);
        mockClients.Setup(c => c.Group(It.IsAny<string>())).Returns(mockClientProxy.Object);
        
        // Also mock All, User, and others that might be called
        mockClients.Setup(c => c.All).Returns(mockClientProxy.Object);
        mockClients.Setup(c => c.User(It.IsAny<string>())).Returns(mockClientProxy.Object);
        
        return mockHub;
    }
}
