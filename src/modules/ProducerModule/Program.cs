namespace ProducerModule
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
    using MQTTnet;
    using MQTTnet.Client;

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
            ModuleClient ioTHubModuleClient = await ModuleClient.CreateFromEnvironmentAsync(settings);
            await ioTHubModuleClient.OpenAsync();
            Console.WriteLine("IoT Hub module client initialized.");

            try
            {
                // connect
                mqttFactory = new MqttFactory();

                mqttClient = mqttFactory.CreateMqttClient();

                mqttClientOptions = new MqttClientOptionsBuilder()
                    .WithTcpServer("192.168.1.89", 1883)
                    .WithClientId("producer")
                    .Build();

                mqttClient.DisconnectedAsync += OnDisconnected;

                await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

                Console.WriteLine("MQTT Client connected");

                var thread = new Thread(() => ThreadBody());
                thread.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception: {ex.Message}");
            }
        }

        private static IMqttClient mqttClient = null;

        private static MqttFactory mqttFactory = null;

        private static MqttClientOptions mqttClientOptions = null;

        private static string send_message_topic = "producer/telemetry";

        private static async void ThreadBody()
        {
            while(true)
            {
                try
                {
                    var message = new MqttApplicationMessageBuilder()
                    .WithTopic(send_message_topic)
                    .WithPayload(Encoding.UTF8.GetBytes("Message sent by producer"))
                    .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                    .Build();

                    await mqttClient.PublishAsync(message);
                    Console.WriteLine($"Message sent at {DateTime.UtcNow.ToString("u")}");
                }
                catch
                {
                    System.Console.WriteLine("Client not found. Message skipped.");
                }

                Thread.Sleep(5000); 
            }
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
