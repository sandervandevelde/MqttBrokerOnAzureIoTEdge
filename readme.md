# Mqtt Broker On Azure IoT Edge

This is a demonstration on how to deploy an MQTT Broker on Azure IoT Edge. Next to the generic MQTT broker, a custom MQTT message producer module and an MQTT message consumer module is deployed. MQTT messages are then sent to the cloud.

## Blog Post

More background information and a demonstration of the logic is available at [my blog](https://sandervandevelde.wordpress.com/2022/08/25/running-hivemq-mqtt-broker-on-azure-iot-edge/).

![image](https://user-images.githubusercontent.com/694737/186622650-f463dc9a-d8a6-4033-ac91-eac145cd2b0c.png)

## Disclaimer

Note: Neither am I related to HiveMQ nor do I get any compensation for you using HiveMQ. This simple Docker support was the reason to check it out.

You can just use your own MQTT broker, as docker container or anywhere else in the local network, as long as you keep the topics on par.

## License

This repo is available under MIT license.
