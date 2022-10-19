using LiteDB;
using System;
using Files.Shared.Extensions;
using JsonSerializer = System.Text.Json.JsonSerializer;

#nullable enable

namespace Files.App.Helpers.LayoutPreferences
{
    public class LayoutPrefsDb : IDisposable
    {
        private readonly LiteDatabase db;

        public LayoutPrefsDb(string connection, bool shared = false)
        {
            db = new LiteDatabase(new ConnectionString(connection)
            {
                Connection = shared ? ConnectionType.Shared : ConnectionType.Direct,
                Upgrade = true
            }, new BsonMapper() { IncludeFields = true });
        }

        public void SetPreferences(string filePath, ulong? frn, LayoutPreferences? prefs)
        {
            // Get a collection (or create, if doesn't exist)
            var col = db.GetCollection<LayoutDbPrefs>("layoutprefs");

            var tmp = _FindPreferences(filePath, frn);
            if (tmp == null)
            {
                if (prefs != null)
                {
                    // Insert new tagged file (Id will be auto-incremented)
                    var newPref = new LayoutDbPrefs()
                    {
                        FilePath = filePath,
                        Frn = frn,
                        Prefs = prefs
                    };
                    col.Insert(newPref);
                    col.EnsureIndex(x => x.Frn);
                    col.EnsureIndex(x => x.FilePath);
                }
            }
            else
            {
                if (prefs != null)
                {
                    // Update file tag
                    tmp.Prefs = prefs;
                    col.Update(tmp);
                }
                else
                {
                    // Remove file tag
                    col.Delete(tmp.Id);
                }
            }
        }

        public LayoutPreferences? GetPreferences(string? filePath = null, ulong? frn = null)
        {
            return _FindPreferences(filePath, frn)?.Prefs;
        }

        private LayoutDbPrefs? _FindPreferences(string? filePath = null, ulong? frn = null)
        {
            // Get a collection (or create, if doesn't exist)
            var col = db.GetCollection<LayoutDbPrefs>("layoutprefs");

            if (filePath != null)
            {
                var tmp = col.FindOne(x => x.FilePath == filePath);
                if (tmp != null)
                {
                    if (frn != null)
                    {
                        // Keep entry updated
                        tmp.Frn = frn;
                        col.Update(tmp);
                    }
                    return tmp;
                }
            }
            if (frn != null)
            {
                var tmp = col.FindOne(x => x.Frn == frn);
                if (tmp != null)
                {
                    if (filePath != null)
                    {
                        // Keep entry updated
                        tmp.FilePath = filePath;
                        col.Update(tmp);
                    }
                    return tmp;
                }
            }
            return null;
        }

        public void ResetAll(Func<LayoutDbPrefs, bool>? predicate = null)
        {
            var col = db.GetCollection<LayoutDbPrefs>("layoutprefs");
            if (predicate is null)
            {
                col.DeleteAll();
            }
            else
            {
                col.DeleteMany(x => predicate(x));
            }
        }

        public void ApplyToAll(Action<LayoutDbPrefs> updateAction, Func<LayoutDbPrefs, bool>? predicate = null)
        {
            var col = db.GetCollection<LayoutDbPrefs>("layoutprefs");
            var allDocs = predicate is null ? col.FindAll() : col.Find(x => predicate(x));
            allDocs.ForEach(x => updateAction(x));
            col.Update(allDocs);
        }

        public void Dispose()
        {
            db.Dispose();
        }

        public void Import(string json)
        {
            var dataValues = JsonSerializer.Deserialize<LayoutDbPrefs[]>(json);
            var col = db.GetCollection<LayoutDbPrefs>("layoutprefs");
            col.DeleteAll();
            col.InsertBulk(dataValues);
        }

        public string Export()
        {
            return JsonSerializer.Serialize(db.GetCollection<LayoutDbPrefs>("layoutprefs").FindAll());
        }

        public class LayoutDbPrefs
        {
            [BsonId]
            public int Id { get; set; }
            public ulong? Frn { get; set; }
            public string FilePath { get; set; } = string.Empty;
            public LayoutPreferences Prefs { get; set; } = LayoutPreferences.DefaultLayoutPreferences;
        }
    }
}
