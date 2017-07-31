using System.IO;
using System.Linq;
using System.Xml.Linq;
using EnvDTE;
using Newtonsoft.Json.Linq;
using System;


namespace TemplateCreator.Shared {
    public class TemplateCreator {
        private JObject _templateInfo { get; set; }
        public void AddMissingFiles(Project proj) {
            if(proj == null) {
                throw new ArgumentNullException("proj");
            }

            string projectName = Path.GetFileNameWithoutExtension(proj.FullName);
            string projectDir = Path.GetDirectoryName(proj.FullName);
            string templateJsonDir = Path.Combine(projectDir, ".template.config");
            if (!Directory.Exists(templateJsonDir)) {
                Directory.CreateDirectory(templateJsonDir);
            }
            // see which files are missing and add then create those
            
            bool hasTemplateJsonFile = File.Exists(Path.Combine(templateJsonDir, "template.json"));
            bool hasVsHostFile = File.Exists(Path.Combine(templateJsonDir, $"{projectName}.vstemplate"));
            bool hasVstemplateFile = File.Exists(Path.Combine(templateJsonDir, "vs-2017.3.host.json"));
            bool hasCliHostFile = File.Exists(Path.Combine(templateJsonDir, "dotnetcli.host.json"));

            if(hasTemplateJsonFile && 
                hasVsHostFile &&
                hasVstemplateFile &&
                hasCliHostFile) {
                // nothing to do
                return;
            }

            JObject templateData = GetTemplateJsonDataFromUser(proj);
            CreateTemplateJsonIfNotExists(Path.Combine(templateJsonDir, "template.json"), proj.FullName, templateData);

        }

        private void CreateTemplateJsonIfNotExists(string templateJsonPath,string projectFilepath,JObject templateData) {
            if(templateJsonPath == null) {
                throw new ArgumentNullException("filepath");
            }
            if(templateData == null) {
                throw new ArgumentNullException("templateData");
            }

            if (File.Exists(templateJsonPath)) {
                return;
            }

            File.WriteAllText(templateJsonPath, templateData.ToString());
        }

        private void CreateVsTemplateFileIfNotExists(string vstemplateFilepath, string projectFilepath, JObject templateData) {
            if (string.IsNullOrWhiteSpace(vstemplateFilepath)) {
                throw new ArgumentNullException("vstemplateFilepath");
            }
            if(templateData == null) {
                throw new ArgumentNullException("templateData");
            }

            if (File.Exists(vstemplateFilepath)) {
                return;
            }


        }

            private JObject GetTemplateJsonDataFromUser(Project proj) {
            if (_templateInfo == null) {
                string fullPath = proj.FullName;
                string name = Path.GetFileNameWithoutExtension(fullPath);
                var win = new InfoCollectorDialog(name);
                win.CenterInVs();
                if (win.ShowDialog().GetValueOrDefault()) {
                    const string solutionTemplate = @"{
    ""author"": """",
    ""classifications"": [ ],
    ""description"": """",
    ""name"": """",
    ""defaultName"": """",
    ""identity"": """",
    ""groupIdentity"": """",
    ""tags"": { },
    ""shortName"": """",
    ""sourceName"": """",
    ""guids"": [ ]
}";

                    var o = JObject.Parse(solutionTemplate);
                    o["author"] = win.AuthorTextBox.Text;
                    o["name"] = win.FriendlyNameTextBox.Text;
                    o["defaultName"] = win.DefaultNameTextBox.Text;
                    o["sourceName"] = Path.GetFileNameWithoutExtension(proj.FullName);
                    o["shortName"] = win.ShortNameTextBox.Text;

                    var guids = (JArray)o["guids"];
                    string projectGuid = ExtractProjectGuid(fullPath);

                    if (!string.IsNullOrEmpty(projectGuid)) {
                        guids.Add(ExtractProjectGuid(fullPath));
                    }

                    _templateInfo = o;
                }
            }

            return _templateInfo;
        }

        private string ExtractProjectGuid(string fullPath) {
            var doc = XDocument.Load(fullPath);
            XElement element = doc.Descendants().FirstOrDefault(x => x.Name.LocalName == "ProjectGuid");
            return element?.Value;
        }
    }
}
