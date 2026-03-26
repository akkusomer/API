using System;
using BCrypt.Net;

namespace HashGen
{
    class Program
    {
        static void Main(string[] args)
        {
            string password = "Admin123!";
            string hash = BCrypt.Net.BCrypt.HashPassword(password);
            Console.WriteLine(hash);
        }
    }
}
