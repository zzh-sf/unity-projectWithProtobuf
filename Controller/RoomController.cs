using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketGameProtool;
using ConsoleApp3.Server;
namespace ConsoleApp3.Controller
{
    internal class RoomController:BaseController
    {
        public RoomController() {
            requestCode = RequestCode.Room;
        }
        public MainPack CreateRoom(Server.Server server, Client client, MainPack pack) {
            Console.WriteLine("RoomController.CreateRoom called for room: " + pack.RoomPac[0]?.RoomName);
            
            Console.WriteLine("Room creation result: " + pack.ReturnCode);
            return server.CreateRoom(client, pack);
        }
        public MainPack FindRoom(Server.Server server, Client client, MainPack pack) { 
        return server.FindRoom();
        }
        public MainPack JoinRoom(Server.Server server, Client client, MainPack pack)
        {
            return server.JoinRoom(client, pack);
        }
        public MainPack Exit(Server.Server server, Client client, MainPack pack) {
            return server.ExitRoom(client, pack);
        }
        public MainPack Chat(Server.Server server, Client client, MainPack pack) {
            server.Chat(client, pack);
            return null;  // 聊天消息通过广播发送，不需要返回特定响应
        }
        public MainPack StartGame(Server.Server server, Client client, MainPack pack) {
            // 检查玩家是否在房间中
            if (client.GetRoom == null)
            {
                Console.WriteLine("Client is not in any room");
                pack.ReturnCode = ReturnCode.Fail;
                return pack;
            }
            
            ReturnCode result = client.GetRoom.StartGame(client);
            
            // 返回 StartGame 响应，确认游戏开始请求已接受
            pack.ReturnCode = result;
            
            Console.WriteLine("Game start request accepted, result: " + result);
            return pack;
        }
        
        public MainPack GameStart(Server.Server server, Client client, MainPack pack) {
            // 处理客户端对游戏开始的确认响应
            Console.WriteLine("Client " + client.userName + " confirmed game start");
            pack.ReturnCode = ReturnCode.Success;
            return pack;
        }
    }
}
