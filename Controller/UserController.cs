using SocketGameProtool;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ConsoleApp3.Server;

namespace ConsoleApp3.Controller
{
    internal class UserController:BaseController
    {
        public UserController()
        {
            requestCode  = RequestCode.User;
        }
        
        // 修复方法名称拼写错误，从"Longon"改为"Logon"
        public MainPack Logon(Server.Server server, Client client, MainPack pack) 
        {
            Console.WriteLine("Processing login request for user: " + pack.LoginPack?.Username);
            
            MainPack returnPack = new MainPack();
            returnPack.RequestCode = pack.RequestCode;
            returnPack.Actioncode = pack.Actioncode;
            
            try
            {
                if (client.Logon(pack)) { 
                    returnPack.ReturnCode = ReturnCode.Success;
                    Console.WriteLine("User login successful: " + pack.LoginPack?.Username);
                }
                else {
                    returnPack.ReturnCode = ReturnCode.Fail;
                    Console.WriteLine("User login failed: " + pack.LoginPack?.Username);
                }
            }
            catch (Exception ex)
            {
                returnPack.ReturnCode = ReturnCode.Fail;
                Console.WriteLine("Exception during login processing: " + ex.Message);
                Console.WriteLine("Stack trace: " + ex.StackTrace);
            }
            
            return returnPack;
        }
        public MainPack Login(Server.Server server, Client client, MainPack pack)
        {
            Console.WriteLine("Processing login request for user: " + pack.LoginPack?.Username);

            MainPack returnPack = new MainPack();
            returnPack.RequestCode = pack.RequestCode;
            returnPack.Actioncode = pack.Actioncode;

            try
            {
                if (client.Login(pack))
                {
                    returnPack.ReturnCode = ReturnCode.Success;
                    Console.WriteLine("User login successful: " + pack.LoginPack?.Username);
                }
                else
                {
                    returnPack.ReturnCode = ReturnCode.Fail;
                    Console.WriteLine("User login failed: " + pack.LoginPack?.Username);
                }
            }
            catch (Exception ex)
            {
                returnPack.ReturnCode = ReturnCode.Fail;
                Console.WriteLine("Exception during login processing: " + ex.Message);
                Console.WriteLine("Stack trace: " + ex.StackTrace);
            }

            return returnPack;
        }
    }
}