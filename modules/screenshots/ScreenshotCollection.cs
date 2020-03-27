﻿//-----------------------------------------------------------------------
// <copyright file="ScreenshotCollection.cs" company="Gavin Kendall">
//     Copyright (c) Gavin Kendall. All rights reserved.
// </copyright>
// <author>Gavin Kendall</author>
// <summary></summary>
//-----------------------------------------------------------------------
using System;
using System.IO;
using System.Text;
using System.Xml;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace AutoScreenCapture
{
    /// <summary>
    /// A collection class to store and manage Screenshot objects.
    /// </summary>
    public class ScreenshotCollection
    {
        private XmlDocument xDoc;
        private List<Slide> _slideList;
        private List<string> _slideNameList;
        private List<Screenshot> _screenshotList;
        private ImageFormatCollection _imageFormatCollection;
        private ScreenCollection _screenCollection;

        // Required when multiple threads are writing to the same file.
        private readonly Mutex _mutexWriteFile = new Mutex();

        private const string XML_FILE_INDENT_CHARS = "   ";
        private const string XML_FILE_SCREENSHOT_NODE = "screenshot";
        private const string XML_FILE_SCREENSHOTS_NODE = "screenshots";
        private const string XML_FILE_ROOT_NODE = "autoscreen";

        private const string SCREENSHOT_VIEWID = "viewid";
        private const string SCREENSHOT_DATE = "date";
        private const string SCREENSHOT_TIME = "time";
        private const string SCREENSHOT_PATH = "path";
        private const string SCREENSHOT_FORMAT = "format";
        private const string SCREENSHOT_SCREEN = "screen";
        private const string SCREENSHOT_COMPONENT = "component";
        private const string SCREENSHOT_SLIDENAME = "slidename";
        private const string SCREENSHOT_SLIDEVALUE = "slidevalue";
        private const string SCREENSHOT_WINDOW_TITLE = "windowtitle";
        private const string SCREENSHOT_PROCESS_NAME = "processname";
        private const string SCREENSHOT_LABEL = "label";

        private readonly string SCREENSHOTS_XPATH;
        private readonly string SCREENSHOT_XPATH;

        private  string AppCodename { get; set; }
        private  string AppVersion { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public ScreenshotCollection(ImageFormatCollection imageFormatCollection, ScreenCollection screenCollection)
        {
            StringBuilder sbScreenshots = new StringBuilder();
            sbScreenshots.Append("/");
            sbScreenshots.Append(XML_FILE_ROOT_NODE);
            sbScreenshots.Append("/");
            sbScreenshots.Append(XML_FILE_SCREENSHOTS_NODE);

            StringBuilder sbScreenshot = new StringBuilder();
            sbScreenshot.Append("/");
            sbScreenshot.Append(XML_FILE_ROOT_NODE);
            sbScreenshot.Append("/");
            sbScreenshot.Append(XML_FILE_SCREENSHOTS_NODE);
            sbScreenshot.Append("/");
            sbScreenshot.Append(XML_FILE_SCREENSHOT_NODE);

            SCREENSHOTS_XPATH = sbScreenshots.ToString();
            SCREENSHOT_XPATH = sbScreenshot.ToString();

            _screenshotList = new List<Screenshot>();
            Log.Write("Initialized screenshot list");

            _slideList = new List<Slide>();
            Log.Write("Initialized slide list");

            _slideNameList = new List<string>();
            Log.Write("Initialized slide name list");

            _imageFormatCollection = imageFormatCollection;
            _screenCollection = screenCollection;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="screenshot"></param>
        public void Add(Screenshot screenshot)
        {
            lock (_screenshotList)
            {
                if (!_screenshotList.Contains(screenshot))
                {
                    _screenshotList.Add(screenshot);
                }

                if (screenshot.Slide != null && !string.IsNullOrEmpty(screenshot.Slide.Name))
                {
                    if (!_slideNameList.Contains(screenshot.Slide.Name))
                    {
                        _slideNameList.Add(screenshot.Slide.Name);
                        _slideList.Add(screenshot.Slide);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public Screenshot Get(int index)
        {
            return (Screenshot)_screenshotList[index];
        }

        /// <summary>
        /// 
        /// </summary>
        public int Count
        {
            get { return _screenshotList.Count; }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterType"></param>
        /// <returns></returns>
        public List<string> GetFilterValueList(string filterType)
        {
            if (filterType.Equals("Image Format"))
            {
                return LoadXmlFileAndReturnNodeValues("format", null, "format");
            }

            if (filterType.Equals("Label"))
            {
                return LoadXmlFileAndReturnNodeValues("label", null, "label");
            }

            if (filterType.Equals("Process Name"))
            {
                return LoadXmlFileAndReturnNodeValues("processname", null, "processname");
            }

            if (filterType.Equals("Window Title"))
            {
                return LoadXmlFileAndReturnNodeValues("windowtitle", null, "windowtitle");
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterType"></param>
        /// <param name="filterValue"></param>
        /// <returns></returns>
        public List<string> GetDatesByFilter(string filterType, string filterValue)
        {
            if (!string.IsNullOrEmpty(filterValue))
            {
                if (filterType.Equals("Image Format"))
                {
                    return LoadXmlFileAndReturnNodeValues("format", filterValue, "date");
                }

                if (filterType.Equals("Label"))
                {
                    return LoadXmlFileAndReturnNodeValues("label", filterValue, "date");
                }

                if (filterType.Equals("Process Name"))
                {
                    return LoadXmlFileAndReturnNodeValues("processname", filterValue, "date");
                }

                if (filterType.Equals("Window Title"))
                {
                    return LoadXmlFileAndReturnNodeValues("windowtitle", filterValue, "date");
                }
            }

            return LoadXmlFileAndReturnNodeValues("date", null, "date");
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="filterType"></param>
        /// <param name="filterValue"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public List<Slide> GetSlides(string filterType, string filterValue, string date)
        {
            if (string.IsNullOrEmpty(date))
            {
                return null;
            }

            Log.Write("Getting slides from screenshot list");

            LoadXmlFileAndAddScreenshots(date);

            if (!string.IsNullOrEmpty(filterValue))
            {
                if (filterType.Equals("Image Format"))
                {
                    Log.Write("Getting slides from screenshot list based on Image Format filter");
                    return _screenshotList.Where(x => x.Format != null && !string.IsNullOrEmpty(x.Format.Name) && x.Format.Name.Equals(filterValue) && x.Date.Equals(date)).GroupBy(x => x.Slide.Name).Select(x => x.First().Slide).ToList();
                }

                if (filterType.Equals("Label"))
                {
                    Log.Write("Getting slides from screenshot list based on Label filter");
                    return _screenshotList.Where(x => x.Label != null && !string.IsNullOrEmpty(x.Label) && x.Label.Equals(filterValue) && x.Date.Equals(date)).GroupBy(x => x.Slide.Name).Select(x => x.First().Slide).ToList();
                }

                if (filterType.Equals("Process Name"))
                {
                    Log.Write("Getting slides from screenshot list based on Process Name filter");
                    return _screenshotList.Where(x => x.ProcessName != null && !string.IsNullOrEmpty(x.ProcessName) && x.ProcessName.Equals(filterValue) && x.Date.Equals(date)).GroupBy(x => x.Slide.Name).Select(x => x.First().Slide).ToList();
                }

                if (filterType.Equals("Window Title"))
                {
                    Log.Write("Getting slides from screenshot list based on Window Title filter");
                    return _screenshotList.Where(x => x.WindowTitle != null && !string.IsNullOrEmpty(x.WindowTitle) && x.WindowTitle.Equals(filterValue) && x.Date.Equals(date)).GroupBy(x => x.Slide.Name).Select(x => x.First().Slide).ToList();
                }
            }

            return _slideList.Where(x => x.Date.Equals(date)).GroupBy(x => x.Name).Select(x => x.First()).ToList();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="slideName"></param>
        /// <param name="viewId"></param>
        /// <returns></returns>
        public Screenshot GetScreenshot(string slideName, Guid viewId)
        {
            Screenshot foundScreenshot = new Screenshot();

            if (_screenshotList != null)
            {
                lock (_screenshotList)
                {
                    foreach (Screenshot screenshot in _screenshotList)
                    {
                        if (screenshot.Slide != null && !string.IsNullOrEmpty(screenshot.Slide.Name) && screenshot.Slide.Name.Equals(slideName) && screenshot.ViewId.Equals(viewId))
                        {
                            foundScreenshot = screenshot;
                            break;
                        }
                    }
                }
            }

            return foundScreenshot;
        }

        /// <summary>
        /// Loads screenshot references from the screenshots.xml file into an XmlDocument so it's available in memory.
        /// The old way of loading screenshots also had the application construct each Screenshot object from an XML screenshot node and add it to the collection. This would take a long time to load for a large screenshots.xml file.
        /// The new way (as of version 2.2.5.0) will only load XML screenshot nodes whenever necessary.
        /// </summary>
        public void LoadXmlFile()
        {
            try
            {
                _mutexWriteFile.WaitOne();

                if (_screenshotList != null && !File.Exists(FileSystem.ScreenshotsFile))
                {
                    Log.Write("Could not find \"" + FileSystem.ScreenshotsFile + "\" so creating default version");

                    XmlWriterSettings xSettings = new XmlWriterSettings
                    {
                        Indent = true,
                        CloseOutput = true,
                        CheckCharacters = true,
                        Encoding = Encoding.UTF8,
                        NewLineChars = Environment.NewLine,
                        IndentChars = XML_FILE_INDENT_CHARS,
                        NewLineHandling = NewLineHandling.Entitize,
                        ConformanceLevel = ConformanceLevel.Document
                    };

                    using (XmlWriter xWriter = XmlWriter.Create(FileSystem.ScreenshotsFile, xSettings))
                    {
                        xWriter.WriteStartDocument();
                        xWriter.WriteStartElement(XML_FILE_ROOT_NODE);
                        xWriter.WriteAttributeString("app", "version", XML_FILE_ROOT_NODE, Settings.ApplicationVersion);
                        xWriter.WriteAttributeString("app", "codename", XML_FILE_ROOT_NODE, Settings.ApplicationCodename);
                        xWriter.WriteStartElement(XML_FILE_SCREENSHOTS_NODE);

                        xWriter.WriteEndElement();
                        xWriter.WriteEndElement();
                        xWriter.WriteEndDocument();

                        xWriter.Flush();
                        xWriter.Close();
                    }

                    Log.Write("Created \"" + FileSystem.ScreenshotsFile + "\"");
                }

                if (File.Exists(FileSystem.ScreenshotsFile))
                {
                    xDoc = new XmlDocument();

                    lock (xDoc)
                    {
                        xDoc.Load(FileSystem.ScreenshotsFile);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("ScreenshotCollection::LoadXmlFile", ex);
            }
            finally
            {
                _mutexWriteFile.ReleaseMutex();
            }
        }

        /// <summary>
        /// Loads a list of node values from the screenshots.xml file based on a node name.
        /// LoadXmlFileAndReturnNodeValues(nodeName: "label", nodeValue: null, nodeReturn: "label") retrieves all label node values of label nodes.
        /// LoadXmlFileAndReturnNodeValues(nodeName: "label", nodeValue: "My label", nodeReturn: "label") retrieves all label node values of label nodes matching "My label".
        /// LoadXmlFileAndReturnNodeValues(nodeName: "label", nodeValue: "My label", nodeReturn: "date") retrieves all date node values of label nodes matching "My label".
        /// </summary>
        /// <param name="nodeName">The name of the node to search for in the XML file. This will become part of an XPath query.</param>
        /// <param name="nodeValue">The value of the node to search for in the XML file. If given a value, it will be used in an XPath query to search for a specific node value once a node is found using the node name. If given null, the node name will be used to retrieve all nodes matching the node name.</param>
        /// <param name="nodeReturn">The value of the node that will be returned based on the node name.</param>
        /// <returns>A list of node values.</returns>
        public List<string> LoadXmlFileAndReturnNodeValues(string nodeName, string nodeValue, string nodeReturn)
        {
            try
            {
                XmlNodeList xNodes = null;

                if (string.IsNullOrEmpty(nodeName))
                {
                    return null;
                }

                _mutexWriteFile.WaitOne();

                if (xDoc != null)
                {
                    lock (xDoc)
                    {
                        if (string.IsNullOrEmpty(nodeValue))
                        {
                            Log.Write("Loading node values by " + nodeName + " from \"" + FileSystem.ScreenshotsFile + "\" using XPath query \"" + SCREENSHOT_XPATH + "/" + nodeName + "\"");
                            xNodes = xDoc.SelectNodes(SCREENSHOT_XPATH + "/" + nodeName);
                        }
                        else
                        {
                            Log.Write("Loading node values by " + nodeName + " from \"" + FileSystem.ScreenshotsFile + "\" using XPath query \"" + SCREENSHOT_XPATH + "[" + nodeName + "='" + nodeValue + "']\"");
                            xNodes = xDoc.SelectNodes(SCREENSHOT_XPATH + "[" + nodeName + "='" + nodeValue + "']");
                        }

                        if (xNodes != null)
                        {
                            List<string> nodeValues = new List<string>();

                            foreach (XmlNode xNode in xNodes)
                            {
                                XmlNodeReader xReader = new XmlNodeReader(xNode);

                                while (xReader.Read())
                                {
                                    if (xReader.IsStartElement())
                                    {
                                        if (xReader.Name.Equals(nodeReturn))
                                        {
                                            xReader.Read();

                                            if (!string.IsNullOrEmpty(xReader.Value) && !nodeValues.Contains(xReader.Value))
                                            {
                                                nodeValues.Add(xReader.Value);
                                            }
                                        }
                                    }
                                }

                                xReader.Close();
                            }

                            return nodeValues;
                        }
                        else
                        {
                            Log.Write("WARNING: Unable to load node values from \"" + FileSystem.ScreenshotsFile + "\"");
                        }
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Log.Write("ScreenshotCollection::LoadXmlFileAndReturnNodeValues", ex);
                return null;
            }
            finally
            {
                _mutexWriteFile.ReleaseMutex();
            }
        }

        /// <summary>
        /// Loads the screenshots taken on a particular day from the screenshots.xml file.
        /// </summary>
        /// <param name="date">The date to load screenshots from.</param>
        public void LoadXmlFileAndAddScreenshots(string date)
        {
            try
            {
                XmlNodeList xScreenshots = null;

                if (string.IsNullOrEmpty(date))
                {
                    return;
                }

                _mutexWriteFile.WaitOne();

                if (xDoc != null)
                {
                    lock (xDoc)
                    {
                        AppVersion = xDoc.SelectSingleNode("/autoscreen").Attributes["app:version"]?.Value;
                        AppCodename = xDoc.SelectSingleNode("/autoscreen").Attributes["app:codename"]?.Value;

                        Log.Write("Loading screenshots taken on " + date + " from \"" + FileSystem.ScreenshotsFile + "\" using XPath query \"" + SCREENSHOT_XPATH + "[date='" + date + "']" + "\"");
                        xScreenshots = xDoc.SelectNodes(SCREENSHOT_XPATH + "[date='" + date + "']");

                        if (xScreenshots != null)
                        {
                            Log.Write("Loading " + xScreenshots.Count + " screenshots from \"" + FileSystem.ScreenshotsFile + "\" ...");

                            foreach (XmlNode xScreenshot in xScreenshots)
                            {
                                Screenshot screenshot = new Screenshot();
                                screenshot.Slide = new Slide();

                                XmlNodeReader xReader = new XmlNodeReader(xScreenshot);

                                while (xReader.Read())
                                {
                                    if (xReader.IsStartElement())
                                    {
                                        switch (xReader.Name)
                                        {
                                            case SCREENSHOT_VIEWID:
                                                xReader.Read();
                                                screenshot.ViewId = Guid.Parse(xReader.Value);

                                                if (Log.DebugMode)
                                                    Log.Write("[" + screenshot.ViewId + "] View Id = " + screenshot.ViewId);
                                                break;

                                            case SCREENSHOT_DATE:
                                                xReader.Read();
                                                screenshot.Date = xReader.Value;
                                                screenshot.Slide.Date = xReader.Value;

                                                if (Log.DebugMode)
                                                    Log.Write("[" + screenshot.ViewId + "] Date = " + screenshot.Date);
                                                break;

                                            case SCREENSHOT_TIME:
                                                xReader.Read();
                                                screenshot.Time = xReader.Value;

                                                if (Log.DebugMode)
                                                    Log.Write("[" + screenshot.ViewId + "] Time = " + screenshot.Time);
                                                break;

                                            case SCREENSHOT_PATH:
                                                xReader.Read();
                                                screenshot.Path = xReader.Value;

                                                if (Log.DebugMode)
                                                    Log.Write("[" + screenshot.ViewId + "] Path = " + screenshot.Path);
                                                break;

                                            case SCREENSHOT_FORMAT:
                                                xReader.Read();
                                                screenshot.Format = _imageFormatCollection.GetByName(xReader.Value);

                                                if (Log.DebugMode)
                                                    Log.Write("[" + screenshot.ViewId + "] Format = " + screenshot.Format);
                                                break;

                                            // 2.1 used "screen" for its definition of each display/monitor whereas 2.2 uses "component".
                                            // Active Window is now represented by 0 rather than 5.
                                            case SCREENSHOT_SCREEN:
                                                if (Settings.VersionManager.IsOldAppVersion(AppCodename, AppVersion) &&
                                                    Settings.VersionManager.Versions.Get("Clara", "2.1.8.2") != null && string.IsNullOrEmpty(AppCodename) && string.IsNullOrEmpty(AppVersion))
                                                {
                                                    xReader.Read();

                                                    screenshot.Screen = Convert.ToInt32(xReader.Value);

                                                    screenshot.Component = screenshot.Screen == 5 ? 0 : screenshot.Screen;
                                                }

                                                if (Log.DebugMode)
                                                    Log.Write("[" + screenshot.ViewId + "] Component = " + screenshot.Component);
                                                break;

                                            // We still want to support "component" since this was introduced in version 2.2 as the new representation for "screen".
                                            case SCREENSHOT_COMPONENT:
                                                xReader.Read();
                                                screenshot.Component = Convert.ToInt32(xReader.Value);

                                                if (screenshot.Component == -1)
                                                {
                                                    screenshot.ScreenshotType = ScreenshotType.Region;
                                                }
                                                else if (screenshot.Component == 0)
                                                {
                                                    screenshot.ScreenshotType = ScreenshotType.ActiveWindow;
                                                }
                                                else
                                                {
                                                    screenshot.ScreenshotType = ScreenshotType.Screen;
                                                }

                                                if (Log.DebugMode)
                                                    Log.Write("[" + screenshot.ViewId + "] Component = " + screenshot.Component);
                                                break;

                                            case SCREENSHOT_SLIDENAME:
                                                xReader.Read();
                                                screenshot.Slide.Name = xReader.Value;

                                                if (Log.DebugMode)
                                                    Log.Write("[" + screenshot.ViewId + "] Slide Name = " + screenshot.Slide.Name);
                                                break;

                                            case SCREENSHOT_SLIDEVALUE:
                                                xReader.Read();
                                                screenshot.Slide.Value = xReader.Value;

                                                if (Log.DebugMode)
                                                    Log.Write("[" + screenshot.ViewId + "] Slide Value = " + screenshot.Slide.Value);
                                                break;

                                            case SCREENSHOT_WINDOW_TITLE:
                                                xReader.Read();
                                                screenshot.WindowTitle = xReader.Value;

                                                if (Log.DebugMode)
                                                    Log.Write("[" + screenshot.ViewId + "] Window Title = " + screenshot.WindowTitle);
                                                break;

                                            case SCREENSHOT_PROCESS_NAME:
                                                xReader.Read();
                                                screenshot.ProcessName = xReader.Value;

                                                if (Log.DebugMode)
                                                    Log.Write("[" + screenshot.ViewId + "] Process Name = " + screenshot.ProcessName);
                                                break;

                                            case SCREENSHOT_LABEL:
                                                xReader.Read();
                                                screenshot.Label = xReader.Value;

                                                if (Log.DebugMode)
                                                    Log.Write("[" + screenshot.ViewId + "] Label = " + screenshot.Label);
                                                break;
                                        }
                                    }
                                }

                                xReader.Close();

                                if (Settings.VersionManager.IsOldAppVersion(AppCodename, AppVersion))
                                {
                                    if (Settings.VersionManager.Versions.Get("Clara", "2.1.8.2") != null && string.IsNullOrEmpty(AppCodename) && string.IsNullOrEmpty(AppVersion))
                                    {
                                        // We need to associate the screenshot's view ID with the component's view ID
                                        // because this special ID value is used for figuring out what screenshot image to display.
                                        screenshot.ViewId = _screenCollection.GetByComponent(screenshot.Component).ViewId;

                                        string windowTitle = "*Screenshot imported from an old version of Auto Screen Capture*";

                                        Regex rgxOldSlidename = new Regex(@"^(?<Date>\d{4}-\d{2}-\d{2}) (?<Time>(?<Hour>\d{2})-(?<Minute>\d{2})-(?<Second>\d{2})-(?<Millisecond>\d{3}))");

                                        string hour = rgxOldSlidename.Match(screenshot.Slide.Name).Groups["Hour"].Value;
                                        string minute = rgxOldSlidename.Match(screenshot.Slide.Name).Groups["Minute"].Value;
                                        string second = rgxOldSlidename.Match(screenshot.Slide.Name).Groups["Second"].Value;
                                        string millisecond = rgxOldSlidename.Match(screenshot.Slide.Name).Groups["Millisecond"].Value;

                                        screenshot.Date = rgxOldSlidename.Match(screenshot.Slide.Name).Groups["Date"].Value;
                                        screenshot.Time = hour + ":" + minute + ":" + second + "." + millisecond;

                                        screenshot.Slide.Name = "{date=" + screenshot.Date + "}{time=" + screenshot.Time + "}";
                                        screenshot.Slide.Value = screenshot.Time + " [" + windowTitle + "]";

                                        screenshot.WindowTitle = windowTitle;
                                    }

                                    // Remove all the existing XML child nodes from the old XML screenshot.
                                    xScreenshot.RemoveAll();

                                    // Prepare the new XML child nodes for the old XML screenshot ...

                                    XmlElement xViewId = xDoc.CreateElement(SCREENSHOT_VIEWID);
                                    xViewId.InnerText = screenshot.ViewId.ToString();

                                    XmlElement xDate = xDoc.CreateElement(SCREENSHOT_DATE);
                                    xDate.InnerText = screenshot.Date;

                                    XmlElement xTime = xDoc.CreateElement(SCREENSHOT_TIME);
                                    xTime.InnerText = screenshot.Time;

                                    XmlElement xPath = xDoc.CreateElement(SCREENSHOT_PATH);
                                    xPath.InnerText = screenshot.Path;

                                    XmlElement xFormat = xDoc.CreateElement(SCREENSHOT_FORMAT);
                                    xFormat.InnerText = screenshot.Format.Name;

                                    XmlElement xComponent = xDoc.CreateElement(SCREENSHOT_COMPONENT);
                                    xComponent.InnerText = screenshot.Component.ToString();

                                    XmlElement xSlidename = xDoc.CreateElement(SCREENSHOT_SLIDENAME);
                                    xSlidename.InnerText = screenshot.Slide.Name;

                                    XmlElement xSlidevalue = xDoc.CreateElement(SCREENSHOT_SLIDEVALUE);
                                    xSlidevalue.InnerText = screenshot.Slide.Value;

                                    XmlElement xWindowTitle = xDoc.CreateElement(SCREENSHOT_WINDOW_TITLE);
                                    xWindowTitle.InnerText = screenshot.WindowTitle;

                                    XmlElement xProcessName = xDoc.CreateElement(SCREENSHOT_PROCESS_NAME);
                                    xProcessName.InnerText = screenshot.ProcessName;

                                    XmlElement xLabel = xDoc.CreateElement(SCREENSHOT_LABEL);
                                    xLabel.InnerText = screenshot.Label;

                                    // Create the new XML child nodes for the old XML screenshot so that it's now in the format of the new XML screenshot.
                                    xScreenshot.AppendChild(xViewId);
                                    xScreenshot.AppendChild(xDate);
                                    xScreenshot.AppendChild(xTime);
                                    xScreenshot.AppendChild(xPath);
                                    xScreenshot.AppendChild(xFormat);
                                    xScreenshot.AppendChild(xComponent);
                                    xScreenshot.AppendChild(xSlidename);
                                    xScreenshot.AppendChild(xSlidevalue);
                                    xScreenshot.AppendChild(xWindowTitle);
                                    xScreenshot.AppendChild(xProcessName);
                                    xScreenshot.AppendChild(xLabel);
                                }

                                if (!string.IsNullOrEmpty(screenshot.Date) &&
                                        !string.IsNullOrEmpty(screenshot.Time) &&
                                        !string.IsNullOrEmpty(screenshot.Path) &&
                                        screenshot.Format != null &&
                                        !string.IsNullOrEmpty(screenshot.Slide.Name) &&
                                        !string.IsNullOrEmpty(screenshot.Slide.Value) &&
                                        !string.IsNullOrEmpty(screenshot.WindowTitle))
                                {
                                    // Since we're loading existing screenshots from the XML file each screenshot needs to be flagged as being "Saved"
                                    // because they were already saved to the file before loading them. The "Saved" flag should only be used when we're
                                    // saving new screenshots to avoid saving the entire screenshots collection.
                                    //
                                    // In other words, any screenshot flagged with a "Saved" value of "true" will be treated as if it is already in the file.
                                    // A screenshot flagged with a "Saved" value of "false" will be treated as a new screenshot that should be saved to the file.
                                    screenshot.Saved = true;

                                    Add(screenshot);
                                }
                            }
                        }
                        else
                        {
                            Log.Write("WARNING: Unable to load screenshots taken on " + date + " from \"" + FileSystem.ScreenshotsFile + "\"");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write("ScreenshotCollection::LoadXmlFileAndAddScreenshots", ex);
            }
            finally
            {
                _mutexWriteFile.ReleaseMutex();
            }
        }

        /// <summary>
        /// Saves the screenshots in the collection to the screenshots.xml file.
        /// This method will also delete old screenshot references based on the number of days screenshots should be kept.
        /// </summary>
        /// <param name="keepScreenshotsForDays">The number of days screenshots should be kept.</param>
        public void SaveToXmlFile(int keepScreenshotsForDays)
        {
            try
            {
                _mutexWriteFile.WaitOne();

                if (string.IsNullOrEmpty(FileSystem.ScreenshotsFile))
                {
                    FileSystem.ScreenshotsFile = FileSystem.DefaultScreenshotsFile;

                    if (xDoc == null)
                    {
                        xDoc = new XmlDocument();

                        XmlElement rootElement = xDoc.CreateElement(XML_FILE_ROOT_NODE);
                        XmlElement xScreenshots = xDoc.CreateElement(XML_FILE_SCREENSHOTS_NODE);

                        XmlAttribute attributeVersion = xDoc.CreateAttribute("app", "version", "autoscreen");
                        XmlAttribute attributeCodename = xDoc.CreateAttribute("app", "codename", "autoscreen");

                        attributeVersion.Value = Settings.ApplicationVersion;
                        attributeCodename.Value = Settings.ApplicationCodename;

                        rootElement.Attributes.Append(attributeVersion);
                        rootElement.Attributes.Append(attributeCodename);

                        rootElement.AppendChild(xScreenshots);

                        xDoc.AppendChild(rootElement);

                        xDoc.Save(FileSystem.ScreenshotsFile);
                    }

                    if (File.Exists(FileSystem.ConfigFile))
                    {
                        using (StreamWriter sw = File.AppendText(FileSystem.ConfigFile))
                        {
                            sw.WriteLine("ScreenshotsFile=" + FileSystem.ScreenshotsFile);
                        }
                    }
                }

                lock (_screenshotList)
                {
                    if (_screenshotList != null && _screenshotList.Count > 0 && keepScreenshotsForDays > 0)
                    {
                        List<Screenshot> screenshotsToDelete = _screenshotList.Where(x => !string.IsNullOrEmpty(x.Date) && Convert.ToDateTime(x.Date) <= DateTime.Now.Date.AddDays(-keepScreenshotsForDays)).ToList();

                        if (screenshotsToDelete != null && screenshotsToDelete.Count > 0)
                        {
                            foreach (Screenshot screenshot in screenshotsToDelete)
                            {
                                XmlNodeList nodesToDelete = xDoc.SelectNodes(SCREENSHOT_XPATH + "[" + SCREENSHOT_DATE + "='" + screenshot.Date + "']");

                                foreach (XmlNode node in nodesToDelete)
                                {
                                    node.ParentNode.RemoveChild(node);
                                }

                                if (File.Exists(screenshot.Path))
                                {
                                    File.Delete(screenshot.Path);
                                }

                                _screenshotList.Remove(screenshot);
                                _slideList.Remove(screenshot.Slide);
                                _slideNameList.Remove(screenshot.Slide.Name);
                            }
                        }
                    }

                    for (int i = 0; i < _screenshotList.Count; i++)
                    {
                        Screenshot screenshot = _screenshotList[i];

                        // A new screenshot that needs to be written to the file will have its "Saved" property set to "false" so make sure to check that.
                        if (!screenshot.Saved && xDoc != null && screenshot?.Format != null && !string.IsNullOrEmpty(screenshot.Format.Name))
                        {
                            XmlElement xScreenshot = xDoc.CreateElement(XML_FILE_SCREENSHOT_NODE);

                            XmlElement xViedId = xDoc.CreateElement(SCREENSHOT_VIEWID);
                            xViedId.InnerText = screenshot.ViewId.ToString();

                            XmlElement xDate = xDoc.CreateElement(SCREENSHOT_DATE);
                            xDate.InnerText = screenshot.Date;

                            XmlElement xTime = xDoc.CreateElement(SCREENSHOT_TIME);
                            xTime.InnerText = screenshot.Time;

                            XmlElement xPath = xDoc.CreateElement(SCREENSHOT_PATH);
                            xPath.InnerText = screenshot.Path;

                            XmlElement xFormat = xDoc.CreateElement(SCREENSHOT_FORMAT);
                            xFormat.InnerText = screenshot.Format.Name;

                            XmlElement xComponent = xDoc.CreateElement(SCREENSHOT_COMPONENT);
                            xComponent.InnerText = screenshot.Component.ToString();

                            XmlElement xSlidename = xDoc.CreateElement(SCREENSHOT_SLIDENAME);
                            xSlidename.InnerText = screenshot.Slide.Name;

                            XmlElement xSlidevalue = xDoc.CreateElement(SCREENSHOT_SLIDEVALUE);
                            xSlidevalue.InnerText = screenshot.Slide.Value;

                            XmlElement xWindowTitle = xDoc.CreateElement(SCREENSHOT_WINDOW_TITLE);
                            xWindowTitle.InnerText = screenshot.WindowTitle;

                            XmlElement xProcessName = xDoc.CreateElement(SCREENSHOT_PROCESS_NAME);
                            xProcessName.InnerText = screenshot.ProcessName;

                            XmlElement xLabel = xDoc.CreateElement(SCREENSHOT_LABEL);
                            xLabel.InnerText = screenshot.Label;

                            xScreenshot.AppendChild(xViedId);
                            xScreenshot.AppendChild(xDate);
                            xScreenshot.AppendChild(xTime);
                            xScreenshot.AppendChild(xPath);
                            xScreenshot.AppendChild(xFormat);
                            xScreenshot.AppendChild(xComponent);
                            xScreenshot.AppendChild(xSlidename);
                            xScreenshot.AppendChild(xSlidevalue);
                            xScreenshot.AppendChild(xWindowTitle);
                            xScreenshot.AppendChild(xProcessName);
                            xScreenshot.AppendChild(xLabel);

                            XmlNode xScreenshots = xDoc.SelectSingleNode(SCREENSHOTS_XPATH);

                            if (xScreenshots != null)
                            {
                                if (xScreenshots.HasChildNodes)
                                {
                                    xScreenshots.InsertAfter(xScreenshot, xScreenshots.LastChild);
                                }
                                else
                                {
                                    xScreenshots.AppendChild(xScreenshot);
                                }

                                // Make sure to set this property to "true" so we only write out new screenshots (those with "Saved" set to "false").
                                screenshot.Saved = true;

                                _screenshotList[i] = screenshot;
                            }
                        }
                    }

                    if (xDoc != null)
                    {
                        lock (xDoc)
                        {
                            xDoc.Save(FileSystem.ScreenshotsFile);
                        }
                    }
                }
            }
            finally
            {
                _mutexWriteFile.ReleaseMutex();
            }
        }
    }
}