using System.Collections.Generic;
using System.IO;
using NAnt.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace BuildListener
{
    public class Listener : IBuildListener
    {
        private string _logFileName;
        private readonly Dictionary<Project, Models.Project> _projects = new Dictionary<Project, Models.Project>();
        private readonly Dictionary<Target, Models.Target> _targets = new Dictionary<Target, Models.Target>();
        private readonly List<Models.Task> _taskOrder = new List<Models.Task>();

        private static readonly JsonSerializer Serializer = new JsonSerializer
        {
            Formatting = Formatting.Indented,
            PreserveReferencesHandling = PreserveReferencesHandling.Objects
        };

        private void Flush()
        {
            if (_logFileName == null)
                return;
            using(var fs = File.Create(_logFileName))
            using(var w = new StreamWriter(fs))
            {
                var build = new Models.Build { Projects = new Models.Project[_projects.Count], TaskOrder = _taskOrder };
                _projects.Values.CopyTo(build.Projects, 0);
                Serializer.Serialize(w, build);
            }
        }

        public void BuildStarted(object sender, BuildEventArgs e)
        {
            if (string.IsNullOrEmpty(_logFileName) && e.Project.Properties.Contains("buildvisualizer.logfile"))
                _logFileName = e.Project.Properties["buildvisualizer.logfile"];

            _projects[e.Project] = new Models.Project
            {
                BaseDirectory = e.Project.BaseDirectory,
                BuildFileLocalName = e.Project.BuildFileLocalName,
                BuildFileUri = e.Project.BuildFileUri.ToString(),
                BuildTargets = new string[e.Project.BuildTargets.Count],
                ProjectName = e.Project.ProjectName,
                PlatformName = e.Project.PlatformName,
                TargetFrameworkName = e.Project.TargetFramework.Name,
                TargetFrameworkVersion = e.Project.TargetFramework.Version.ToString()
            };

            e.Project.BuildTargets.CopyTo(_projects[e.Project].BuildTargets, 0);
            Flush();
        }

        public void BuildFinished(object sender, BuildEventArgs e)
        {
            Flush();
        }

        public void TargetStarted(object sender, BuildEventArgs e)
        {
            var location = e.Target.GetLocation().ToString();
            var target = new Models.Target
            {
                Location = location,
                Dependencies = new string[e.Target.Dependencies.Count],
                Description = e.Target.Description,
                Executed = e.Target.Executed,
                IfCondition = e.Target.IfCondition,
                Name = e.Target.Name,
                UnlessCondition = e.Target.UnlessCondition
            };
            e.Target.Dependencies.CopyTo(target.Dependencies, 0);
            _targets[e.Target] = target;
            _projects[e.Project].Targets.Add(target);
            Flush();
        }

        public void TargetFinished(object sender, BuildEventArgs e)
        {
        }

        public void TaskStarted(object sender, BuildEventArgs e)
        {
            var task = new Models.Task
            {
                Type = e.Task.GetType().FullName,
                Target = e.Task.Parent is Target target ? _targets[target] : null,
                Location = e.Task.GetLocation().ToString(),
                FailOnError = e.Task.FailOnError,
                Name = e.Task.Name
            };

            foreach (var prop in e.Task.GetType().GetProperties())
            {
                if (prop.DeclaringType == typeof(Task) || 
                    prop.DeclaringType == typeof(Element) || 
                    !prop.CanRead || 
                    prop.GetIndexParameters().Length > 0)
                    continue;

                try
                {
                    var token = JToken.FromObject(prop.GetValue(e.Task, new object[0]), Serializer);
                    task.ExtraProperties[prop.Name] = token;
                }
                catch
                {
                    // ignored
                }
            }

            _projects[e.Project].Tasks.Add(task);
            _taskOrder.Add(task);
            Flush();
        }

        public void TaskFinished(object sender, BuildEventArgs e)
        {
        }

        public void MessageLogged(object sender, BuildEventArgs e)
        {
        }
    }
}
