using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Models
{
    [JsonObject]
    public class Build
    {
        public Project[] Projects { get; set; }
        public List<Task> TaskOrder { get; set; }
    }

    [JsonObject]
    public class Project
    {
        public string BaseDirectory { get; set; }
        public string BuildFileLocalName { get; set; }
        public string BuildFileUri { get; set; }
        public string[] BuildTargets { get; set; }
        public string ProjectName { get; set; }
        public string PlatformName { get; set; }
        public string TargetFrameworkName { get; set; }
        public string TargetFrameworkVersion { get; set; }

        public List<Target> Targets { get; } = new List<Target>();
        public List<Task> Tasks { get; } = new List<Task>();
    }
    [JsonObject]
    public class Target
    {
        public string Name { get; set; }
        public string Location { get; set; }
        public bool Executed { get; set; }
        public string IfCondition { get; set; }
        public string UnlessCondition { get; set; }
        public string Description { get; set; }
        public string[] Dependencies { get; set; }
    }

    [JsonObject]
    public class Task
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Location { get; set; }
        public Target Target { get; set; }
        public bool FailOnError { get; set; }

        [JsonExtensionData]
        public Dictionary<string, JToken> ExtraProperties { get; } = new Dictionary<string, JToken>();
    }
}
