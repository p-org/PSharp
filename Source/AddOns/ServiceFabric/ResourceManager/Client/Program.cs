namespace ResourceManager.Client.Console
{
    using Grpc.Core;
    using System;
    using static ResourceManager.Contracts.ResourceManagerService;

    class Program
    {
        public static void Main()
        {
            Console.Write("Please enter address : ");
            string address = Console.ReadLine();
            Channel channel = new Channel(address, ChannelCredentials.Insecure);
            ResourceManagerServiceClient client = new ResourceManagerServiceClient(channel);
            channel.ShutdownAsync().Wait();
            Console.WriteLine("Done!!");
            Console.ReadLine();
        }
    }
}
