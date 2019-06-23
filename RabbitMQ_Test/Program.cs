using System;
using System.Text;
using System.Threading;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace RabbitMQ_Test {
    class Program {
        static void Main(string[] args) {
            var factory = new ConnectionFactory() { HostName = "192.168.1.2" };

            var connection = factory.CreateConnection();

            var channel = connection.CreateModel();

            channel.QueueDeclare(
                    queue: "convert",
                    durable: true, // 持久性，確保RMQ出問題可以繼續存在
                    exclusive: false, // 不獨佔
                    autoDelete: false, // 不自動清除
                    arguments: null);
            channel.QueueDeclare(
                    queue: "convert-success",
                    durable: true, // 持久性，確保RMQ出問題可以繼續存在
                    exclusive: false, // 不獨佔
                    autoDelete: false, // 不自動清除
                    arguments: null);
            channel.QueueDeclare(
                    queue: "convert-fail",
                    durable: true, // 持久性，確保RMQ出問題可以繼續存在
                    exclusive: false, // 不獨佔
                    autoDelete: false, // 不自動清除
                    arguments: null);

            var convertSuccess = new EventingBasicConsumer(channel);
            convertSuccess.Received += (model, ea) => {
                var path = Encoding.UTF8.GetString(ea.Body);
                Console.WriteLine("轉檔成功: " + path);
            };

            var convertFail = new EventingBasicConsumer(channel);
            convertFail.Received += (model, ea) => {
                var message = Encoding.UTF8.GetString(ea.Body);
                Console.WriteLine("轉檔失敗: " + message);
            };


            channel.BasicConsume(
                queue: "convert-success",
                autoAck: true, // 自動ACK
                consumer: convertSuccess);
            channel.BasicConsume(
                queue: "convert-fail",
                autoAck: true, // 自動ACK
                consumer: convertFail);

            channel.BasicPublish(string.Empty, "convert", null, Encoding.UTF8.GetBytes(@"D:\不存在檔案.mp4"));
            channel.BasicPublish(string.Empty, "convert", null, Encoding.UTF8.GetBytes(@"D:\SampleFiles\SampleVideo_1280x720_10mb.mp4"));

            Console.ReadKey();
        }
    }
}
