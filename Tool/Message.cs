using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using SocketGameProtool;

namespace ConsoleApp3.Tool
{
    internal class Message
    {
        private byte[] buffer = new byte[1024]; // 缓冲区
        private int startIndex = 0; // 当前缓冲区起始位置
        
        public byte[] Buffer
        {
            get { return buffer; }
        }
        
        public int StartIndex
        {
            get { return startIndex; }
        }
        
        public int Remsize // 剩余空间
        {
            get { return buffer.Length - startIndex; }
        }
        
        // 解析数据
        public void ReadBuffer(int len, Action<MainPack> handleDataCallback)
        {
            Console.WriteLine("ReadBuffer called with " + len + " bytes");
            startIndex += len;
            while (true)
            {
                if (startIndex <= 4) return; // 无法构成一个完整的消息长度字段
                int count = BitConverter.ToInt32(buffer, 0); // 获取消息长度
                Console.WriteLine("Expected message length: " + count);
                
                if ((startIndex - 4) >= count) // 数据足够构成一个完整消息
                {
                    try
                    {
                        // 构造消息体（跳过长度字段）
                        byte[] data = new byte[count];
                        Array.Copy(buffer, 4, data, 0, count);
                        
                        // 反序列化
                        MainPack pack = MainPack.Parser.ParseFrom(data);
                        Console.WriteLine("Deserialized message: RequestCode=" + pack.RequestCode + ", ActionCode=" + pack.Actioncode);
                        
                        // 处理消息
                        handleDataCallback(pack);
                        
                        // 将剩余未处理的数据移到缓冲区开头
                        int totalProcessed = count + 4; // 消息长度+数据
                        Array.Copy(buffer, totalProcessed, buffer, 0, startIndex - totalProcessed);
                        startIndex -= totalProcessed;
                    }
                    catch (InvalidProtocolBufferException ex)
                    {
                        Console.WriteLine("Failed to parse protobuf message: " + ex.Message);
                        // 出错时重置缓冲区
                        startIndex = 0;
                        return;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error processing message: " + ex.Message);
                        // 出错时重置缓冲区
                        startIndex = 0;
                        return;
                    }
                }
                else
                {
                    Console.WriteLine("Not enough data for complete message. Have " + (startIndex - 4) + " bytes, need " + count + " bytes");
                    return;
                }
            }
        }
        
        // 打包数据
        public static byte[] PackData(MainPack pack)
        {
            try
            {
                // 序列化消息体
                byte[] data = pack.ToByteArray();
                
                // 构造消息长度字段
                int len = data.Length;
                byte[] lenBytes = BitConverter.GetBytes(len);
                
                // 构造完整消息（长度+数据）
                byte[] newBuffer = new byte[len + 4];
                lenBytes.CopyTo(newBuffer, 0);
                data.CopyTo(newBuffer, 4);
                
                return newBuffer;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error packing data: " + ex.Message);
                return new byte[0];
            }
        }
    }
}
