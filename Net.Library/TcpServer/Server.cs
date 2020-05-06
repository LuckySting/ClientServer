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
    public class Server
    {
        static string directory;
        static int currFileNamber;
        TcpListener serverListener;

        public Server()
        {
            directory = DateTime.Today.ToString("yyyy-mm-dd");
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            } else
            {
                currFileNamber = Directory.GetFiles(directory).Length;
            }

            serverListener = new TcpListener(IPAddress.Loopback, 8080);
        }

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

        public async Task TurnOnListener()
        {
            try
            {
                if (serverListener != null)
                    serverListener.Start();
                while (true)
                {
                    OperationResult result = await ReceivePacketFromClient();
                    if (result.Result == Result.Fail)
                        Console.WriteLine("Unexpected error: " + result.Message);
                    else
                        Console.WriteLine("New message from client: " + result.Message);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Cannot turn on listener: " + e.Message);
            }
        }

        public async Task<OperationResult> ReceivePacketFromClient()
        {
            try
            {
                Console.WriteLine("Waiting for connections...");
                StringBuilder recievedMessage = new StringBuilder();
                TcpClient client = serverListener.AcceptTcpClient();

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

                if (header[0] == "type=text")
                {
                    var resp = await ReceiveMessageFromClient(stream);
                    stream.Close();
                    client.Close();
                    return resp;
                } else if (header[0] == "type=file")
                {
                    var resp = await ReceiveFileFromClient(stream, header[1]);
                    stream.Close();
                    client.Close();
                    return resp;
                } else
                {
                    throw new Exception("Unknown packet type");
                }
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
        }

        public async Task<OperationResult> ReceiveMessageFromClient(NetworkStream stream)
        {
            try
            {
                Console.WriteLine("Message received!");
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

        public async Task<OperationResult> ReceiveFileFromClient(NetworkStream stream, string extention)
        {
            try
            {
                Console.WriteLine("File received!");
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

                fileStream.Write(file.ToArray(), 0, file.Count);

                fileStream.Close();

                return new OperationResult(Result.OK, "Recieved " + fileName);
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
        }

        public OperationResult SendMessageToClient(string message)
        {
            try
            {
                TcpClient client = serverListener.AcceptTcpClient();
                NetworkStream stream = client.GetStream();

                byte[] data = Encoding.UTF8.GetBytes(message);
                stream.Write(data, 0, data.Length);

                stream.Close();
                client.Close();
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
            return new OperationResult(Result.OK, "");
        }
    }
}