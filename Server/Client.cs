using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using ConsoleApp3.Tool;
using ConsoleApp3.DATA;
using SocketGameProtool;

namespace ConsoleApp3.Server
{
    internal class Client
    {
        private Socket _socket;
        Message message;
        UserData userData;
        Server server;
        
        public UserData GetUserData
        {
            get { return userData; }
        }
        
        public Client(Socket socket)
        {
            message = new Message();
            userData = new UserData();
            _socket = socket;
            server = null; // 明确初始化为null
            StartReceiving();
        }
        
        // 添加设置服务器引用的方法
        public void SetServer(Server server)
        {
            this.server = server;
        }
        
        void StartReceiving()
        {
            if (_socket == null || !_socket.Connected)
            {
                Console.WriteLine("Socket is not connected, cannot start receiving");
                return;
            }
            
            try
            {
                _socket.BeginReceive(message.Buffer, 0, message.Buffer.Length, SocketFlags.None, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to start receiving: " + ex.Message);
                CloseSocket();
            }
        }
        
        void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                if (_socket == null || !_socket.Connected) 
                {
                    Console.WriteLine("Socket is not connected in receive callback");
                    return;
                }
                
                int len = _socket.EndReceive(ar);
                Console.WriteLine("Received " + len + " bytes from client");
                
                if (len == 0)
                {
                    Console.WriteLine("Client disconnected");
                    CloseSocket();
                    return;
                }
                
                message.ReadBuffer(len, HandleRequest);
                StartReceiving();
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Socket has been disposed");
            }
            catch (SocketException se)
            {
                Console.WriteLine("Socket error in receive callback: " + se.Message);
                CloseSocket();
            }
            catch (Exception ex)
            {
                Console.WriteLine("General error in receive callback: " + ex.Message);
                CloseSocket();
            }
        }
        
        public void Send(MainPack pack) 
        { 
            if (_socket == null || !_socket.Connected)
            {
                Console.WriteLine("Cannot send data, socket is not connected");
                return;
            }
            
            try
            {
                byte[] data = Message.PackData(pack);
                int bytesSent = _socket.Send(data);
                Console.WriteLine("Response sent to client: " + pack.ReturnCode + " (" + bytesSent + " bytes)");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send data: " + ex.Message);
            }
        }
        
        void HandleRequest(MainPack pack)
        {
            Console.WriteLine("Received request: " + pack.Actioncode);
            if (server != null)
            {
                server.HandleRequest(pack, this);
            }
            else
            {
                Console.WriteLine("Server reference is null, cannot handle request");
            }
        }
        
        public bool Logon(MainPack pack)
        {
            Console.WriteLine("Client.Logon called with username: " + pack.LoginPack?.Username);
            if (userData != null)
            {
                bool result = userData.Logon(pack);
                Console.WriteLine("UserData.Logon returned: " + result);
                return result;
            }
            else
            {
                Console.WriteLine("UserData is null, cannot perform login");
                return false;
            }
        }
        
        private void CloseSocket()
        {
            try
            {
                if (_socket != null && _socket.Connected)
                {
                    _socket.Shutdown(SocketShutdown.Both);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error shutting down socket: " + ex.Message);
            }
            finally
            {
                if (_socket != null)
                {
                    _socket.Close();
                    _socket = null;
                }
                Console.WriteLine("Client socket closed");
            }
        }
    }
}