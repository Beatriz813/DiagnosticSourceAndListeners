// See https://aka.ms/new-console-template for more information

/* DiagnosticSource é uma abstração
DiagnosticListener é a implementação de DiagnosticSource
IObserver vs IObservable (Observador vs Observavel)
 */
using DiagnosticSourceAndListener;
using System.Diagnostics;
using System.Net.Http;
using System.Reflection;

MyListener TheListener = new MyListener();
TheListener.Listening();        // Registra os eventos
//HTTPClient Client = new HTTPClient();
//Client.SendWebRequest("https://docs.microsoft.com/dotnet/core/diagnostics/");


var clientegrpc = new ClienteGrpc();
string retorno = clientegrpc.SayHello();
Console.WriteLine($"Retorno chamada ==> {retorno}");

class HTTPClient
{
    private static DiagnosticSource httpLogger = new DiagnosticListener("System.Net.Http");
    public byte[] SendWebRequest(string url)
    {
        if (httpLogger.IsEnabled("RequestStart"))
        {
            httpLogger.Write("RequestStart", new { Url = url });
        }
        //Pretend this sends an HTTP request to the url and gets back a reply.
        byte[] reply = new byte[] { };
        return reply;
    }
}

/* Para criar um observador é preciso passa uma ação para ser executada depois e uma para ser excutada quando o evento estiver completo
 
 */
class Observer<T> : IObserver<T>
{
    private Action<T> _onNext;
    private Action _onCompleted;
    public Observer(Action<T> onNext, Action onCompleted)
    {
        _onNext = onNext ?? new Action<T>(_ => { });
        _onCompleted = onCompleted ?? new Action(() => { });
    }
    public void OnCompleted() { _onCompleted(); }
    public void OnError(Exception error) { }
    public void OnNext(T value) { _onNext(value); }
    
}


static class PropertyExtensions
{
    public static object GetProperty(this object _this, string propertyName)
    {
        return _this.GetType().GetTypeInfo().GetDeclaredProperty(propertyName)?.GetValue(_this);
    }
}

class MyListener
{
    IDisposable networkSubscription;
    IDisposable listenerSubscription;
    private readonly object allListeners = new();
    public void Listening()
    {
        Action<KeyValuePair<string, object>> whenHeard = delegate (KeyValuePair<string, object> data)
        {
            if (data.Key == "Grpc.Net.Client.GrpcOut.Stop")
            {
                var response = data.Value.GetProperty("Response") as HttpResponseMessage;
                //Console.WriteLine($"Resposta pelo evento ==> {data.Value}");
            }
            if (data.Key == "System.Net.Http.Response")
            {
                var response = data.Value.GetProperty("Response");
                var content = response.GetProperty("Content") as HttpContent;
                var conteudo = content.ReadAsStringAsync().Result;
                Console.WriteLine($"Conteudo => {conteudo}");
            }
            Console.WriteLine($"Data received: {data.Key}: {data.Value}");
        };
        Action<DiagnosticListener> onNewListener = delegate (DiagnosticListener listener)
        {
            Console.WriteLine($"New Listener discovered: {listener.Name}");
            //Suscribe to the specific DiagnosticListener of interest.
            // Quando um DiagnosticListener com o nome System.Net.Http for criado
            if (listener.Name == "System.Net.Http")
            {
                //Use lock to ensure the callback code is thread safe.
                lock (allListeners)
                {
                    if (networkSubscription != null)
                    {
                        networkSubscription.Dispose();
                    }
                    IObserver<KeyValuePair<string, object>> iobserver = new Observer<KeyValuePair<string, object>>(whenHeard, null);
                    networkSubscription = listener.Subscribe(iobserver);
                }

            }
            //if (listener.Name == "Grpc.Net.Client")
            //{
            //    lock (allListeners)
            //    {
            //        if (networkSubscription != null)
            //        {
            //            networkSubscription.Dispose();
            //        }
            //        IObserver<KeyValuePair<string, object>> iobserver = new Observer<KeyValuePair<string, object>>(whenHeard, null);
            //        networkSubscription = listener.Subscribe(iobserver);
            //    }
            //}
            if (listener.Name == "HttpHandlerDiagnosticListener")
            {
                lock (allListeners)
                {
                    if (networkSubscription != null)
                    {
                        networkSubscription.Dispose();
                    }
                    IObserver<KeyValuePair<string, object>> iobserver = new Observer<KeyValuePair<string, object>>(whenHeard, null);
                    networkSubscription = listener.Subscribe(iobserver);
                }
            }
        };
        //Subscribe to discover all DiagnosticListeners
        IObserver<DiagnosticListener> observer = new Observer<DiagnosticListener>(onNewListener, null);
        //When a listener is created, invoke the onNext function which calls the delegate.
        // AllListeners é um observável
        // Sempre que um DiagnpsticListener for criado os metodos do observador observer serão chamados (onNewListener)
        listenerSubscription = DiagnosticListener.AllListeners.Subscribe(observer);
    }
    // Typically you leave the listenerSubscription subscription active forever.
    // However when you no longer want your callback to be called, you can
    // call listenerSubscription.Dispose() to cancel your subscription to the IObservable.
}


