namespace Kivikko.PipeAppRunner.Tests;

public class ConnectionTests
{
    [Test]
    public void ConnectionTest()
    {
        var server = PipeServer.Create();
        server.Start();
        var client = PipeClient.CreateAndStart(server.Name);
        
        Assert.Multiple(() =>
        {
            Assert.That(client.IsConnected, Is.True);
            Assert.That(server.IsConnected, Is.True);
        });
    }

    [Test]
    public void WrongNameConnectionTest()
    {
        var server = PipeServer.Create();
        server.Start();
        var client = PipeClient.CreateAndStart("wrong pipe name");
        
        Assert.Multiple(() =>
        {
            Assert.That(client.IsConnected, Is.False);
            Assert.That(server.IsConnected, Is.False);
        });
    }
    
    [Test]
    public async Task SendMessageTest()
    {
        var server = PipeServer.Create();
        server.Start();
        var client = PipeClient.CreateAndStart(server.Name);

        string? clientReceivedMessage = null;
        string? serverReceivedMessage = null;

        server.MessageReceived += (_, message) =>
        {
            Console.WriteLine($"Server received a message: {message}");
            serverReceivedMessage = message;
        };
        
        client.MessageReceived += (_, message) =>
        {
            Console.WriteLine($"Client received a message: {message}");
            clientReceivedMessage = message;
        };

        const string messageFromClient = "Hello server";
        const string messageFromServer = "Hello client";
        
        await client.SendMessage(messageFromClient);
        await server.SendMessage(messageFromServer);
        
        await Task.Delay(10);
        
        Assert.Multiple(() =>
        {
            Assert.That(clientReceivedMessage, Is.EqualTo(messageFromServer));
            Assert.That(serverReceivedMessage, Is.EqualTo(messageFromClient));
        });
    }
}