using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using System.Net.Sockets;
using System.Net;
using ConsoleApp3.Controller;
using SocketGameProtool;
using MySql.Data.MySqlClient;
using ConsoleApp3.DATA;
using SocketGameProtool;

namespace ConsoleApp3.Server
{
    class Server
    {
        private Socket socket;
        private List<Client> clientList = new List<Client>();
        private ControllerManager controllerManager;
        private bool isRunning = false;

        public Server(int port)
        {
            try
            {
                controllerManager = new ControllerManager(this);//创建控制器管理器
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(new IPEndPoint(IPAddress.Any, port));
                socket.Listen(100);//设置连接队列长度为100
                isRunning = true;
                Console.WriteLine("Server started on port " + port);
                StartAccept();
            }
            catch (SocketException se)
            {
                Console.WriteLine("Socket error when starting server: " + se.Message);
                Console.WriteLine("Error code: " + se.SocketErrorCode);
                isRunning = false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("General error when starting server: " + ex.Message);
                isRunning = false;
            }
        }

        void StartAccept()
        {
            // 正确检查服务器Socket是否可用
            if (socket == null || !isRunning)
            {
                Console.WriteLine("Server socket is not available, cannot start accepting connections");
                return;
            }
            
            try
            {
                socket.BeginAccept(new AsyncCallback(AcceptCallback), socket);
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Server socket has been disposed, cannot start accepting connections");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to start accepting connections: " + ex.Message);
                // 如果是Socket异常，可能需要停止服务器
                if (ex is SocketException)
                {
                    isRunning = false;
                }
            }
        }
        
        void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                if (socket == null || !isRunning)
                {
                    Console.WriteLine("Server is not running, stopping accept callback");
                    return;
                }
                
                Socket clientSocket = socket.EndAccept(ar);
                Console.WriteLine("New client connected: " + clientSocket.RemoteEndPoint);
                
                // 创建客户端并设置服务器引用
                Client client = new Client(clientSocket);
                client.SetServer(this); // 设置服务器引用
                clientList.Add(client);
                
                Console.WriteLine("Client added to client list. Total clients: " + clientList.Count);
            }
            catch (ObjectDisposedException)
            {
                Console.WriteLine("Server socket has been disposed");
                isRunning = false;
                return;
            }
            catch (SocketException se)
            {
                Console.WriteLine("Socket error in accept callback: " + se.Message);
                // 某些Socket错误可能需要停止服务器
                if (se.SocketErrorCode == SocketError.Interrupted || 
                    se.SocketErrorCode == SocketError.InvalidArgument)
                {
                    isRunning = false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("General error in accept callback: " + ex.Message);
            }
            finally
            {
                // 继续接受新的连接，除非服务器已停止
                if (isRunning)
                {
                    StartAccept();
                }
                else
                {
                    Console.WriteLine("Server is not running, stopped accepting new connections");
                }
            }
        }

        public void HandleRequest(MainPack pack, Client client) {
            Console.WriteLine("Server handling request: " + pack.Actioncode);
            if (controllerManager != null)
            {
                controllerManager.HandleRequest(pack, client);
            }
            else
            {
                Console.WriteLine("ControllerManager is null, cannot handle request");
            }
        }
        
        // 添加服务器停止方法
        public void Stop()
        {
            isRunning = false;
            
            // 关闭所有客户端连接
            Console.WriteLine("Closing " + clientList.Count + " client connections");
            foreach (var client in clientList)
            {
                // 这里可能需要添加客户端关闭逻辑
            }
            clientList.Clear();
            
            // 关闭服务器Socket
            if (socket != null)
            {
                try
                {
                    socket.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error closing server socket: " + ex.Message);
                }
                finally
                {
                    socket = null;
                }
            }
            
            Console.WriteLine("Server stopped");
        }
    }
}