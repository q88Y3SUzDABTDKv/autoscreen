﻿//-----------------------------------------------------------------------
// <copyright file="SettingCollection.cs" company="Gavin Kendall">
//     Copyright (c) Gavin Kendall. All rights reserved.
// </copyright>
// <author>Gavin Kendall</author>
// <summary></summary>
//-----------------------------------------------------------------------
namespace AutoScreenCapture
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Xml;

    public class SettingCollection : IEnumerable<Setting>
    {
        private List<Setting> _settingList = new List<Setting>();

        private const int MAX_FILE_SIZE = 5242880;
        private const string XML_FILE_INDENT_CHARS = "   ";
        private const string XML_FILE_SETTING_NODE = "setting";
        private const string XML_FILE_SETTINGS_NODE = "settings";
        private const string XML_FILE_ROOT_NODE = "autoscreen";

        private const string SETTING_KEY = "key";
        private const string SETTING_VALUE = "value";
        private const string SETTING_XPATH = "/" + XML_FILE_ROOT_NODE + "/" + XML_FILE_SETTINGS_NODE + "/" + XML_FILE_SETTING_NODE;

        public string AppCodename { get; set; }
        public string AppVersion { get; set; }
        public string Filepath { get; set; }

        public List<Setting>.Enumerator GetEnumerator()
        {
            return _settingList.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return ((IEnumerable<Setting>)_settingList).GetEnumerator();
        }

        IEnumerator<Setting> IEnumerable<Setting>.GetEnumerator()
        {
            return GetEnumerator();
        }

        public void Add(Setting setting)
        {
            // Make sure we only add the setting to the list if it doesn't exist in the list yet.
            if (!KeyExists(setting.Key))
            {
                _settingList.Add(setting);
            }
        }

        private void Remove(Setting setting)
        {
            if (KeyExists(setting.Key))
            {
                _settingList.Remove(setting);
            }
        }

        private void RemoveByKey(string key)
        {
            if (KeyExists(key))
            {
                Setting setting = GetByKey(key, null, false);

                Remove(setting);
            }
        }

        public void SetValueByKey(string key, object value)
        {
            RemoveByKey(key);
            Add(new Setting(key, value));
        }

        /// <summary>
        /// Gets a setting by its key.
        /// If the setting is found it will return the Setting object.
        /// If the setting is not found a new Setting will be created with the provided key and default value.
        /// </summary>
        /// <param name="key">The key to use for finding an existing Setting or for creating a new Setting.</param>
        /// <param name="defaultValue">The default value to use if the Setting cannot be found.</param>
        /// <param name="createKeyIfNotFound">Create a new Setting based on the key name if it does not exist.</param>
        /// <returns>Setting object (either existing or new).</returns>
        public Setting GetByKey(string key, object defaultValue, bool createKeyIfNotFound)
        {
            foreach (Setting setting in _settingList)
            {
                if (setting.Key.Equals(key))
                {
                    return setting;
                }
            }

            if (createKeyIfNotFound)
            {
                Setting newSetting = new Setting(key, defaultValue);
                Add(newSetting);

                return newSetting;
            }

            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public Setting GetByKey(string key, object defaultValue)
        {
            return GetByKey(key, defaultValue, true);
        }

        /// <summary>
        /// Checks if the setting key exists.
        /// </summary>
        /// <param name="key">The setting key to check.</param>
        /// <returns>Returns true if the key exists. Returns false if the key does not exist.</returns>
        public bool KeyExists(string key)
        {
            foreach (Setting setting in _settingList)
            {
                if (setting.Key.Equals(key))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Loads the settings.
        /// </summary>
        public void Load()
        {
            if (Directory.Exists(FileSystem.SettingsFolder) && File.Exists(Filepath))
            {
                FileInfo fileInfo = new FileInfo(Filepath);

                // Check the size of the settings file.
                // Delete the file if it's too big so we don't hang.
                if (fileInfo.Length > MAX_FILE_SIZE)
                {
                    File.Delete(Filepath);
                    return;
                }

                XmlDocument xDoc = new XmlDocument();
                xDoc.Load(Filepath);

                AppVersion = xDoc.SelectSingleNode("/autoscreen").Attributes["app:version"]?.Value;
                AppCodename = xDoc.SelectSingleNode("/autoscreen").Attributes["app:codename"]?.Value;

                XmlNodeList xSettings = xDoc.SelectNodes(SETTING_XPATH);

                foreach (XmlNode xSetting in xSettings)
                {
                    Setting setting = new Setting();
                    XmlNodeReader xReader = new XmlNodeReader(xSetting);

                    while (xReader.Read())
                    {
                        if (xReader.IsStartElement())
                        {
                            switch (xReader.Name)
                            {
                                case SETTING_KEY:
                                    xReader.Read();
                                    setting.Key = xReader.Value;
                                    break;

                                case SETTING_VALUE:
                                    xReader.Read();
                                    setting.Value = xReader.Value;
                                    break;
                            }
                        }
                    }

                    xReader.Close();

                    if (!string.IsNullOrEmpty(setting.Key))
                    {
                        Add(setting);
                    }
                }
            }
        }

        /// <summary>
        /// Saves the settings.
        /// </summary>
        public void Save()
        {
            if (Directory.Exists(FileSystem.SettingsFolder))
            {
                XmlWriterSettings xSettings = new XmlWriterSettings();
                xSettings.Indent = true;
                xSettings.CloseOutput = true;
                xSettings.CheckCharacters = true;
                xSettings.Encoding = Encoding.UTF8;
                xSettings.NewLineChars = Environment.NewLine;
                xSettings.IndentChars = XML_FILE_INDENT_CHARS;
                xSettings.NewLineHandling = NewLineHandling.Entitize;
                xSettings.ConformanceLevel = ConformanceLevel.Document;

                if (File.Exists(Filepath))
                {
                    File.Delete(Filepath);
                }

                using (XmlWriter xWriter = XmlWriter.Create(Filepath, xSettings))
                {
                    xWriter.WriteStartDocument();
                    xWriter.WriteStartElement(XML_FILE_ROOT_NODE);
                    xWriter.WriteAttributeString("app", "version", XML_FILE_ROOT_NODE, Settings.ApplicationVersion);
                    xWriter.WriteAttributeString("app", "codename", XML_FILE_ROOT_NODE, Settings.ApplicationCodename);
                    xWriter.WriteStartElement(XML_FILE_SETTINGS_NODE);

                    foreach (object obj in _settingList)
                    {
                        Setting setting = (Setting) obj;

                        xWriter.WriteStartElement(XML_FILE_SETTING_NODE);
                        xWriter.WriteElementString(SETTING_KEY, setting.Key);
                        xWriter.WriteElementString(SETTING_VALUE, setting.Value.ToString());

                        xWriter.WriteEndElement();
                    }

                    xWriter.WriteEndElement();
                    xWriter.WriteEndElement();
                    xWriter.WriteEndDocument();

                    xWriter.Flush();
                    xWriter.Close();
                }
            }
        }

        public void Upgrade()
        {
            if (Settings.VersionManager.IsOldAppVersion(AppCodename, AppVersion))
            {
                SettingCollection oldUserSettings = (SettingCollection)this.MemberwiseClone();
                oldUserSettings._settingList = new List<Setting>(_settingList);

                Settings.VersionManager.OldUserSettings = oldUserSettings;

                if (Settings.VersionManager.Versions.Get("Clara", "2.1.8.2") != null) // Is this version 2.1.8.2 (or older)?
                {
                    // Go through the old settings and get the old values from them to be used for the new settings.

                    // 2.1 used a setting named "DaysOldWhenRemoveSlides", but 2.2 uses "DeleteScreenshotsOlderThanDays".
                    if (KeyExists("DaysOldWhenRemoveSlides"))
                    {
                        SetValueByKey("DeleteScreenshotsOlderThanDays",
                            Convert.ToInt32(
                                GetByKey("DaysOldWhenRemoveSlides", 0, createKeyIfNotFound: false).Value));
                    }

                    // 2.1 used a setting named "Interval", but 2.2 uses "ScreenshotDelay".
                    if (KeyExists("Interval"))
                    {
                        SetValueByKey("ScreenshotDelay",
                            Convert.ToInt32(GetByKey("Interval", 60000, createKeyIfNotFound: false).Value));
                    }

                    // Remove the old settings.
                    // When upgrading from 2.1 to 2.2 we should end up with about 20 settings instead of 60
                    // because 2.1 was limited to using Screen 1, Screen 2, Screen 3, Screen 4, and Active Window
                    // so there were settings dedicated to all the properties associated with them (such as Name, X, Y, Width, and Height).
                    RemoveByKey("ScreenshotsDirectory");
                    RemoveByKey("ScheduleImageFormat");
                    RemoveByKey("SlideSkip");
                    RemoveByKey("ImageResolutionRatio");
                    RemoveByKey("ImageFormatFilter");
                    RemoveByKey("ImageFormatFilterIndex");
                    RemoveByKey("Interval");
                    RemoveByKey("SlideshowDelay");
                    RemoveByKey("SlideSkipCheck");
                    RemoveByKey("Screen1X");
                    RemoveByKey("Screen1Y");
                    RemoveByKey("Screen1Width");
                    RemoveByKey("Screen1Height");
                    RemoveByKey("Screen2X");
                    RemoveByKey("Screen2Y");
                    RemoveByKey("Screen2Width");
                    RemoveByKey("Screen2Height");
                    RemoveByKey("Screen3X");
                    RemoveByKey("Screen3Y");
                    RemoveByKey("Screen3Width");
                    RemoveByKey("Screen3Height");
                    RemoveByKey("Screen4X");
                    RemoveByKey("Screen4Y");
                    RemoveByKey("Screen4Width");
                    RemoveByKey("Screen4Height");
                    RemoveByKey("Screen1Name");
                    RemoveByKey("Screen2Name");
                    RemoveByKey("Screen3Name");
                    RemoveByKey("Screen4Name");
                    RemoveByKey("Screen5Name");
                    RemoveByKey("Macro");
                    RemoveByKey("JpegQualityLevel");
                    RemoveByKey("DaysOldWhenRemoveSlides");
                    RemoveByKey("CaptureScreen1");
                    RemoveByKey("CaptureScreen2");
                    RemoveByKey("CaptureScreen3");
                    RemoveByKey("CaptureScreen4");
                    RemoveByKey("CaptureActiveWindow");
                    RemoveByKey("AutoReset");
                    RemoveByKey("Mouse");
                    RemoveByKey("StartButtonImageFormat");
                    RemoveByKey("Schedule");
                }

                // Now that we've upgraded all the settings we should save them to disk.
                Save();
            }
        }
    }
}