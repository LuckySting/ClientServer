using System;
using System.Text;
using System.Net.Sockets;
using System.IO;

namespace SomeProject.Library.Client
{
    public class Client
    {
        public TcpClient tcpClient;

        public OperationResult ReceiveMessageFromServer()
        {
            try
            {
                tcpClient = new TcpClient("127.0.0.1", 8080);
                StringBuilder recievedMessage = new StringBuilder();
                byte[] data = new byte[256];
                NetworkStream stream = tcpClient.GetStream();

                do
                {
                    int bytes = stream.Read(data, 0, data.Length);
                    recievedMessage.Append(Encoding.UTF8.GetString(data, 0, bytes));
                }
                while (stream.DataAvailable);
                stream.Close();
                tcpClient.Close();

                return new OperationResult(Result.OK, recievedMessage.ToString());
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.ToString());
            }
        }

        public OperationResult SendMessageToServer(string message)
        {
            try
            {
                tcpClient = new TcpClient("127.0.0.1", 8080);
                NetworkStream stream = tcpClient.GetStream();
                var header = Encoding.UTF8.GetBytes("<type=text>");
                var data = System.Text.Encoding.UTF8.GetBytes(message);
                byte[] packet = new byte[header.Length + data.Length];
                header.CopyTo(packet, 0);
                data.CopyTo(packet, header.Length);
                stream.Write(packet, 0, packet.Length);
                stream.Close();
                tcpClient.Close();
                return new OperationResult(Result.OK, "") ;
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
        }

        public OperationResult SendFileToServer(string filePath, string extention)
        {
            try
            {
                tcpClient = new TcpClient("127.0.0.1", 8080);
                NetworkStream stream = tcpClient.GetStream();
                var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read);
                var data = new byte[fileStream.Length];
                fileStream.Read(data, 0, (int)fileStream.Length);
                var header = Encoding.UTF8.GetBytes("<type=file,ext=" + extention + ">");
                byte[] packet = new byte[header.Length + data.Length];
                header.CopyTo(packet, 0);
                data.CopyTo(packet, header.Length);
                stream.Write(packet, 0, (packet.Length));
                fileStream.Close();
                stream.Close();
                tcpClient.Close();
                return new OperationResult(Result.OK, "");
            }
            catch (Exception e)
            {
                return new OperationResult(Result.Fail, e.Message);
            }
        }
    }
}
