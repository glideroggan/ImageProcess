using System.Globalization;
using System.Text;
using contracts;
using Microsoft.Data.Sqlite;

namespace storage_sqllite;

/* TODO:
 *  - use Guid in db?
 *      https://stackoverflow.com/questions/18954130/can-we-use-guid-as-a-primary-key-in-sqlite-database
 * 
 * 
 */

public class StorageSqlLite : IStorageProvider
{
    public StorageSqlLite()
    {
        using var connection = new SqliteConnection("Data Source=faces.db");
        connection.Open();
        var command = connection.CreateCommand();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS faces (
    id text PRIMARY KEY,
    faceid text,
    name text,
    expire text NOT NULL,
    image blob
) WITHOUT ROWID";
        command.ExecuteNonQuery();
        command.CommandText = @"
CREATE TABLE IF NOT EXISTS face_encodings (
    faceid text PRIMARY KEY,
    encoding text NOT NULL
) WITHOUT ROWID";
        command.ExecuteNonQuery();
        connection.Close();
    }


    public async Task AddFacesAsync(string name, DateOnly expireDate, byte[]? blob = null, params Face[] faces)
    {
        // TODO: make support to add several faceId at the same time
        var face = faces.First();
        using var connection = new SqliteConnection("Data Source=faces.db");
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
        command.Parameters.AddWithValue("$faceid", face.Id.ToString("N"));
        command.Parameters.AddWithValue("$name", name);
        command.Parameters.AddWithValue("$expire", expireDate.ToString("yyyy-MM-dd"));
        if (blob != null)
        {
            command.Parameters.AddWithValue("$blob", blob ?? null);
            command.CommandText =
                "insert into faces (id, faceid, name, image, expire) values($id, $faceid, $name, $blob, $expire)";
        }
        else
        {
            command.CommandText =
                "insert into faces (id, faceid, name, image, expire) values($id, $faceid, $name, null, $expire)";
        }

        await command.ExecuteNonQueryAsync();
    }

    public async Task<Dictionary<string, List<Guid>>> GetKnownFacesAsync()
    {
        var res = new Dictionary<string, List<Guid>>();
        using var connection = new SqliteConnection("Data Source=faces.db");
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = @"SELECT name, faceid FROM faces where expire > Date()";
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(0);
            var faceId = reader.GetString(1);

            if (res.ContainsKey(name))
            {
                res[name].Add(Guid.Parse(faceId));
            }
            else
            {
                res.Add(name, new List<Guid> { Guid.Parse(faceId) });
            }
        }

        return res;
    }

    public async Task SaveFaceEncodingAsync(IEnumerable<MyFaceEncoding> newFaces)
    {
        /*
         * add to table face_encodings
         * encoding needs to be turn into text (sqlite), so parsing this needs some special attention
         */
        using var connection = new SqliteConnection("Data Source=faces.db");
        await connection.OpenAsync();
        foreach (var face in newFaces)
        {
            var command = connection.CreateCommand();
            command.Parameters.AddWithValue("$faceid", face.FaceId.ToString("N"));
            command.Parameters.AddWithValue("$encoding", TurnIntoText(face.FaceEncoding));

            command.CommandText = "INSERT INTO face_encodings (faceid, encoding) values($faceid, $encoding)";
            await command.ExecuteNonQueryAsync();
        }
        
        
    }

    private string TurnIntoText(double[] arr)
    {
        // PERF: this creates alot of allocations, we could use an array pool of strings
        // or not have this as a function, so that we can reuse the same string?
        var builder = new StringBuilder();
        foreach (var val in arr)
        {
            builder.Append(CultureInfo.InvariantCulture, $"{val},");
        }

        return builder.ToString();
    }
}