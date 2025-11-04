using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MySql.Data.MySqlClient;
using SocketGameProtool;
using ConsoleApp3.Server;

namespace ConsoleApp3.DATA
{
    internal class UserData
    {
        private MySqlConnection conn;
        private string connectionString = "server=localhost;user=root;database=protobuf;port=3306;password=12345";
        
        public UserData()
        {
            try
            {
                conn = new MySqlConnection(connectionString);
                Console.WriteLine("Database connection initialized");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to initialize database connection: " + ex.Message);
            }
        }
        
        public bool Logon(MainPack pack)
        {
            // 检查数据库连接状态
            if (conn == null)
            {
                Console.WriteLine("Database connection is null");
                return false;
            }
            
            string username = pack.LoginPack.Username;
            string password = pack.LoginPack.Password;
            string sql = "INSERT INTO userdata(username, password) VALUES(@username, @password)";
            
            try
            {
                // 只在连接关闭时打开连接
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                    Console.WriteLine("Database connection opened");
                }
                
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);
                    int result = cmd.ExecuteNonQuery();
                    Console.WriteLine("User registered successfully. Rows affected: " + result);
                    return result > 0; // 插入成功
                }
            }
            catch (MySqlException mysqlEx)
            {
                Console.WriteLine("MySQL Exception during registration: " + mysqlEx.Message);
                Console.WriteLine("Error Number: " + mysqlEx.Number);
                // 1062是MySQL重复键错误代码
                if (mysqlEx.Number == 1062)
                {
                    Console.WriteLine("Registration failed: Username already exists");
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine("General Exception during registration: " + ex.Message);
                return false;
            }
            finally
            {
                // 根据需要决定是否关闭连接，保持连接可以提高性能
                // 但在实际应用中可能需要实现连接池
                // if (conn.State == ConnectionState.Open)
                // {
                //     conn.Close();
                //     Console.WriteLine("Database connection closed");
                // }
            }
        }
        
        // 添加一个方法来测试数据库连接
        public bool TestConnection()
        {
            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }
                Console.WriteLine("Database connection test successful");
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Database connection test failed: " + ex.Message);
                return false;
            }
            finally
            {
                // 测试后关闭连接
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }
        
        // 添加一个方法来查询所有用户数据
        public void GetAllUsers()
        {
            string sql = "SELECT username, password FROM userdata";
            
            try
            {
                if (conn.State != ConnectionState.Open)
                {
                    conn.Open();
                }
                
                using (MySqlCommand cmd = new MySqlCommand(sql, conn))
                {
                    using (MySqlDataReader reader = cmd.ExecuteReader())
                    {
                        Console.WriteLine("User data in database:");
                        Console.WriteLine("Username\tPassword");
                        Console.WriteLine("------------------------");
                        
                        while (reader.Read())
                        {
                            Console.WriteLine(reader["username"] + "\t\t" + reader["password"]);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error retrieving user data: " + ex.Message);
            }
            finally
            {
                // 查询后关闭连接
                if (conn.State == ConnectionState.Open)
                {
                    conn.Close();
                }
            }
        }
    }
}
        
