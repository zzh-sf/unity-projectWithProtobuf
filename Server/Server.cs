using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using System.Net.Sockets;
using System.Net;
using ConsoleApp3.Controller;
using MySql.Data.MySqlClient;
using ConsoleApp3.DATA;
using System.Security.Cryptography.X509Certificates;
using SocketGameProtool;
namespace ConsoleApp3.Server
{
    class Server
    {
        private Socket socket;
        private List<Client> clientList = new List<Client>();
        private ControllerManager controllerManager;
        private bool isRunning = false;
        private List<Room> roomList = new List<Room>();
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
        public MainPack CreateRoom(Client client, MainPack pack) 
        {
            try
            {
                // 检查玩家是否已经在房间中
                if (client.GetRoom != null)
                {
                    pack.ReturnCode = ReturnCode.Fail;
                    Console.WriteLine("Client is already in a room, cannot create new room");
                    return pack;
                }
                
                Room room = new Room(client, pack.RoomPac[0]);
                roomList.Add(room);
                
                // 返回更新后的房间信息
                pack.RoomPac.Clear();
                pack.RoomPac.Add(room.RoomPack);
                
                // 添加玩家列表
                foreach (PlayerPack p in room.GetPlayerInFo()) { 
                    pack.PlayPack.Add(p);
                }
                
                Console.WriteLine("Room created successfully. Room name: " + room.roomname + ", Total rooms: " + roomList.Count);
                pack.ReturnCode = ReturnCode.Success;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error creating room: " + ex.Message);
                pack.ReturnCode = ReturnCode.Fail;
                return pack;
            }
            return pack;
        }
        public MainPack FindRoom() {
            MainPack pack = new MainPack();
            pack.Actioncode = ActionCode.FindRoom;
            try
            {
                if (roomList.Count == 0) { 
                pack.ReturnCode= ReturnCode.Fail;
                    return pack;
                }
                foreach (Room room in roomList)
                {
                    pack.Actioncode = ActionCode.FindRoom;
                    pack.RoomPac.Add(room.RoomPack);
                }
                pack.ReturnCode = ReturnCode.Success;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error finding room: " + ex.Message);
                pack.ReturnCode = ReturnCode.Fail;
            }
            return pack;
        }
        public MainPack JoinRoom(Client client, MainPack pack) 
        {
            // 检查玩家是否已经在房间中
            if (client.GetRoom != null)
            {
                pack.ReturnCode = ReturnCode.Fail;
                Console.WriteLine("Client is already in a room");
                return pack;
            }
            
            // 查找第一个可用的房间
            foreach (Room r in roomList) {
                if (r.state == 0 && r.clientList.Count < r.maxnum)
                {
                    r.Join(client);
                    
                    // 添加房间信息
                    pack.RoomPac.Add(r.RoomPack);
                    // 添加玩家列表
                    foreach (PlayerPack p in r.GetPlayerInFo()) {
                        pack.PlayPack.Add(p);
                    }
                    pack.ReturnCode = ReturnCode.Success;
                    Console.WriteLine("Client joined room: " + r.roomname);
                    return pack;
                }
            }
            // 没有找到可用房间
            pack.ReturnCode = ReturnCode.NotRoom;
            Console.WriteLine("No available room found");
            return pack;
        }
        public MainPack ExitRoom(Client client, MainPack pack)
        {
            if (client.GetRoom == null)
            {
                pack.ReturnCode = ReturnCode.Fail;
                Console.WriteLine("Client is not in any room");
                return pack;
            }
            client.GetRoom.Exit(this,client);
            pack.ReturnCode = ReturnCode.Success;
            Console.WriteLine("Client exited room");
            return pack;
        }
        public void RemoveRoom(Room room) { 
        roomList.Remove(room);
          Console.WriteLine("Room removed: " + room.roomname);
        }
        public MainPack Chat(Client client, MainPack pack) {
            // 检查玩家是否在房间中
            if (client.GetRoom == null)
            {
                Console.WriteLine("Client is not in any room, cannot send chat message");
                return null;
            }
            
            // 创建新的消息包，避免修改原始数据
            MainPack chatPack = new MainPack();
            chatPack.Actioncode = ActionCode.Chat;
            chatPack.Str = client.userName + ":" + pack.Str;
            
            // 广播给房间内所有玩家
            client.GetRoom.Broadcast(chatPack);
            
            Console.WriteLine("Chat message broadcasted: " + chatPack.Str);
            return null;  // 聊天消息只需要广播，不需要返回给发送者
        }
        public MainPack StartGame(Client client, MainPack pack) { 
            // 检查玩家是否在房间中
            if (client.GetRoom == null)
            {
                Console.WriteLine("Client is not in any room, cannot start game");
                return null;
            }
            
            ReturnCode result = client.GetRoom.StartGame(client);
            Console.WriteLine("Game start request processed, result: " + result);
            return null;  // 游戏开始通过广播通知，不需要返回特定响应
        }
    }
}