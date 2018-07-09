using System;
using System.IO;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace SheepReaper.Dexter_proto
{
    public class WritableOptions<T> : IWritableOptions<T> where T : class, new()
    {
        // TODO: ReImplement IHostingEnvironment when this is actually a web app.
        private readonly IHostingEnvironment _environment;
        private readonly string _file;
        private readonly IOptionsMonitor<T> _options;
        private readonly string _section;

        public WritableOptions(
            IHostingEnvironment environment,
            IOptionsMonitor<T> options,
            string section,
            string file)
        {
            _environment = environment;
            _options = options;
            _section = section;
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
            var sectionObject = jObject.TryGetValue(_section, out var section)
                ? JsonConvert.DeserializeObject<T>(section.ToString())
                : Value ?? new T();

            applyChanges(sectionObject);

            jObject[_section] = JObject.Parse(JsonConvert.SerializeObject(sectionObject));
            File.WriteAllText(physicalPath, JsonConvert.SerializeObject(jObject, Formatting.Indented));
        }
    }
}