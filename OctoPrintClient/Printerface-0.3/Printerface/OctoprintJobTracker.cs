﻿using System;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
namespace OctoprintClient
{
    /// <summary>
    /// Tracks Jobs, can get Progress or other information, start, stop or pause jobs.
    /// </summary>
    public class OctoprintJobTracker : OctoprintTracker
    {

        /// <summary>
        /// Initializes a Jobtracker, this shouldn't be done directly and is part of the Connection it needs anyway
        /// </summary>
        /// <param name="con">The Octoprint connection it connects to.</param>
        public OctoprintJobTracker(OctoprintConnection con) : base(con)
        {
        }


        /// <summary>
        /// Action for Eventhandling the Websocket Job info
        /// </summary>
        public event Action<OctoprintJobInfo> JobinfoHandlers;
        public bool JobListens()
        {
            return JobinfoHandlers != null;
        }
        public void CallJob(OctoprintJobInfo i)
        {
            JobinfoHandlers(i);
        }

        /// <summary>
        /// Action for Eventhandling the Websocket Progress info
        /// </summary>
        public event Action<OctoprintJobProgress> ProgressinfoHandlers;
        public bool ProgressListens()
        {
            return ProgressinfoHandlers != null;
        }
        public void CallProgress(OctoprintJobProgress p)
        {
            ProgressinfoHandlers.Invoke(p);
        }


        /// <summary>
        /// Gets info of the current job
        /// </summary>
        /// <returns>The info.</returns>
        public OctoprintJobInfo GetInfo()
        {
            OctoprintJobInfo result = new OctoprintJobInfo();
            string jobInfo = Connection.Get("api/job");
            JObject data = JsonConvert.DeserializeObject<JObject>(jobInfo);
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            JToken job = data.Value<JToken>("job");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            result.EstimatedPrintTime = job.Value<int?>("estimatedPrintTime") ?? -1;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            JToken filament = job.Value<JToken>("filament");
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            if (filament.HasValues)
                result.Filament = new OctoprintFilamentInfo
                {
                    Lenght = filament.Value<int?>("length") ?? -1,
                    Volume = filament.Value<int?>("volume") ?? -1
                };
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            JToken file = job.Value<JToken>("file");
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            result.File = new OctoprintFile
            {
                Name = file.Value<String>("name") ?? "",
                Origin = file.Value<String>("origin") ?? "",
                Size = file.Value<int?>("size") ?? -1,
                Date = file.Value<int?>("date") ?? -1
            };
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            return result;
        }

        /// <summary>
        /// Gets the progress of the current job
        /// </summary>
        /// <returns>The progress.</returns>
        public OctoprintJobProgress GetProgress()
        {
            string jobInfo = Connection.Get("api/job");
            JObject data = JsonConvert.DeserializeObject<JObject>(jobInfo);
            OctoprintJobProgress result = new OctoprintJobProgress();
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            JToken progress = data.Value<JToken>("progress");
#pragma warning restore CS8602 // Dereference of a possibly null reference.
#pragma warning disable CS8602 // Dereference of a possibly null reference.
            result.Completion = progress.Value<double?>("completion") ?? -1.0;
#pragma warning restore CS8602 // Dereference of a possibly null reference.
            result.Filepos = progress.Value<int?>("filepos") ?? -1;
            result.PrintTime = progress.Value<int?>("printTime") ?? -1;
            result.PrintTimeLeft = progress.Value<int?>("printTimeLeft") ?? -1;
            return result;
        }

        /// <summary>
        /// Posts a command with a certain <paramref name="action"/>.
        /// </summary>
        /// <returns>The Http Result</returns>
        /// <param name="command">The Command to execute on the Job.</param>
        /// <param name="action">The exact action withing the command to take.</param>
        private string Post(string command, string action)
        {
            string returnValue = string.Empty;
            JObject data = new JObject
            {
                { "command", command }
            };
            if (action != "")
            {
                data.Add("action", action);
            }
            try
            {
                returnValue = Connection.PostJson("api/job", data);
            }
            catch (WebException e)
            {
#pragma warning disable CS8602 // Dereference of a possibly null reference.
                switch (((HttpWebResponse)e.Response).StatusCode)
                {
                    case HttpStatusCode.Conflict:
                        return "409 Current jobstate is incompatible with this type of interaction";

                    default:
                        return "unknown webexception occured";
                }
#pragma warning restore CS8602 // Dereference of a possibly null reference.

            }
            return returnValue;
        }

        /// <summary>
        /// Starts the job.
        /// </summary>
        /// <returns>The Http Result</returns>
        public string StartJob()
        {
            return Post("start", "");
        }

        /// <summary>
        /// Cancels the job.
        /// </summary>
        /// <returns>The Http Result</returns>
        public string CancelJob()
        {
            return Post("cancel", "");
        }

        /// <summary>
        /// Restarts the job.
        /// </summary>
        /// <returns>The Http Result</returns>
        public string RestartJob()
        {
            return Post("restart", "");
        }


        /// <summary>
        /// Pauses the job.
        /// </summary>
        /// <returns>The Http Result</returns>
        public string PauseJob()
        {
            return Post("pause", "pause");
        }

        /// <summary>
        /// Resumes the job.
        /// </summary>
        /// <returns>The Http Result</returns>
        public string ResumeJob()
        {
            return Post("pause", "resume");
        }

        /// <summary>
        /// Pauses the job if it runs, resumes the Job if it is paused.
        /// </summary>
        /// <returns>The Http Result</returns>
        public string ToggleJob()
        {
            return Post("pause", "toggle");
        }
    }
    public class OctoprintFilamentInfo
    {
        public int Lenght { get; set; }
        public double Volume { get; set; }
    }
    public class OctoprintJobInfo
    {
        public OctoprintFile File { get; set; }
        public int EstimatedPrintTime { get; set; }
        public OctoprintFilamentInfo Filament { get; set; }
    }
    public class OctoprintJobProgress
    {
        public Double Completion { get; set; }
        public int Filepos { get; set; }
        public int PrintTime { get; set; }
        public int PrintTimeLeft { get; set; }
        public override string ToString()
        {
            if (Filepos != -1)
                return "Completion: " + Completion + "\nFilepos: " + Filepos + "\nPrintTime: " + PrintTime + "\nPrintTimeLeft: " + PrintTimeLeft + "\n";
            else
                return "No Job found running";
        }
    }
}
