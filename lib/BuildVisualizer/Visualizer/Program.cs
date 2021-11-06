using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Visualizer
{
    internal static class Program
    {
        private static readonly string[] ExcludedTaskTypes =
        {
            "NAnt.Core.Tasks.PropertyTask",
            "NAnt.Core.Tasks.IncludeTask"
        };

        private static void Main(string[] args)
        {
            var serializer = JsonSerializer.Create(new ()
            {
                PreserveReferencesHandling = PreserveReferencesHandling.Objects
            });

            using var fs = File.OpenRead(args[0]);
            using var textReader = new StreamReader(fs);
            using var jsonReader = new JsonTextReader(textReader);
            var build = serializer.Deserialize<Build>(jsonReader) ?? throw new InvalidDataException();
            var sb = new StringBuilder($@"
digraph build {{");
            for (var i = 0; i < build.Projects.Length; i++)
            {
                var project = build.Projects[i];
                sb.Append(
                    $@"
    subgraph clusterProject{i} {{
        label=<"
                );
                if (!string.IsNullOrWhiteSpace(project.ProjectName))
                    sb.Append($"<b>{project.ProjectName}</b>");
                if (!string.IsNullOrWhiteSpace(project.BuildFileLocalName))
                    sb.Append($"<br/><i>{project.BuildFileLocalName}</i>");
                sb.Append(
                    @">;"
                );

                foreach (var target in project.Targets.Where(x => project.Tasks.Any(
                    t => t.Target == x && !ExcludedTaskTypes.Contains(t.Type)
                )))
                {
                    sb.Append(
                        $@"
        subgraph clusterTarget{project.Targets.IndexOf(target)} {{
            label=<"
                    );
                    if (!string.IsNullOrWhiteSpace(target.Name))
                        sb.Append($"<b>{target.Name}</b>");
                    if (!string.IsNullOrWhiteSpace(target.Location))
                        sb.Append($"<br/><i>{target.Location}</i>");
                    sb.Append(@">;");

                    foreach (var task in project.Tasks.Where(
                        t => t.Target == target && !ExcludedTaskTypes.Contains(t.Type)
                    ))
                    {
                        sb.AppendTask(build, task);
                    }

                    sb.Append(
                        @"
        }"
                    );
                }

                foreach (var task in project.Tasks.Where(t => t.Target == null && !ExcludedTaskTypes.Contains(t.Type)))
                    sb.AppendTask(build, task);

                sb.Append(
                      @"        
    }"
                  );
            }

            sb.AppendLine()
              .Append(
                  string.Join(
                      " -> ",
                      build.TaskOrder
                           .Select((task, inx) => (task, inx))
                           .Where(x => !ExcludedTaskTypes.Contains(x.task.Type))
                           .Select(x => $"task{x.inx}")
                  )
              )
              .Append(
                  @"
}"
              );

            var baseDirs = build.Projects.Select(p => p.BaseDirectory.Split(Path.DirectorySeparatorChar)).ToArray();
            var baseDir = string.Join(
                Path.DirectorySeparatorChar,
                baseDirs.First(x => x.Length == baseDirs.Min(y => y.Length))
                        .TakeWhile((x, i) => baseDirs.All(y => y[i] == x))
            );
            var dot = MakeRelativePaths(baseDir, sb.ToString());

            Console.WriteLine(dot);
        }

        private static string MakeRelativePaths(string root, string text)
        {
            var split = root.Split(Path.DirectorySeparatorChar);
            for (var i = 0; i < split.Length - 1; ++i)
            {
                text = text.Replace(
                    string.Join(Path.DirectorySeparatorChar, split.Take(split.Length - i)),
                    i == 0 ? "." : string.Join(Path.DirectorySeparatorChar, Enumerable.Repeat("..", i))
                );
            }
            return text;
        }

        private static readonly string[] ExcludedAttributes = { "OutputWriter", "InputWriter", "ErrorWriter" };

        private static void AppendTask(this StringBuilder sb, Build build, Task task)
        {
            sb.Append($@"
                task{build.TaskOrder.IndexOf(task)} [
                    shape=none
                    label=<");
            using (var xml = XmlWriter.Create(sb, new ()
            {
                Indent = false,
                ConformanceLevel = ConformanceLevel.Fragment,
                NewLineOnAttributes = false,
                OmitXmlDeclaration = true,
                WriteEndDocumentOnClose = true
            }))
            {
                xml.WriteStartElement("table");
                xml.WriteAttributeString("cellpadding", "5");
                xml.WriteStartElement("tr");
                xml.WriteStartElement("td");
                xml.WriteAttributeString("bgcolor", "black");
                xml.WriteAttributeString("align", "center");
                xml.WriteAttributeString("colspan", "2");
                xml.WriteStartElement("font");
                xml.WriteAttributeString("color", "white");
                xml.WriteStartElement("b");
                if (string.IsNullOrWhiteSpace(task.Name))
                    xml.WriteRaw("&nbsp;");
                else
                    xml.WriteString(task.Name);
                xml.WriteEndElement();
                xml.WriteEndElement();
                xml.WriteEndElement();
                xml.WriteEndElement();
                xml.WriteStartElement("tr");
                xml.WriteStartElement("td");
                xml.WriteAttributeString("align", "center");
                xml.WriteAttributeString("colspan", "2");
                xml.WriteStartElement("i");
                if (string.IsNullOrWhiteSpace(task.Location))
                    xml.WriteRaw("&nbsp;");
                else
                    xml.WriteString(task.Location);
                xml.WriteEndElement();
                xml.WriteEndElement();
                xml.WriteEndElement();
                xml.WriteStartElement("tr");
                xml.WriteStartElement("td");
                xml.WriteAttributeString("align", "left");
                xml.WriteString("Type");
                xml.WriteEndElement();
                xml.WriteStartElement("td");
                xml.WriteAttributeString("align", "left");
                xml.WriteString(task.Type);
                xml.WriteEndElement();
                xml.WriteEndElement();
                xml.WriteStartElement("tr");
                xml.WriteStartElement("td");
                xml.WriteAttributeString("align", "left");
                xml.WriteString("FailOnError");
                xml.WriteEndElement();
                xml.WriteStartElement("td");
                xml.WriteAttributeString("align", "left");
                xml.WriteString(task.FailOnError.ToString());
                xml.WriteEndElement();
                xml.WriteEndElement();
                foreach (var (key, value) in task.ExtraProperties.Where(x => !ExcludedAttributes.Contains(x.Key)))
                {
                    xml.WriteStartElement("tr");
                    xml.WriteStartElement("td");
                    xml.WriteAttributeString("align", "left");
                    xml.WriteString(key);
                    xml.WriteEndElement();
                    xml.WriteStartElement("td");
                    xml.WriteAttributeString("align", "left");
                    if ((value as JObject)?.ContainsKey("FullPath") ?? false)
                        xml.WriteString(value["FullPath"]?.ToString());
                    else if (key == "Encoding")
                        xml.WriteString(value["EncodingName"]?.ToString());
                    else
                        xml.WriteWrappedText(value.ToString());
                    xml.WriteEndElement();
                    xml.WriteEndElement();
                }
                xml.WriteEndElement();
            }
            sb.Append(@">
                ]");
        }

        private static void WriteWrappedText(this XmlWriter xml, string text)
        {
            const int wrapLength = 40;

            if (string.IsNullOrWhiteSpace(text))
            {
                xml.WriteRaw("&nbsp;");
                return;
            }

            var breakChars = " \n\t-:;.,!/\\".ToCharArray();
            foreach (var line in text.Split('\n'))
            {
                var offset = 0;
                while (offset < line.Length)
                {
                    var pos = -1;
                    if (offset + wrapLength < line.Length)
                    {
                        pos = line.LastIndexOfAny(breakChars, offset + wrapLength, offset);
                        if (pos < offset && offset + wrapLength < line.Length)
                            pos = line.IndexOfAny(breakChars, offset + wrapLength);
                    }
                    if (pos < 0)
                    {
                        xml.WriteString(line[offset..]);
                        break;
                    }
                    xml.WriteString(line[offset..pos]);
                    xml.WriteStartElement("br");
                    xml.WriteAttributeString("align", "left");
                    xml.WriteEndElement();
                    offset = pos + 1;
                }
                xml.WriteStartElement("br");
                xml.WriteAttributeString("align", "left");
                xml.WriteEndElement();
            }
        }
    }
}
