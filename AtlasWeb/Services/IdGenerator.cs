using System.Security.Cryptography;

namespace AtlasWeb.Services
{
    public static class IdGenerator
    {
        public static Guid CreateV7()
        {
            byte[] bytes = new byte[16];
            RandomNumberGenerator.Fill(bytes);
            long timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            bytes[0] = (byte)((timestamp >> 40) & 0xFF);
            bytes[1] = (byte)((timestamp >> 32) & 0xFF);
            bytes[2] = (byte)((timestamp >> 24) & 0xFF);
            bytes[3] = (byte)((timestamp >> 16) & 0xFF);
            bytes[4] = (byte)((timestamp >> 8) & 0xFF);
            bytes[5] = (byte)(timestamp & 0xFF);

            bytes[6] = (byte)((bytes[6] & 0x0F) | 0x70);
            bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);

            return new Guid(bytes);
        }
    }
}