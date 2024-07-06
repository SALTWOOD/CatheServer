namespace CatheServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CatheApiServer server = new CatheApiServer(25565);
            server.Start();
            server.Wait();
        }
    }
}
