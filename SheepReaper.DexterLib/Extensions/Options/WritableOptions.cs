using System;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SheepReaper.Extensions.Options
{
    internal class WritableOptions<T> : IWritableOptions<T> where T : class, new()
    {
        private readonly IHostingEnvironment _environment;
        private readonly IOptionsMonitor<T> _options;
        private readonly string _key;
        private readonly string _file;

        public WritableOptions(IHostingEnvironment environment, IOptionsMonitor<T> options, string key, string file)
        {
            _environment = environment;
            _options = options;
            _key = key;
            _file = file;
        }

        public T Value => _options.CurrentValue;

        public T Get(string name)
        {
            return _options.Get(name);
        }

        public void Update(Action<T> applyChanges)
        {
            var fileProvider = _environment.ContentRootFileProvider;
            var fileInfo = fileProvider.GetFileInfo(_file);
            var physicalPath = fileInfo.PhysicalPath;

            var jObject = JsonConvert.DeserializeObject<JObject>(File.ReadAllText(physicalPath));
            var sectionObject = jObject.TryGetValue(_key, out var section)
                ? JsonConvert.DeserializeObject<T>(section.ToString())
                : Value ?? new T();

            applyChanges(sectionObject);

            jObject[_key] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));
            File.WriteAllText(physicalPath, JsonConvert.SerializeObject(jObject, Formatting.Indented));
        }
    }
}