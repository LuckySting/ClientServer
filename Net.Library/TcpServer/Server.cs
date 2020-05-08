using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SomeProject.Library.Server
{
    /// <summary>
    /// Класс сервера для принятия файлов и текстовых сообщений
    /// </summary>
    public class Server
    {
        static string directory;
        static int currFileNamber;
        static int maxConnections = 3;
        static int currentConnections = 0;
        TcpListener serverListener;

        public Server()
        {
            directory = DateTime.Today.ToString("yyyy-MM-dd");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            } else
            {
                currFileNamber = Directory.GetFiles(directory).Length;
            }

            serverListener = new TcpListener(IPAddress.Loopback, 8080);
        }

        /// <summary>
        /// Логирует завершение сервера
        /// </summary>
        /// <returns>Сервер завершен успешно?</returns>
        public bool TurnOffListener()
        {
            try
            {
                if (serverListener != null)
                    serverListener.Stop();
                return true;
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot turn off listener: " + e.Message);
                return false;
            }
        }

        /// <summary>
        /// Запуск сервера на прослушиваение порта 8080, каждый клиент обрабатывается отдельным потоком
        /// </summary>
        /// <returns></returns>
        public async Task TurnOnListener()
        {
            try
            {
                if (serverListener != null)
                    serverListener.Start();
                while (true)
                {
                    Console.WriteLine("Waiting for connections...");
                    TcpClient client = await serverListener.AcceptTcpClientAsync();
                    Console.WriteLine("Accepted connection: " + (currentConnections + 1));
                    _ = ReceivePacketFromClient(client);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot turn on listener: " + e.Message);
            }
        }

        /// <summary>
        /// Асинхронно принимает пакет от клиента, определяет его тип и вызывает соответствующий обработчик
        /// </summary>
        /// <param name="client">клиент</param>
        public async Task ReceivePacketFromClient(TcpClient client)
        {
            try
            {
                
                StringBuilder recievedMessage = new StringBuilder();

                Interlocked.Increment(ref currentConnections);
                int clientNum = currentConnections;
                if (currentConnections == maxConnections)
                {
                    throw new Exception("Client " + clientNum + " declined: Server is overloaded");
                }
                //TODO:  Убрать задержку
                await Task.Delay(10000);
                byte[] data = new byte[1];
                NetworkStream stream = client.GetStream();

                do
                {
                    int bytes = stream.Read(data, 0, data.Length);
                    recievedMessage.Append(Encoding.UTF8.GetString(data, 0, bytes));
                    if (data[0] == 62)
                    {
                        break;
                    }
                } while (stream.DataAvailable);

                var header = recievedMessage.ToString().Trim(new char[] { '<', '>' }).Split(',');

                OperationResult resp;

                if (header[0] == "type=text")
                {
                    resp = await ReceiveMessageFromClient(stream);
                } else if (header[0] == "type=file")
                {
                    resp = await ReceiveFileFromClient(stream, header[1]);
                } else
                {
                    resp = new OperationResult(Result.Fail, "Unknown packet type");
                }

                

                if (resp.Result == Result.OK)
                {
                    Console.WriteLine("New message from client " + clientNum + ": " + resp.Message);
                    _ = await SendMessageToClient(stream, "Success");
                } else
                {
                    _ = await SendMessageToClient(stream, resp.Message);
                }

                stream.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error: " + e.Message);
            }
            finally
            {
                Interlocked.Decrement(ref currentConnections);
                client.Close();
            }
        }

        /// <summary>
        /// Асинхронно принимает текстовое сообщение
        /// </summary>
        /// <param name="stream">Сетевой поток клиента</param>
        /// <returns>Результат операции</returns>
        public async Task<OperationResult> ReceiveMessageFromClient(NetworkStream stream)
        {
            try
            {
                StringBuilder recievedMessage = new StringBuilder();

                byte[] data = new byte[256];

                do
                {
                    int bytes = stream.Read(data, 0, data.Length);
                    recievedMessage.Append(Encoding.UTF8.GetString(data, 0, bytes));
                }
                while (stream.DataAvailable);

                return new OperationResult(Result.OK, recievedMessage.ToString());
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
        }

        /// <summary>
        /// Асинхронно принимает файл от клиента
        /// </summary>
        /// <param name="stream">Сетевой поток клиента</param>
        /// <param name="extention">Расширение файла</param>
        public async Task<OperationResult> ReceiveFileFromClient(NetworkStream stream, string extention)
        {
            try
            {
                var ext = extention.Split('=')[1];
                var fileNumber = Interlocked.Increment(ref currFileNamber);
                var fileName = "File" + fileNumber + "." + ext;
                var fileStream = File.Create(Path.Combine(directory, fileName));

                byte[] data = new byte[256];

                var file = new List<byte>();

                int offset = 0;
                do
                {
                    int bytes = stream.Read(data, 0, data.Length);
                    foreach (var b in data)
                    {
                        file.Add(b);
                    }

                    offset += bytes;
                }
                while (stream.DataAvailable);

                await fileStream.WriteAsync(file.ToArray(), 0, file.Count);

                fileStream.Close();

                return new OperationResult(Result.OK, "Recieved " + fileName);
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
        }

        /// <summary>
        /// Отправляет сообщение клиенту
        /// </summary>
        /// <param name="message">сообщение</param>
        /// <returns></returns>
        public async Task<OperationResult> SendMessageToClient(NetworkStream stream, string message)
        {
            try
            {
                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);
                stream.Close();
                return new OperationResult(Result.OK, "");
            }
            catch (Exception e)
            {
                stream.Close();
                return new OperationResult(Result.Fail, e.Message);
            }
        }
    }
}