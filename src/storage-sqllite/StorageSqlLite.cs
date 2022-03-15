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
    image blob
) WITHOUT ROWID";
        command.ExecuteNonQuery();
        connection.Close();
    }

    public async Task AddFacesAsync(string name, params Face[] faces)
    {
        var face = faces.First();
        using var connection = new SqliteConnection("Data Source=faces.db");
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.Parameters.AddWithValue("$id", Guid.NewGuid().ToString("N"));
        command.Parameters.AddWithValue("$faceid", face.Id.ToString("N"));
        command.Parameters.AddWithValue("$name", name);
        command.CommandText = "insert into faces (id, faceid, name) values($id, $faceid, $name)";
        await command.ExecuteNonQueryAsync();
    }

    public async Task<Dictionary<string, List<Guid>>> GetKnownFacesAsync()
    {
        var res = new Dictionary<string, List<Guid>>();
        using var connection = new SqliteConnection("Data Source=faces.db");
        await connection.OpenAsync();
        var command = connection.CreateCommand();
        command.CommandText = @"SELECT name, faceid FROM faces";
        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var name = reader.GetString(0);
            var faceId = reader.GetString(0);

            if (res.ContainsKey(name))
            {
                res[name].Add(Guid.Parse(faceId));
            }
            else
            {
                res.Add(name, new List<Guid> {Guid.Parse(faceId)});
            }
        }

        return res;
    }
}