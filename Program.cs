using Microsoft.Data.SqlClient;
using System.Data;
using System.Text;
using System.Transactions;
using System.Xml.Linq;


public static class Program
{
    private static string connectionstring = "Server=localhost;Database=TestBlobs;Trusted_Connection=True;Encrypt=False";
    public static void Main(string[] args)
    {

        //var size = 1000;
        //var size = 10000;
        //var size = 100000; 
        var size = 1000000; 
        //var size = 10000000; 
        //var size = 100000000; 
        Console.WriteLine($"Size: {size}");
        for (int i = 0; i < 5; i++)
        {
            //TruncateData();

            var number = 200;

            InsertData(number, size);

            var b1 = ASyncCall();
            var watchAsync = System.Diagnostics.Stopwatch.StartNew();
            var b = ASyncCall();
            b.Wait();
            watchAsync.Stop();

            SyncCall();;
            var watchSync = System.Diagnostics.Stopwatch.StartNew();
            SyncCall();
            watchSync.Stop();

            Console.WriteLine($"Number: {(i + 1) * number}, Sync: {watchSync.Elapsed.TotalSeconds}, Async:  {watchAsync.Elapsed.TotalSeconds} ");
        }
        //DatePerfTest();
    }


    private static async Task ASyncCall()
    {
        var stringBuilder2 = new StringBuilder();
        using (var conn = new SqlConnection(connectionstring))
        {
            using (var cmd = new SqlCommand("SELECT * FROM TestBlobs;", conn))
            {
                cmd.CommandType = System.Data.CommandType.Text;

                conn.Open();
                var reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    Guid id = reader.GetGuid(0);
                    stringBuilder2.AppendLine(id.ToString());
                }
            }
        }
    }
    private static void SyncCall()
    {
        var stringBuilder = new StringBuilder();

        using (var conn = new SqlConnection(connectionstring))
        {
            using (var cmd = new SqlCommand("SELECT * FROM TestBlobs;", conn))
            {
                cmd.CommandType = System.Data.CommandType.Text;
                conn.Open();
                var reader = cmd.ExecuteReader();

                while (reader.Read())
                {
                    Guid id = reader.GetGuid(0);
                    stringBuilder.AppendLine(id.ToString());
                }
            }
        }
    }

    static IEnumerable<string> NextStrings(this Random rnd, string allowedChars, (int Min, int Max) length, int count)
    {
        ISet<string> usedRandomStrings = new HashSet<string>();
        (int min, int max) = length;
        char[] chars = new char[max];
        int setLength = allowedChars.Length;

        while (count-- > 0)
        {
            int stringLength = rnd.Next(min, max + 1);

            for (int i = 0; i < stringLength; ++i)
            {
                chars[i] = allowedChars[rnd.Next(setLength)];
            }

            string randomString = new string(chars, 0, stringLength);

            if (usedRandomStrings.Add(randomString))
            {
                yield return randomString;
            }
            else
            {
                count++;
            }
        }
    }

    static void InsertData(int number, int size)
    {

        const string AllowedChars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz#@$^*()";
        Random rng = new Random();

        var sqlCon = new SqlConnection(connectionstring);
        sqlCon.Open();
        //using var trans = new TransactionScope();
        foreach (string randomString in rng.NextStrings(AllowedChars, (size, size), number))
        {
            using SqlCommand cmd = new SqlCommand("insert into TestBlobs (Id, data) values (@id, @data)", sqlCon);
            cmd.CommandTimeout = 500;
            cmd.Parameters.Add("@id", SqlDbType.UniqueIdentifier).Value = Guid.NewGuid();
            cmd.Parameters.Add("@data", SqlDbType.VarChar).Value = randomString;
            cmd.ExecuteNonQuery();
        }
        //trans.Complete();
    }
}