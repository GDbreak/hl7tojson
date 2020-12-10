using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using NHapi;
using NHapi.Base.Parser;
using NHapi.Model.V23.Message;
using Newtonsoft.Json;
using System.Xml;

namespace HL7toJSON
{
    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;
        private FileSystemWatcher _folderWatcher;
        private readonly string _inputFolder;
        private readonly string _outputFolder;
        private readonly string _processedFolder;

        public Worker(ILogger<Worker> logger, IOptions<AppSettings> settings)
        {
            _logger = logger;
            _inputFolder = settings.Value.InputFolder;
            _outputFolder = settings.Value.OutputFolder;
            _processedFolder = settings.Value.ProcessedFolder;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await Task.CompletedTask;
        }


        public override Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Service Starting");
            if (!Directory.Exists(_inputFolder))
            {
                _logger.LogWarning($"Please make sure the InputFolder [{_inputFolder}] exists, then restart the service.");
                return Task.CompletedTask;
            }

            _logger.LogInformation($"Binding Events from Input Folder: {_inputFolder}");
            _folderWatcher = new FileSystemWatcher(_inputFolder, "*.hl7")
            {
                NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.LastWrite | NotifyFilters.FileName |
                                  NotifyFilters.DirectoryName
            };
            _folderWatcher.Created += Input_OnChanged;
            _folderWatcher.EnableRaisingEvents = true;

            return base.StartAsync(cancellationToken);
        }


        protected void Input_OnChanged(object source, FileSystemEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Created)
            {

                try
                {
                    _logger.LogInformation($"InBound Change Event Triggered by [{e.FullPath}]");


                    string text = System.IO.File.ReadAllText(e.FullPath);

                    File.Move(e.FullPath, _processedFolder.ToString() + '/' + Path.GetFileName(e.FullPath));

                    var ourPipeParser = new PipeParser();
                    // parse the string format message into a Java message object
                    var hl7Message = ourPipeParser.Parse(text);

                    //cast to ACK message to get access to ACK message data
                    var ackResponseMessage = hl7Message as ACK;
                    if (ackResponseMessage != null)
                    {
                        //access message data and display it
                        //note that I am using encode method at the end to convert it back to string for display
                        var mshSegmentMessageData = ackResponseMessage.MSH;
                        _logger.LogInformation("Message Type is " + mshSegmentMessageData.MessageType.MessageType);
                        _logger.LogInformation("Message Control Id is " + mshSegmentMessageData.MessageControlID);
                        _logger.LogInformation("Message Timestamp is " + mshSegmentMessageData.DateTimeOfMessage.TimeOfAnEvent.GetAsDate());
                        _logger.LogInformation("Sending Facility is " + mshSegmentMessageData.SendingFacility.NamespaceID.Value);

                        //update message data in MSA segment
                        ackResponseMessage.MSA.AcknowledgementCode.Value = "AR";
                    }

                    // Display the updated HL7 message using Pipe delimited format
                    _logger.LogInformation("HL7 Pipe Delimited Message Output:");
                    _logger.LogInformation(ourPipeParser.Encode(hl7Message));

                    // instantiate an XML parser that NHAPI provides
                    var ourXmlParser = new DefaultXMLParser();

                    // convert from default encoded message into XML format, and send it to standard out for display
                    _logger.LogInformation("HL7 XML Formatted Message Output:");
                    _logger.LogInformation(ourXmlParser.Encode(hl7Message));
                    XmlDocument doc = new XmlDocument();
                    doc.LoadXml(ourXmlParser.Encode(hl7Message));

                    string json = JsonConvert.SerializeXmlNode(doc);

                    XmlDocument outputxml = JsonConvert.DeserializeXmlNode(json);


                    _logger.LogInformation(outputxml.ToString());

                    File.WriteAllText(_outputFolder + '/' + Path.GetFileNameWithoutExtension(e.FullPath) + ".json", json);
                    using (Stream file = File.Create(_outputFolder + '/' + Path.GetFileNameWithoutExtension(e.FullPath) + ".xml"))
                    {
                        outputxml.Save(file);
                    }



                }
                catch (Exception ex)
                {
                    _logger.LogInformation($"Error occured -> {ex.StackTrace}");
                }
            }

            _logger.LogInformation("Done with Inbound Change Event");
        }
        
        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Stopping Service");
            _folderWatcher.EnableRaisingEvents = false;
            await base.StopAsync(cancellationToken);
        }


        public override void Dispose()
        {
            _logger.LogInformation("Disposing Service");
            _folderWatcher.Dispose();
            base.Dispose();
        }
    }
}
