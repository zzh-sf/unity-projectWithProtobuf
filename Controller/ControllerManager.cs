using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SocketGameProtool;
using System.Reflection;
using ConsoleApp3.Server;

namespace ConsoleApp3.Controller
{
    internal class ControllerManager
    {
        private Dictionary<RequestCode,BaseController> controlDict=new Dictionary<RequestCode, BaseController>();
        private Server.Server server;
        
        public ControllerManager() {
            try
            {
                UserController userController = new UserController();
                controlDict.Add(userController._requestCode, userController);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error initializing UserController: " + ex.Message);
            }
        }
        
        public ControllerManager(Server.Server server) {
            try
            {
                this.server = server;
                UserController userController = new UserController();
                controlDict.Add(userController._requestCode, userController);
                Console.WriteLine("ControllerManager initialized successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error initializing ControllerManager: " + ex.Message);
            }
        }
        
        public void HandleRequest(MainPack pack, Client client) {
            Console.WriteLine("ControllerManager.HandleRequest called: " + pack.Actioncode);
            try
            {
                if (controlDict.TryGetValue(pack.RequestCode, out BaseController controller))
                {
                    Console.WriteLine("Found controller for request code: " + pack.RequestCode);
                    
                    // 检查ActionCode是否为Logon
                    if (pack.Actioncode == ActionCode.Logon)
                    {
                        Console.WriteLine("Handling Logon action");
                        // 直接调用Logon方法，避免反射可能带来的问题
                        if (controller is UserController userController)
                        {
                            MainPack resultPack = userController.Logon(server, client, pack);
                            Console.WriteLine("UserController.Logon returned result pack with ReturnCode: " + (resultPack?.ReturnCode ?? ReturnCode.Fail));
                            if (resultPack != null) 
                            {
                                client.Send(resultPack);
                            }
                            return;
                        }
                    }
                    
                    // 对于其他ActionCodes，使用反射方式
                    string methodName = pack.Actioncode.ToString();
                    Console.WriteLine("Trying to invoke method: " + methodName);
                    MethodInfo method = controller.GetType().GetMethod(methodName);
                    if (method == null)
                    {
                        Console.WriteLine("No method found for action code: " + pack.Actioncode);
                        return;
                    }
                    
                    object[] parameters = new object[] { server, client, pack };
                    object result = method.Invoke(controller, parameters);
                    if (result != null) 
                    {
                        client.Send(result as MainPack);
                    }
                }
                else
                {
                    Console.WriteLine("No controller found for request code: " + pack.RequestCode);
                }
            }
            catch (TargetException te)
            {
                Console.WriteLine("Target exception when invoking method: " + te.Message);
            }
            catch (TargetInvocationException tie)
            {
                Console.WriteLine("Target invocation exception: " + tie.InnerException?.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("General error when handling request: " + ex.Message);
            }
        }
    }
}