using Grpc.Core;
using Grpc.Net.Client;
using GrpcServer; 
using System.Threading.Tasks;
using System;
using System.Net.Http;

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("==============================================");
        Console.WriteLine("   gRPC JWT Authentication Demo Client");
        Console.WriteLine("==============================================\n");

        var serverAddress = "http://localhost:5247";
        string? token = null;

        // Configure for HTTP/2
        var httpHandler = new SocketsHttpHandler
        {
            EnableMultipleHttp2Connections = true
        };

        var channel = GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions
        {
            HttpHandler = httpHandler
        });

        try
        {
            while (true)
            {
                Console.WriteLine("Menu:");
                Console.WriteLine("1. Login");
                Console.WriteLine("2. Call SayHello (requires authentication)");
                Console.WriteLine("3. Get Secure Data (requires Admin role)");
                Console.WriteLine("4. Exit");
                Console.Write("\nSelect option: ");

                var choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            token = await LoginAsync(channel);
                            break;
                        case "2":
                            await CallSayHelloAsync(channel, token);
                            break;
                        case "3":
                            await GetSecureDataAsync(channel, token);
                            break;
                        case "4":
                            Console.WriteLine("Goodbye");
                            return;
                        default:
                            Console.WriteLine("Invalid option!");
                            break;
                    }
                }
                catch (RpcException ex)
                {
                    Console.WriteLine($"gRPC Error: {ex.Status.StatusCode}");
                    Console.WriteLine($"   Detail: {ex.Status.Detail}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
            }
        }
        finally
        {
            await channel.ShutdownAsync();
            httpHandler.Dispose();
        }
    }

    static async Task<string> LoginAsync(GrpcChannel channel)
    {
        Console.Write("\n👤 Username: ");
        var username = Console.ReadLine();
        Console.Write("Password: ");
        var password = Console.ReadLine();

        var authClient = new Auth.AuthClient(channel);
        var response = await authClient.LoginAsync(new LoginRequest
        {
            Username = username,
            Password = password
        });

        if (response.Success)
        {
            Console.WriteLine($"{response.Message}");
            Console.WriteLine($"Token: {response.Token[..Math.Min(50, response.Token.Length)]}...");
            return response.Token;
        }

        Console.WriteLine($"Login failed: {response.Message}");
        return string.Empty;
    }

    static async Task CallSayHelloAsync(GrpcChannel channel, string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Please login first!");
            return;
        }

        Console.Write("Enter your name: ");
        var name = Console.ReadLine();

        var client = new Greeter.GreeterClient(channel);
        var metadata = new Metadata
        {
            { "Authorization", $"Bearer {token}" }
        };

        var response = await client.SayHelloAsync(
            new HelloRequest { Name = name },
            metadata
        );

        Console.WriteLine($"Server Response: {response.Message}");
    }

    static async Task GetSecureDataAsync(GrpcChannel channel, string? token)
    {
        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("Please login first!");
            return;
        }

        Console.Write("Enter data ID: ");
        var id = Console.ReadLine();

        var client = new Greeter.GreeterClient(channel);
        var metadata = new Metadata
        {
            { "Authorization", $"Bearer {token}" }
        };

        var response = await client.GetSecureDataAsync(
            new DataRequest { Id = id },
            metadata
        );

        Console.WriteLine($"Server Response: {response.Data}");
    }
}
