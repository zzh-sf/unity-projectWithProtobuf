using SocketGameProtool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleApp3.Server;

namespace ConsoleApp3.Controller
{
    internal class GameCollector : BaseController
    {
        public GameCollector()
        {
            requestCode = RequestCode.Game;
        }

        /// <summary>
        /// 处理玩家状态更新（移动、攻击等）
        /// 只允许客户端控制与自己 username 匹配的角色
        /// </summary>
        public MainPack UpState(Server.Server server, Client client, MainPack pack)
        {
            // 检查玩家是否在房间中
            if (client.GetRoom == null)
            {
                Console.WriteLine("Client is not in any room, cannot update state");
                pack.ReturnCode = ReturnCode.Fail;
                return pack;
            }

            try
            {
                // 检查是否有玩家数据
                if (pack.PlayPack == null || pack.PlayPack.Count == 0)
                {
                    Console.WriteLine("No player data in pack");
                    return null;
                }

                // 关键安全验证：检查客户端是否只控制自己的角色
                foreach (var playerPack in pack.PlayPack)
                {
                    // 验证 PlayerId 和 PlayerName 是否与客户端 username 匹配
                    if (playerPack.PlayerId != client.userName || playerPack.PlayerName != client.userName)
                    {
                        Console.WriteLine("[SECURITY] Client " + client.userName + 
                                        " attempted to control another player: " + playerPack.PlayerName + 
                                        " (PlayerId: " + playerPack.PlayerId + ")");
                        
                        // 拒绝非法请求
                        pack.ReturnCode = ReturnCode.Fail;
                        pack.Str = "You can only control your own character";
                        return pack;
                    }
                }

                // 更新服务端存储的玩家状态
                foreach (var playerPack in pack.PlayPack)
                {
                    // 更新客户端的 UserInfo
                    if (playerPack.PlayerName == client.userName)
                    {
                        // 更新 HP
                        if (playerPack.Hp > 0)
                        {
                            client.userInfo.HP = playerPack.Hp;
                        }

                        // 更新位置信息
                        if (playerPack.PosPack != null)
                        {
                            if (client.userInfo.Pos == null)
                            {
                                client.userInfo.Pos = new PosPack();
                            }
                            client.userInfo.Pos.PosX = playerPack.PosPack.PosX;
                            client.userInfo.Pos.PosY = playerPack.PosPack.PosY;
                            client.userInfo.Pos.RotZ = playerPack.PosPack.RotZ;
                            client.userInfo.Pos.GunRotZ = playerPack.PosPack.GunRotZ;
                        }

                        Console.WriteLine("[UPDATE] " + client.userName + " state - HP: " + client.userInfo.HP + 
                                        ", Pos: (" + (client.userInfo.Pos?.PosX ?? 0) + ", " + (client.userInfo.Pos?.PosY ?? 0) + ")" +
                                        ", Rot: " + (client.userInfo.Pos?.RotZ ?? 0));
                    }
                }

                // 创建新的消息包用于广播，避免修改原始数据
                MainPack broadcastPack = new MainPack();
                broadcastPack.Actioncode = ActionCode.UpState;
                broadcastPack.RequestCode = RequestCode.Game;

                // 复制玩家数据（包含完整的 PlayerPack 信息）
                foreach (var player in pack.PlayPack)
                {
                    PlayerPack clonedPlayer = new PlayerPack();
                    clonedPlayer.PlayerName = player.PlayerName;
                    clonedPlayer.PlayerId = player.PlayerId;
                    clonedPlayer.Hp = player.Hp;
                    
                    // 复制位置信息
                    if (player.PosPack != null)
                    {
                        clonedPlayer.PosPack = new PosPack();
                        clonedPlayer.PosPack.PosX = player.PosPack.PosX;
                        clonedPlayer.PosPack.PosY = player.PosPack.PosY;
                        clonedPlayer.PosPack.RotZ = player.PosPack.RotZ;
                        clonedPlayer.PosPack.GunRotZ = player.PosPack.GunRotZ;
                    }
                    
                    broadcastPack.PlayPack.Add(clonedPlayer);
                }

                // 广播给房间内所有玩家（包括发送者）
                client.GetRoom.Broadcast(broadcastPack);

                Console.WriteLine("[BROADCAST] Player " + client.userName + " state broadcasted to room " + client.GetRoom.roomname);

                // 返回 null 表示不需要单独响应，状态已通过广播发送
                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error broadcasting player state: " + ex.Message);
                Console.WriteLine("Stack trace: " + ex.StackTrace);
                pack.ReturnCode = ReturnCode.Fail;
                return pack;
            }
        }
    }
}
