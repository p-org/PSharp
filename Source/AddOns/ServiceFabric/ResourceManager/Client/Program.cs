namespace ResourceManager.Client.Console
{
    using Grpc.Core;
    using System;
    using static ResourceManager.Contracts.ResourceManagerService;

    class Program
    {
        public static void Main()
        {
            Console.Write("Please enter address: ");
            string address = Console.ReadLine();
            Channel channel = new Channel(address, ChannelCredentials.Insecure);
            ResourceManagerServiceClient client = new ResourceManagerServiceClient(channel);
            client.CreateResource(new Contracts.CreateResourceRequest()
            {
                RequestId = Guid.NewGuid().ToString(),
                ResourceType = "SomeType"
            });

            channel.ShutdownAsync().Wait();
            Console.WriteLine("Done!!");
            Console.ReadLine();
        }
    }
}
