using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Threading;
using XWidget.FFMpeg;

namespace FFMpegConverter {
    class Program {
        static void Main(string[] args) {
            var sdConverter = new FFMpegConverterBuilder()
                .SetExecutePath(@"D:\FFMpeg-Runtime\bin\ffmpeg.exe")
                .ConfigVideo(v => {
                    v.SetSize(CommonSize.SD);
                })
                .Build();

            var factory = new ConnectionFactory() { HostName = "192.168.1.2" };
            var connection = factory.CreateConnection();
            var channel = connection.CreateModel();

            var consumer = new EventingBasicConsumer(channel);
            consumer.Received += (model, ea) => {
                var path = Encoding.UTF8.GetString(ea.Body);
                var temp_outputPath = @"D:\OutputFiles\" + Guid.NewGuid() + ".mp4";

                sdConverter.Convert(path, temp_outputPath)
                    .Subscribe(
                        (ConvertResult result) => {
                            channel.BasicAck(ea.DeliveryTag, false); // 任務處理完畢，剔除Queue

                            if (result.ExitCode == 0) {
                                // 送出轉檔成功訊息
                                channel.BasicPublish(string.Empty, "convert-success", null, Encoding.UTF8.GetBytes(temp_outputPath));
                            } else {
                                // 送出轉檔失敗訊息
                                channel.BasicPublish(string.Empty, "convert-fail", null, Encoding.UTF8.GetBytes(path));
                            }
                        }, (Exception e) => {
                            channel.BasicAck(ea.DeliveryTag, false); // 任務處理完畢，剔除Queue

                            // 送出轉檔失敗訊息
                            channel.BasicPublish(string.Empty, "convert-fail", null, Encoding.UTF8.GetBytes(path + Environment.NewLine + e.ToString()));
                        });
            };

            channel.BasicConsume(
                queue: "convert",
                autoAck: false,
                consumer: consumer);

            Console.ReadKey();
        }
    }
}
