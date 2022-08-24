namespace ListenerModule
{
    using System;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Runtime.Loader;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Azure.Devices.Client;
    using Microsoft.Azure.Devices.Client.Transport.Mqtt;

    using MQTTnet.Client;
    using MQTTnet;

    using Newtonsoft.Json;

    class Program
    {
        static void Main(string[] args)
        {
            Init().Wait();

            // Wait until the app unloads or is cancelled
            var cts = new CancellationTokenSource();
            AssemblyLoadContext.Default.Unloading += (ctx) => cts.Cancel();
            Console.CancelKeyPress += (sender, cpe) => cts.Cancel();
            WhenCancelled(cts.Token).Wait();
        }

        /// <summary>
        /// Handles cleanup operations when app is cancelled or unloads
        /// </summary>
        public static Task WhenCancelled(CancellationToken cancellationToken)
        {
            var tcs = new TaskCompletionSource<bool>();
            cancellationToken.Register(s => ((TaskCompletionSource<bool>)s).SetResult(true), tcs);
            return tcs.Task;
        }

        /// <summary>
        /// Initializes the ModuleClient and sets up the callback to receive
        /// messages containing temperature information
        /// </summary>
        static async Task Init()
        {
            MqttTransportSettings mqttSetting = new MqttTransportSettings(TransportType.Mqtt_Tcp_Only);
            ITransportSettings[] settings = { mqttSetting };

            // Open a connection to the Edge runtime
            ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            try
            {
                // connect
                mqttFactory = new MqttFactory();

                mqttClient = mqttFactory.CreateMqttClient();

                mqttClientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("192.168.1.89", 1883)
                    .WithClientId("listener")
                    .Build();

                mqttClient.DisconnectedAsync += OnDisconnected;

                mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;

                await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                Console.WriteLine("MQTT Client connected");

                //// subscribe for MQTT incoming messages
                var mqttSubscribeOptionsdirectmethod = mqttFactory.CreateSubscribeOptionsBuilder()
                    .WithTopicFilter(f => { f.WithTopic(subscribe_topic_filter); })
                    .Build();
                await mqttClient.SubscribeAsync(mqttSubscribeOptionsdirectmethod, CancellationToken.None);

                System.Console.WriteLine($"Subscribed for topic '{subscribe_topic_filter}'");

                while (true)
                {
                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }

            System.Console.WriteLine("Exiting...");
        }

        private static ModuleClient ioTHubModuleClient = null;
        private static IMqttClient mqttClient = null;
        private static MqttFactory mqttFactory = null;
        private static MqttClientOptions mqttClientOptions = null;
        public static string subscribe_topic_filter => "producer/telemetry/#";


        private static Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            if (args.ApplicationMessage.Topic.StartsWith(subscribe_topic_filter.Replace("/#", "")))
            {
                var cloudMessage = new CloudMessage("producer", DateTime.UtcNow, args.ApplicationMessage.ConvertPayloadToString());

                var jsonMessage = JsonConvert.SerializeObject(cloudMessage);

                var messageBytes  = Encoding.UTF8.GetBytes(jsonMessage);

                using (var pipeMessage = new Message(messageBytes))
                {
                    ioTHubModuleClient.SendEventAsync("output1", pipeMessage).Wait();
                
                    Console.WriteLine($"Message '{jsonMessage}' sent");
                }
            }
            else
            {
                System.Console.WriteLine($"Unknown message '{args.ApplicationMessage.ConvertPayloadToString()}' on topic '{args.ApplicationMessage.Topic}'");
            }

            return Task.CompletedTask;
        }

        private static async Task OnDisconnected(MqttClientDisconnectedEventArgs e)
        {
            System.Console.WriteLine("Disconnect detected. Reconnect in 30 seconds...");
            await Task.Delay(TimeSpan.FromSeconds(30));
            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);
            System.Console.WriteLine("Reconnected.");
        }
    }
}
