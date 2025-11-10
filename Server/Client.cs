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
        public string userName;
        public UserInfo userInfo { get; set; }
        public Room GetRoom {
            get;set;
        }
        public class UserInfo {
            public string UserName { set; get; }
            public int HP { set; get; }
            public PosPack Pos { get; set; }
        }
        public UserData GetUserData
        {
            get { return userData; }
        }
        
        public Client(Socket socket)
        {
            message = new Message();
            userData = new UserData();
            userInfo = new UserInfo();  // 初始化 userInfo
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
                // 输出详细的消息信息
                Console.WriteLine("Sending message - ActionCode: " + pack.Actioncode + 
                                ", RequestCode: " + pack.RequestCode + 
                                ", ReturnCode: " + pack.ReturnCode + 
                                ", PlayPack count: " + pack.PlayPack.Count + 
                                ", Str: " + (pack.Str ?? "null"));
                
                // 如果有玩家列表，输出每个玩家的详细信息
                if (pack.PlayPack.Count > 0)
                {
                    Console.WriteLine("  Players in pack:");
                    foreach (var player in pack.PlayPack)
                    {
                        Console.WriteLine("    - " + player.PlayerName + " (ID: " + player.PlayerId + 
                                        ", HP: " + player.Hp + 
                                        ", Pos: " + (player.PosPack != null ? 
                                        "(" + player.PosPack.PosX + ", " + player.PosPack.PosY + ")" : "null") + ")");
                    }
                }
                
                byte[] data = Message.PackData(pack);
                Console.WriteLine("Serialized to " + data.Length + " bytes");
                
                int bytesSent = _socket.Send(data);
                Console.WriteLine("Successfully sent " + bytesSent + " bytes to " + userName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to send data: " + ex.Message);
                Console.WriteLine("Stack trace: " + ex.StackTrace);
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
                if (result)
                {
                    userName = pack.LoginPack?.Username;  // 注册成功后保存用户名
                    Console.WriteLine("User registered successfully: " + userName);
                }
                Console.WriteLine("UserData.Logon returned: " + result);
                return result;
            }
            else
            {
                Console.WriteLine("UserData is null, cannot perform registration");
                return false;
            }
        }
        public bool Login(MainPack pack) {
            Console.WriteLine("Client.Login called with username: " + pack.LoginPack?.Username);
            if (userData != null)
            {
                bool result = userData.Login(pack);
                if (result)
                {
                    userName = pack.LoginPack?.Username;  // 登录成功后保存用户名
                    Console.WriteLine("User logged in successfully: " + userName);
                }
                Console.WriteLine("UserData.Login returned: " + result);
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
                // 先处理房间退出逻辑，避免递归
                if (GetRoom != null && server != null) { 
                    Room tempRoom = GetRoom;
                    GetRoom = null;  // 先置空避免递归
                    tempRoom.Exit(server, this);
                }
                
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