using Grpc.Net.Client;
using GrpcServerTeste;

namespace DiagnosticSourceAndListener
{
    public class ClienteGrpc
    {
        private GrpcChannel _channel;
        private Greeter.GreeterClient _greeterClient;
        public ClienteGrpc()
        {
            _channel = GrpcChannel.ForAddress("http://localhost:5066");
            _greeterClient = new Greeter.GreeterClient(_channel);
        }

        public string SayHello ()
        {
            var response = _greeterClient.SayHello(new HelloRequest
            {
                Name = "Beatriz"
            });
            return response.Message;
        }
    }
}
