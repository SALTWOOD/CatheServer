namespace CatheServer
{
    internal class Program
    {
        static void Main(string[] args)
        {
            CatheApiServer server = new CatheApiServer(8888);
            server.Start();
            server.Wait();
        }
    }
}
