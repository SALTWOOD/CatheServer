using SQLite;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CatheServer.Modules.Database
{
    public class DatabaseHandler
    {

        private SQLiteConnection _db;

        public DatabaseHandler()
        {
            _db = new SQLiteConnection("./database.sqlite");
            _db.CreateTable<UserEntity>();
        }

        public void AddEntity<T>(T instance)
        {
            _db.Insert(instance);
        }

        public List<T> QueryByIndex<T>(string key, object index) where T : new()
        {
            return _db.Query<T>($"SELECT * FROM {typeof(T).Name} WHERE {key} = \"{index}\";");
        }

        public T GetEntity<T>(T instance) where T : new() // 避免插进去拔不出来
        {
            return _db.Get<T>(instance);
        }

        public IEnumerable<T> GetEntities<T>() where T : new()
        {
            var items = _db.Query<T>($"SELECT * FROM {typeof(T).Name}");

            foreach (var item in items)
            {
                yield return item;
            }
        }
    }
}
