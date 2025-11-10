using Google.Protobuf.Collections;
using SocketGameProtool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace ConsoleApp3.Server
{
    internal class Room
    {
        private RoomPack roomPack;
        public string roomname;
        public int maxnum;
        public int state;//0:available, 1:full, 2:closed
        public List<Client> clientList = new List<Client>();
        public List<Client> ClientList { get => clientList; }  // 添加公开属性
        public RoomPack RoomPack { get { roomPack.Curnum = clientList.Count;return roomPack; } }
        public Room(Client client,RoomPack pack) { 
            roomPack = pack;
            this.roomname = pack.RoomName;
            this.maxnum = pack.Maxnum;
            this.state = pack.State;
            this.clientList.Add(client);
            client.GetRoom = this;
            // 移除错误的 LoginPack 访问，应该在用户登录时已经设置 userName
            Console.WriteLine("Room initialized: " + roomname + ", Max: " + maxnum + ", State: " + state);
        }
        public RepeatedField<PlayerPack> GetPlayerInFo() { 
            RepeatedField<PlayerPack> pack = new RepeatedField<PlayerPack>();
            foreach (Client client in clientList) {
                PlayerPack player = new PlayerPack();
                player.PlayerName = client.userName ?? "Unknown";  // 使用正确的属性名 PlayerName
                pack.Add(player);
            }
            return pack;
        }
        
        // 更新房间状态
        private void UpdateRoomState()
        {
            if (clientList.Count >= maxnum)
            {
                state = 1; // 已满
                roomPack.State = 1;
            }
            else if (clientList.Count > 0)
            {
                state = 0; // 可用
                roomPack.State = 0;
            }
            else
            {
                state = 2; // 已关闭
                roomPack.State = 2;
            }
            
            // 更新当前人数
            roomPack.Curnum = clientList.Count;
        }
        public void Broadcast(MainPack pack) {
            // 广播给房间内所有玩家
            Console.WriteLine("Broadcasting to " + clientList.Count + " players in room " + roomname);
            foreach (Client c in clientList)
            {
                Console.WriteLine("  -> Sending to player: " + c.userName);
                c.Send(pack);
            }
        }
        public void Join(Client client) {
            // 检查是否已经在房间中
            if (clientList.Contains(client))
            {
                Console.WriteLine("Player " + client.userName + " already in room " + roomname);
                return;
            }
            
            // 检查房间是否已满
            if (clientList.Count >= maxnum)
            {
                Console.WriteLine("Room " + roomname + " is full");
                return;
            }
            
            clientList.Add(client);
            client.GetRoom = this;
            
            // 更新房间状态
            UpdateRoomState();
            
            // 先给房间内原有玩家广播（不包括新加入的）
            MainPack broadcastPack = new MainPack();
            broadcastPack.Actioncode = ActionCode.PlayerList;
            foreach (PlayerPack player in GetPlayerInFo()) { 
                broadcastPack.PlayPack.Add(player);
            }
            
            // 广播给原有玩家（房主等）
            for (int i = 0; i < clientList.Count - 1; i++)
            {
                clientList[i].Send(broadcastPack);
            }
            
            Console.WriteLine("Player " + client.userName + " joined room " + roomname + " (" + clientList.Count + "/" + maxnum + ")");
            Console.WriteLine("Broadcasting player list to " + (clientList.Count - 1) + " existing players");
        }
        public void Exit(Server server, Client client) {
            // 检查玩家是否在房间中
            if (!clientList.Contains(client))
            {
                Console.WriteLine("Player " + client.userName + " is not in room " + roomname);
                return;
            }
            
            bool isOwner = clientList.Count > 0 && clientList[0] == client;
            
            // 先从列表移除玩家
            clientList.Remove(client);
            client.GetRoom = null;
            
            // 如果是房主退出且房间还有其他人，解散房间
            if (isOwner && clientList.Count > 0)
            {
                Console.WriteLine("Room owner left, dismissing room: " + roomname);
                // 通知所有剩余玩家房间被解散
                MainPack dismissPack = new MainPack();
                dismissPack.Actioncode = ActionCode.Exit;
                dismissPack.ReturnCode = ReturnCode.Success;
                Broadcast(dismissPack);
                
                // 清空所有玩家的房间引用
                foreach (Client c in clientList.ToList())
                {
                    c.GetRoom = null;
                }
                clientList.Clear();
                server.RemoveRoom(this);
                return;
            }
            
            // 如果房间已空，删除房间
            if (clientList.Count == 0)
            {
                Console.WriteLine("Room is empty, removing: " + roomname);
                server.RemoveRoom(this);
                return;
            }
            
            // 更新房间状态
            UpdateRoomState();
            
            // 普通玩家退出，广播更新后的玩家列表
            MainPack pack = new MainPack();
            pack.Actioncode = ActionCode.PlayerList;
            foreach (PlayerPack player in GetPlayerInFo()) { 
                pack.PlayPack.Add(player);
            }
            Broadcast(pack);
            Console.WriteLine("Player " + client.userName + " exited room " + roomname + " (" + clientList.Count + "/" + maxnum + ")");
        }
        public ReturnCode StartGame(Client client) {
            // 检查玩家是否在房间中
            if (client.GetRoom == null)
            {
                Console.WriteLine("Client is not in any room");
                return ReturnCode.Fail;
            }
            
            // 检查是否为房主
            if (client != clientList[0]) {
                Console.WriteLine("Client is not the room owner");
                return ReturnCode.Fail;
            }
            
            // 在新线程中执行倒计时
            Thread starttime = new Thread(new ThreadStart(() =>
            {
                try
                {
                    // 发送游戏开始倒计时消息
                    for (int i = 5; i > 0; i--)
                    {
                        MainPack pack = new MainPack();
                        pack.Actioncode = ActionCode.StartGame;  // 使用正确的 ActionCode
                        pack.Str = "Game will start in " + i + " seconds";
                        Broadcast(pack);
                        Console.WriteLine("Broadcasting: " + pack.Str);
                        Thread.Sleep(1000);
                    }
                    
                    // 发送游戏正式开始消息
                    MainPack startPack = new MainPack();
                    startPack.Actioncode = ActionCode.GameStart;
                    startPack.Str = "Game Started!";
                    
                    // 添加房间内所有玩家的信息
                    int playerIndex = 0;
                    foreach (var client in clientList) {
                        PlayerPack player = new PlayerPack();
                        
                        // 设置玩家基本信息
                        player.PlayerName = client.userName;
                        player.PlayerId = client.userName;  // 使用用户名作为 PlayerId
                        
                        // 初始化玩家HP
                        client.userInfo.HP = 100;
                        player.Hp = client.userInfo.HP;
                        
                        // 初始化玩家位置（分配不同的出生点）
                        if (client.userInfo.Pos == null)
                        {
                            client.userInfo.Pos = new PosPack();
                        }
                        
                        // 为每个玩家分配不同的出生位置（避免全部为0）
                        client.userInfo.Pos.PosX = playerIndex * 5.0f;  // 每个玩家间隔5个单位
                        client.userInfo.Pos.PosY = playerIndex * 3.0f;
                        client.userInfo.Pos.RotZ = 0.0f;
                        client.userInfo.Pos.GunRotZ = 0.0f;
                        
                        // 复制位置信息到 PlayerPack
                        player.PosPack = new PosPack();
                        player.PosPack.PosX = client.userInfo.Pos.PosX;
                        player.PosPack.PosY = client.userInfo.Pos.PosY;
                        player.PosPack.RotZ = client.userInfo.Pos.RotZ;
                        player.PosPack.GunRotZ = client.userInfo.Pos.GunRotZ;
                        
                        startPack.PlayPack.Add(player);
                        
                        Console.WriteLine("Added player to game start: " + player.PlayerName + 
                                        " (HP: " + player.Hp + ", Pos: (" + player.PosPack.PosX + ", " + player.PosPack.PosY + "))");
                        
                        playerIndex++;
                    }
                    
                    Console.WriteLine("Broadcasting game start with " + startPack.PlayPack.Count + " players");
                    
                    // 验证 startPack 中的数据
                    Console.WriteLine("Verifying startPack before broadcast:");
                    Console.WriteLine("  ActionCode: " + startPack.Actioncode);
                    Console.WriteLine("  Str: " + startPack.Str);
                    Console.WriteLine("  PlayPack.Count: " + startPack.PlayPack.Count);
                    foreach (var p in startPack.PlayPack)
                    {
                        Console.WriteLine("    Player: " + p.PlayerName + ", HP: " + p.Hp + 
                                        ", Pos: " + (p.PosPack != null ? "(" + p.PosPack.PosX + ", " + p.PosPack.PosY + ")" : "null"));
                    }
                    
                    Broadcast(startPack);
                    Console.WriteLine("Game started broadcasted");
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error in game start thread: " + ex.Message);
                }
            }));
            starttime.Start();
            return ReturnCode.Success;
        }
    }
}
